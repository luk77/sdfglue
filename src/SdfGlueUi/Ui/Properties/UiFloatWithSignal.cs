//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueUi.Ui.Components;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppCore.Utils;
using SingleDocAppFramework.Ui.Properties;
using System.Globalization;
using System.Numerics;

namespace SdfGlueUi.Ui.Properties
{
    // Float editor with an optional binding to a signal (only with DataModel.UseSignals).
    // A bound parameter shows the signal (name, sparkline, current value) instead of the value editor.
    public class UiFloatWithSignal : UiFloat
    {
        // onBindingChanged - called after the binding is changed (also on undo/redo), e.g. to rebuild the shader
        public static void Build(ref int id, string name, ExFloat obj, float speed, LimitsType limitsType, float minVal, float maxVal, bool editInDegrees, SignalsCollection? signals = null, OnValueChanged? onBindingChanged = null)
        {
            float valF = obj.Val;
            if (editInDegrees)
                valF *= GMath.RadToDeg;

            //Build(id, name, ref valF, speed, limitsType, minVal, maxVal);

            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ExFloatWithSignal? objWithSignal = null;
            if (SdfGlueCore.Model.DataModel.UseSignals && signals != null)
            {
                objWithSignal = obj as ExFloatWithSignal;
                if (objWithSignal != null)
                    BuildBindingButton(ref id, name, objWithSignal, signals, onBindingChanged);
            }

            ImGui.SetNextItemWidth(-1);

            if (objWithSignal != null && objWithSignal.SignalId != 0 && signals != null)
            {
                BuildBoundSignalInfo(ref id, objWithSignal, signals, editInDegrees);
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

                if (editInDegrees)
                    valF *= GMath.DegToRad;

                obj.Val = valF;

                AddUndoHandler(name, obj);
            }
            ImGui.NextColumn();
        }

        private static void BuildBindingButton(ref int id, string name, ExFloatWithSignal obj, SignalsCollection signals, OnValueChanged? onBindingChanged)
        {
            bool isBound = obj.SignalId != 0;

            ImGui.PushID(id++);
            if (isBound)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            if (ImGui.Button("~"))
                ImGui.OpenPopup("menu_signal");
            if (isBound)
                ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Drive the value by a signal");

            if (MenuSignals.BuildPopup("menu_signal", signals, obj.SignalId, out int newSignalId))
                SetBinding(obj, newSignalId, onBindingChanged);
            ImGui.PopID();

            ImGui.SameLine();
        }

        private static void SetBinding(ExFloatWithSignal obj, int newSignalId, OnValueChanged? onBindingChanged)
        {
            int oldSignalId = obj.SignalId;
            if (oldSignalId == newSignalId)
                return;

            obj.SignalId = newSignalId;
            onBindingChanged?.Invoke();

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    obj.SignalId = oldSignalId;
                    onBindingChanged?.Invoke();
                },
                delegate
                {
                    // redo
                    obj.SignalId = newSignalId;
                    onBindingChanged?.Invoke();
                }
                ));
        }

        private static void BuildBoundSignalInfo(ref int id, ExFloatWithSignal obj, SignalsCollection signals, bool editInDegrees)
        {
            SignalInstance? signal = signals.FindById(obj.SignalId);
            string label = MenuSignals.GetSignalLabel(signals, obj.SignalId);

            ImGui.PushID(id++);
            if (signal == null)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), label);
            }
            else
            {
                float height = ImGui.GetFrameHeight();
                SignalPlots.BuildSparkline("sparkline", signal, signals, new Vector2(height * 2.5f, height));
                ImGui.SameLine();

                if (signal.Enabled.Val)
                    ImGui.Text(String.Format(CultureInfo.InvariantCulture, "{0} = {1:0.###}", label, signal.GetCurrentValue()));
                else
                    ImGui.TextDisabled(String.Format("{0} (disabled)", label));
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(String.Format(CultureInfo.InvariantCulture,
                    "Driven by signal: {0}\nStatic value: {1:0.###}{2}\n(used when the signal is disabled or missing, and in exported code)",
                    label, editInDegrees ? obj.Val * GMath.RadToDeg : obj.Val, editInDegrees ? " deg (the signal value is in radians)" : ""));
            }
            ImGui.PopID();
        }
    }
}
