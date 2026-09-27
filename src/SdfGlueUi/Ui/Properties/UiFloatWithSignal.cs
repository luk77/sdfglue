//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using SingleDocAppFramework.Ui.Properties;

namespace SdfGlueUi.Ui.Properties
{
    public class UiFloatWithSignal : UiFloat
    {

        public static void Build(ref int id, string name, ExFloat obj, float speed, LimitsType limitsType, float minVal, float maxVal, bool editInDegrees, SignalsCollection? signals = null)
        {
            float valF = obj.Val;
            if (editInDegrees)
                valF *= GMath.RadToDeg;

            //Build(id, name, ref valF, speed, limitsType, minVal, maxVal);

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            SignalInstance? signalRef = null;
            if (SdfGlueCore.Model.DataModel.UseSignals)
            {
                ExFloatWithSignal? objWithSignal = obj as ExFloatWithSignal;
                if (objWithSignal != null && signals != null)
                {
                    SignalInstance? oldInst = objWithSignal.SignalRef;

                    ImGui.PushID(id++);
                    if (ImGui.Button("*"))
                    {
                        ImGui.OpenPopup("menu_" + name);
                    }
                    ImGui.PopID();

                    objWithSignal.SignalRef = Components.MenuSignals.BuildPopup("menu_" + name, signals, objWithSignal.SignalRef);

                    signalRef = objWithSignal.SignalRef;

                    ImGui.SameLine();
                }
            }

            ImGui.SetNextItemWidth(-1);

            if (signalRef != null)
            {
                ImGui.PushID(id++);
                ImGui.Text(signalRef.Name.Val);
                ImGui.PopID();
            }
            else
            {
                ImGui.PushID(id++);
                switch(limitsType)
                {
                    case LimitsType.None:       ImGui.DragFloat("##value", ref valF, speed);                     break;
                    case LimitsType.Min:        ImGui.DragFloat("##value", ref valF, speed, minVal);             break;
                    case LimitsType.MinMax:     ImGui.DragFloat("##value", ref valF, speed, minVal, maxVal);     break;
                }
                ImGui.PopID();
            }
            ImGui.NextColumn();

            if (editInDegrees)
                valF *= GMath.DegToRad;

            obj.Val = valF;

            AddUndoHandler(name, obj);
        }

    }
}
