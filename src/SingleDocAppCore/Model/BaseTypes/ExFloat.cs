//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Model.BaseTypes
{
    public abstract class ExFloat : ExSimpleType<float>
    {
        public ExFloat() { }
        public ExFloat(float val) : base(val) {}
        //public override ISimpleType Copy() { return new ExFloat(Val); }
    }
}
