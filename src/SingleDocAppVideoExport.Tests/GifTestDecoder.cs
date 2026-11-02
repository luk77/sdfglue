//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport.Tests
{
    // A small GIF decoder for the tests: the blocks, LZW, the local color tables, transparency and the disposal
    // "do not dispose" (the frames are drawn over the previous picture). Throws on an invalid file.
    public sealed class GifTestDecoder
    {
        public sealed class Frame
        {
            public  int     Left, Top, Width, Height;
            public  int     Delay;              // 1/100 s
            public  int     TransparentIndex    = -1;
            public  int     Disposal;
            public  int     NumOfColors;        // entries of the local color table
            public  int[]   Picture             = Array.Empty<int>();   // the whole screen after this frame, 0xRRGGBB
        }

        public  int             Width;
        public  int             Height;
        public  bool            Loops;
        public  List<Frame>     Frames          = new List<Frame>();

        public static GifTestDecoder Decode(byte[] data)
        {
            GifTestDecoder gif = new GifTestDecoder();
            int p = 0;

            string signature = System.Text.Encoding.ASCII.GetString(data, 0, 6);
            if (signature != "GIF89a")
                throw new InvalidDataException("Not a GIF89a file: " + signature);
            p = 6;

            gif.Width   = data[p] | (data[p + 1] << 8);
            gif.Height  = data[p + 2] | (data[p + 3] << 8);
            int packed  = data[p + 4];
            p += 7;
            if ((packed & 0x80) != 0)
                p += 3 * (1 << ((packed & 7) + 1));     // global color table (not used by the encoder)

            int[]   screen          = new int[gif.Width * gif.Height];
            int     delay           = 0;
            int     transparent     = -1;
            int     disposal        = 0;

            while (true)
            {
                int block = data[p++];
                if (block == 0x3B)
                    break;

                if (block == 0x21)
                {
                    int label = data[p++];
                    if (label == 0xF9)
                    {
                        if (data[p] != 4)
                            throw new InvalidDataException("Invalid graphic control extension.");
                        int flags   = data[p + 1];
                        delay       = data[p + 2] | (data[p + 3] << 8);
                        transparent = (flags & 1) != 0 ? data[p + 4] : -1;
                        disposal    = (flags >> 2) & 7;
                        p += 5;
                        if (data[p++] != 0)
                            throw new InvalidDataException("Missing block terminator.");
                    }
                    else
                    {
                        if (label == 0xFF && data[p] == 11 && System.Text.Encoding.ASCII.GetString(data, p + 1, 11) == "NETSCAPE2.0")
                            gif.Loops = true;
                        p = SkipSubBlocks(data, p);
                    }
                    continue;
                }

                if (block != 0x2C)
                    throw new InvalidDataException(String.Format("Unknown block 0x{0:X2} at {1}.", block, p - 1));

                Frame frame = new Frame();
                frame.Left      = data[p]     | (data[p + 1] << 8);
                frame.Top       = data[p + 2] | (data[p + 3] << 8);
                frame.Width     = data[p + 4] | (data[p + 5] << 8);
                frame.Height    = data[p + 6] | (data[p + 7] << 8);
                int imagePacked = data[p + 8];
                p += 9;
                if ((imagePacked & 0x80) == 0)
                    throw new InvalidDataException("The frame has no local color table.");
                if ((imagePacked & 0x40) != 0)
                    throw new InvalidDataException("Interlaced frames are not expected.");
                if (frame.Left + frame.Width > gif.Width || frame.Top + frame.Height > gif.Height)
                    throw new InvalidDataException("The frame is outside of the screen.");

                frame.NumOfColors = 1 << ((imagePacked & 7) + 1);
                int[] colors = new int[frame.NumOfColors];
                for (int i=0; i<frame.NumOfColors; i++, p+=3)
                    colors[i] = (data[p] << 16) | (data[p + 1] << 8) | data[p + 2];

                int minCodeSize = data[p++];
                List<byte> lzwData = new List<byte>();
                while (true)
                {
                    int length = data[p++];
                    if (length == 0)
                        break;
                    for (int i=0; i<length; i++)
                        lzwData.Add(data[p + i]);
                    p += length;
                }

                byte[] indices = DecodeLzw(lzwData.ToArray(), minCodeSize, frame.Width * frame.Height);

                for (int y=0; y<frame.Height; y++)
                {
                    for (int x=0; x<frame.Width; x++)
                    {
                        int index = indices[y * frame.Width + x];
                        if (index == transparent)
                            continue;
                        if (index >= colors.Length)
                            throw new InvalidDataException("Color index outside of the color table.");
                        screen[(frame.Top + y) * gif.Width + frame.Left + x] = colors[index];
                    }
                }

                frame.Delay             = delay;
                frame.TransparentIndex  = transparent;
                frame.Disposal          = disposal;
                frame.Picture           = (int[])screen.Clone();
                gif.Frames.Add(frame);

                delay       = 0;
                transparent = -1;
                disposal    = 0;
            }

            return gif;
        }

        private static int SkipSubBlocks(byte[] data, int p)
        {
            while (true)
            {
                int length = data[p++];
                if (length == 0)
                    return p;
                p += length;
            }
        }

        public static byte[] DecodeLzw(byte[] data, int minCodeSize, int numOfPixels)
        {
            int clearCode   = 1 << minCodeSize;
            int endCode     = clearCode + 1;
            int codeSize    = minCodeSize + 1;
            int nextCode    = clearCode + 2;

            int[]   prefix  = new int[4096];
            byte[]  suffix  = new byte[4096];
            byte[]  first   = new byte[4096];
            for (int i=0; i<clearCode; i++)
            {
                suffix[i]   = (byte)i;
                first[i]    = (byte)i;
                prefix[i]   = -1;
            }

            List<byte>  output  = new List<byte>(numOfPixels);
            Stack<byte> stack   = new Stack<byte>();
            int         bitPos  = 0;
            int         prev    = -1;
            bool        ended   = false;

            while (!ended)
            {
                if (bitPos + codeSize > data.Length * 8)
                    throw new InvalidDataException("The LZW data ended without the end code.");
                int code = 0;
                for (int b=0; b<codeSize; b++, bitPos++)
                {
                    if ((data[bitPos >> 3] & (1 << (bitPos & 7))) != 0)
                        code |= 1 << b;
                }

                if (code == clearCode)
                {
                    codeSize = minCodeSize + 1;
                    nextCode = clearCode + 2;
                    prev     = -1;
                    continue;
                }
                if (code == endCode)
                {
                    ended = true;
                    continue;
                }

                if (prev < 0)
                {
                    if (code >= clearCode)
                        throw new InvalidDataException("The first code after clear is not a pixel.");
                    output.Add((byte)code);
                    prev = code;
                    continue;
                }

                int emitted;
                byte firstOfCode;
                if (code < nextCode)
                {
                    emitted     = code;
                    firstOfCode = first[code];
                }
                else if (code == nextCode)
                {
                    emitted     = -1;      // prev + first(prev)
                    firstOfCode = first[prev];
                }
                else
                {
                    throw new InvalidDataException(String.Format("Invalid LZW code {0} (next {1}).", code, nextCode));
                }

                if (nextCode < 4096)
                {
                    prefix[nextCode]    = prev;
                    suffix[nextCode]    = firstOfCode;
                    first[nextCode]     = first[prev];
                    nextCode++;
                    if (nextCode == (1 << codeSize) && codeSize < 12)
                        codeSize++;
                }

                int c = emitted >= 0 ? emitted : nextCode - 1;
                while (c >= 0)
                {
                    stack.Push(suffix[c]);
                    c = prefix[c];
                }
                while (stack.Count > 0)
                    output.Add(stack.Pop());

                prev = code;
            }

            if (output.Count != numOfPixels)
                throw new InvalidDataException(String.Format("LZW: {0} pixels decoded, {1} expected.", output.Count, numOfPixels));
            return output.ToArray();
        }
    }
}
