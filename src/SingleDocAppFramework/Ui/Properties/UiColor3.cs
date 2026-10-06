//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using System.Numerics;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiColor3
    {
        // onEditFinished - called when an edit is finished (drag released, Enter, color picker closed, ...) and after undo/redo
        public static void Build(ref int id, string name, ExVector3 obj, OnValueChanged? onEditFinished = null)
        {
            Build(ref id, name, ref obj.Val);
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionVector3(obj, onEditFinished), onEditFinished);
        }

        public static void Build(ref int id, string name, ref Vector3 val)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.ColorEdit3("##2f", ref val, ImGuiColorEditFlags.Float);
            ImGui.PopID();
            ImGui.NextColumn();
        }
    }
}
