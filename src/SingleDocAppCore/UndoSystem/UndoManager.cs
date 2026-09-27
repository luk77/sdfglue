//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.UndoSystem
{
    // Global undo/redo stacks (singleton).
    // Usage: after a value is edited (e.g. ImGui.IsItemDeactivatedAfterEdit()) the UI saves an UndoAction
    // (ActionSimpleType<T>, ActionString or ActionDelegates). Actions based on Ex* types use PrevVal
    // as the "old" value, so PrevVal must be reset (IResetable.ResetPrevVal()) after loading a document.
    // Call ClearAll() after new/open document - actions refer to objects of the previous document.
    public class UndoManager
    {
        private Stack<UndoAction>        undoStack_     = new Stack<UndoAction>();
        private Stack<UndoAction>        redoStack_     = new Stack<UndoAction>();

        private static UndoManager      instance_ = new UndoManager();

        public static UndoManager Instance
        {
            get
            {
                //if (instance_ == null)
                //    instance_ = new UndoManager();

                return instance_;
            }
        }

        public void ClearAll()
        {
            undoStack_.Clear();
            redoStack_.Clear();
        }

        public bool CanUndo { get { return undoStack_.Count > 0; } }
        public bool CanRedo { get { return redoStack_.Count > 0; } }

        public void DoUndo()
        {
            if (undoStack_.Count == 0)
                return;

            UndoAction act = undoStack_.Pop();
            redoStack_.Push(act);
            act.ApplyUndo();
        }

        public void DoRedo()
        {
            if (redoStack_.Count == 0)
                return;

            UndoAction act = redoStack_.Pop();
            undoStack_.Push(act);
            act.ApplyRedo();
        }

        public void SaveAction(UndoAction action)
        {
            undoStack_.Push(action);
            redoStack_.Clear();
        }

    }
}
