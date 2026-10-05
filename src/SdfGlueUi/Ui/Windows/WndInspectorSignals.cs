//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueUi.Ui.Components;
using SingleDocAppCore.Input;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppFramework.Ui.Properties;
using System.Globalization;
using System.Numerics;

namespace SdfGlueUi.Ui.Windows
{
    // Inspector: editor of a signal (source, operators, plot)
    public partial class WndInspector
    {
        private int     signalPlotWindowIndex_  = 1;    // SignalPlots.TimeWindows

        private void BuildEditorForSignal(ref int index, SignalInstance signal)
        {
            SignalsCollection signals = GetModel().Signals;

            ImGui.Separator();
            ImGui.Text(signal.Name.Val);

            BeginNodePropertyGrid(GetDefaultFirstColumnWidth());
            UiString.BuildReadonly  (ref index, "Id"        , signal.Id.ToString());
            UiString.Build          (ref index, "Name"      , signal.Name);
            UiBool.Build            (ref index, "Enabled"   , signal.Enabled);
            UiString.BuildReadonly  (ref index, "Value"     , signal.Enabled.Val ? signal.GetCurrentValue().ToString("0.####", CultureInfo.InvariantCulture) : "(disabled)");
            UiString.BuildReadonly  (ref index, "Used by"   , String.Format("{0} parameter(s)", GetModel().CountSignalUsers(signal.Id)));
            EndNodePropertyGrid();

            // plot
            ImGui.PushID(index++);
            ImGui.SetNextItemWidth(100.0f * Executor.GetWindowsScaling());
            ImGui.Combo("Time window", ref signalPlotWindowIndex_, SignalPlots.TimeWindowNames, SignalPlots.TimeWindowNames.Length);
            float seconds = SignalPlots.TimeWindows[Math.Clamp(signalPlotWindowIndex_, 0, SignalPlots.TimeWindows.Length - 1)];
            SignalPlots.BuildPlot("plot", new List<SignalInstance>() { signal }, signals, new Vector2(-1.0f, 140.0f * Executor.GetWindowsScaling()), seconds, signals.GetRealTime());
            ImGui.PopID();

            // source
            ImGui.PushID(index++);
            if (ImGui.CollapsingHeader("Source", ImGuiTreeNodeFlags.DefaultOpen))
            {
                BeginNodePropertyGrid(GetDefaultFirstColumnWidth());
                BuildSignalSourceType(ref index, signal);
                BuildSignalElementParameters(ref index, signal.Source, signal);

                if (signal.Source is SignalSrcInputChannel && Executor.GetInputSystem() == null)
                {
                    ImGui.NextColumn();
                    ImGui.TextDisabled("The input system is disabled (value = 0).");
                    ImGui.NextColumn();
                }
                EndNodePropertyGrid();
            }
            ImGui.PopID();

            // operators
            ImGui.PushID(index++);
            if (ImGui.CollapsingHeader("Operators", ImGuiTreeNodeFlags.DefaultOpen))
            {
                BeginNodePropertyGrid(GetDefaultFirstColumnWidth());
                BuildSignalOperators(ref index, signal);
                EndNodePropertyGrid();
            }
            ImGui.PopID();
        }

        private void BuildSignalSourceType(ref int index, SignalInstance signal)
        {
            ImGui.PushID(index++);
            ImGui.Text("Type");
            ImGui.NextColumn();
            if (ImGui.Button(SignalTypesRegistry.GetDisplayName(signal.Source), new Vector2(-1, 0)))
                ImGui.OpenPopup("menu_source_type");

            string? typeName = BuildSignalTypesPopup("menu_source_type", SignalTypesRegistry.Sources);
            if (typeName != null && typeName != signal.Source.TypeName)
            {
                SignalSource oldSource = signal.Source;
                SignalSource? newSource = SignalTypesRegistry.CreateSource(typeName);
                if (newSource != null)
                {
                    newSource.CopyCompatibleValuesFrom(oldSource);
                    signal.SetSource(newSource);

                    // undo/redo support
                    UndoManager.Instance.SaveAction(new ActionDelegates(
                        delegate { signal.SetSource(oldSource); },  // undo
                        delegate { signal.SetSource(newSource); }   // redo
                        ));
                }
            }
            ImGui.NextColumn();
            ImGui.PopID();
        }

