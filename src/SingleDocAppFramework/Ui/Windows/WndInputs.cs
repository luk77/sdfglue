//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Input;
using SingleDocAppCore.Model;
using SingleDocAppCore.Utils;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Ui.Components;
using SingleDocAppFramework.Ui.Properties;
using System.Globalization;
using System.Numerics;
using System.Xml;

namespace SingleDocAppFramework.Ui.Windows
{
    // Editor of input channels (SdAppWindow.Inputs): channels, their bindings, "Learn" and device status.
    // Not registered by the framework - an application with the input system enabled registers it
    // (optionally a derived class, e.g. to show where a channel is used: GetChannelUsageInfo()).
    // Changes are saved automatically (InputSystem.NotifyChanged()).
    public class WndInputs : UiWindowBase
    {
        // Title is a key in layout files - do not change it
        public override string Title => "Inputs";

        private const float     PlotSeconds     = 5.0f;

        private static readonly string[]    PadNames        = CreatePadNames();
        private static readonly string[]    ButtonNames     = Enum.GetNames<GamepadButtonId>();
        private static readonly string[]    AxisNames       = Enum.GetNames<GamepadAxisId>();
        private static readonly string[]    RangeNames      = { "0..1", "-1..1" };

        private static string[] CreatePadNames()
        {
            string[] names = new string[17];
            names[0] = "Any";
            for (int i = 1; i < names.Length; i++)
                names[i] = i.ToString(CultureInfo.InvariantCulture);
            return names;
        }

        // Text shown in a channel (e.g. "Used by 2 signals"), null - nothing
        protected virtual string? GetChannelUsageInfo(InputChannel channel)
        {
            return null;
        }

        public override void Build()
        {
            BuildWindow(delegate()
            {
                InputSystem? inputs = Executor.GetInputSystem();
                if (inputs == null)
                {
                    ImGui.TextDisabled("The input system is disabled.");
                    return;
                }

                BuildToolbar(inputs);
                BuildDevicesStatus(inputs);
                BuildLearnStatus(inputs);

                ImGui.Separator();

                InputChannel? channelToDelete = null;
                foreach(InputChannel channel in inputs.Channels.Channels)
                {
                    if (BuildChannel(inputs, channel))
                        channelToDelete = channel;
                }

                // the collection is modified outside the loop
                if (channelToDelete != null)
                    inputs.RemoveChannel(channelToDelete);
            });
        }

        private void BuildToolbar(InputSystem inputs)
        {
            if (ImGui.Button("Add channel"))
            {
                inputs.Channels.AddChannel();
                inputs.NotifyChanged();
            }
            ImGui.SameLine();
            if (ImGui.Button("Rescan devices"))
                inputs.RescanDevices();
            ImGui.SameLine();
            if (ImGui.Button("Restore defaults"))
                ImGui.OpenPopup("confirm_restore");

            if (ImGui.BeginPopup("confirm_restore"))
            {
                ImGui.Text("Replace all input channels with the default ones?");
                if (ImGui.Button("Restore defaults"))
                {
                    inputs.RestoreDefaults();
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }
        }

        private static void BuildDevicesStatus(InputSystem inputs)
        {
            foreach(IInputDevice device in inputs.Devices)
                ImGui.TextDisabled(String.Format("{0}: {1}", device.Name, device.GetStatus()));
        }

        private static void BuildLearnStatus(InputSystem inputs)
        {
            if (inputs.IsLearning())
            {
                ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.2f, 1.0f), String.Format(CultureInfo.InvariantCulture,
                    "Learn: press a key, a gamepad button, move a stick or a MIDI control... (Esc - cancel, {0:0}s)", inputs.GetLearnTimeLeft()));
            }
            else if (inputs.LastLearnMessage != null)
            {
                ImGui.TextDisabled(inputs.LastLearnMessage);
            }
        }

