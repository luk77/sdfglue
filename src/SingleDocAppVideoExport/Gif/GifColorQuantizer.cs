//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppVideoExport.Gif
{
    // The palette of one GIF frame (up to 256 colors) and the mapping of colors to it.
    //   - an image with few colors (<= maxColors) gets exactly its colors (lossless, IsExact)
    //   - otherwise median cut on a 5-5-5 histogram; the colors of the boxes are the averages of the real colors
    // Mapping: the nearest palette color, cached in a 6-6-6 lookup table (filled lazily, reset per palette).
    // Colors are 0xRRGGBB ints; negative values are skipped (transparent pixels).
    public sealed class GifColorQuantizer
    {
        public  const int                   MaxColors       = 256;

        private const int                   HistBits        = 5;
        private const int                   HistSize        = 1 << (3 * HistBits);
        private const int                   LutBits         = 6;
        private const int                   LutSize         = 1 << (3 * LutBits);

        private readonly byte[]             palette_        = new byte[MaxColors * 3];     // R, G, B
        private int                         count_;
        private bool                        isExact_;
        private readonly Dictionary<int, int> exactMap_     = new Dictionary<int, int>();

        // histogram
        private readonly int[]              histCount_      = new int[HistSize];
        private readonly long[]             histR_          = new long[HistSize];
        private readonly long[]             histG_          = new long[HistSize];
        private readonly long[]             histB_          = new long[HistSize];
        private readonly int[]              bins_           = new int[HistSize];           // non-empty bins, reordered by the cut
        private readonly int[]              sortKeys_       = new int[HistSize];

        private readonly int[]              lut_            = new int[LutSize];

        public int      Count           { get { return count_;   } }
        public bool     IsExact         { get { return isExact_; } }

        public byte GetR(int index) { return palette_[index * 3 + 0]; }
        public byte GetG(int index) { return palette_[index * 3 + 1]; }
        public byte GetB(int index) { return palette_[index * 3 + 2]; }

        // Builds the palette of colors[0..length) (negative entries are skipped). Returns the number of colors (>= 1).
        public int Build(int[] colors, int length, int maxColors)
        {
            maxColors = Math.Clamp(maxColors, 1, MaxColors);

            Array.Fill(lut_, -1);
            if (!TryBuildExact(colors, length, maxColors))
                BuildMedianCut(colors, length, maxColors);

            return count_;
        }

        // The palette index of a color (0xRRGGBB)
        public int Map(int rgb)
        {
            if (isExact_ && exactMap_.TryGetValue(rgb, out int exactIndex))
                return exactIndex;
            return Map((rgb >> 16) & 0xFF, (rgb >> 8) & 0xFF, rgb & 0xFF);
        }

        // The nearest palette color (components 0..255)
        public int Map(int r, int g, int b)
        {
            int key = ((r >> (8 - LutBits)) << (2 * LutBits)) | ((g >> (8 - LutBits)) << LutBits) | (b >> (8 - LutBits));
            int index = lut_[key];
            if (index < 0)
            {
                // the center of the cell
                int half = 1 << (7 - LutBits);
                index = FindNearest(((r >> (8 - LutBits)) << (8 - LutBits)) + half, ((g >> (8 - LutBits)) << (8 - LutBits)) + half,
                                    ((b >> (8 - LutBits)) << (8 - LutBits)) + half);
                lut_[key] = index;
            }
            return index;
        }

        // The average distance of the palette colors to their nearest neighbours (per component, 0..255) -
        // the strength of the ordered dithering
        public float GetAverageColorSpacing()
        {
            if (count_ < 2)
                return 0.0f;

            double sum = 0.0;
            for (int i=0; i<count_; i++)
            {
                int best = int.MaxValue;
                for (int j=0; j<count_; j++)
                {
                    if (i == j)
                        continue;
                    int dr = palette_[i * 3 + 0] - palette_[j * 3 + 0];
                    int dg = palette_[i * 3 + 1] - palette_[j * 3 + 1];
                    int db = palette_[i * 3 + 2] - palette_[j * 3 + 2];
                    best = Math.Min(best, dr * dr + dg * dg + db * db);
                }
                sum += Math.Sqrt(best / 3.0);
            }
            return (float)(sum / count_);
        }

        private int FindNearest(int r, int g, int b)
        {
            int best        = 0;
            int bestDist    = int.MaxValue;
            for (int i=0; i<count_; i++)
            {
                int dr = palette_[i * 3 + 0] - r;
                int dg = palette_[i * 3 + 1] - g;
                int db = palette_[i * 3 + 2] - b;
                // weights close to the sensitivity of the eye (green > red > blue)
                int dist = 3 * dr * dr + 4 * dg * dg + 2 * db * db;
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = i;
                }
            }
            return best;
        }

        private bool TryBuildExact(int[] colors, int length, int maxColors)
        {
            exactMap_.Clear();
            for (int i=0; i<length; i++)
            {
                int c = colors[i];
                if (c < 0 || exactMap_.ContainsKey(c))
                    continue;
                if (exactMap_.Count >= maxColors)
                {
                    exactMap_.Clear();
                    isExact_ = false;
                    return false;
                }
                exactMap_.Add(c, exactMap_.Count);
            }

            count_ = Math.Max(1, exactMap_.Count);
            if (exactMap_.Count == 0)
            {
                palette_[0] = palette_[1] = palette_[2] = 0;
            }
            foreach (KeyValuePair<int, int> kv in exactMap_)
            {
                palette_[kv.Value * 3 + 0] = (byte)((kv.Key >> 16) & 0xFF);
                palette_[kv.Value * 3 + 1] = (byte)((kv.Key >>  8) & 0xFF);
                palette_[kv.Value * 3 + 2] = (byte)( kv.Key        & 0xFF);
            }
            isExact_ = true;
            return true;
        }

        private void BuildMedianCut(int[] colors, int length, int maxColors)
        {
            Array.Clear(histCount_);
            Array.Clear(histR_);
            Array.Clear(histG_);
            Array.Clear(histB_);

            for (int i=0; i<length; i++)
            {
                int c = colors[i];
                if (c < 0)
                    continue;
                int r = (c >> 16) & 0xFF;
                int g = (c >>  8) & 0xFF;
                int b =  c        & 0xFF;
                int bin = ((r >> 3) << 10) | ((g >> 3) << 5) | (b >> 3);
                histCount_[bin]++;
                histR_[bin] += r;
                histG_[bin] += g;
                histB_[bin] += b;
            }

            int numOfBins = 0;
            for (int bin=0; bin<HistSize; bin++)
            {
                if (histCount_[bin] > 0)
                    bins_[numOfBins++] = bin;
            }

            // boxes: ranges of bins_
            List<(int Start, int End)> boxes = new List<(int Start, int End)> { (0, numOfBins) };
            while (boxes.Count < maxColors)
            {
                int     bestBox     = -1;
                long    bestScore   = 0;
                int     bestAxis    = 0;
                for (int i=0; i<boxes.Count; i++)
                {
                    (int start, int end) = boxes[i];
                    if (end - start < 2)
                        continue;
                    GetBoxStats(start, end, out long count, out int axis, out int range);
                    long score = count * range;
                    if (range > 0 && score > bestScore)
                    {
                        bestScore   = score;
                        bestBox     = i;
                        bestAxis    = axis;
                    }
                }
                if (bestBox < 0)
                    break;

                (int s, int e) = boxes[bestBox];
                int split = SplitBox(s, e, bestAxis);
                boxes[bestBox] = (s, split);
                boxes.Add((split, e));
            }

            count_ = boxes.Count;
            for (int i=0; i<boxes.Count; i++)
            {
                long n = 0, sr = 0, sg = 0, sb = 0;
                for (int k=boxes[i].Start; k<boxes[i].End; k++)
                {
                    int bin = bins_[k];
                    n  += histCount_[bin];
                    sr += histR_[bin];
                    sg += histG_[bin];
                    sb += histB_[bin];
                }
                n = Math.Max(1, n);
                palette_[i * 3 + 0] = (byte)((sr + n / 2) / n);
                palette_[i * 3 + 1] = (byte)((sg + n / 2) / n);
                palette_[i * 3 + 2] = (byte)((sb + n / 2) / n);
            }
            isExact_ = false;
        }

        private static int Component(int bin, int axis)
        {
            return (bin >> (10 - 5 * axis)) & 0x1F;     // axis 0: R, 1: G, 2: B
        }

        private void GetBoxStats(int start, int end, out long count, out int axis, out int range)
        {
            int minR = 31, minG = 31, minB = 31, maxR = 0, maxG = 0, maxB = 0;
            count = 0;
            for (int k=start; k<end; k++)
            {
                int bin = bins_[k];
                int r = Component(bin, 0), g = Component(bin, 1), b = Component(bin, 2);
                minR = Math.Min(minR, r); maxR = Math.Max(maxR, r);
                minG = Math.Min(minG, g); maxG = Math.Max(maxG, g);
                minB = Math.Min(minB, b); maxB = Math.Max(maxB, b);
                count += histCount_[bin];
            }
            int rangeR = maxR - minR, rangeG = maxG - minG, rangeB = maxB - minB;
            axis  = (rangeG >= rangeR && rangeG >= rangeB) ? 1 : (rangeR >= rangeB ? 0 : 2);
            range = Math.Max(rangeR, Math.Max(rangeG, rangeB));
        }

        // Sorts the bins of the box along the axis and returns the index of the split (by the median of the pixel count)
        private int SplitBox(int start, int end, int axis)
        {
            int length = end - start;
            for (int k=start; k<end; k++)
                sortKeys_[k] = Component(bins_[k], axis);
            Array.Sort(sortKeys_, bins_, start, length);

            long total = 0;
            for (int k=start; k<end; k++)
                total += histCount_[bins_[k]];

            long half = total / 2;
            long sum  = 0;
            int split = start + 1;
            for (int k=start; k<end - 1; k++)
            {
                sum += histCount_[bins_[k]];
                split = k + 1;
                if (sum >= half)
                    break;
            }

            // do not split between bins with the same value on the axis (when possible)
            int key = sortKeys_[split - 1];
            int moved = split;
            while (moved < end && sortKeys_[moved] == key)
                moved++;
            if (moved < end)
                return moved;
            moved = split;
            while (moved > start + 1 && sortKeys_[moved - 1] == sortKeys_[moved])
                moved--;
            return moved;
        }
    }
}
