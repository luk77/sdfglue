//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui;
using SingleDocAppAnimation;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Platform;
using SingleDocAppVideoExport;
using SingleDocAppVideoExport.Gif;
using SingleDocAppVideoExportWin;
using System.Diagnostics;

namespace SdfGlueEditor.Application
{
    // Animation export (File -> Export animation...): PNG image sequence, MP4 video or animated GIF.
    // The export is done in steps - Step() is called by EditorMainWindow instead of the normal rendering,
    // so the UI (progress, Cancel) stays responsive. The frames are rendered and read back on the main thread and
    // written by a FrameSinkWorker (background thread: PNG files, the MP4 / GIF encoders); when the writer is behind,
    // the step ends and the rendering continues in the next frame of the application.
    // Both clocks are advanced by the export (also when paused): the time of the session (iTime) and the playhead of
    // the animation timeline (keyframes). The first saved frame has the start time, prerendered frames are before it.
    // The time of the session, the playhead and the preview resolution are restored at the end (also on cancel / error).
    public class AnimationExportController
    {
        private const string            DialogTitle             = "Export animation";

        // rendering time per Step() (at least one frame is rendered, unless the writer is behind)
        private static readonly double  StepTimeBudgetMs        = 50.0;

        private static readonly FileFilter  Mp4FileFilter       = new FileFilter("MP4 video", "*.mp4");
        private static readonly FileFilter  GifFileFilter       = new FileFilter("Animated GIF", "*.gif");

        private enum StepResult
        {
            Rendered,       // continue
            Waiting,        // the writer is behind - next step
            Done,           // all frames rendered, finished or failed
        }

        private SdfGlueAppContext       ctx_;
        private RenderController        render_;

        private AnimationExportProgress progress_               = new AnimationExportProgress();
        private Stopwatch               stopwatch_              = new Stopwatch();
        private TimeState?              savedTimeState_         = null;
        private ExportAnimationSettings settings_               = new ExportAnimationSettings();    // copy taken on start
        private string                  output_                 = "";       // folder (PNG) or file (MP4, GIF)
        private double                  deltaTime_              = 0.0;
        private double                  startTime_              = 0.0;      // time of the session of the first saved frame

        // the animation timeline
        private double                  animStart_              = 0.0;      // playhead of the first saved frame
        private double                  savedPlayhead_          = 0.0;
        private bool                    savedPlaying_           = false;

        // writer (created at the first saved frame - its size is the size of the rendered frame)
        private IFrameSink?             sink_;
        private FrameSinkWorker?        worker_;

        public AnimationExportController(SdfGlueAppContext ctx, RenderController render)
        {
            ctx_    = ctx;
            render_ = render;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }

        public bool IsActive { get { return progress_.IsActive; } }

        public AnimationExportProgress GetProgress()
        {
            progress_.Elapsed               = stopwatch_.Elapsed;
            progress_.CurrentTime           = ctx_.Playback.CurrentTime;
            progress_.NumOfWrittenFrames    = worker_ != null ? worker_.NumOfWrittenFrames : 0;
            return progress_;
        }

        // Save dialog for the output file of an MP4 / GIF export, null when cancelled
        public string? SelectOutputFile(ExportAnimationFormat format, string? currentFile)
        {
            string? initialDirectory = null;
            try
            {
                if (!String.IsNullOrWhiteSpace(currentFile))
                {
                    string? dir = Path.GetDirectoryName(Path.GetFullPath(currentFile));
                    if (dir != null && Directory.Exists(dir))
                        initialDirectory = dir;
                }
            }
            catch (Exception)
            {
                // invalid path in the settings - no initial directory
            }

            FileFilter filter = (format == ExportAnimationFormat.Gif) ? GifFileFilter : Mp4FileFilter;
            string? filePath = ctx_.Platform.SaveFileDialog(filter, initialDirectory);
            if (String.IsNullOrEmpty(filePath))
                return null;

            return AddExtension(filePath, format);
        }

