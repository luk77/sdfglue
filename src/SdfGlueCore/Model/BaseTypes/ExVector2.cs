//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Numerics;

namespace SdfGlueCore.Model.BaseTypes
{
    public class ExVector2 : ExSimpleType<Vector2>
    {
        public ExVector2() { }
        public ExVector2(Vector2 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector2(Val); }
        public override string FormatAsStringForUniform()
        {
            return String.Format("vec2({0}, {1})"
                                    , Val.X.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.Y.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture));
        }
    }
}
