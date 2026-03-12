//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;

namespace SdfGlueUi.Ui.Properties
{
    public class UiString
    {
        public static void AddUndoHandler(string name, ExString obj)
        {
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionString(obj));
            }
        }

        public static void Build(ref int id, string name, ExString obj)
        {
            Build(ref id, name, ref obj.Val);
            AddUndoHandler(name, obj);
        }

        public static void Build(ref int id, string name, ref string val)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.InputText("##edit", ref val, 64);
            ImGui.PopID();
            ImGui.NextColumn();
        }

        public static void BuildReadonly(ref int id, string name, string val)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.Text(val);
            ImGui.PopID();
            ImGui.NextColumn();
        }
    }
}
