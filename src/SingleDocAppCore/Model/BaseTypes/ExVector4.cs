//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Numerics;

namespace SingleDocAppCore.Model.BaseTypes
{
    public class ExVector4 : ExSimpleType<Vector4>
    {
        public ExVector4() { }
        public ExVector4(Vector4 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector4(Val); }
    }
}
