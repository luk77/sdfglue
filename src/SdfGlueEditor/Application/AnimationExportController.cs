//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueUi.Ui;
using SingleDocAppCore.Model.BaseTypes;
using System.Diagnostics;
using System.Drawing;

namespace SdfGlueEditor.Application
{
    // Image sequence export (File -> Export image sequence...).
    // The export is done in steps - Step() is called by EditorMainWindow instead of the normal rendering,
    // so the UI (progress, Cancel) stays responsive. Time is advanced by the export (also when paused):
    // the first saved frame has the start time, prerendered frames are before it.
    // The time of the session and the preview resolution are restored at the end (also on cancel / error).
    public class AnimationExportController
    {
        // rendering time per Step() (at least one frame is rendered)
        private static readonly double  StepTimeBudgetMs        = 50.0;

        private SdfGlueAppContext       ctx_;
        private RenderController        render_;

        private AnimationExportProgress progress_               = new AnimationExportProgress();
        private Stopwatch               stopwatch_              = new Stopwatch();
        private TimeState?              savedTimeState_         = null;
        private ExportAnimationSettings settings_               = new ExportAnimationSettings();    // copy taken on start
        private double                  deltaTime_              = 0.0;

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
            progress_.Elapsed       = stopwatch_.Elapsed;
            progress_.CurrentTime   = GetModel().CurrentTime;
            return progress_;
        }

        // Returns false if the export was not started (invalid settings, cancelled by the user)
        public bool Start()
        {
            if (IsActive)
                return false;

            ExportAnimationSettings settings = ctx_.Settings.ExportAnimSettings;

            if (!ValidateSettings(settings, out string outputFolder))
                return false;

            settings_.CopyFrom(settings);
            settings_.OutputFolder = outputFolder;
            deltaTime_ = 1.0 / settings_.Fps;

            DataModel model = GetModel();
            savedTimeState_ = model.CaptureTimeState();

            // resolution (the frame counter is reset - accumulation of frames starts from scratch)
            if (settings_.ResolutionIndex >= 0)
            {
                model.RenderResolutionOverride = ctx_.Settings.GetExportAnimResolution();
                render_.PreviewResolutionChanged();
            }

            // time
            double startTime = settings_.UseStartTime ? settings_.StartTime : model.CurrentTime;
            if (settings_.UseStartTime)
                model.ResetFrameCounter();
            model.CurrentTime = startTime - settings_.NumOfFramesToPrerender * deltaTime_;

            progress_                           = new AnimationExportProgress();
            progress_.IsActive                  = true;
            progress_.IsPrerendering            = settings_.NumOfFramesToPrerender > 0;
            progress_.NumOfFramesToPrerender    = settings_.NumOfFramesToPrerender;
            progress_.NumOfFramesToExport       = settings_.NumOfFramesToExport;
            progress_.OutputFolder              = outputFolder;

            stopwatch_.Restart();

            IntCoords resolution = model.GetRenderResolution();
            Console.WriteLine("Export image sequence: {0} frames ({1} prerendered), {2}x{3}, {4} fps, start time {5:0.###} s -> {6}",
                settings_.NumOfFramesToExport, settings_.NumOfFramesToPrerender, resolution.X, resolution.Y, settings_.Fps, startTime,
                Path.Combine(outputFolder, settings_.GetFileName(0)));

            return true;
        }

        // Renders (and saves) frames until the time budget of this step is used
        public void Step()
        {
            if (!IsActive)
                return;

            Stopwatch stepStopwatch = Stopwatch.StartNew();
            do
            {
                if (!RenderNextFrame())
                    return;
            }
            while (IsActive && stepStopwatch.Elapsed.TotalMilliseconds < StepTimeBudgetMs);
        }

        public void Cancel()
        {
            if (!IsActive)
                return;

            Finish(String.Format("Export image sequence cancelled: {0} of {1} frames saved.", progress_.NumOfSavedFrames, progress_.NumOfFramesToExport));
        }

