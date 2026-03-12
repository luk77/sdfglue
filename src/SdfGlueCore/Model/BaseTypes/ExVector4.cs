//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Numerics;

namespace SdfGlueCore.Model.BaseTypes
{
    public class ExVector4 : ExSimpleType<Vector4>
    {
        public ExVector4() { }
        public ExVector4(Vector4 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector4(Val); }
        public override string FormatAsStringForUniform()
        {
            return String.Format("vec4({0}, {1}, {2}, {3})"
                                    , Val.X.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.Y.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.Z.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.W.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture));
        }
    }
}
