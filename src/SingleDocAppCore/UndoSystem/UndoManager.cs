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
    //
    // Unsaved changes: the document is modified when the top of the undo stack differs from the one
    // remembered by MarkSavePoint() (so undo back to the saved state makes it unmodified again).
    // Edits without an undo action should call MarkModified().
    public class UndoManager
    {
        private Stack<UndoAction>        undoStack_     = new Stack<UndoAction>();
        private Stack<UndoAction>        redoStack_     = new Stack<UndoAction>();

        private UndoAction?             savePointAction_    = null;     // top of the undo stack when the document was saved
        private bool                    forceModified_      = false;    // change without an undo action

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

        // Also marks the document as unmodified (new/open document)
        public void ClearAll()
        {
            undoStack_.Clear();
            redoStack_.Clear();

            savePointAction_    = null;
            forceModified_      = false;
        }

        public bool CanUndo { get { return undoStack_.Count > 0; } }
        public bool CanRedo { get { return redoStack_.Count > 0; } }

        public bool IsModified
        {
            get
            {
                if (forceModified_)
                    return true;

                UndoAction? top = undoStack_.Count > 0 ? undoStack_.Peek() : null;
                return top != savePointAction_;
            }
        }

        // Call after the document was saved
        public void MarkSavePoint()
        {
            savePointAction_    = undoStack_.Count > 0 ? undoStack_.Peek() : null;
            forceModified_      = false;
        }

        // Call after a change of the document that is not recorded as an undo action
        public void MarkModified()
        {
            forceModified_      = true;
        }

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
