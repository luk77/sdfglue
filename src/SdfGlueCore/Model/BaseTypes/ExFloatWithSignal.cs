//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;

namespace SdfGlueCore.Model.BaseTypes
{
    // Float parameter which can be driven by a signal (see SignalBinding).
    // The signal is referenced by Id (0 = none), so a deleted signal only leaves an unresolved binding.
    public  class ExFloatWithSignal : ExFloat
    {
        public int SignalId = 0;

        public override ISimpleType Copy()
        {
            ExFloatWithSignal copy = new ExFloatWithSignal(Val);
            copy.SignalId = SignalId;
            return copy;
        }

        public ExFloatWithSignal() {}
        public ExFloatWithSignal(float val) : base(val) {}
    }
}