        // Returns true if the channel should be deleted
        private bool BuildChannel(InputSystem inputs, InputChannel channel)
        {
            bool delete = false;

            ImGui.PushID(channel.Id);

            float scaling           = Executor.GetWindowsScaling();
            float rightPartWidth    = 300.0f * scaling;
            float rightPartX        = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X - rightPartWidth;

            bool open = ImGui.TreeNodeEx(String.Format("{0}###channel", channel.GetLabel()), ImGuiTreeNodeFlags.AllowOverlap);

            // value, sparkline, learn, delete (right side of the header)
            float height = ImGui.GetFrameHeight();
            ImGui.SameLine();
            ImGui.SetCursorPosX(Math.Max(ImGui.GetCursorPosX(), rightPartX));
            BuildSparkline(inputs, channel, new Vector2(80.0f * scaling, height));
            ImGui.SameLine();
            ImGui.Text(channel.Value.ToString("0.00", CultureInfo.InvariantCulture).PadLeft(5));
            ImGui.SameLine();
            BuildLearnButton(inputs, channel, -1, "Learn...");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Add a binding by pressing a key / button or moving a control");
            ImGui.SameLine();
            if (ImGui.Button("X"))
                ImGui.OpenPopup("confirm_delete");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Delete the channel");
            if (ImGui.BeginPopup("confirm_delete"))
            {
                ImGui.Text(String.Format("Delete the channel \"{0}\"?", channel.Name));
                if (ImGui.Button("Delete"))
                {
                    delete = true;
                    ImGui.CloseCurrentPopup();
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel"))
                    ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
            }

            if (open)
            {
                BuildChannelContent(inputs, channel);
                ImGui.TreePop();
            }

            ImGui.PopID();
            return delete;
        }

        private void BuildChannelContent(InputSystem inputs, InputChannel channel)
        {
            int index = 1;

            BeginPropertyGrid(GetDefaultFirstColumnWidth());
            UiString.BuildReadonly(ref index, "Id", channel.Id.ToString(CultureInfo.InvariantCulture));
            string name = channel.Name;
            UiString.Build(ref index, "Name", ref name);
            if (name != channel.Name)
            {
                channel.Name = name;
                inputs.NotifyChanged();
            }
            string? usage = GetChannelUsageInfo(channel);
            if (usage != null)
                UiString.BuildReadonly(ref index, "Used by", usage);
            EndPropertyGrid();

            int bindingToDelete = -1;
            for (int i = 0; i < channel.Bindings.Count; i++)
            {
                if (BuildBinding(inputs, channel, i, ref index))
                    bindingToDelete = i;
            }
            if (bindingToDelete >= 0)
            {
                if (inputs.IsLearning(channel, bindingToDelete))
                    inputs.CancelLearn();
                channel.Bindings.RemoveAt(bindingToDelete);
                inputs.NotifyChanged();
            }

            if (ImGui.Button("Add binding"))
                ImGui.OpenPopup("menu_add_binding");
            if (ImGui.BeginPopup("menu_add_binding"))
            {
                foreach((string typeName, string displayName) in InputBindingFactory.Types)
                {
                    if (ImGui.MenuItem(displayName))
                    {
                        InputBinding? binding = InputBindingFactory.Create(typeName);
                        if (binding != null)
                        {
                            channel.Bindings.Add(binding);
                            inputs.NotifyChanged();
                        }
                    }
                }
                ImGui.EndPopup();
            }
        }

        // Returns true if the binding should be deleted
        private bool BuildBinding(InputSystem inputs, InputChannel channel, int bindingIndex, ref int index)
        {
            InputBinding binding = channel.Bindings[bindingIndex];
            bool delete = false;

            ImGui.PushID(bindingIndex + 1000);

            ImGui.Separator();
            ImGui.Text(binding.GetDisplayName());
            ImGui.SameLine();
            ImGui.TextDisabled(String.Format(CultureInfo.InvariantCulture, "= {0:0.00}", binding.Evaluate(inputs.State)));
            ImGui.SameLine();
            BuildLearnButton(inputs, channel, bindingIndex, "Learn");
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Replace the control by pressing a key / button or moving a control");
            ImGui.SameLine();
            if (ImGui.Button("X"))
                delete = true;
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Delete the binding");

            // the binding is compared before/after editing - any change is saved
            string before = GetBindingXml(binding);

            BeginPropertyGrid(GetDefaultFirstColumnWidth());
            switch(binding)
            {
                case InputBindingKey key:                       BuildKeyBinding(inputs, key, ref index);            break;
                case InputBindingGamepadButton button:          BuildGamepadButtonBinding(button, ref index);       break;
                case InputBindingGamepadAxis axis:              BuildGamepadAxisBinding(axis, ref index);           break;
                case InputBindingMidiController controller:     BuildMidiControllerBinding(inputs, controller, ref index); break;
                case InputBindingMidiNote note:                 BuildMidiNoteBinding(inputs, note, ref index);      break;
            }
            EndPropertyGrid();

            if (GetBindingXml(binding) != before)
                inputs.NotifyChanged();

            ImGui.PopID();
            return delete;
        }

        private static string GetBindingXml(InputBinding binding)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement root = xmlDoc.CreateElement("Root");
            xmlDoc.AppendChild(root);
            binding.Serialize(xmlDoc, root);
            return root.InnerXml;
        }

        private static void BuildLearnButton(InputSystem inputs, InputChannel channel, int bindingIndex, string label)
        {
            bool learning = inputs.IsLearning(channel, bindingIndex);
            if (learning)
                ImGui.PushStyleColor(ImGuiCol.Button, ImGui.GetStyle().Colors[(int)ImGuiCol.ButtonActive]);
            if (ImGui.Button(learning ? "Cancel" : label))
            {
                if (learning)
                    inputs.CancelLearn();
                else
                    inputs.StartLearn(channel, bindingIndex);
            }
            if (learning)
                ImGui.PopStyleColor();
        }

