//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.UndoSystem.Actions
{
    public class ActionDelegates : UndoAction 
    {
        private CommonUndoRedoDelegate      onUndo_;
        private CommonUndoRedoDelegate      onRedo_;

        public ActionDelegates(CommonUndoRedoDelegate onUndo, CommonUndoRedoDelegate onRedo)
        {
            onUndo_ = onUndo;
            onRedo_ = onRedo;
        }

        internal override void ApplyUndo()
        {
            if (onUndo_ != null)
                onUndo_();
        }

        internal override void ApplyRedo()
        {
            if (onRedo_ != null)
                onRedo_();
        }
    }
}
