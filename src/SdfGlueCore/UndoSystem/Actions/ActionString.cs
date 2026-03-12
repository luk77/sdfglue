//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.UndoSystem.Actions
{
    public class ActionString : UndoAction 
    {
        private ExString            obj_;
        private string?             oldVal_;
        private string?             newVal_;
        private DataModel.OnValueChanged?       onValueChanged_     = null;

        public ActionString(ExString obj, DataModel.OnValueChanged? onValueChanged = null)
        {
            obj_    = obj;
            oldVal_ = obj.PrevVal;
            newVal_ = obj.Val;
            onValueChanged_ = onValueChanged;

            obj.ResetPrevVal();
        }

        internal override void ApplyUndo()
        {
            obj_.Val        = oldVal_;
            obj_.PrevVal    = oldVal_;

            if (onValueChanged_ != null)
                onValueChanged_();
        }

        internal override void ApplyRedo()
        {
            obj_.Val        = newVal_;
            obj_.PrevVal    = newVal_;

            if (onValueChanged_ != null)
                onValueChanged_();
        }
    }
}
