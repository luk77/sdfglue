//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SingleDocAppVideoExport;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SdfGlueEditor.Application
{
    // Image sequence: one PNG file per frame (ExportAnimationSettings.GetFileName). Written on the thread of
    // FrameSinkWorker. A cancelled export keeps the files saved so far.
    public class PngSequenceSink : IFrameSink
    {
        private readonly string                     outputFolder_;
        private readonly ExportAnimationSettings    settings_;

        public PngSequenceSink(string outputFolder, ExportAnimationSettings settings)
        {
            outputFolder_   = outputFolder;
            settings_       = settings;
        }

        public void Begin(int width, int height, double fps)
        {
        }

        public void AddFrame(VideoFrame frame)
        {
            string filePath = Path.Combine(outputFolder_, settings_.GetFileName(frame.Index));

            GCHandle handle = GCHandle.Alloc(frame.Pixels, GCHandleType.Pinned);
            try
            {
                // the Bitmap uses the pinned buffer (no copy); saved before the buffer is released
                using Bitmap bmp = new Bitmap(frame.Width, frame.Height, frame.Stride, PixelFormat.Format32bppArgb, handle.AddrOfPinnedObject());
                bmp.Save(filePath, ImageFormat.Png);
            }
            catch (Exception ex)
            {
                throw new IOException(String.Format("Cannot save image:\n{0}\n\n{1}", filePath, ex.Message), ex);
            }
            finally
            {
                handle.Free();
            }
        }

        public void Finish()
        {
        }

        public void Abort()
        {
        }
    }
}
