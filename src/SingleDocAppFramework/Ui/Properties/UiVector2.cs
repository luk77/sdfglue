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
    public class UiVector2
    {
        // onEditFinished - called when an edit is finished (drag released, Enter, Tab, ...) and after undo/redo
        public static void AddUndoHandler(string name, ExVector2 obj, OnValueChanged? onEditFinished = null)
        {
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionVector2(obj, onEditFinished), onEditFinished);
        }

        public static void Build(ref int id, string name, ExVector2 obj, float speed, OnValueChanged? onEditFinished = null)
        {
            Build(ref id, name, ref obj.Val, speed);
            AddUndoHandler(name, obj, onEditFinished);
        }

        public static void Build(ref int id, string name, ref Vector2 val, float speed)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.DragFloat2("##value", ref val, speed);
            ImGui.PopID();
            ImGui.NextColumn();
        }
    }
}
