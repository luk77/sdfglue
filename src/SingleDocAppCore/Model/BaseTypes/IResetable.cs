//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Model.BaseTypes
{
    // Implemented by all classes that have to call ResetPrevVal()
    // because of the undo/redo system (PrevVal is the value before the current edit).
    public interface IResetable
    {
        void ResetPrevVal();
    }
}