        private static void BuildSparkline(InputSystem inputs, InputChannel channel, Vector2 size)
        {
            ValueHistory history = channel.History;
            UiPlotSeries series = new UiPlotSeries(channel.Name, UiLinePlot.GetDefaultColor(channel.Id - 1), history.Count, history.GetTime, history.GetValue);
            float xMax = inputs.GetRealTime();
            UiLinePlot.BuildSparkline("sparkline", size, series, xMax - PlotSeconds, xMax);
        }

        //-------------------------------------------------------------------
        // Editors of binding types
        //-------------------------------------------------------------------

        private static void BuildKeyBinding(InputSystem inputs, InputBindingKey binding, ref int index)
        {
            string[] keyNames = KeyboardInputDevice.GetKeyNames().Where(k => !inputs.ReservedKeys.Contains(k)).ToArray();
            int keyIndex = Array.IndexOf(keyNames, binding.Key);
            if (keyIndex < 0)
            {
                // unknown or reserved key (e.g. a hand-edited file) - shown, but can't be selected again
                keyNames = keyNames.Append(binding.Key).ToArray();
                keyIndex = keyNames.Length - 1;
            }
            UiComboBox.Build(ref index, "Key", keyNames, ref keyIndex);
            binding.Key = keyNames[keyIndex];

            if (inputs.ReservedKeys.Contains(binding.Key))
            {
                ImGui.NextColumn();
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.2f, 1.0f), "The key is reserved by the application");
                ImGui.NextColumn();
            }
        }

        private static void BuildGamepadButtonBinding(InputBindingGamepadButton binding, ref int index)
        {
            BuildPad(ref index, ref binding.Pad);
            int button = (int)binding.Button;
            UiComboBox.Build(ref index, "Button", ButtonNames, ref button);
            binding.Button = (GamepadButtonId)button;
        }

        private static void BuildGamepadAxisBinding(InputBindingGamepadAxis binding, ref int index)
        {
            BuildPad(ref index, ref binding.Pad);
            int axis = (int)binding.Axis;
            UiComboBox.Build(ref index, "Axis", AxisNames, ref axis);
            binding.Axis = (GamepadAxisId)axis;
            UiFloat.Build   (ref index, "Deadzone"      , ref binding.Deadzone      , 0.005f, LimitsType.MinMax, 0.0f, 0.95f);
            UiFloat.Build   (ref index, "Sensitivity"   , ref binding.Sensitivity   , 0.01f , LimitsType.MinMax, 0.0f, 10.0f);
            UiBool.Build    (ref index, "Invert"        , ref binding.Invert);
            BuildRange(ref index, ref binding.Range);
        }

        private static void BuildMidiControllerBinding(InputSystem inputs, InputBindingMidiController binding, ref int index)
        {
            BuildMidiDevice(inputs, ref index, ref binding.Device);
            UiInt.Build     (ref index, "Channel"           , ref binding.Channel   , 0.1f, 1, 16);
            UiInt.Build     (ref index, "Controller (CC)"   , ref binding.Controller, 0.2f, 0, 127);
            UiBool.Build    (ref index, "Invert"            , ref binding.Invert);
            BuildRange(ref index, ref binding.Range);
        }

        private static void BuildMidiNoteBinding(InputSystem inputs, InputBindingMidiNote binding, ref int index)
        {
            BuildMidiDevice(inputs, ref index, ref binding.Device);
            UiInt.Build     (ref index, "Channel"           , ref binding.Channel   , 0.1f, 1, 16);
            UiInt.Build     (ref index, String.Format("Note ({0})", InputBindingFactory.GetNoteName(binding.Note)), ref binding.Note, 0.2f, 0, 127);
            UiBool.Build    (ref index, "Use velocity"      , ref binding.UseVelocity);
        }

        private static void BuildPad(ref int index, ref int pad)
        {
            int padIndex = Math.Clamp(pad + 1, 0, PadNames.Length - 1);
            UiComboBox.Build(ref index, "Gamepad", PadNames, ref padIndex);
            pad = padIndex - 1;
        }

        private static void BuildRange(ref int index, ref InputValueRange range)
        {
            int rangeIndex = (int)range;
            UiComboBox.Build(ref index, "Range", RangeNames, ref rangeIndex);
            range = (InputValueRange)rangeIndex;
        }

        // "Any" + connected devices (+ the current one if it is not connected)
        private static void BuildMidiDevice(InputSystem inputs, ref int index, ref string device)
        {
            List<string> names = new List<string>() { "Any" };
            foreach(MidiDeviceSnapshot midi in inputs.State.MidiDevices)
                names.Add(midi.Name);
            if (device.Length > 0 && !names.Contains(device))
                names.Add(device);

            int deviceIndex = (device.Length > 0) ? names.IndexOf(device) : 0;
            UiComboBox.Build(ref index, "Device", names.ToArray(), ref deviceIndex);
            device = (deviceIndex > 0) ? names[deviceIndex] : "";
        }
    }
}
