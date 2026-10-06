//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Model;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiInt
    {
        // onEditFinished - called when an edit is finished (drag released, Enter, Tab, ...) and after undo/redo
        public static void AddUndoHandler(string name, ExInt obj, OnValueChanged? onEditFinished = null)
        {
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionInt(obj, onEditFinished), onEditFinished);
        }

        public static void Build(ref int id, string name, ExInt obj, float speed, LimitsType limitsType, int minVal, int maxVal, OnValueChanged? onEditFinished = null)
        {
            int valI = obj.Val;

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);

            ImGui.PushID(id++);
            switch(limitsType)
            {
                case LimitsType.None:       ImGui.DragInt("##value", ref valI, speed);                   break;
                case LimitsType.Min:        ImGui.DragInt("##value", ref valI, speed, minVal);           break;
                case LimitsType.MinMax:     ImGui.DragInt("##value", ref valI, speed, minVal, maxVal);   break;
            }
            ImGui.PopID();

            ImGui.NextColumn();

            obj.Val = valI;

            AddUndoHandler(name, obj, onEditFinished);
        }

        public static void Build(ref int id, string name, ExInt obj, float speed)
        {
            Build(ref id, name, ref obj.Val, speed);
            AddUndoHandler(name, obj);
        }

        public static void Build(ref int id, string name, ref int val, float speed)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.DragInt("##value", ref val, speed);
            ImGui.PopID();
            ImGui.NextColumn();
        }

        public static void Build(ref int id, string name, ref int val, float speed, int min, int max)
        {
            ImGui.PushID(id);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            ImGui.DragInt("##value", ref val, speed, min, max);
            ImGui.PopID();
            ImGui.NextColumn();
        }
    }
}
