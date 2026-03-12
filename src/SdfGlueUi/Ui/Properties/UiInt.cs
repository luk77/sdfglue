//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using SdfGlueCore.Utils;
using System.Xml.Linq;

namespace SdfGlueUi.Ui.Properties
{
    public class UiInt
    {
        public static void AddUndoHandler(string name, ExInt obj)
        {
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionInt(obj));
            }
        }

        public static void Build(ref int id, string name, ExInt obj, FunctionDefParameter param)
        {
            int valI = obj.Val;

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);

            ImGui.PushID(id++);
            switch(param.LimitsType)
            {
                case SdfParamLimitsType.None:       ImGui.DragInt("##value", ref valI, param.ValSpeed);                                         break;
                case SdfParamLimitsType.Min:        ImGui.DragInt("##value", ref valI, param.ValSpeed, (int)param.MinVal);                      break;
                case SdfParamLimitsType.MinMax:     ImGui.DragInt("##value", ref valI, param.ValSpeed, (int)param.MinVal, (int)param.MaxVal);   break;
            }
            ImGui.PopID();

            ImGui.NextColumn();

            obj.Val = valI;

            AddUndoHandler(name, obj);
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
