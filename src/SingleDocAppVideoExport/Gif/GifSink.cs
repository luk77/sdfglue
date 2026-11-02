//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport.Gif
{
    // Animated GIF file. The frames can be scaled down (scale 2: half the width and height, the average of 2x2 pixels).
    // The file is written under a temporary name and renamed at the end - a cancelled export leaves no file
    // (and does not destroy an existing one).
    public sealed class GifSink : IFrameSink
    {
        private readonly string         filePath_;
        private readonly string         tempPath_;
        private readonly GifOptions     options_;
        private          int            scale_;

        private FileStream?             file_;
        private BufferedStream?         stream_;
        private GifEncoder?             encoder_;
        private byte[]                  scaled_     = Array.Empty<byte>();
        private double                  fps_;

        public GifSink(string filePath, GifOptions options, int scale = 1)
        {
            filePath_   = filePath;
            tempPath_   = filePath + ".tmp";
            options_    = options;
            scale_      = Math.Max(1, scale);
        }

        // The size of the GIF for a frame size (at least 1x1)
        public static void GetOutputSize(int width, int height, int scale, out int outWidth, out int outHeight)
        {
            scale       = Math.Max(1, scale);
            outWidth    = Math.Max(1, width  / scale);
            outHeight   = Math.Max(1, height / scale);
        }

        public void Begin(int width, int height, double fps)
        {
            // a frame smaller than the blocks is not scaled down
            scale_ = Math.Max(1, Math.Min(scale_, Math.Min(width, height)));
            GetOutputSize(width, height, scale_, out int outWidth, out int outHeight);
            fps_ = fps;

            file_       = new FileStream(tempPath_, FileMode.Create, FileAccess.Write, FileShare.None);
            stream_     = new BufferedStream(file_, 1 << 20);
            encoder_    = new GifEncoder(stream_, outWidth, outHeight, options_);
            if (scale_ > 1)
                scaled_ = new byte[outWidth * outHeight * 4];
        }

        public void AddFrame(VideoFrame frame)
        {
            if (encoder_ == null)
                throw new InvalidOperationException("GifSink: Begin was not called.");

            int delay = GifEncoder.GetFrameDelay(frame.Index, fps_);
            if (scale_ > 1)
            {
                Downscale(frame, scale_, scaled_, encoder_.Width, encoder_.Height);
                encoder_.AddFrame(scaled_, encoder_.Width * 4, delay);
            }
            else
            {
                encoder_.AddFrame(frame.Pixels, frame.Stride, delay);
            }
        }

        public void Finish()
        {
            if (encoder_ == null)
                throw new InvalidOperationException("GifSink: Begin was not called.");

            encoder_.Finish();
            Close();
            File.Move(tempPath_, filePath_, true);
        }

        public void Abort()
        {
            try
            {
                Close();
                if (File.Exists(tempPath_))
                    File.Delete(tempPath_);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: GifSink: cannot remove the temporary file {0}. {1}", tempPath_, ex.Message);
            }
        }

        private void Close()
        {
            stream_?.Dispose();
            file_?.Dispose();
            stream_     = null;
            file_       = null;
            encoder_    = null;
        }

        // The average of scale x scale blocks (the remaining right / bottom pixels are dropped)
        public static void Downscale(VideoFrame frame, int scale, byte[] dst, int outWidth, int outHeight)
        {
            byte[]  src     = frame.Pixels;
            int     stride  = frame.Stride;
            int     n       = scale * scale;
            for (int y=0; y<outHeight; y++)
            {
                for (int x=0; x<outWidth; x++)
                {
                    int b = 0, g = 0, r = 0, a = 0;
                    for (int sy=0; sy<scale; sy++)
                    {
                        int p = (y * scale + sy) * stride + x * scale * 4;
                        for (int sx=0; sx<scale; sx++, p+=4)
                        {
                            b += src[p + 0];
                            g += src[p + 1];
                            r += src[p + 2];
                            a += src[p + 3];
                        }
                    }
                    int d = (y * outWidth + x) * 4;
                    dst[d + 0] = (byte)((b + n / 2) / n);
                    dst[d + 1] = (byte)((g + n / 2) / n);
                    dst[d + 2] = (byte)((r + n / 2) / n);
                    dst[d + 3] = (byte)((a + n / 2) / n);
                }
            }
        }
    }
}
