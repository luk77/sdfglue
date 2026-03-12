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
    public class UiColor3
    {
        public static void Build(ref int id, string name, ExVector3 obj)
        {
            Build(ref id, name, ref obj.Val);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionVector3(obj));
            }
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
