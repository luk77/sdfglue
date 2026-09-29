//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.UndoSystem;

namespace SingleDocAppFramework.Ui.Properties
{
    // Saves the undo action of a value edited with an ImGui widget (call HandleLastItem right after the widget).
    // Normally the action is saved when the widget is deactivated after an edit (IsItemDeactivatedAfterEdit).
    // If the widget disappears while it is being edited (e.g. another node is selected while typing a name),
    // ImGui never reports its deactivation - then EndFrame() saves the action, so the edit is undoable
    // and the document is marked as modified.
    public static class UndoEditTracker
    {
        private static uint                     pendingItemId_          = 0;
        private static Func<bool>?              pendingIsChanged_       = null;
        private static Func<UndoAction>?        pendingCreateAction_    = null;
        private static bool                     pendingSubmitted_       = false;    // the pending widget was submitted in this frame

        // isChanged    - true if the value differs from the one stored for undo (PrevVal)
        // createAction - creates the undo action (and resets PrevVal)
        public static void HandleLastItem(Func<bool> isChanged, Func<UndoAction> createAction)
        {
            uint itemId = ImGui.GetItemID();

            if (ImGui.IsItemActivated())
            {
                pendingItemId_          = itemId;
                pendingIsChanged_       = isChanged;
                pendingCreateAction_    = createAction;
            }

            bool isPendingItem = pendingCreateAction_ != null && itemId == pendingItemId_;
            if (isPendingItem)
                pendingSubmitted_ = true;

            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                UndoManager.Instance.SaveAction(createAction());
                if (isPendingItem)
                    ClearPending();
            }
            else if (isPendingItem && ImGui.IsItemDeactivated())
            {
                ClearPending();
            }
        }

        // Call once per frame, after all windows are submitted
        public static void EndFrame()
        {
            if (pendingCreateAction_ == null)
                return;

            if (!pendingSubmitted_)
            {
                // the edited widget was not submitted in this frame - its deactivation will never be reported
                if (pendingIsChanged_ != null && pendingIsChanged_())
                    UndoManager.Instance.SaveAction(pendingCreateAction_());
                ClearPending();
                return;
            }

            pendingSubmitted_ = false;
        }

        private static void ClearPending()
        {
            pendingItemId_          = 0;
            pendingIsChanged_       = null;
            pendingCreateAction_    = null;
            pendingSubmitted_       = false;
        }
    }
}
