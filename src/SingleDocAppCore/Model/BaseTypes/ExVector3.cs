//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Numerics;

namespace SingleDocAppCore.Model.BaseTypes
{
    public class ExVector3 : ExSimpleType<Vector3>
    {
        public ExVector3() : base(new Vector3()) {}
        public ExVector3(Vector3 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector3(Val); }
    }
}
