//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiString
    {
        public static void AddUndoHandler(string name, ExString obj)
        {
            UndoEditTracker.HandleLastItem(() => obj.PrevVal != obj.Val, () => new ActionString(obj));
        }

        public static void Build(ref int id, string name, ExString obj)
        {
            // ImGui.InputText does not accept null; keep null until the user types something
            string val = obj.Val ?? "";
            Build(ref id, name, ref val);
            if (obj.Val != null || val.Length > 0)
                obj.Val = val;
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