        private static string? BuildSignalTypesPopup(string popupName, List<SignalTypeInfo> types)
        {
            string? selected = null;
            if (ImGui.BeginPopup(popupName))
            {
                foreach (SignalTypeInfo info in types)
                {
                    if (ImGui.MenuItem(info.DisplayName))
                        selected = info.TypeName;
                }
                ImGui.EndPopup();
            }
            return selected;
        }

        private void BuildSignalOperators(ref int index, SignalInstance signal)
        {
            List<SignalOperator> opList = signal.Operators;

            int     indexForDelete      = -1;
            int     indexForMoveUp      = -1;
            int     indexForMoveDown    = -1;
            int     indexForReplace     = -1;
            string? typeForReplace      = null;

            for (int i = 0; i < opList.Count; i++)
            {
                SignalOperator op = opList[i];

                ImGui.PushID(index++);

                // enabled + type (click: change the type)
                UiBool.BuildSimpleCheckBoxWithUndo(ref index, op.Enabled);
                ImGui.SameLine();
                if (ImGui.Button(SignalTypesRegistry.GetDisplayName(op), new Vector2(-1, 0)))
                    ImGui.OpenPopup("menu_op_type");
                string? typeName = BuildSignalTypesPopup("menu_op_type", SignalTypesRegistry.Operators);
                if (typeName != null && typeName != op.TypeName)
                {
                    indexForReplace = i;
                    typeForReplace  = typeName;
                }
                ImGui.NextColumn();

                // menu
                if (ImGui.Button("..."))
                    ImGui.OpenPopup("op_menu");
                if (ImGui.BeginPopup("op_menu"))
                {
                    if (ImGui.MenuItem("Move Up"))      { indexForMoveUp    = i; }
                    if (ImGui.MenuItem("Move Down"))    { indexForMoveDown  = i; }
                    if (ImGui.MenuItem("Delete"))       { indexForDelete    = i; }
                    ImGui.EndPopup();
                }
                ImGui.NextColumn();

                if (op.Enabled.Val)
                    BuildSignalElementParameters(ref index, op, signal);

                ImGui.PopID();
                ImGui.Separator();
            }

            // adding at the end of the list
            ImGui.PushID(index++);
            ImGui.NextColumn();
            if (ImGui.Button("Add operator"))
                ImGui.OpenPopup("menu_add_op");
            string? typeToAdd = BuildSignalTypesPopup("menu_add_op", SignalTypesRegistry.Operators);
            ImGui.NextColumn();
            ImGui.PopID();

            // The collection is modified outside the loop
            if (typeToAdd != null)
            {
                SignalOperator? newOp = SignalTypesRegistry.CreateOperator(typeToAdd);
                if (newOp != null)
                {
                    int insertIndex = opList.Count;
                    opList.Insert(insertIndex, newOp);
                    UndoManager.Instance.SaveAction(new ActionDelegates(
                        delegate { opList.RemoveAt(insertIndex); },         // undo
                        delegate { opList.Insert(insertIndex, newOp); }     // redo
                        ));
                }
            }
            if (indexForReplace != -1)
            {
                SignalOperator oldOp = opList[indexForReplace];
                SignalOperator? newOp = SignalTypesRegistry.CreateOperator(typeForReplace);
                if (newOp != null)
                {
                    int replaceIndex = indexForReplace;
                    newOp.CopyCompatibleValuesFrom(oldOp);
                    newOp.Enabled.Initialize(oldOp.Enabled.Val);
                    opList[replaceIndex] = newOp;
                    UndoManager.Instance.SaveAction(new ActionDelegates(
                        delegate { opList[replaceIndex] = oldOp; },         // undo
                        delegate { opList[replaceIndex] = newOp; }          // redo
                        ));
                }
            }
            if (indexForDelete != -1)
            {
                int deleteIndex = indexForDelete;
                SignalOperator opDeleted = opList[deleteIndex];
                opList.RemoveAt(deleteIndex);
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate { opList.Insert(deleteIndex, opDeleted); },    // undo
                    delegate { opList.RemoveAt(deleteIndex); }              // redo
                    ));
            }
            if (indexForMoveUp >= 1)
            {
                int i = indexForMoveUp;
                SwapOperators(opList, i - 1, i);
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate { SwapOperators(opList, i - 1, i); },          // undo
                    delegate { SwapOperators(opList, i - 1, i); }           // redo
                    ));
            }
            if (indexForMoveDown != -1 && indexForMoveDown < (opList.Count - 1))
            {
                int i = indexForMoveDown;
                SwapOperators(opList, i, i + 1);
                UndoManager.Instance.SaveAction(new ActionDelegates(
                    delegate { SwapOperators(opList, i, i + 1); },          // undo
                    delegate { SwapOperators(opList, i, i + 1); }           // redo
                    ));
            }
        }

        private static void SwapOperators(List<SignalOperator> opList, int i, int j)
        {
            (opList[i], opList[j]) = (opList[j], opList[i]);
        }

        private void BuildSignalElementParameters(ref int index, SignalElement element, SignalInstance owner)
        {
            foreach (SignalParamDef def in element.ParamDefs)
            {
                if (!element.Values.TryGetValue(def.Name, out ISimpleType? val))
                    continue;

                switch (def.Type)
                {
                    case SignalParamType.Float:
                        if (val is ExFloat exFloat)
                            UiFloat.Build(ref index, def.DisplayName, exFloat, def.Speed, def.LimitsType, def.MinVal, def.MaxVal, false);
                        break;

                    case SignalParamType.Int:
                        if (val is ExInt exInt)
                            UiInt.Build(ref index, def.DisplayName, exInt, def.Speed, def.LimitsType, (int)def.MinVal, (int)def.MaxVal);
                        break;

                    case SignalParamType.Bool:
                        if (val is ExInt exBool)
                            UiBool.Build(index++, def.DisplayName, exBool);
                        break;

                    case SignalParamType.Enum:
                        if (val is ExInt exEnum && def.EnumNames != null)
                            BuildEnumWithUndo(ref index, def.DisplayName, exEnum, def.EnumNames);
                        break;

                    case SignalParamType.SignalRef:
                        if (val is ExInt exRef)
                            BuildSignalRef(ref index, def.DisplayName, exRef, owner);
                        break;

                    case SignalParamType.InputChannelRef:
                        if (val is ExInt exChannel)
                            BuildInputChannelRef(ref index, def.DisplayName, exChannel);
                        break;
                }
            }
        }

        private static void BuildEnumWithUndo(ref int index, string name, ExInt obj, string[] names)
        {
            ImGui.PushID(index++);
            ImGui.Text(name);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            int val = obj.Val;
            if (ImGui.Combo("", ref val, names, names.Length))
            {
                obj.Val = val;
                UndoManager.Instance.SaveAction(new ActionInt(obj));
            }
            ImGui.NextColumn();
            ImGui.PopID();
        }

        // Combo with input channels (user data - SingleDocAppCore.Input), "(missing)" for an unknown id
        private void BuildInputChannelRef(ref int index, string name, ExInt obj)
        {
            InputSystem? inputs = Executor.GetInputSystem();
            InputChannel? current = inputs?.Channels.FindById(obj.Val);
            string label = (current != null) ? current.GetLabel() : String.Format("(missing #{0})", obj.Val);

            ImGui.PushID(index++);
            ImGui.Text(name);
            ImGui.NextColumn();
            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo("##channel", label))
            {
                if (inputs != null)
                {
                    foreach(InputChannel channel in inputs.Channels.Channels)
                    {
                        if (ImGui.Selectable(channel.GetLabel(), channel.Id == obj.Val) && channel.Id != obj.Val)
                        {
                            obj.Val = channel.Id;
                            UndoManager.Instance.SaveAction(new ActionInt(obj));
                        }
                    }
                }
                ImGui.EndCombo();
            }
            if (current != null && ImGui.IsItemHovered())
                ImGui.SetTooltip("Input channels are edited in the \"Inputs\" window");
            ImGui.NextColumn();
            ImGui.PopID();
        }

        private void BuildSignalRef(ref int index, string name, ExInt obj, SignalInstance owner)
        {
            SignalsCollection signals = GetModel().Signals;

            ImGui.PushID(index++);
            ImGui.Text(name);
            ImGui.NextColumn();
            if (ImGui.Button(MenuSignals.GetSignalLabel(signals, obj.Val), new Vector2(-1, 0)))
                ImGui.OpenPopup("menu_signal_ref");
            if (MenuSignals.BuildPopup("menu_signal_ref", signals, obj.Val, out int newId, owner.Id))
            {
                obj.Val = newId;
                UndoManager.Instance.SaveAction(new ActionInt(obj));
            }
            ImGui.NextColumn();
            ImGui.PopID();
        }
    }
}