        // Returns false if the export was not started (invalid settings, cancelled by the user)
        public bool Start()
        {
            if (IsActive)
                return false;

            ExportAnimationSettings settings = ctx_.Settings.ExportAnimSettings;

            if (!ValidateSettings(settings, out string output))
                return false;

            settings_.CopyFrom(settings);
            output_     = output;
            deltaTime_  = 1.0 / settings_.Fps;

            PlaybackState playback = ctx_.Playback;
            savedTimeState_ = playback.CaptureTimeState();

            AnimTransport transport = ctx_.Animation.Transport;
            savedPlayhead_  = transport.Playhead;
            savedPlaying_   = transport.Playing;
            transport.Playing = false;

            // resolution (the frame counter is reset - accumulation of frames starts from scratch)
            if (settings_.ResolutionIndex >= 0)
            {
                ctx_.RenderResolutionOverride = ctx_.Settings.GetExportAnimResolution();
                render_.PreviewResolutionChanged();
            }

            // time: both clocks start at the start time, or each one at its current time
            startTime_ = settings_.UseStartTime ? settings_.StartTime : playback.CurrentTime;
            animStart_ = settings_.UseStartTime ? settings_.StartTime : savedPlayhead_;
            if (settings_.UseStartTime)
                playback.ResetFrameCounter();
            playback.CurrentTime = startTime_ - settings_.NumOfFramesToPrerender * deltaTime_;

            progress_                           = new AnimationExportProgress();
            progress_.IsActive                  = true;
            progress_.IsPrerendering            = settings_.NumOfFramesToPrerender > 0;
            progress_.NumOfFramesToPrerender    = settings_.NumOfFramesToPrerender;
            progress_.NumOfFramesToExport       = settings_.NumOfFramesToExport;
            progress_.Output                    = settings_.IsVideoFile() ? output_ : Path.Combine(output_, settings_.GetFileName(0));

            stopwatch_.Restart();

            IntCoords resolution = ctx_.GetRenderResolution();
            Console.WriteLine("Export animation ({0}): {1} frames ({2} prerendered), {3}x{4}, {5} fps, start time {6:0.###} s, timeline {7:0.###} s -> {8}",
                settings_.Format, settings_.NumOfFramesToExport, settings_.NumOfFramesToPrerender, resolution.X, resolution.Y, settings_.Fps,
                startTime_, animStart_, progress_.Output);

            return true;
        }

        // Renders frames until the time budget of this step is used (or the writer is behind)
        public void Step()
        {
            if (!IsActive)
                return;

            if (worker_ != null && worker_.Error != null)
            {
                Fail(worker_.Error.Message);
                return;
            }

            if (progress_.IsFinishing)
            {
                CheckFinished();
                return;
            }

            Stopwatch stepStopwatch = Stopwatch.StartNew();
            do
            {
                if (RenderNextFrame() != StepResult.Rendered)
                    return;
            }
            while (IsActive && stepStopwatch.Elapsed.TotalMilliseconds < StepTimeBudgetMs);
        }

        public void Cancel()
        {
            if (!IsActive)
                return;

            worker_?.Abort();
            Finish(String.Format("Export animation cancelled: {0} of {1} frames written.", GetNumOfWrittenFrames(), progress_.NumOfFramesToExport));
        }

        private int GetNumOfWrittenFrames()
        {
            return worker_ != null ? worker_.NumOfWrittenFrames : 0;
        }

        private StepResult RenderNextFrame()
        {
            if (progress_.IsPrerendering)
            {
                RenderAndAdvance();
                progress_.NumOfPrerenderedFrames++;
                if (progress_.NumOfPrerenderedFrames >= progress_.NumOfFramesToPrerender)
                    progress_.IsPrerendering = false;
                return StepResult.Rendered;
            }

            // a buffer for the frame first: the time advances only with a rendered frame
            VideoFrame? frame = (worker_ != null) ? worker_.TryRentFrame() : null;
            if (worker_ != null && frame == null)
            {
                if (worker_.IsCompleted)
                {
                    // the writer has ended before all frames were rendered
                    Fail(worker_.Error != null ? worker_.Error.Message : "The writer has stopped.");
                    return StepResult.Done;
                }
                return StepResult.Waiting;
            }

            RenderAndAdvance();

            RenderPassData? pass = GetModel().GetRPassDataForPreview();
            if (pass == null)
            {
                Fail("No image to export.");
                return StepResult.Done;
            }

            if (worker_ == null)
            {
                IntCoords size = pass.GetFrameSize();
                if (!StartWriter(size.X, size.Y))
                    return StepResult.Done;
                frame = worker_!.TryRentFrame();
            }

            if (frame == null || !pass.ReadFrameBgra(frame.Pixels, frame.Width, frame.Height))
            {
                Fail("Unable to read the rendered frame.");
                return StepResult.Done;
            }

            worker_!.Submit(frame);
            progress_.NumOfRenderedFrames++;

            if (progress_.NumOfRenderedFrames >= progress_.NumOfFramesToExport)
            {
                // the rest is done by the writer (the last frames, closing the file)
                worker_.Complete();
                progress_.IsFinishing = true;
                return StepResult.Done;
            }

            return StepResult.Rendered;
        }

        private void RenderAndAdvance()
        {
            // the playhead of the timeline follows the time of the session (from its own start)
            AnimTransport transport = ctx_.Animation.Transport;
            if (transport.Timebase.IsAvailable)
            {
                double playhead = animStart_ + (ctx_.Playback.CurrentTime - startTime_);
                transport.Playhead = Math.Clamp(playhead, 0.0, Math.Max(0.0, transport.Timebase.Length));
            }

            render_.RenderFrame();
            ctx_.AdvanceTime(deltaTime_);
        }

