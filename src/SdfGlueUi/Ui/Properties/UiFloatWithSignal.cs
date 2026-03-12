/* NOT USED
//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using System;

namespace SdfGlueUi.Ui.Properties
{
    public class UiFloatWithSignal
    {
        public static void AddUndoHandler(string name, ExFloatWithSignal obj)
        {
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                //Console.WriteLine("Undo save: {0}: {1}", name, obj.Val);
                UndoManager.Instance.SaveAction(new ActionFloat(obj));
            }
        }

        public static void Build(ref int id, string name, SignalsCollection signals, ExFloatWithSignal obj, float speed)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();
            if (SdfGlueCore.Model.DataModel.UseSignals)
            {
                SignalInstance oldInst = obj.SignalRef;

                ImGui.PushID(id++);
                if (ImGui.Button("*"))
                {
                    ImGui.OpenPopup("menu_" + name);
                }
                ImGui.PopID();

                obj.SignalRef = Components.MenuSignals.BuildPopup("menu_" + name, signals, obj.SignalRef);

                ImGui.SameLine();
            }
            ImGui.SetNextItemWidth(-1);
            if (obj.SignalRef != null)
            {
                ImGui.PushID(id++);
                ImGui.Text(obj.SignalRef.Name.Val);
                ImGui.PopID();
            }
            else
            {
                ImGui.PushID(id++);
                ImGui.DragFloat("##value", ref obj.Val, speed);
                ImGui.PopID();
            }
            ImGui.NextColumn();

            AddUndoHandler(name, obj);
        }

    }
}
*/