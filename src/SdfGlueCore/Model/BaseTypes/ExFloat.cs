//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;

namespace SdfGlueCore.Model.BaseTypes
{
    public abstract class ExFloat : ExSimpleType<float>
    {
        //public static readonly string      FloatFormat             = "G";
        public static readonly string      FloatFormat             = "0.00000";

        public ExFloat() { }
        public ExFloat(float val) : base(val) {}
        //public override ISimpleType Copy() { return new ExFloat(Val); }
        public override string FormatAsStringForUniform() { return Val.ToString(FloatFormat, CultureInfo.InvariantCulture); }
    }
}
