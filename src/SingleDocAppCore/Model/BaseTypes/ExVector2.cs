//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Numerics;

namespace SingleDocAppCore.Model.BaseTypes
{
    public class ExVector2 : ExSimpleType<Vector2>
    {
        public ExVector2() { }
        public ExVector2(Vector2 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector2(Val); }
    }
}
