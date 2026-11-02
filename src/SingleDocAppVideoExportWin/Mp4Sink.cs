//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppVideoExport;
using System.Runtime.InteropServices;

namespace SingleDocAppVideoExportWin
{
    // MP4 file with H.264 video (High profile, constant average bitrate), encoded by Windows Media Foundation:
    // the Sink Writer converts the RGB32 frames to the format of the encoder (a hardware encoder when available,
    // otherwise the software encoder of Windows). H.264 needs an even width and height - the last column / row of
    // an odd size is dropped. The file is written under a temporary name and renamed at the end.
    public sealed class Mp4Sink : IFrameSink
    {
        // the bitrate presets: bits per pixel per frame
        public  const double    LowBitsPerPixel         = 0.05;
        public  const double    MediumBitsPerPixel      = 0.1;
        public  const double    HighBitsPerPixel        = 0.2;

        public  const int       MinBitrate              = 100_000;
        public  const int       MaxBitrate              = 200_000_000;

        private readonly string         filePath_;
        private readonly string         tempPath_;
        private readonly int            bitrate_;
        private readonly bool           allowHardware_;

        private bool                    isStarted_      = false;    // MFStartup
        private IMFSinkWriter?          writer_;
        private int                     streamIndex_;
        private int                     encodedWidth_;
        private int                     encodedHeight_;
        private double                  fps_;

        // bitrate: bits per second (see GetBitrate)
        public Mp4Sink(string filePath, int bitrate, bool allowHardware = true)
        {
            filePath_       = filePath;
            tempPath_       = filePath + ".tmp";
            bitrate_        = Math.Clamp(bitrate, MinBitrate, MaxBitrate);
            allowHardware_  = allowHardware;
        }

        // the bitrate for a quality (bits per pixel per frame)
        public static int GetBitrate(int width, int height, double fps, double bitsPerPixel)
        {
            double bitrate = (double)width * height * fps * bitsPerPixel;
            return (int)Math.Clamp(bitrate, MinBitrate, MaxBitrate);
        }

        // the size of the video for a frame size (even)
        public static void GetEncodedSize(int width, int height, out int encodedWidth, out int encodedHeight)
        {
            encodedWidth    = width  & ~1;
            encodedHeight   = height & ~1;
        }

        // true when the encoder was a hardware one (for the log); valid after Begin
        public bool UsesHardware { get; private set; }

        public void Begin(int width, int height, double fps)
        {
            GetEncodedSize(width, height, out encodedWidth_, out encodedHeight_);
            if (encodedWidth_ < 2 || encodedHeight_ < 2)
                throw new ArgumentException(String.Format("The frame is too small for MP4: {0}x{1}", width, height));

            fps_        = fps;

            MediaFoundation.Check(MediaFoundation.MFStartup(MediaFoundation.MF_VERSION, MediaFoundation.MFSTARTUP_LITE), "MFStartup");
            isStarted_ = true;

            if (allowHardware_)
            {
                try
                {
                    writer_ = CreateWriter(true);
                    UsesHardware = true;
                }
                catch (COMException ex)
                {
                    Console.WriteLine("Mp4Sink: hardware encoder not available ({0}), using the software encoder.", ex.Message);
                    ReleaseWriter();
                }
            }
            if (writer_ == null)
            {
                writer_ = CreateWriter(false);
                UsesHardware = false;
            }
        }

        public void AddFrame(VideoFrame frame)
        {
            if (writer_ == null)
                throw new InvalidOperationException("Mp4Sink: Begin was not called.");

            int rowSize = encodedWidth_ * 4;
            int length  = rowSize * encodedHeight_;

            IMFMediaBuffer? buffer = null;
            IMFSample?      sample = null;
            try
            {
                MediaFoundation.Check(MediaFoundation.MFCreateMemoryBuffer(length, out buffer), "MFCreateMemoryBuffer");
                buffer.Lock(out IntPtr data, out int _, out int _);
                try
                {
                    // top-down rows (MF_MT_DEFAULT_STRIDE > 0), the odd last column is dropped
                    for (int y=0; y<encodedHeight_; y++)
                        Marshal.Copy(frame.Pixels, y * frame.Stride, IntPtr.Add(data, y * rowSize), rowSize);
                }
                finally
                {
                    buffer.Unlock();
                }
                buffer.SetCurrentLength(length);

                MediaFoundation.Check(MediaFoundation.MFCreateSample(out sample), "MFCreateSample");
                sample.AddBuffer(buffer);

                long time       = GetSampleTime(frame.Index);
                long nextTime   = GetSampleTime(frame.Index + 1);
                sample.SetSampleTime(time);
                sample.SetSampleDuration(nextTime - time);

                writer_.WriteSample(streamIndex_, sample);
            }
            finally
            {
                MediaFoundation.Release(sample);
                MediaFoundation.Release(buffer);
            }
        }

        public void Finish()
        {
            if (writer_ == null)
                throw new InvalidOperationException("Mp4Sink: Begin was not called.");

            writer_.Finalize_();
            ReleaseWriter();
            Shutdown();
            File.Move(tempPath_, filePath_, true);
        }

        public void Abort()
        {
            try
            {
                ReleaseWriter();
                Shutdown();
                if (File.Exists(tempPath_))
                    File.Delete(tempPath_);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Mp4Sink: cannot remove the temporary file {0}. {1}", tempPath_, ex.Message);
            }
        }

        // 100 ns units
        private long GetSampleTime(int frameIndex)
        {
            return (long)Math.Round(frameIndex * 10_000_000.0 / fps_);
        }

