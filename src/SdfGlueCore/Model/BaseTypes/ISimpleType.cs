//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    public interface ISimpleType : IResetable
    {
        ISimpleType Copy();
        Type GetValueType();
        string FormatAsStringForUniform();
    }
}