        // false - the export was finished (completed or failed)
        private bool RenderNextFrame()
        {
            DataModel model = GetModel();

            render_.RenderFrame();

            if (progress_.IsPrerendering)
            {
                model.AdvanceTime(deltaTime_);
                progress_.NumOfPrerenderedFrames++;
                if (progress_.NumOfPrerenderedFrames >= progress_.NumOfFramesToPrerender)
                    progress_.IsPrerendering = false;
            }
            else
            {
                model.AdvanceTime(deltaTime_);

                using Bitmap? bmp = model.GetRPassDataForPreview()?.GetFrameAsBitmap();
                if (bmp == null)
                {
                    Fail("Unable to get bitmap data.");
                    return false;
                }

                // stop on the first error (the error is reported by SaveImageToFile)
                string filePath = Path.Combine(settings_.OutputFolder!, settings_.GetFileName(progress_.NumOfSavedFrames));
                if (!render_.SaveImageToFile(bmp, filePath))
                {
                    Finish(String.Format("Export image sequence failed: {0} of {1} frames saved.", progress_.NumOfSavedFrames, progress_.NumOfFramesToExport));
                    return false;
                }

                progress_.NumOfSavedFrames++;
            }

            if (progress_.NumOfSavedFrames >= progress_.NumOfFramesToExport)
            {
                Finish(String.Format("Export image sequence finished: {0} frames saved in {1:0.0} s.", progress_.NumOfSavedFrames, stopwatch_.Elapsed.TotalSeconds));
                return false;
            }

            return true;
        }

        private void Fail(string message)
        {
            Console.WriteLine("ERROR: Export image sequence: {0}", message);
            Finish(String.Format("Export image sequence failed: {0} of {1} frames saved.", progress_.NumOfSavedFrames, progress_.NumOfFramesToExport));
            ctx_.Platform.ShowErrorMessage("Export image sequence", message);
        }

        // Restores the state of the session
        private void Finish(string message)
        {
            stopwatch_.Stop();
            progress_.IsActive = false;

            DataModel model = GetModel();

            if (model.RenderResolutionOverride != null)
            {
                model.RenderResolutionOverride = null;
                render_.PreviewResolutionChanged();
            }

            if (savedTimeState_ != null)
            {
                model.RestoreTimeState(savedTimeState_);
                savedTimeState_ = null;
            }

            // the frame buffers contain exported frames - accumulation of frames starts from scratch
            model.ResetFrameCounter();

            Console.WriteLine(message);
        }

        private bool ValidateSettings(ExportAnimationSettings settings, out string outputFolder)
        {
            outputFolder = "";

            if (GetModel().GetRPassDataForPreview() == null)
                return ShowError("No image to export.");

            if (settings.Fps <= 0)
                return ShowError("FPS must be greater than 0.");

            if (settings.NumOfFramesToExport <= 0)
                return ShowError("Number of frames to export must be greater than 0.");

            if (settings.NumOfFramesToPrerender < 0)
                return ShowError("Number of frames to prerender can not be negative.");

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

            if (!Directory.Exists(outputFolder))
            {
                if (!ctx_.Platform.AskOkCancel("Export image sequence", String.Format("The folder does not exist:\n{0}\n\nCreate it?", outputFolder)))
                    return false;

                try
                {
                    Directory.CreateDirectory(outputFolder);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR: Cannot create folder: {0}. {1}", outputFolder, ex.Message);
                    return ShowError(String.Format("Cannot create folder:\n{0}\n\n{1}", outputFolder, ex.Message));
                }
            }

            int numOfExistingFiles = 0;
            for (int f=0; f<settings.NumOfFramesToExport; f++)
            {
                if (File.Exists(Path.Combine(outputFolder, settings.GetFileName(f))))
                    numOfExistingFiles++;
            }

            if (numOfExistingFiles > 0)
            {
                string message = String.Format("{0} file(s) will be overwritten in:\n{1}\n\nContinue?", numOfExistingFiles, outputFolder);
                if (!ctx_.Platform.AskOkCancel("Export image sequence", message))
                    return false;
            }

            return true;
        }

        private bool ShowError(string message)
        {
            Console.WriteLine("ERROR: Export image sequence: {0}", message);
            ctx_.Platform.ShowErrorMessage("Export image sequence", message);
            return false;
        }
    }
}
