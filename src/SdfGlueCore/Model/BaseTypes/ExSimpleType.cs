//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    public abstract class ExSimpleType<T> : ISimpleType where T : struct //allow non-nullable value types only
    {
        public T PrevVal;
        public T Val;

        public ExSimpleType()
        {
        }

        public ExSimpleType(T val)
        {
            Initialize(val);
        }

        public void Initialize(T val)
        {
            Val         = val;
            PrevVal     = val;
        }

        public abstract string FormatAsStringForUniform();

        //public ISimpleType Copy()
        //{
        //    ExSimpleType<T> newObj = new ExSimpleType<T>(Val);
        //    newObj.PrevVal = PrevVal;
        //    return newObj;
        //}

        public abstract ISimpleType Copy();

        public Type GetValueType()
        {
            return Val.GetType();
        }

        public object GetValueAsObject()
        {
            return Val;
        }

        public void ResetPrevVal()
        {
            PrevVal = Val;
        }
    }
}
