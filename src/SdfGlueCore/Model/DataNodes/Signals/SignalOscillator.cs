//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalOscillator : SignalInstance
    {
        public ExFloatSimple Amplitude  = new ExFloatSimple(1.0f);
        public ExFloatSimple Frequency  = new ExFloatSimple(1.0f);
        public ExFloatSimple OffsetX    = new ExFloatSimple(0.0f);
        public ExFloatSimple OffsetY    = new ExFloatSimple(0.0f);

        private double time_ = 0.0f;

        public override void Update(double deltaTime)
        {
            time_ += deltaTime;
            value_ = OffsetY.Val + Amplitude.Val * (float)Math.Sin(Frequency.Val * Math.PI * (time_ + OffsetX.Val));
        }
    }
}
