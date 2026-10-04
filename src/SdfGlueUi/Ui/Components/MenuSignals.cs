//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueUi.Ui.Components
{
    public class MenuSignals
    {
        // Label of a signal reference (signals are referenced by id)
        public static string GetSignalLabel(SignalsCollection signals, int signalId)
        {
            if (signalId == 0)
                return "None";

            SignalInstance? signal = signals.FindById(signalId);
            if (signal == null)
                return String.Format("<missing signal {0}>", signalId);

            return String.IsNullOrEmpty(signal.Name.Val) ? String.Format("<signal {0}>", signalId) : signal.Name.Val;
        }

        // Menu items: "None" + all signals. Returns true if an item was clicked (selectedId: 0 = none).
        // excludeId - signal not shown in the list (e.g. the signal itself, for references between signals)
        public static bool Build(SignalsCollection signals, int currentId, out int selectedId, int excludeId = 0)
        {
            selectedId = currentId;

            if (ImGui.MenuItem("None", "", currentId == 0))
            {
                selectedId = 0;
                return true;
            }

            ImGui.Separator();

            foreach (TreeNode node in signals.Children)
            {
                if (node is not SignalInstance signal || signal.Id == excludeId)
                    continue;

                ImGui.PushID(signal.Id);
                if (ImGui.MenuItem(GetSignalLabel(signals, signal.Id), "", signal.Id == currentId))
                    selectedId = signal.Id;
                ImGui.PopID();
            }

            return selectedId != currentId;
        }

        public static bool BuildPopup(string menuName, SignalsCollection signals, int currentId, out int selectedId, int excludeId = 0)
        {
            selectedId = currentId;
            if (!ImGui.BeginPopup(menuName))
                return false;

            bool changed = Build(signals, currentId, out selectedId, excludeId);

            ImGui.EndPopup();

            return changed;
        }
    }
}