        private bool StartWriter(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                Fail("No image to export.");
                return false;
            }

            try
            {
                sink_   = CreateSink(width, height);
                worker_ = new FrameSinkWorker(sink_, width, height, settings_.Fps);
                worker_.Start();
                return true;
            }
            catch (Exception ex)
            {
                Fail(ex.Message);
                return false;
            }
        }

        private IFrameSink CreateSink(int width, int height)
        {
            switch (settings_.Format)
            {
                case ExportAnimationFormat.Mp4:
                {
                    Mp4Sink.GetEncodedSize(width, height, out int encodedWidth, out int encodedHeight);
                    if (encodedWidth != width || encodedHeight != height)
                        Console.WriteLine("Export animation: MP4 needs an even size - the video is {0}x{1} (frames {2}x{3}).", encodedWidth, encodedHeight, width, height);
                    int bitrate = GetMp4Bitrate(encodedWidth, encodedHeight);
                    Console.WriteLine("Export animation: MP4 bitrate {0:0.0} Mbit/s", bitrate / 1e6);
                    return new Mp4Sink(output_, bitrate);
                }

                case ExportAnimationFormat.Gif:
                {
                    GifOptions options = new GifOptions
                    {
                        Dither          = ToGifDither(settings_.GifDither),
                        Loop            = settings_.GifLoop,
                        OptimizeDelta   = settings_.GifOptimizeDelta,
                    };
                    GifSink.GetOutputSize(width, height, settings_.GifScale, out int gifWidth, out int gifHeight);
                    Console.WriteLine("Export animation: GIF {0}x{1}", gifWidth, gifHeight);
                    return new GifSink(output_, options, settings_.GifScale);
                }

                default:
                {
                    ExportAnimationSettings settings = new ExportAnimationSettings();
                    settings.CopyFrom(settings_);
                    return new PngSequenceSink(output_, settings);
                }
            }
        }

        private int GetMp4Bitrate(int width, int height)
        {
            switch (settings_.Mp4Quality)
            {
                case ExportMp4Quality.Low:      return Mp4Sink.GetBitrate(width, height, settings_.Fps, Mp4Sink.LowBitsPerPixel);
                case ExportMp4Quality.Medium:   return Mp4Sink.GetBitrate(width, height, settings_.Fps, Mp4Sink.MediumBitsPerPixel);
                case ExportMp4Quality.High:     return Mp4Sink.GetBitrate(width, height, settings_.Fps, Mp4Sink.HighBitsPerPixel);
                default:                        return (int)Math.Clamp(settings_.Mp4BitrateMbps * 1e6, Mp4Sink.MinBitrate, Mp4Sink.MaxBitrate);
            }
        }

        private static GifDither ToGifDither(ExportGifDither dither)
        {
            switch (dither)
            {
                case ExportGifDither.None:              return GifDither.None;
                case ExportGifDither.FloydSteinberg:    return GifDither.FloydSteinberg;
                default:                                return GifDither.Ordered;
            }
        }

        // all frames rendered: waits for the writer
        private void CheckFinished()
        {
            if (worker_ == null || !worker_.IsCompleted)
                return;

            if (worker_.Error != null)
            {
                Fail(worker_.Error.Message);
                return;
            }

            string encoder = "";
            if (sink_ is Mp4Sink mp4)
                encoder = mp4.UsesHardware ? " (hardware encoder)" : " (software encoder)";
            Finish(String.Format("Export animation finished: {0} frames written in {1:0.0} s{2} -> {3}", worker_.NumOfWrittenFrames, stopwatch_.Elapsed.TotalSeconds,
                                 encoder, progress_.Output));
        }

        private void Fail(string message)
        {
            Console.WriteLine("ERROR: Export animation: {0}", message);
            worker_?.Abort();
            Finish(String.Format("Export animation failed: {0} of {1} frames written.", GetNumOfWrittenFrames(), progress_.NumOfFramesToExport));
            ctx_.Platform.ShowErrorMessage(DialogTitle, message);
        }

        // Restores the state of the session
        private void Finish(string message)
        {
            stopwatch_.Stop();
            progress_.IsActive      = false;
            progress_.IsFinishing   = false;

            if (worker_ != null)
            {
                worker_.Dispose();
                worker_ = null;
            }
            sink_ = null;

            if (ctx_.RenderResolutionOverride != null)
            {
                ctx_.RenderResolutionOverride = null;
                render_.PreviewResolutionChanged();
            }

            if (savedTimeState_ != null)
            {
                ctx_.Playback.RestoreTimeState(savedTimeState_);
                savedTimeState_ = null;
            }

            AnimTransport transport = ctx_.Animation.Transport;
            transport.Playhead  = savedPlayhead_;
            transport.Playing   = savedPlaying_;

            // the frame buffers contain exported frames - accumulation of frames starts from scratch
            ctx_.Playback.ResetFrameCounter();

            Console.WriteLine(message);
        }

