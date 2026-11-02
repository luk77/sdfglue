//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppVideoExport.Gif;
using System.Drawing;
using System.Drawing.Imaging;
using Xunit;

namespace SingleDocAppVideoExport.Tests
{
    public class GifTests
    {
        // ---------------------------------------------------------------- helpers

        private static VideoFrame MakeFrame(int width, int height, Func<int, int, int> rgbAt)
        {
            VideoFrame frame = new VideoFrame(width, height);
            for (int y=0; y<height; y++)
            {
                for (int x=0; x<width; x++)
                {
                    int c = rgbAt(x, y);
                    int p = (y * width + x) * 4;
                    frame.Pixels[p + 0] = (byte)(c & 0xFF);
                    frame.Pixels[p + 1] = (byte)((c >> 8) & 0xFF);
                    frame.Pixels[p + 2] = (byte)((c >> 16) & 0xFF);
                    frame.Pixels[p + 3] = 255;
                }
            }
            return frame;
        }

        private static byte[] Encode(GifOptions options, double fps, params VideoFrame[] frames)
        {
            using MemoryStream stream = new MemoryStream();
            GifEncoder encoder = new GifEncoder(stream, frames[0].Width, frames[0].Height, options);
            for (int i=0; i<frames.Length; i++)
                encoder.AddFrame(frames[i].Pixels, frames[i].Stride, GifEncoder.GetFrameDelay(i, fps));
            encoder.Finish();
            return stream.ToArray();
        }

        private static int Palette16(int x, int y)
        {
            // 16 colors with different 5-bit histogram bins
            int i = (x / 3 + y / 2) & 15;
            return ((i * 16) << 16) | ((255 - i * 16) << 8) | ((i * 37) & 0xFF);
        }

        private static int Gradient(int x, int y)
        {
            return ((x * 255 / 199) << 16) | ((y * 255 / 99) << 8) | (((x + y) * 255 / 298) & 0xFF);
        }

        // ---------------------------------------------------------------- LZW

        [Theory]
        [InlineData(2, 1)]
        [InlineData(2, 1000)]
        [InlineData(4, 5000)]
        [InlineData(8, 100000)]     // the dictionary is cleared many times
        [InlineData(8, 0)]
        public void Lzw_RoundTrip(int minCodeSize, int count)
        {
            Random random = new Random(1234 + count);
            byte[] indices = new byte[count];
            int numOfSymbols = 1 << minCodeSize;
            for (int i=0; i<count; i++)
            {
                // runs and noise: both short and long dictionary entries
                indices[i] = (i % 97 < 50) ? (byte)((i / 13) % numOfSymbols) : (byte)random.Next(numOfSymbols);
            }

            using MemoryStream stream = new MemoryStream();
            new GifLzwEncoder().Encode(stream, indices, count, minCodeSize);
            byte[] encoded = stream.ToArray();

            Assert.Equal(minCodeSize, encoded[0]);
            Assert.Equal(0, encoded[^1]);   // block terminator
            List<byte> data = new List<byte>();
            int p = 1;
            while (encoded[p] != 0)
            {
                int length = encoded[p];
                Assert.InRange(length, 1, 255);
                data.AddRange(encoded.Skip(p + 1).Take(length));
                p += length + 1;
            }
            Assert.Equal(encoded.Length - 1, p);

            byte[] decoded = GifTestDecoder.DecodeLzw(data.ToArray(), minCodeSize, count);
            Assert.Equal(indices, decoded);
        }

        // ---------------------------------------------------------------- quantizer

        [Fact]
        public void Quantizer_FewColors_IsExact()
        {
            int[] colors = new int[1000];
            for (int i=0; i<colors.Length; i++)
                colors[i] = Palette16(i % 40, i / 40);
            colors[5] = -1;     // transparent pixels are skipped

            GifColorQuantizer q = new GifColorQuantizer();
            int count = q.Build(colors, colors.Length, 256);

            Assert.True(q.IsExact);
            Assert.Equal(16, count);
            foreach (int c in colors.Where(c => c >= 0))
            {
                int index = q.Map(c);
                Assert.Equal(c, (q.GetR(index) << 16) | (q.GetG(index) << 8) | q.GetB(index));
            }
        }

