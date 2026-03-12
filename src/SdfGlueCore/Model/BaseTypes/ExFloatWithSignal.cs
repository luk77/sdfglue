//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes.Signals;

namespace SdfGlueCore.Model.BaseTypes
{
    public  class ExFloatWithSignal : ExFloat
    {
        public SignalInstance? SignalRef;

        public override ISimpleType Copy()
        {
            ExFloatWithSignal copy = new ExFloatWithSignal(Val);
            copy.SignalRef = SignalRef;
            return copy;
        }

        public ExFloatWithSignal() {}
        public ExFloatWithSignal(float val) : base(val) {}
    }
}
