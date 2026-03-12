//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    public class ExBool : ExSimpleType<bool>
    {
        public ExBool() { }
        public ExBool (bool val) : base(val) {} 
        public override ISimpleType Copy() { return new ExBool(Val); }
        public override string FormatAsStringForUniform()
        {
            return "Unsupported type: bool: " + Val;
        }
    }
}
