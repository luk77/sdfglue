//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueUi.Ui.Components;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using SingleDocAppFramework.Ui.Properties;
using System.Globalization;
using System.Numerics;

namespace SdfGlueUi.Ui.Properties
{
    // Vector (vec2/vec3/vec4) editor with optional bindings of every channel (X, Y, Z, W) to signals
    // (only with DataModel.UseSignals, otherwise it is the plain UiVector2/3/4 / UiColor3 editor).
    //
    // Main row:  [>] Name   [~] [ X | Y | Z ]        - "~" opens a menu with a submenu per channel,
    //                                                  a bound channel shows the current value (read only)
    // Sub-rows (expanded, open automatically if a channel is bound):
    //                X      [~] [ value ] or the signal (sparkline, name, value) - like UiFloatWithSignal
    //
    // The whole parameter uses one id from the id counter, so expanding it does not change ids of the next rows.
    public static class UiVectorWithSignal
    {
        private static readonly string[]    ColorChannelNames       = { "R", "G", "B", "A" };

        // onBindingChanged - called after a binding is changed (also on undo/redo), e.g. to rebuild the shader
        public static void Build(ref int id, string name, ExVector2 obj, float speed, bool editInDegrees, SignalsCollection? signals = null, OnValueChanged? onBindingChanged = null)
        {
            if (!DataModel.UseSignals || signals == null || obj is not ExVector2WithSignal objWithSignal)
            {
                Vector2 valV = obj.Val;
                if (editInDegrees)
                    valV *= GMath.RadToDeg;
                UiVector2.Build(ref id, name, ref valV, speed);
                if (editInDegrees)
                    valV *= GMath.DegToRad;
                obj.Val = valV;

                UiVector2.AddUndoHandler(name, obj);
                return;
            }

            float[] values = { obj.Val.X, obj.Val.Y };
            BuildRows(ref id, name, objWithSignal, values, speed, editInDegrees, false, signals, onBindingChanged,
                delegate
                {
                    obj.Val = new Vector2(values[0], values[1]);
                    UiVector2.AddUndoHandler(name, obj);
                });
        }

        public static void Build(ref int id, string name, ExVector3 obj, float speed, bool editInDegrees, bool isColor, SignalsCollection? signals = null, OnValueChanged? onBindingChanged = null)
        {
            if (!DataModel.UseSignals || signals == null || obj is not ExVector3WithSignal objWithSignal)
            {
                Vector3 valV = obj.Val;
                if (isColor)
                    UiColor3.Build(ref id, name, ref valV);
                else
                {
                    if (editInDegrees)
                        valV *= GMath.RadToDeg;
                    UiVector3.Build(ref id, name, ref valV, speed);
                    if (editInDegrees)
                        valV *= GMath.DegToRad;
                }
                obj.Val = valV;

                UiVector3.AddUndoHandler(name, obj);
                return;
            }

            float[] values = { obj.Val.X, obj.Val.Y, obj.Val.Z };
            BuildRows(ref id, name, objWithSignal, values, speed, editInDegrees, isColor, signals, onBindingChanged,
                delegate
                {
                    obj.Val = new Vector3(values[0], values[1], values[2]);
                    UiVector3.AddUndoHandler(name, obj);
                });
        }

        public static void Build(ref int id, string name, ExVector4 obj, float speed, bool editInDegrees, SignalsCollection? signals = null, OnValueChanged? onBindingChanged = null)
        {
            if (!DataModel.UseSignals || signals == null || obj is not ExVector4WithSignal objWithSignal)
            {
                Vector4 valV = obj.Val;
                if (editInDegrees)
                    valV *= GMath.RadToDeg;
                UiVector4.Build(ref id, name, ref valV, speed);
                if (editInDegrees)
                    valV *= GMath.DegToRad;
                obj.Val = valV;

                UiVector4.AddUndoHandler(name, obj);
                return;
            }

            float[] values = { obj.Val.X, obj.Val.Y, obj.Val.Z, obj.Val.W };
            BuildRows(ref id, name, objWithSignal, values, speed, editInDegrees, false, signals, onBindingChanged,
                delegate
                {
                    obj.Val = new Vector4(values[0], values[1], values[2], values[3]);
                    UiVector4.AddUndoHandler(name, obj);
                });
        }

