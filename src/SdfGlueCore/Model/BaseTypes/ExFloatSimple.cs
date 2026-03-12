//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    public class ExFloatSimple : ExFloat
    {
        public override ISimpleType Copy()
        {
            ExFloatSimple copy = new ExFloatSimple(Val);
            return copy;
        }

        public ExFloatSimple() {}
        public ExFloatSimple(float val) : base(val) {}
    }
}
