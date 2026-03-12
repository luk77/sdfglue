//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    public class ExString : ISimpleType
    {
        public string? PrevVal;
        public string? Val;

        public ExString(string? val)
        {
            Val         = val;
            PrevVal     = val;
        }

        public override string? ToString()
        {
            return Val;
        }

        public ISimpleType Copy()
        {
            ExString newObj = new ExString(Val);
            newObj.PrevVal = PrevVal;
            return newObj;
        }

        public Type GetValueType()
        {
            if (Val == null)
            {
                Console.WriteLine("WARNING: GetValueType() Val = null");
                // to jest dzikie... tylko po to żeby nie było "non-nullable" warninga...
                return typeof(ISimpleType);
            }

            return Val.GetType();
        }

        public object? GetValueAsObject()
        {
            return Val;
        }

        public void ResetPrevVal()
        {
            PrevVal = Val;
        }

        public string FormatAsStringForUniform()
        {
            return "Unsupported type: string: " + Val;
        }

    }
}