        [Fact]
        public void Quantizer_ManyColors_UsesAllEntriesAndIsClose()
        {
            int[] colors = new int[200 * 100];
            for (int y=0; y<100; y++)
                for (int x=0; x<200; x++)
                    colors[y * 200 + x] = Gradient(x, y);

            GifColorQuantizer q = new GifColorQuantizer();
            int count = q.Build(colors, colors.Length, 255);

            Assert.False(q.IsExact);
            Assert.Equal(255, count);

            double sumError = 0.0;
            foreach (int c in colors)
            {
                int index = q.Map(c);
                Assert.InRange(index, 0, count - 1);
                sumError += Math.Abs(((c >> 16) & 0xFF) - q.GetR(index)) + Math.Abs(((c >> 8) & 0xFF) - q.GetG(index)) + Math.Abs((c & 0xFF) - q.GetB(index));
            }
            // 255 colors for a smooth 3D gradient: a few levels per component on average
            Assert.True(sumError / colors.Length / 3.0 < 8.0, String.Format("average error {0}", sumError / colors.Length / 3.0));
        }

        // ---------------------------------------------------------------- the encoder

        [Fact]
        public void FrameDelay_SumIsTheDuration()
        {
            foreach (double fps in new[] { 25.0, 30.0, 50.0, 24.0 })
            {
                int sum = 0;
                for (int i=0; i<(int)fps * 3; i++)
                {
                    int delay = GifEncoder.GetFrameDelay(i, fps);
                    Assert.True(delay >= 2, String.Format("fps {0}: delay {1}", fps, delay));
                    sum += delay;
                }
                Assert.Equal(300, sum);
            }
        }

        [Fact]
        public void Encoder_FewColors_IsLossless()
        {
            VideoFrame frame = MakeFrame(40, 30, Palette16);
            byte[] gifData = Encode(new GifOptions { Loop = true }, 30, frame);

            GifTestDecoder gif = GifTestDecoder.Decode(gifData);
            Assert.Equal(40, gif.Width);
            Assert.Equal(30, gif.Height);
            Assert.True(gif.Loops);
            Assert.Single(gif.Frames);
            for (int y=0; y<30; y++)
                for (int x=0; x<40; x++)
                    Assert.Equal(Palette16(x, y), gif.Frames[0].Picture[y * 40 + x]);
        }

        [Fact]
        public void Encoder_NoLoop_HasNoNetscapeExtension()
        {
            byte[] gifData = Encode(new GifOptions { Loop = false }, 30, MakeFrame(8, 8, Palette16));
            Assert.False(GifTestDecoder.Decode(gifData).Loops);
        }

        [Theory]
        [InlineData(GifDither.None)]
        [InlineData(GifDither.Ordered)]
        [InlineData(GifDither.FloydSteinberg)]
        public void Encoder_Gradient_IsClose(GifDither dither)
        {
            VideoFrame frame = MakeFrame(200, 100, Gradient);
            byte[] gifData = Encode(new GifOptions { Dither = dither }, 30, frame);

            GifTestDecoder gif = GifTestDecoder.Decode(gifData);
            int[] picture = gif.Frames[0].Picture;

            // the average over 8x8 blocks stays close to the source (dithering keeps the local average)
            double maxBlockError = 0.0;
            for (int by=0; by<100/8; by++)
            {
                for (int bx=0; bx<200/8; bx++)
                {
                    double[] src = new double[3], dst = new double[3];
                    for (int y=by*8; y<by*8+8; y++)
                    {
                        for (int x=bx*8; x<bx*8+8; x++)
                        {
                            int s = Gradient(x, y), d = picture[y * 200 + x];
                            for (int c=0; c<3; c++)
                            {
                                src[c] += (s >> (16 - 8 * c)) & 0xFF;
                                dst[c] += (d >> (16 - 8 * c)) & 0xFF;
                            }
                        }
                    }
                    for (int c=0; c<3; c++)
                        maxBlockError = Math.Max(maxBlockError, Math.Abs(src[c] - dst[c]) / 64.0);
                }
            }
            Assert.True(maxBlockError < 12.0, String.Format("{0}: max block error {1}", dither, maxBlockError));
        }

