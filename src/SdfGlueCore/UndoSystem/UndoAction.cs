//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.UndoSystem
{
    public abstract class UndoAction
    {
        public delegate void CommonUndoRedoDelegate();

        internal abstract void ApplyUndo();
        internal abstract void ApplyRedo();
    }
}
