//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport
{
    // One frame: 32-bit pixels, byte order B, G, R, A (as System.Drawing Format32bppArgb), rows top-down,
    // stride = Width * 4. The buffers are reused by FrameSinkWorker - a sink must not keep a frame after AddFrame.
    public sealed class VideoFrame
    {
        public  readonly int        Width;
        public  readonly int        Height;
        public  readonly byte[]     Pixels;
        public           int        Index;          // 0, 1, 2 ... (the order of the frames)

        public VideoFrame(int width, int height)
        {
            Width   = width;
            Height  = height;
            Pixels  = new byte[width * height * 4];
        }

        public int Stride { get { return Width * 4; } }
    }

    // The destination of the exported frames (an image sequence, a video file...).
    // All methods are called on the writer thread of FrameSinkWorker, in this order:
    //   Begin, AddFrame (for every frame), Finish (all frames written) or Abort (cancelled or failed).
    // Errors are reported by exceptions (the message is shown to the user).
    public interface IFrameSink
    {
        void Begin      (int width, int height, double fps);
        void AddFrame   (VideoFrame frame);
        void Finish     ();
        // must not throw; removes a partial output file (if any)
        void Abort      ();
    }
}
