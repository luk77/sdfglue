//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    // Tym interfejsem są otagowane wszystkie klasy które muszą wołać ResetPrevVal()
    // w związku z systemem undo/redo.
    public interface IResetable
    {
        void ResetPrevVal();
    }
}
