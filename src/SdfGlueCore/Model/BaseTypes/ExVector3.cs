//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Numerics;

namespace SdfGlueCore.Model.BaseTypes
{
    public class ExVector3 : ExSimpleType<Vector3>
    {
        public ExVector3() : base(new Vector3()) {}
        public ExVector3(Vector3 val) : base(val) {}
        public override ISimpleType Copy() { return new ExVector3(Val); }
        public override string FormatAsStringForUniform()
        {
            return String.Format("vec3({0}, {1}, {2})"
                                    , Val.X.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.Y.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture)
                                    , Val.Z.ToString(ExFloat.FloatFormat, CultureInfo.InvariantCulture));
        }
    }
}
