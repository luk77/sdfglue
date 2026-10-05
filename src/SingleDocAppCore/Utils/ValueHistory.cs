//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Utils
{
    // Ring buffer with recent (time, value) samples, e.g. for plots. Runtime only - not serialized.
    public class ValueHistory
    {
        public const int        DefaultCapacity     = 4096;     // ~68 s at 60 updates per second

        private float[]         times_;
        private float[]         values_;
        private int             start_              = 0;
        private int             count_              = 0;

        public ValueHistory(int capacity = DefaultCapacity)
        {
            times_  = new float[capacity];
            values_ = new float[capacity];
        }

        public int Count        { get { return count_; } }
        public int Capacity     { get { return times_.Length; } }

        public void Clear()
        {
            start_ = 0;
            count_ = 0;
        }

        public void Add(float time, float value)
        {
            int index = (start_ + count_) % times_.Length;
            times_[index]   = time;
            values_[index]  = value;

            if (count_ < times_.Length)
                count_++;
            else
                start_ = (start_ + 1) % times_.Length;
        }

        // i = 0 is the oldest sample
        public float GetTime(int i)     { return times_ [(start_ + i) % times_.Length]; }
        public float GetValue(int i)    { return values_[(start_ + i) % times_.Length]; }

        public float GetLastTime()      { return count_ > 0 ? GetTime(count_ - 1) : 0.0f; }

        // Index of the first sample with time >= minTime
        public int FindFirstIndex(float minTime)
        {
            int lo = 0;
            int hi = count_;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (GetTime(mid) < minTime)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }
    }
}
