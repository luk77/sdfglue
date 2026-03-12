//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes.Signals;

namespace SdfGlueUi.Ui.Components
{
    public class MenuSignals
    {
        public static SignalInstance Build(SignalsCollection signals, SignalInstance oldVal, DataModel.OnValueChanged onValueChanged = null)
        {
            SignalInstance selected = null;
            if (ImGui.MenuItem("None"))
            {
                selected = null;
                if (selected != oldVal)
                {
                    if (onValueChanged != null)
                        onValueChanged();

                    return selected; // value changed (to null)
                }
                return oldVal; // no change
            }
            foreach (SignalInstance inst in signals.Children)
            {
                if (ImGui.MenuItem(inst.Name.Val))
                {
                    selected = inst;
                }
            }

            if (selected != null && selected != oldVal)
            {
                if (onValueChanged != null)
                    onValueChanged();

                return selected; // value changed
            }
            return oldVal; // no change
        }

        public static SignalInstance BuildPopup(string menuName, SignalsCollection signals, SignalInstance oldVal, DataModel.OnValueChanged onValueChanged = null)
        {
            if (!ImGui.BeginPopup(menuName))
                return oldVal;

            SignalInstance inst = Build(signals, oldVal, onValueChanged);

            ImGui.EndPopup();

            return inst;
        }

    }
}