        private void GetFrameRate(out int numerator, out int denominator)
        {
            if (Math.Abs(fps_ - Math.Round(fps_)) < 1e-9)
            {
                numerator   = (int)Math.Round(fps_);
                denominator = 1;
            }
            else
            {
                numerator   = (int)Math.Round(fps_ * 1000.0);
                denominator = 1000;
            }
        }

        private IMFSinkWriter CreateWriter(bool hardware)
        {
            IMFAttributes?  attributes  = null;
            IMFMediaType?   outputType  = null;
            IMFMediaType?   inputType   = null;
            IMFSinkWriter?  writer      = null;
            try
            {
                MediaFoundation.Check(MediaFoundation.MFCreateAttributes(out attributes, 2), "MFCreateAttributes");
                attributes.SetUINT32(MediaFoundation.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, hardware ? 1 : 0);
                // the container is given explicitly: the temporary file has no .mp4 extension
                attributes.SetGUID(MediaFoundation.MF_TRANSCODE_CONTAINERTYPE, MediaFoundation.MFTranscodeContainerType_MPEG4);

                if (File.Exists(tempPath_))
                    File.Delete(tempPath_);
                MediaFoundation.Check(MediaFoundation.MFCreateSinkWriterFromURL(tempPath_, IntPtr.Zero, attributes, out writer), "MFCreateSinkWriterFromURL");

                GetFrameRate(out int fpsNum, out int fpsDen);

                // output: H.264
                MediaFoundation.Check(MediaFoundation.MFCreateMediaType(out outputType), "MFCreateMediaType");
                IMFAttributes output = (IMFAttributes)outputType;
                output.SetGUID  (MediaFoundation.MF_MT_MAJOR_TYPE           , MediaFoundation.MFMediaType_Video);
                output.SetGUID  (MediaFoundation.MF_MT_SUBTYPE              , MediaFoundation.MFVideoFormat_H264);
                output.SetUINT32(MediaFoundation.MF_MT_AVG_BITRATE          , bitrate_);
                output.SetUINT32(MediaFoundation.MF_MT_INTERLACE_MODE       , MediaFoundation.MFVideoInterlace_Progressive);
                output.SetUINT32(MediaFoundation.MF_MT_MPEG2_PROFILE        , MediaFoundation.eAVEncH264VProfile_High);
                output.SetUINT64(MediaFoundation.MF_MT_FRAME_SIZE           , MediaFoundation.Pack(encodedWidth_, encodedHeight_));
                output.SetUINT64(MediaFoundation.MF_MT_FRAME_RATE           , MediaFoundation.Pack(fpsNum, fpsDen));
                output.SetUINT64(MediaFoundation.MF_MT_PIXEL_ASPECT_RATIO   , MediaFoundation.Pack(1, 1));
                // BT.709 limited range, written to the stream: players do not have to guess (they guess BT.601 for small sizes)
                output.SetUINT32(MediaFoundation.MF_MT_YUV_MATRIX           , MediaFoundation.MFVideoTransferMatrix_BT709);
                output.SetUINT32(MediaFoundation.MF_MT_VIDEO_PRIMARIES      , MediaFoundation.MFVideoPrimaries_BT709);
                output.SetUINT32(MediaFoundation.MF_MT_TRANSFER_FUNCTION    , MediaFoundation.MFVideoTransFunc_709);
                output.SetUINT32(MediaFoundation.MF_MT_VIDEO_NOMINAL_RANGE  , MediaFoundation.MFNominalRange_16_235);
                writer.AddStream(outputType, out streamIndex_);

                // input: RGB32 (B, G, R, X), top-down
                MediaFoundation.Check(MediaFoundation.MFCreateMediaType(out inputType), "MFCreateMediaType");
                IMFAttributes input = (IMFAttributes)inputType;
                input.SetGUID   (MediaFoundation.MF_MT_MAJOR_TYPE           , MediaFoundation.MFMediaType_Video);
                input.SetGUID   (MediaFoundation.MF_MT_SUBTYPE              , MediaFoundation.MFVideoFormat_RGB32);
                input.SetUINT32 (MediaFoundation.MF_MT_INTERLACE_MODE       , MediaFoundation.MFVideoInterlace_Progressive);
                input.SetUINT32 (MediaFoundation.MF_MT_DEFAULT_STRIDE       , encodedWidth_ * 4);
                input.SetUINT32 (MediaFoundation.MF_MT_ALL_SAMPLES_INDEPENDENT, 1);
                input.SetUINT64 (MediaFoundation.MF_MT_FRAME_SIZE           , MediaFoundation.Pack(encodedWidth_, encodedHeight_));
                input.SetUINT64 (MediaFoundation.MF_MT_FRAME_RATE           , MediaFoundation.Pack(fpsNum, fpsDen));
                input.SetUINT64 (MediaFoundation.MF_MT_PIXEL_ASPECT_RATIO   , MediaFoundation.Pack(1, 1));
                writer.SetInputMediaType(streamIndex_, inputType, null);

                writer.BeginWriting();

                IMFSinkWriter result = writer;
                writer = null;
                return result;
            }
            finally
            {
                MediaFoundation.Release(writer);
                MediaFoundation.Release(inputType);
                MediaFoundation.Release(outputType);
                MediaFoundation.Release(attributes);
            }
        }

        private void ReleaseWriter()
        {
            MediaFoundation.Release(writer_);
            writer_ = null;
        }

        private void Shutdown()
        {
            if (!isStarted_)
                return;
            MediaFoundation.MFShutdown();
            isStarted_ = false;
        }
    }
}
