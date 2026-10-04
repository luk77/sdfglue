//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueUi.Ui.Components;
using SingleDocAppCore.Model.DataNodes;
using System.Globalization;
using System.Numerics;

namespace SdfGlueUi.Ui.Windows
{
    // Plot of all signals (registered only with DataModel.UseSignals)
    public class WndSignals : UiWindowSdfGlue
    {
        public override string Title => "Signals";

        private int             timeWindowIndex_    = 1;    // SignalPlots.TimeWindows
        private bool            paused_             = false;
        private float           pausedTime_         = 0.0f;
        private HashSet<int>    hiddenSignals_      = new HashSet<int>();   // ids, runtime only

        public override void Build()
        {
            BuildWindow(delegate()
            {
                SignalsCollection signals = GetModel().Signals;

                // toolbar
                ImGui.SetNextItemWidth(100.0f * Executor.GetWindowsScaling());
                ImGui.Combo("Time window", ref timeWindowIndex_, SignalPlots.TimeWindowNames, SignalPlots.TimeWindowNames.Length);
                ImGui.SameLine();
                if (ImGui.Checkbox("Pause plot", ref paused_))
                    pausedTime_ = signals.GetRealTime();

                if (signals.GetChildrenCount() == 0)
                {
                    ImGui.TextDisabled("No signals. Add a signal in the Project Explorer (Signals > Add new signal).");
                    return;
                }

                // list of signals
                List<SignalInstance> visibleSignals = new List<SignalInstance>();
                float listWidth = 220.0f * Executor.GetWindowsScaling();
                if (ImGui.BeginChild("signals_list", new Vector2(listWidth, 0.0f), ImGuiChildFlags.Borders))
                {
                    foreach (TreeNode node in signals.Children)
                    {
                        if (node is not SignalInstance signal)
                            continue;

                        ImGui.PushID(signal.Id);
                        bool visible = !hiddenSignals_.Contains(signal.Id);
                        if (ImGui.Checkbox("##visible", ref visible))
                        {
                            if (visible)
                                hiddenSignals_.Remove(signal.Id);
                            else
                                hiddenSignals_.Add(signal.Id);
                        }
                        ImGui.SameLine();
                        ImGui.ColorButton("##color", SignalPlots.GetColor(signal), ImGuiColorEditFlags.NoTooltip, new Vector2(ImGui.GetTextLineHeight()));
                        ImGui.SameLine();

                        string label = MenuSignals.GetSignalLabel(signals, signal.Id);
                        string valueText = signal.Enabled.Val ? signal.GetCurrentValue().ToString("0.###", CultureInfo.InvariantCulture) : "off";
                        if (ImGui.Selectable(String.Format("{0} = {1}", label, valueText), GetModel().SelectedNode == signal))
                            GetModel().ImportantNodeToSelect = signal;
                        ImGui.PopID();

                        if (visible && signal.Enabled.Val)
                            visibleSignals.Add(signal);
                    }
                }
                ImGui.EndChild();

                ImGui.SameLine();

                float seconds = SignalPlots.TimeWindows[Math.Clamp(timeWindowIndex_, 0, SignalPlots.TimeWindows.Length - 1)];
                float xMax = paused_ ? pausedTime_ : signals.GetRealTime();
                SignalPlots.BuildPlot("signals_plot", visibleSignals, signals, new Vector2(-1.0f, -1.0f), seconds, xMax);
            });
        }
    }
}
