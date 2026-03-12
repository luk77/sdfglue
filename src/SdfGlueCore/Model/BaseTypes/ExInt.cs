//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;

namespace SdfGlueCore.Model.BaseTypes
{
    public class ExInt : ExSimpleType<int>
    {
        public ExInt() { }
        public ExInt  (int val) : base(val) {} 
        public override ISimpleType Copy() { return new ExInt(Val); }
        public override string FormatAsStringForUniform() { return Val.ToString(CultureInfo.InvariantCulture); }
    }
}
