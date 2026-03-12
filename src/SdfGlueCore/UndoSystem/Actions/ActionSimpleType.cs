//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using System.Numerics;

namespace SdfGlueCore.UndoSystem.Actions
{
    public abstract class ActionSimpleType<T> : UndoAction where T : struct //allow non-nullable value types only
    {
        private ExSimpleType<T>                 obj_;
        private T                               oldVal_;
        private T                               newVal_;
        private DataModel.OnValueChanged?       onValueChanged_     = null;

        public ActionSimpleType(ExSimpleType<T> obj, DataModel.OnValueChanged? onValueChanged)
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

    public class ActionBool     : ActionSimpleType<bool>    { public ActionBool     (ExBool  obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }
    public class ActionInt      : ActionSimpleType<int>     { public ActionInt      (ExInt   obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }
    public class ActionFloat    : ActionSimpleType<float>   { public ActionFloat    (ExFloat obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }
    public class ActionVector2  : ActionSimpleType<Vector2> { public ActionVector2  (ExVector2 obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }
    public class ActionVector3  : ActionSimpleType<Vector3> { public ActionVector3  (ExVector3 obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }
    public class ActionVector4  : ActionSimpleType<Vector4> { public ActionVector4  (ExVector4 obj, DataModel.OnValueChanged? onValueChanged = null) : base(obj, onValueChanged) {} }

}
