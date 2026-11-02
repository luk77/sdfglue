//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport.Gif
{
    public enum GifDither
    {
        None,
        Ordered,            // Bayer 8x8: stable in an animation (no crawling noise)
        FloydSteinberg,     // error diffusion: smoother still images, the noise changes from frame to frame
    }

    public class GifOptions
    {
        // browsers show frames with a delay below 2/100 s with 10/100 s - faster GIFs play slower
        public  const int           MaxRecommendedFps   = 50;

        public  GifDither           Dither              = GifDither.Ordered;
        public  bool                Loop                = true;     // false: played once
        // pixels that did not change are transparent and the frame is cropped to the changed area (smaller files)
        public  bool                OptimizeDelta       = true;
        // a pixel is unchanged when no component differs more than this (0: exactly the same)
        public  int                 DeltaThreshold      = 0;
    }

    // Animated GIF (GIF89a) writer: every frame has its own palette (GifColorQuantizer), LZW (GifLzwEncoder).
    // A frame that is the same as the previous one is not written - its delay is added to the previous frame.
    // Usage: new GifEncoder(stream, w, h, options) -> AddFrame* -> Finish. The stream is not closed.
    public sealed class GifEncoder
    {
        private static readonly int[]   Bayer8x8 =
        {
             0, 32,  8, 40,  2, 34, 10, 42,
            48, 16, 56, 24, 50, 18, 58, 26,
            12, 44,  4, 36, 14, 46,  6, 38,
            60, 28, 52, 20, 62, 30, 54, 22,
             3, 35, 11, 43,  1, 33,  9, 41,
            51, 19, 59, 27, 49, 17, 57, 25,
            15, 47,  7, 39, 13, 45,  5, 37,
            63, 31, 55, 23, 61, 29, 53, 21,
        };

        private readonly Stream             stream_;
        private readonly int                width_;
        private readonly int                height_;
        private readonly GifOptions         options_;

        private readonly GifColorQuantizer  quantizer_      = new GifColorQuantizer();
        private readonly GifLzwEncoder      lzw_            = new GifLzwEncoder();

        private readonly int[]              current_;       // 0xRRGGBB of the new frame
        private readonly int[]              shown_;         // the source colors of the pixels shown now (delta)
        private          bool               hasShown_       = false;
        private readonly int[]              work_;          // the colors of the written rectangle, -1: transparent
        private readonly byte[]             indices_;
        private          float[]            errors_         = Array.Empty<float>();

        // the last frame is written when the next different frame comes (its delay can grow)
        private readonly MemoryStream       pendingFrame_   = new MemoryStream();
        private          bool               hasPending_     = false;
        private          int                pendingDelay_   = 0;
        private          int                pendingTransparentIndex_ = -1;
        private          bool               isFinished_     = false;

        public GifEncoder(Stream stream, int width, int height, GifOptions options)
        {
            if (width <= 0 || height <= 0 || width > 65535 || height > 65535)
                throw new ArgumentException(String.Format("Invalid GIF size: {0}x{1}", width, height));

            stream_     = stream;
            width_      = width;
            height_     = height;
            options_    = options;

            current_    = new int[width * height];
            shown_      = new int[width * height];
            work_       = new int[width * height];
            indices_    = new byte[width * height];

            WriteHeader();
        }

        public int Width    { get { return width_;  } }
        public int Height   { get { return height_; } }

        // The delay in 1/100 s of frame i at the frame rate fps (rounded so the total time stays exact)
        public static int GetFrameDelay(int frameIndex, double fps)
        {
            double cs = 100.0 / fps;
            return (int)Math.Round((frameIndex + 1) * cs) - (int)Math.Round(frameIndex * cs);
        }

        // pixels: B, G, R, A (alpha is ignored), rows top-down; delay in 1/100 s
        public void AddFrame(byte[] pixels, int stride, int delay)
        {
            if (isFinished_)
                throw new InvalidOperationException("The GIF is finished.");

            for (int y=0; y<height_; y++)
            {
                int src = y * stride;
                int dst = y * width_;
                for (int x=0; x<width_; x++, src+=4)
                    current_[dst + x] = (pixels[src + 2] << 16) | (pixels[src + 1] << 8) | pixels[src];
            }

            // the changed rectangle
            int x0 = 0, y0 = 0, x1 = width_ - 1, y1 = height_ - 1;
            bool useDelta = options_.OptimizeDelta && hasShown_;
            if (useDelta && !FindChangedRect(out x0, out y0, out x1, out y1))
            {
                // the same picture: the previous frame is shown longer
                pendingDelay_ += delay;
                return;
            }

            int rectW = x1 - x0 + 1;
            int rectH = y1 - y0 + 1;
            bool hasTransparent = false;
            for (int y=0; y<rectH; y++)
            {
                int row = (y0 + y) * width_ + x0;
                for (int x=0; x<rectW; x++)
                {
                    int c = current_[row + x];
                    if (useDelta && IsSame(c, shown_[row + x]))
                    {
                        work_[y * rectW + x] = -1;
                        hasTransparent = true;
                    }
                    else
                    {
                        work_[y * rectW + x] = c;
                        shown_[row + x] = c;
                    }
                }
            }
            if (!hasShown_)
            {
                Array.Copy(current_, shown_, current_.Length);
                hasShown_ = true;
            }

            int numOfColors = quantizer_.Build(work_, rectW * rectH, hasTransparent ? GifColorQuantizer.MaxColors - 1 : GifColorQuantizer.MaxColors);
            int transparentIndex = hasTransparent ? numOfColors : -1;
            MapToIndices(rectW, rectH, x0, y0, transparentIndex);

            FlushPending();
            WriteFrame(x0, y0, rectW, rectH, numOfColors, transparentIndex);
            pendingDelay_ = delay;
        }

        // Writes the last frame and the trailer
        public void Finish()
        {
            if (isFinished_)
                return;
            FlushPending();
            stream_.WriteByte(0x3B);
            stream_.Flush();
            isFinished_ = true;
        }

        private bool IsSame(int a, int b)
        {
            if (a == b)
                return true;
            int t = options_.DeltaThreshold;
            if (t <= 0)
                return false;
            return Math.Abs(((a >> 16) & 0xFF) - ((b >> 16) & 0xFF)) <= t
                && Math.Abs(((a >>  8) & 0xFF) - ((b >>  8) & 0xFF)) <= t
                && Math.Abs(( a        & 0xFF) - ( b        & 0xFF)) <= t;
        }

        private bool FindChangedRect(out int x0, out int y0, out int x1, out int y1)
        {
            x0 = width_; y0 = height_; x1 = -1; y1 = -1;
            for (int y=0; y<height_; y++)
            {
                int row = y * width_;
                for (int x=0; x<width_; x++)
                {
                    if (IsSame(current_[row + x], shown_[row + x]))
                        continue;
                    if (x < x0) x0 = x;
                    if (x > x1) x1 = x;
                    if (y < y0) y0 = y;
                    y1 = y;
                }
            }
            return x1 >= 0;
        }

        private void MapToIndices(int rectW, int rectH, int x0, int y0, int transparentIndex)
        {
            int count = rectW * rectH;
            // an exact palette needs no dithering
            GifDither dither = quantizer_.IsExact ? GifDither.None : options_.Dither;

            if (dither == GifDither.None)
            {
                for (int i=0; i<count; i++)
                {
                    int c = work_[i];
                    indices_[i] = (byte)(c < 0 ? transparentIndex : quantizer_.Map(c));
                }
            }
            else if (dither == GifDither.Ordered)
            {
                float amplitude = Math.Clamp(quantizer_.GetAverageColorSpacing(), 2.0f, 48.0f);
                for (int y=0; y<rectH; y++)
                {
                    for (int x=0; x<rectW; x++)
                    {
                        int i = y * rectW + x;
                        int c = work_[i];
                        if (c < 0)
                        {
                            indices_[i] = (byte)transparentIndex;
                            continue;
                        }
                        // the pattern is fixed to the image (not to the rectangle), so it does not move between frames
                        int bayer = Bayer8x8[((y0 + y) & 7) * 8 + ((x0 + x) & 7)];
                        int offset = (int)MathF.Round(((bayer + 0.5f) / 64.0f - 0.5f) * amplitude);
                        indices_[i] = (byte)quantizer_.Map(Clamp255(((c >> 16) & 0xFF) + offset), Clamp255(((c >> 8) & 0xFF) + offset), Clamp255((c & 0xFF) + offset));
                    }
                }
            }
            else
            {
                MapFloydSteinberg(rectW, rectH, transparentIndex);
            }
        }

        private void MapFloydSteinberg(int rectW, int rectH, int transparentIndex)
        {
            // errors of the current and the next row (R, G, B), one pixel of margin on both sides
            int rowLength = (rectW + 2) * 3;
            if (errors_.Length < rowLength * 2)
                errors_ = new float[rowLength * 2];
            Array.Clear(errors_);

            int cur = 0, next = rowLength;
            for (int y=0; y<rectH; y++)
            {
                Array.Clear(errors_, next, rowLength);
                for (int x=0; x<rectW; x++)
                {
                    int i = y * rectW + x;
                    int c = work_[i];
                    if (c < 0)
                    {
                        indices_[i] = (byte)transparentIndex;
                        continue;
                    }

                    int e = cur + (x + 1) * 3;
                    float r = ((c >> 16) & 0xFF) + errors_[e + 0];
                    float g = ((c >>  8) & 0xFF) + errors_[e + 1];
                    float b = ( c        & 0xFF) + errors_[e + 2];
                    int ri = Clamp255((int)MathF.Round(r));
                    int gi = Clamp255((int)MathF.Round(g));
                    int bi = Clamp255((int)MathF.Round(b));

                    int index = quantizer_.Map(ri, gi, bi);
                    indices_[i] = (byte)index;

                    float er = r - quantizer_.GetR(index);
                    float eg = g - quantizer_.GetG(index);
                    float eb = b - quantizer_.GetB(index);
                    Diffuse(cur  + (x + 2) * 3, er, eg, eb, 7.0f / 16.0f);
                    Diffuse(next + (x    ) * 3, er, eg, eb, 3.0f / 16.0f);
                    Diffuse(next + (x + 1) * 3, er, eg, eb, 5.0f / 16.0f);
                    Diffuse(next + (x + 2) * 3, er, eg, eb, 1.0f / 16.0f);
                }
                (cur, next) = (next, cur);
            }
        }

        private void Diffuse(int e, float er, float eg, float eb, float weight)
        {
            errors_[e + 0] += er * weight;
            errors_[e + 1] += eg * weight;
            errors_[e + 2] += eb * weight;
        }

        private static int Clamp255(int v)
        {
            return v < 0 ? 0 : (v > 255 ? 255 : v);
        }

        private void WriteHeader()
        {
            WriteAscii(stream_, "GIF89a");

            // logical screen descriptor: no global color table, color resolution 8 bits
            WriteUInt16(stream_, width_);
            WriteUInt16(stream_, height_);
            stream_.WriteByte(0x70);
            stream_.WriteByte(0);   // background color index
            stream_.WriteByte(0);   // pixel aspect ratio

            if (options_.Loop)
            {
                // NETSCAPE2.0 application extension: loop forever
                stream_.WriteByte(0x21);
                stream_.WriteByte(0xFF);
                stream_.WriteByte(11);
                WriteAscii(stream_, "NETSCAPE2.0");
                stream_.WriteByte(3);
                stream_.WriteByte(1);
                WriteUInt16(stream_, 0);
                stream_.WriteByte(0);
            }
        }

        // the image descriptor, the local color table and the image data of a frame (to pendingFrame_)
        private void WriteFrame(int x0, int y0, int rectW, int rectH, int numOfColors, int transparentIndex)
        {
            int numOfEntries = numOfColors + (transparentIndex >= 0 ? 1 : 0);
            int bits = 1;
            while ((1 << bits) < numOfEntries)
                bits++;

            MemoryStream s = pendingFrame_;
            s.SetLength(0);

            s.WriteByte(0x2C);
            WriteUInt16(s, x0);
            WriteUInt16(s, y0);
            WriteUInt16(s, rectW);
            WriteUInt16(s, rectH);
            s.WriteByte((byte)(0x80 | (bits - 1)));     // local color table of 2^bits entries

            for (int i=0; i<(1 << bits); i++)
            {
                if (i < numOfColors)
                {
                    s.WriteByte(quantizer_.GetR(i));
                    s.WriteByte(quantizer_.GetG(i));
                    s.WriteByte(quantizer_.GetB(i));
                }
                else
                {
                    s.WriteByte(0);
                    s.WriteByte(0);
                    s.WriteByte(0);
                }
            }

            lzw_.Encode(s, indices_, rectW * rectH, Math.Max(2, bits));

            hasPending_                 = true;
            pendingTransparentIndex_    = transparentIndex;
        }

        private void FlushPending()
        {
            if (!hasPending_)
                return;

            // graphic control extension: disposal 1 (the frame stays - the next frames draw over it), delay, transparency
            int delay = Math.Clamp(pendingDelay_, 0, 65535);
            stream_.WriteByte(0x21);
            stream_.WriteByte(0xF9);
            stream_.WriteByte(4);
            stream_.WriteByte((byte)((1 << 2) | (pendingTransparentIndex_ >= 0 ? 1 : 0)));
            WriteUInt16(stream_, delay);
            stream_.WriteByte((byte)Math.Max(0, pendingTransparentIndex_));
            stream_.WriteByte(0);

            pendingFrame_.WriteTo(stream_);
            hasPending_ = false;
        }

        private static void WriteUInt16(Stream s, int v)
        {
            s.WriteByte((byte)(v & 0xFF));
            s.WriteByte((byte)((v >> 8) & 0xFF));
        }

        private static void WriteAscii(Stream s, string text)
        {
            foreach (char c in text)
                s.WriteByte((byte)c);
        }
    }
}
