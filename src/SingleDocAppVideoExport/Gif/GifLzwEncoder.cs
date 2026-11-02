//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport.Gif
{
    // LZW compression of the image data of GIF (variable code size up to 12 bits, LSB first, 255-byte sub-blocks).
    // Writes: the LZW minimum code size byte, the sub-blocks and the block terminator.
    public sealed class GifLzwEncoder
    {
        private const int       MaxCodeSize     = 12;
        private const int       MaxCode         = (1 << MaxCodeSize) - 1;     // 4095
        private const int       HashSize        = 8192;                       // > 4096 entries (open addressing)

        // dictionary: (prefix code << 8 | byte) -> code
        private readonly int[]  hashKeys_       = new int[HashSize];
        private readonly short[] hashCodes_     = new short[HashSize];

        // output
        private Stream?         stream_;
        private readonly byte[] block_          = new byte[255];
        private int             blockLength_;
        private int             bitBuffer_;
        private int             numOfBits_;

        // minCodeSize: bits per pixel index (2..8; GIF requires at least 2)
        public void Encode(Stream stream, byte[] indices, int count, int minCodeSize)
        {
            if (minCodeSize < 2 || minCodeSize > 8)
                throw new ArgumentOutOfRangeException(nameof(minCodeSize));

            stream_         = stream;
            blockLength_    = 0;
            bitBuffer_      = 0;
            numOfBits_      = 0;

            stream.WriteByte((byte)minCodeSize);

            int clearCode   = 1 << minCodeSize;
            int endCode     = clearCode + 1;
            int codeSize    = minCodeSize + 1;
            int nextCode    = clearCode + 2;

            ClearDictionary();
            WriteCode(clearCode, codeSize);

            if (count > 0)
            {
                int prefix = indices[0];
                for (int i=1; i<count; i++)
                {
                    int k   = indices[i];
                    int key = (prefix << 8) | k;
                    int h   = Find(key);
                    if (hashKeys_[h] == key)
                    {
                        prefix = hashCodes_[h];
                        continue;
                    }

                    WriteCode(prefix, codeSize);

                    if (nextCode <= MaxCode)
                    {
                        hashKeys_[h]    = key;
                        hashCodes_[h]   = (short)nextCode;
                        // the decoder adds its entries one code later: it switches to the next size when it adds
                        // the code (1 << codeSize) - 1, the encoder when it adds (1 << codeSize)
                        if (nextCode == (1 << codeSize) && codeSize < MaxCodeSize)
                            codeSize++;
                        nextCode++;
                    }
                    else
                    {
                        // the dictionary is full: start again
                        WriteCode(clearCode, codeSize);
                        ClearDictionary();
                        codeSize = minCodeSize + 1;
                        nextCode = clearCode + 2;
                    }

                    prefix = k;
                }
                WriteCode(prefix, codeSize);
            }

            WriteCode(endCode, codeSize);
            FlushBits();
            FlushBlock();
            stream.WriteByte(0);    // block terminator
            stream_ = null;
        }

        private void ClearDictionary()
        {
            Array.Fill(hashKeys_, -1);
        }

        // the slot of the key or the free slot where it belongs
        private int Find(int key)
        {
            int h = (int)(((uint)key * 2654435761u) >> 19) & (HashSize - 1);
            while (hashKeys_[h] != -1 && hashKeys_[h] != key)
                h = (h + 1) & (HashSize - 1);
            return h;
        }

        private void WriteCode(int code, int codeSize)
        {
            bitBuffer_ |= code << numOfBits_;
            numOfBits_ += codeSize;
            while (numOfBits_ >= 8)
            {
                WriteByte((byte)(bitBuffer_ & 0xFF));
                bitBuffer_ >>= 8;
                numOfBits_ -= 8;
            }
        }

        private void FlushBits()
        {
            if (numOfBits_ > 0)
                WriteByte((byte)(bitBuffer_ & 0xFF));
            bitBuffer_ = 0;
            numOfBits_ = 0;
        }

        private void WriteByte(byte b)
        {
            block_[blockLength_++] = b;
            if (blockLength_ == block_.Length)
                FlushBlock();
        }

        private void FlushBlock()
        {
            if (blockLength_ == 0)
                return;
            stream_!.WriteByte((byte)blockLength_);
            stream_.Write(block_, 0, blockLength_);
            blockLength_ = 0;
        }
    }
}