        private static string AddExtension(string filePath, ExportAnimationFormat format)
        {
            string extension = ExportAnimationSettings.GetFileExtension(format);
            if (String.IsNullOrEmpty(Path.GetExtension(filePath)))
                return filePath + extension;
            return filePath;
        }

        private bool ValidateSettings(ExportAnimationSettings settings, out string output)
        {
            output = "";

            if (GetModel().GetRPassDataForPreview() == null)
                return ShowError("No image to export.");

            if (settings.Fps <= 0)
                return ShowError("FPS must be greater than 0.");

            if (settings.NumOfFramesToExport <= 0)
                return ShowError("Number of frames to export must be greater than 0.");

            if (settings.NumOfFramesToPrerender < 0)
                return ShowError("Number of frames to prerender can not be negative.");

            switch (settings.Format)
            {
                case ExportAnimationFormat.Mp4:
                    if (settings.Mp4Quality == ExportMp4Quality.Custom && settings.Mp4BitrateMbps <= 0.0)
                        return ShowError("The MP4 bitrate must be greater than 0.");
                    return ValidateOutputFile(settings, out output);

                case ExportAnimationFormat.Gif:
                    if (settings.Fps > ExportAnimationSettings.MaxGifFps)
                        return ShowError(String.Format("GIF supports at most {0} FPS (browsers play faster GIFs slower).\nLower the FPS.", ExportAnimationSettings.MaxGifFps));
                    return ValidateOutputFile(settings, out output);

                default:
                    return ValidateImageSequence(settings, out output);
            }
        }

        private bool ValidateOutputFile(ExportAnimationSettings settings, out string outputFile)
        {
            outputFile = "";

            if (String.IsNullOrWhiteSpace(settings.OutputFile))
                return ShowError("Select the output file.");

            try
            {
                outputFile = AddExtension(Path.GetFullPath(settings.OutputFile), settings.Format);
            }
            catch (Exception ex)
            {
                return ShowError(String.Format("Invalid output file:\n{0}\n\n{1}", settings.OutputFile, ex.Message));
            }

            if (Directory.Exists(outputFile))
                return ShowError(String.Format("The output file is a folder:\n{0}", outputFile));

            string? folder = Path.GetDirectoryName(outputFile);
            if (!String.IsNullOrEmpty(folder) && !EnsureFolderExists(folder))
                return false;

            if (File.Exists(outputFile))
            {
                if (!ctx_.Platform.AskOkCancel(DialogTitle, String.Format("The file will be overwritten:\n{0}\n\nContinue?", outputFile)))
                    return false;
            }

            return true;
        }

        private bool ValidateImageSequence(ExportAnimationSettings settings, out string outputFolder)
        {
            outputFolder = "";

            if (settings.FilePrefix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                return ShowError(String.Format("The file prefix contains invalid characters:\n{0}", settings.FilePrefix));

            if (String.IsNullOrWhiteSpace(settings.OutputFolder))
                return ShowError("Select the output folder.");

            try
            {
                outputFolder = Path.GetFullPath(settings.OutputFolder);
            }
            catch (Exception ex)
            {
                return ShowError(String.Format("Invalid output folder:\n{0}\n\n{1}", settings.OutputFolder, ex.Message));
            }

            if (!EnsureFolderExists(outputFolder))
                return false;

            int numOfExistingFiles = 0;
            for (int f=0; f<settings.NumOfFramesToExport; f++)
            {
                if (File.Exists(Path.Combine(outputFolder, settings.GetFileName(f))))
                    numOfExistingFiles++;
            }

            if (numOfExistingFiles > 0)
            {
                string message = String.Format("{0} file(s) will be overwritten in:\n{1}\n\nContinue?", numOfExistingFiles, outputFolder);
                if (!ctx_.Platform.AskOkCancel(DialogTitle, message))
                    return false;
            }

            return true;
        }

        // Asks to create a missing folder
        private bool EnsureFolderExists(string folder)
        {
            if (Directory.Exists(folder))
                return true;

            if (!ctx_.Platform.AskOkCancel(DialogTitle, String.Format("The folder does not exist:\n{0}\n\nCreate it?", folder)))
                return false;

            try
            {
                Directory.CreateDirectory(folder);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Cannot create folder: {0}. {1}", folder, ex.Message);
                return ShowError(String.Format("Cannot create folder:\n{0}\n\n{1}", folder, ex.Message));
            }
        }

        private bool ShowError(string message)
        {
            Console.WriteLine("ERROR: Export animation: {0}", message);
            ctx_.Platform.ShowErrorMessage(DialogTitle, message);
            return false;
        }
    }
}