        [Fact]
        public void Encoder_Delta_TransparentOnlyWhereUnchanged()
        {
            // a moving square on a gradient
            Func<int, Func<int, int, int>> frameAt = t => (x, y) => (x >= 10 + t * 5 && x < 30 + t * 5 && y >= 20 && y < 40) ? 0xFF0000 : Gradient(x, y);
            VideoFrame[] frames = Enumerable.Range(0, 5).Select(t => MakeFrame(200, 100, frameAt(t))).ToArray();

            byte[] withDelta    = Encode(new GifOptions { OptimizeDelta = true , Dither = GifDither.None }, 25, frames);
            byte[] withoutDelta = Encode(new GifOptions { OptimizeDelta = false, Dither = GifDither.None }, 25, frames);
            Assert.True(withDelta.Length < withoutDelta.Length / 2, String.Format("{0} vs {1} bytes", withDelta.Length, withoutDelta.Length));

            GifTestDecoder a = GifTestDecoder.Decode(withDelta);
            GifTestDecoder b = GifTestDecoder.Decode(withoutDelta);
            Assert.Equal(5, a.Frames.Count);
            Assert.Equal(5, b.Frames.Count);
            for (int i=1; i<5; i++)
            {
                // the frame covers only the changed area
                Assert.True(a.Frames[i].Width <= 30 && a.Frames[i].Height == 20, String.Format("frame {0}: {1}x{2}", i, a.Frames[i].Width, a.Frames[i].Height));
                Assert.Equal(1, a.Frames[i].Disposal);
                // the pixels that did not change keep the previous picture exactly, the changed ones are close to the source
                for (int y=0; y<100; y++)
                {
                    for (int x=0; x<200; x++)
                    {
                        int src  = frameAt(i)(x, y);
                        int shown = a.Frames[i].Picture[y * 200 + x];
                        if (src == frameAt(i - 1)(x, y))
                        {
                            Assert.Equal(a.Frames[i - 1].Picture[y * 200 + x], shown);
                        }
                        else
                        {
                            int error = Math.Abs(((src >> 16) & 0xFF) - ((shown >> 16) & 0xFF)) + Math.Abs(((src >> 8) & 0xFF) - ((shown >> 8) & 0xFF)) + Math.Abs((src & 0xFF) - (shown & 0xFF));
                            Assert.True(error < 60, String.Format("frame {0} ({1},{2}): {3:X6} shown as {4:X6}", i, x, y, src, shown));
                        }
                    }
                }
            }
        }

        [Fact]
        public void Encoder_SameFrames_AreMerged()
        {
            VideoFrame f0 = MakeFrame(16, 16, Palette16);
            VideoFrame f1 = MakeFrame(16, 16, (x, y) => Palette16(x + 1, y));
            byte[] gifData = Encode(new GifOptions { OptimizeDelta = true }, 25, f0, f0, f0, f1, f1);

            GifTestDecoder gif = GifTestDecoder.Decode(gifData);
            Assert.Equal(2, gif.Frames.Count);
            Assert.Equal(12, gif.Frames[0].Delay);     // 3 frames of 4/100 s
            Assert.Equal(8, gif.Frames[1].Delay);
        }

        // System.Drawing (GDI+) as an independent decoder
        [Fact]
        public void Encoder_GdiPlusReadsTheFile()
        {
            VideoFrame[] frames = Enumerable.Range(0, 3).Select(t => MakeFrame(64, 48, (x, y) => Gradient(x * 3 + t * 10, y * 2))).ToArray();
            byte[] gifData = Encode(new GifOptions(), 25, frames);

            using MemoryStream stream = new MemoryStream(gifData);
            using Bitmap bmp = new Bitmap(stream);
            Assert.Equal(64, bmp.Width);
            Assert.Equal(48, bmp.Height);
            Assert.Equal(3, bmp.GetFrameCount(FrameDimension.Time));

            GifTestDecoder gif = GifTestDecoder.Decode(gifData);
            bmp.SelectActiveFrame(FrameDimension.Time, 0);
            for (int y=0; y<48; y+=5)
                for (int x=0; x<64; x+=5)
                    Assert.Equal(gif.Frames[0].Picture[y * 64 + x], bmp.GetPixel(x, y).ToArgb() & 0xFFFFFF);
        }

        [Fact]
        public void Sink_WritesFileAndRemovesTempFile()
        {
            string dir = Path.Combine(Path.GetTempPath(), "SdaVideoExportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "test.gif");
                GifSink sink = new GifSink(path, new GifOptions(), 2);
                sink.Begin(64, 48, 25);
                for (int i=0; i<4; i++)
                {
                    VideoFrame frame = MakeFrame(64, 48, (x, y) => Gradient(x + i * 7, y));
                    frame.Index = i;
                    sink.AddFrame(frame);
                }
                sink.Finish();

                Assert.True(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
                GifTestDecoder gif = GifTestDecoder.Decode(File.ReadAllBytes(path));
                Assert.Equal(32, gif.Width);
                Assert.Equal(24, gif.Height);
                Assert.Equal(16, gif.Frames.Sum(f => f.Delay));

                // abort: no file, the existing one is kept
                GifSink aborted = new GifSink(path, new GifOptions());
                aborted.Begin(64, 48, 25);
                aborted.Abort();
                Assert.False(File.Exists(path + ".tmp"));
                Assert.Equal(32, GifTestDecoder.Decode(File.ReadAllBytes(path)).Width);
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
