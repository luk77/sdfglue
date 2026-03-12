//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using System.Numerics;

namespace SdfGlueUi.Ui.Properties
{
    public class UiVector2
    {
        public static void AddUndoHandler(string name, ExVector2 obj)
        {
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionVector2(obj));
            }
        }

        public static void Build(ref int id, string name, ExVector2 obj, float speed)
        {
            Build(ref id, name, ref obj.Val, speed);
            AddUndoHandler(name, obj);
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
