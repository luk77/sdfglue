//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;

namespace SdfGlueUi.Ui.Properties
{
    public class UiBool
    {
        public static void Build(ref int id, string name, ExBool obj, DataModel.OnValueChanged onValueChanged = null)
        {
            Build(ref id, name, ref obj.Val, onValueChanged);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionBool(obj, onValueChanged));
            }
        }

        public static void Build(ref int id, string name, ref bool val, DataModel.OnValueChanged onValueChanged = null)
        {
            bool oldVal = val;
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.Checkbox("", ref val);
            ImGui.PopID();
            ImGui.NextColumn();

            if (oldVal != val)
            {
                if (onValueChanged != null)
                    onValueChanged();
            }
        }

        // Checkbox który działa na zmiennej typu int
        public static void Build(int id, string name, ExInt obj, DataModel.OnValueChanged onValueChanged = null)
        {
            Build(id, name, ref obj.Val, onValueChanged);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionInt(obj, onValueChanged));
            }
        }

        // Checkbox który działa na zmiennej typu int
        public static void Build(int id, string name, ref int val, DataModel.OnValueChanged onValueChanged = null)
        {
            bool curVal = val > 0;
            bool oldVal = val > 0;
            ImGui.PushID(id);
            ImGui.Text(name);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.Checkbox("", ref curVal);
            ImGui.NextColumn();

            val = curVal ? 1 : 0;

            if (oldVal != curVal)
            {
                if (onValueChanged != null)
                    onValueChanged();
            }
            ImGui.PopID();
        }

        public static void BuildSimpleCheckBoxWithUndo(ref int id, ExBool obj, DataModel.OnValueChanged onValueChanged = null)
        {
            bool oldVal = obj.Val;
            ImGui.PushID(id++);
            ImGui.Checkbox("", ref obj.Val);
            ImGui.PopID();
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: SimpleCheckBox: {0}", obj.Val);
                UndoManager.Instance.SaveAction(new ActionBool(obj, onValueChanged));
            }
            if (oldVal != obj.Val)
            {
                if (onValueChanged != null)
                    onValueChanged();
            }
        }
    }
}
