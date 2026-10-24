//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Numerics;
using ImGuiNET;

namespace SdfGlueUi.Ui.Windows
{
    public class WndPlayback : UiWindowSdfGlue
    {
        public override string Title => "Playback";

        public override void Build()
        {
            BuildWindow(delegate()
            {
                Vector2 butSize = new Vector2(120, 60);

                if (ImGui.Button("<<", butSize))
                {
                    GetPlayback().TimeStepBack();
                }
                ImGui.SameLine();
                if (ImGui.Button(GetPlayback().IsPlaying ? "||" : ">", butSize))
                {
                    GetPlayback().IsPlaying = !GetPlayback().IsPlaying;
                }
                ImGui.SameLine();
                if (ImGui.Button(">>", butSize))
                {
                    GetPlayback().TimeStepForward();
                }

                //ImGui.Separator();

                //BeginPropertyGrid(GetDefaultFirstColumnWidth());
                ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(2, 2));
                ImGui.Columns(3);
                ImGui.Separator();

                int columnWidth0 = 350;
                int columnWidth1 = 200;

                int index = 0;

                // time
                ImGui.PushID(index++);
                ImGui.SetColumnWidth(0, columnWidth0);
                ImGui.Text("Time:");
                ImGui.PopID();
                ImGui.NextColumn();

                ImGui.PushID(index++);
                //ImGui.SetNextItemWidth(100);
                ImGui.SetColumnWidth(1, columnWidth1);
                string timeVal = GetPlayback().CurrentTime.ToString("0.00", CultureInfo.InvariantCulture);
                ImGui.Text(String.Format("{0} s", timeVal));
                ImGui.PopID();
                ImGui.NextColumn();

                ImGui.PushID(index++);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Button("Reset"))
                {
                    GetPlayback().ResetTime();
                }
                ImGui.PopID();
                ImGui.NextColumn();

                // frame
                ImGui.PushID(index++);
                //ImGui.SetNextItemWidth(200);
                ImGui.SetColumnWidth(0, columnWidth0);
                ImGui.Text("Frame:");
                ImGui.PopID();
                ImGui.NextColumn();

                ImGui.PushID(index++);
                //ImGui.SetNextItemWidth(100);
                ImGui.SetColumnWidth(1, columnWidth1);
                string frameVal = GetPlayback().CurrentFrame.ToString(CultureInfo.InvariantCulture);
                ImGui.Text(frameVal);
                ImGui.PopID();
                ImGui.NextColumn();

                ImGui.PushID(index++);
                ImGui.SetNextItemWidth(-1);
                if (ImGui.Button("Reset"))
                {
                    GetPlayback().ResetFrameCounter();
                }
                ImGui.PopID();
                ImGui.NextColumn();

                // avg. render time
                ImGui.PushID(index++);
                ImGui.SetColumnWidth(0, columnWidth0);
                ImGui.Text("Avg. render time:");
                ImGui.PopID();
                ImGui.NextColumn();
                
                ImGui.PushID(index++);
                //ImGui.SetNextItemWidth(100);
                ImGui.SetColumnWidth(1, columnWidth1);
                string avgVal = GetPlayback().AvgRenderTime.ToString("0.000", CultureInfo.InvariantCulture);
                ImGui.Text(String.Format("{0} s", avgVal));
                ImGui.PopID();
                ImGui.NextColumn();
                ImGui.NextColumn();

                // fps
                ImGui.PushID(index++);
                ImGui.SetColumnWidth(0, columnWidth0);
                ImGui.Text("Avg. FPS:");
                ImGui.PopID();
                ImGui.NextColumn();

                ImGui.PushID(index++);
                //ImGui.SetNextItemWidth(100);
                ImGui.SetColumnWidth(1, columnWidth1);
                string fpsVal = GetPlayback().Fps.ToString("0.00", CultureInfo.InvariantCulture);
                ImGui.Text(fpsVal);
                ImGui.PopID();
                ImGui.NextColumn();
                ImGui.NextColumn();



                //EndPropertyGrid();
                ImGui.Columns(1);
                ImGui.Separator();
                ImGui.PopStyleVar();

            });
        }
    }
}
