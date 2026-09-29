//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using System.Numerics;

namespace SingleDocAppFramework.Ui.Properties
{
    public class UiVector3
    {
        public static void AddUndoHandler(string name, ExVector3 obj)
        {
            UndoEditTracker.HandleLastItem(() => !obj.PrevVal.Equals(obj.Val), () => new ActionVector3(obj));
        }

        public static bool Build(ref int id, string name, ExVector3 obj, float speed)
        {
            bool ret = Build(ref id, name, ref obj.Val, speed);
            AddUndoHandler(name, obj);
            return ret;
        }

        public static bool Build(ref int id, string name, ref Vector3 val, float speed)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            bool ret = ImGui.DragFloat3("##value", ref val, speed);
            ImGui.PopID();
            ImGui.NextColumn();
            return ret;
        }
    }
}
