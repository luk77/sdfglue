//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppVideoExportWin;
using Xunit;

namespace SingleDocAppVideoExport.Tests
{
    public class Mp4Tests
    {
        [Fact]
        public void Bitrate_And_EncodedSize()
        {
            Assert.Equal(12_441_600, Mp4Sink.GetBitrate(1920, 1080, 60, Mp4Sink.MediumBitsPerPixel));
            Assert.Equal(Mp4Sink.MinBitrate, Mp4Sink.GetBitrate(16, 9, 1, Mp4Sink.LowBitsPerPixel));
            Mp4Sink.GetEncodedSize(16, 9, out int w, out int h);
            Assert.Equal(16, w);
            Assert.Equal(8, h);
        }

        // Media Foundation is a part of desktop Windows (Windows Server: the "Media Foundation" feature); without it
        // the test checks nothing
        private static bool IsMediaFoundationAvailable()
        {
            return File.Exists(Path.Combine(Environment.SystemDirectory, "mfplat.dll")) && File.Exists(Path.Combine(Environment.SystemDirectory, "mfreadwrite.dll"));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void Sink_WritesMp4(bool allowHardware)
        {
            if (!IsMediaFoundationAvailable())
                return;

            string dir = Path.Combine(Path.GetTempPath(), "SdaVideoExportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "test.mp4");
                Mp4Sink sink = new Mp4Sink(path, 2_000_000, allowHardware);
                // an odd size: the last column and row are dropped
                sink.Begin(161, 121, 30);
                for (int i=0; i<30; i++)
                {
                    VideoFrame frame = new VideoFrame(161, 121) { Index = i };
                    for (int p=0; p<frame.Pixels.Length; p+=4)
                    {
                        int x = (p / 4) % 161;
                        frame.Pixels[p + 0] = (byte)(x + i * 4);
                        frame.Pixels[p + 1] = (byte)(i * 8);
                        frame.Pixels[p + 2] = (byte)(255 - x);
                        frame.Pixels[p + 3] = 255;
                    }
                    sink.AddFrame(frame);
                }
                sink.Finish();

                Assert.True(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
                byte[] data = File.ReadAllBytes(path);
                Assert.True(data.Length > 1000);
                // ISO base media file: the first box is "ftyp"
                Assert.Equal("ftyp", System.Text.Encoding.ASCII.GetString(data, 4, 4));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }

        [Fact]
        public void Sink_AbortRemovesTheFile()
        {
            if (!IsMediaFoundationAvailable())
                return;

            string dir = Path.Combine(Path.GetTempPath(), "SdaVideoExportTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "test.mp4");
                Mp4Sink sink = new Mp4Sink(path, 2_000_000);
                sink.Begin(64, 48, 30);
                sink.AddFrame(new VideoFrame(64, 48) { Index = 0 });
                sink.Abort();
                Assert.False(File.Exists(path));
                Assert.False(File.Exists(path + ".tmp"));
            }
            finally
            {
                Directory.Delete(dir, true);
            }
        }
    }
}