        // values        - static values of the channels (in radians if editInDegrees), modified by the editors
        // onValueEdited - writes the values back to the object and handles undo; called right after every value widget
        private static void BuildRows(ref int id, string name, ISignalBindable obj, float[] values, float speed, bool editInDegrees, bool isColor,
                                      SignalsCollection signals, OnValueChanged? onBindingChanged, Action onValueEdited)
        {
            string[] channelNames = isColor ? ColorChannelNames : SignalBinding.ChannelNames;
            bool     anyBound     = SignalBinding.HasBinding(obj);
            int      localId      = 0;

            ImGui.PushID(id++);

            // main row - name with the expand arrow
            if (anyBound)
                ImGui.SetNextItemOpen(true, ImGuiCond.Once);
            bool isOpen = ImGui.TreeNodeEx(name, ImGuiTreeNodeFlags.NoTreePushOnOpen);
            uint treeNodeId = ImGui.GetItemID();
            ImGui.NextColumn();

            // main row - bindings menu and values
            if (BuildChannelsMenuButton(ref localId, obj, channelNames, signals, onBindingChanged))
            {
                // a channel was bound - show its signal in the sub-rows
                ImGui.GetStateStorage().SetBool(treeNodeId, true);
                isOpen  = true;
                anyBound = SignalBinding.HasBinding(obj);
            }

            if (isColor && !anyBound)
            {
                Vector3 color = new Vector3(values[0], values[1], values[2]);
                ImGui.SetNextItemWidth(-1);
                ImGui.PushID(localId++);
                if (ImGui.ColorEdit3("##2f", ref color, ImGuiColorEditFlags.Float))
                {
                    values[0] = color.X;
                    values[1] = color.Y;
                    values[2] = color.Z;
                }
                ImGui.PopID();
                onValueEdited();
            }
            else
            {
                BuildChannelsInline(ref localId, obj, channelNames, values, speed, editInDegrees, signals, onValueEdited);
            }
            ImGui.NextColumn();

            // sub-rows - one per channel
            if (isOpen)
            {
                for(int ch=0; ch<obj.ChannelCount; ch++)
                {
                    ImGui.PushID(localId++);
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.GetTreeNodeToLabelSpacing());
                    ImGui.Text(channelNames[ch]);
                    ImGui.PopID();
                    ImGui.NextColumn();

                    UiFloatWithSignal.BuildBindingButton(ref localId, obj, ch, signals, onBindingChanged);
                    ImGui.SetNextItemWidth(-1);

                    int signalId = obj.GetSignalId(ch);
                    if (signalId != 0)
                    {
                        UiFloatWithSignal.BuildBoundSignalInfo(ref localId, signalId, signals, values[ch], editInDegrees);
                    }
                    else
                    {
                        float valF = editInDegrees ? values[ch] * GMath.RadToDeg : values[ch];
                        ImGui.PushID(localId++);
                        if (ImGui.DragFloat("##value", ref valF, speed))
                            values[ch] = editInDegrees ? valF * GMath.DegToRad : valF;
                        ImGui.PopID();
                        onValueEdited();
                    }
                    ImGui.NextColumn();
                }
            }

            ImGui.PopID();
        }

        // Compact editors of all channels in one line; a bound channel shows its current value (read only)
        private static void BuildChannelsInline(ref int localId, ISignalBindable obj, string[] channelNames, float[] values, float speed, bool editInDegrees,
                                                SignalsCollection signals, Action onValueEdited)
        {
            int   count   = obj.ChannelCount;
            float spacing = ImGui.GetStyle().ItemInnerSpacing.X;
            float width   = Math.Max(1.0f, (ImGui.GetContentRegionAvail().X - spacing * (count - 1)) / count);

            for(int ch=0; ch<count; ch++)
            {
                if (ch > 0)
                    ImGui.SameLine(0.0f, spacing);
                ImGui.SetNextItemWidth(width);
                ImGui.PushID(localId++);

                int signalId = obj.GetSignalId(ch);
                if (signalId != 0)
                {
                    float valF = SignalBinding.GetChannelValue(obj, ch, values[ch], signals);
                    if (editInDegrees)
                        valF *= GMath.RadToDeg;

                    ImGui.BeginDisabled();
                    ImGui.DragFloat("##value", ref valF, 0.0f, 0.0f, 0.0f, "~%.3f");
                    ImGui.EndDisabled();
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        ImGui.SetTooltip(String.Format(CultureInfo.InvariantCulture, "{0}: driven by signal: {1}", channelNames[ch], MenuSignals.GetSignalLabel(signals, signalId)));
                }
                else
                {
                    float valF = editInDegrees ? values[ch] * GMath.RadToDeg : values[ch];
                    if (ImGui.DragFloat("##value", ref valF, speed))
                        values[ch] = editInDegrees ? valF * GMath.DegToRad : valF;
                    onValueEdited();
                }

                ImGui.PopID();
            }
        }

        // "~" button with a submenu of signals per channel (followed by SameLine). Returns true if a channel was bound.
        private static bool BuildChannelsMenuButton(ref int localId, ISignalBindable obj, string[] channelNames, SignalsCollection signals, OnValueChanged? onBindingChanged)
        {
            bool anyBound = SignalBinding.HasBinding(obj);
            bool bound    = false;

            ImGui.PushID(localId++);
            if (anyBound)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            if (ImGui.Button("~"))
                ImGui.OpenPopup("menu_channels");
            if (anyBound)
                ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Drive the channels by signals");

            if (ImGui.BeginPopup("menu_channels"))
            {
                for(int ch=0; ch<obj.ChannelCount; ch++)
                {
                    int signalId = obj.GetSignalId(ch);

                    ImGui.PushID(ch);
                    if (ImGui.BeginMenu(String.Format("{0}: {1}", channelNames[ch], MenuSignals.GetSignalLabel(signals, signalId))))
                    {
                        if (MenuSignals.Build(signals, signalId, out int newSignalId))
                        {
                            UiFloatWithSignal.SetBinding(obj, ch, newSignalId, onBindingChanged);
                            if (newSignalId != 0)
                                bound = true;
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.PopID();
                }
                ImGui.EndPopup();
            }
            ImGui.PopID();

            ImGui.SameLine();

            return bound;
        }
    }
}
