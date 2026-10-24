//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Numerics;

namespace SingleDocAppAnimation.Ui
{
    // The drawing and the menus of the timeline
    public partial class WndTimeline
    {
        private void Draw(AnimEditorController ed, IAnimRowSource source, Vector2 origin, Vector2 size, float areaX, float rowsY, Func<double, float> X, Func<int, float> RowY)
        {
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            Vector2 end = origin + size;
            float rulerY1 = origin.Y + RulerHeight;
            AnimTransport transport = ed.Transport;
            List<AnimMarker> markers = ed.Host.Markers;
            double length = transport.Timebase.Length;

            dl.PushClipRect(origin, end, true);
            dl.AddRectFilled(origin, end, Style.Back);

            // ---- rows (clipped below the ruler and the markers)
            dl.PushClipRect(new Vector2(origin.X, rowsY), end, true);
            if (rows_.Count == 0 && source.Sections.Count > 0)
                dl.AddText(new Vector2(origin.X + Px(8), rowsY + Px(6)), Style.TextDim, Strings.Get("TlFilterAnimated") + " / " + Strings.Get("TlFilterSelected") + ": -");
            for (int row = 0; row < rows_.Count; row++)
            {
                AnimRow r = rows_[row];
                float y0 = RowY(row), y1 = y0 + RowHeight, cy = (y0 + y1) * 0.5f;
                if (y1 < rowsY || y0 > end.Y)
                    continue;

                // only the selected rows are highlighted; a target the host selected (not its properties): a mark
                uint back = ed.IsRowSelected(r.Ref) ? Style.RowSelected
                          : r.Kind == AnimRowKind.Section ? (r.Section.Active ? Style.RowActive : Style.RowSection)
                          : (row % 2 == 0 ? Style.Row : Style.RowAlt);
                dl.AddRectFilled(new Vector2(origin.X, y0), new Vector2(end.X, y1), back);
                if (r.Kind == AnimRowKind.Target && source.IsSelected(r.Target!))
                    dl.AddRectFilled(new Vector2(origin.X, y0 + Px(1)), new Vector2(origin.X + Px(3), y1 - Px(1)), Style.Accent);

                DrawNames(dl, ed, r, origin.X, areaX, y0, cy);

                // the lane
                dl.PushClipRect(new Vector2(areaX, Math.Max(y0, rowsY)), new Vector2(end.X, y1), true);
                if (r.Kind == AnimRowKind.Section)
                    DrawSectionLane(dl, ed, r, X, y0, y1, cy);
                if (r.Kind == AnimRowKind.Property && r.Track != null)
                    DrawTrackLane(dl, ed, r, X, cy);
                else if (r.Kind != AnimRowKind.Property)
                    DrawAggregateLane(dl, ed, r, X, r.Kind == AnimRowKind.Section ? cy + Px(6) : cy, r.Kind == AnimRowKind.Section ? Px(2.5f) : Px(4));
                dl.PopClipRect();
            }
            if (RowsFooter is string footer)
            {
                dl.PushClipRect(new Vector2(origin.X, rowsY), new Vector2(areaX, end.Y), true);
                dl.AddText(new Vector2(origin.X + Px(6), RowY(rows_.Count) + Px(4)), Style.TextDim, footer);
                dl.PopClipRect();
            }
            dl.PopClipRect();

            // ---- after the end
            float xe = X(length);
            dl.PushClipRect(new Vector2(areaX, origin.Y), end, true);
            dl.AddRectFilled(new Vector2(Math.Max(xe, areaX), origin.Y), end, Style.AfterEnd);
            dl.PopClipRect();

            // ---- ruler
            dl.AddRectFilled(origin, new Vector2(end.X, rulerY1), Style.Ruler);
            dl.PushClipRect(new Vector2(areaX, origin.Y), new Vector2(end.X, rulerY1), true);
            DrawTicks(dl, transport.Timebase, X, origin.Y, rulerY1, areaX, end.X);
            dl.AddLine(new Vector2(xe, origin.Y), new Vector2(xe, rulerY1), Style.End, Px(2));
            dl.PopClipRect();

            // ---- markers
            dl.AddRectFilled(new Vector2(origin.X, rulerY1), new Vector2(end.X, rowsY), Style.Ruler);
            dl.AddText(new Vector2(origin.X + Px(6), rulerY1 + (LabelsHeight - ImGui.GetFontSize()) * 0.5f), Style.TextDim, Strings.Get("TlLabels"));
            dl.PushClipRect(new Vector2(areaX, rulerY1), new Vector2(end.X, rowsY), true);
            for (int i = 0; i < markers.Count; i++)
            {
                float lx = X(markers[i].Time);
                uint col = ed.SelectedMarker == i ? Style.Accent : Style.Marker;
                dl.AddTriangleFilled(new Vector2(lx, rulerY1 + Px(3)), new Vector2(lx + Px(8), rulerY1 + Px(7)), new Vector2(lx, rulerY1 + Px(11)), col);
                dl.AddLine(new Vector2(lx, rulerY1 + Px(2)), new Vector2(lx, rowsY - Px(2)), col, Px(1.5f));
                dl.AddText(new Vector2(lx + Px(10), rulerY1 + (LabelsHeight - ImGui.GetFontSize()) * 0.5f), col, markers[i].Name);
            }
            dl.PopClipRect();

            // ---- playhead
            float xp = X(transport.Playhead);
            dl.PushClipRect(new Vector2(areaX, origin.Y), end, true);
            dl.AddLine(new Vector2(xp, origin.Y), new Vector2(xp, end.Y), Style.Playhead, playheadSnapped_ ? Px(3) : Px(1.5f));
            dl.AddTriangleFilled(new Vector2(xp - Px(5), origin.Y), new Vector2(xp + Px(5), origin.Y), new Vector2(xp, origin.Y + Px(7)), Style.Playhead);
            dl.PopClipRect();

            dl.AddLine(new Vector2(areaX, origin.Y), new Vector2(areaX, end.Y), Style.Splitter, Px(1.5f));
            dl.PopClipRect();
        }

        // the left part of a row: the arrow, the name, the value and the key button
        private void DrawNames(ImDrawListPtr dl, AnimEditorController ed, AnimRow r, float left, float areaX, float y0, float cy)
        {
            float valueX1 = ValueRight(areaX), valueX0 = valueX1 - ValueWidth;
            bool hasValue = r.Kind == AnimRowKind.Property && r.Property != null;
            float textRight = (hasValue ? valueX0 : r.HasKeyButton ? KeyButtonX(areaX) - Px(10) : areaX) - Px(4);
            float ax = left + Px(4) + r.Depth * IndentWidth;

            dl.PushClipRect(new Vector2(left, y0), new Vector2(Math.Max(left, textRight), y0 + RowHeight), true);
            if (r.Expandable)
            {
                float s = Px(4);
                Vector2 c = new Vector2(ax + Px(5), cy);
                if (r.Expanded)
                    dl.AddTriangleFilled(c + new Vector2(-s, -s * 0.6f), c + new Vector2(s, -s * 0.6f), c + new Vector2(0, s * 0.8f), Style.Text);
                else
                    dl.AddTriangleFilled(c + new Vector2(-s * 0.6f, -s), c + new Vector2(-s * 0.6f, s), c + new Vector2(s * 0.8f, 0), Style.Text);
            }
            bool disabled = r.Disabled != null;
            uint col = (r.Kind == AnimRowKind.Section && r.Section.Dim) || disabled ? Style.TextHidden : r.Kind == AnimRowKind.Property && r.Track != null ? Style.KeyAnimated | 0xFF000000 : Style.Text;
            dl.AddText(new Vector2(ax + Px(14), y0 + (RowHeight - ImGui.GetFontSize()) * 0.5f), col, r.Label);
            dl.PopClipRect();

            if (hasValue)
            {
                AnimValue v = r.Property!.Get();
                if (r.Property.Kind == AnimValueKind.Color)
                {
                    Vector2 a = new Vector2(valueX1 - Px(34), y0 + Px(4)), b = new Vector2(valueX1, y0 + RowHeight - Px(4));
                    dl.AddRectFilled(a, b, 0xFF808080);
                    dl.AddRectFilled(a, b, ToImColor(v));
                    dl.AddRect(a, b, 0xFF202020);
                }
                else
                {
                    string text = FormatValue(v, r.Property.Kind, r.Property.Hints);
                    Vector2 ts = ImGui.CalcTextSize(text);
                    dl.AddText(new Vector2(valueX1 - ts.X, y0 + (RowHeight - ts.Y) * 0.5f), disabled ? Style.TextHidden : r.Track != null ? Style.AnimatedValue : Style.Value, text);
                }
            }
            if (r.HasKeyButton && ed.Host.CanKey && !disabled)
                AnimKeyButton.Draw(dl, new Vector2(KeyButtonX(areaX), cy), Px(5), StateOf(ed, r), Style);
        }

        // the keys of the tracks under a row, merged by time
        private void DrawAggregateLane(ImDrawListPtr dl, AnimEditorController ed, AnimRow r, Func<double, float> X, float cy, float radius)
        {
            if (r.Tracks.Count == 0)
                return;
            foreach (double t in AnimTrackEditing.KeyTimes(r.Tracks))
            {
                bool selected = r.Kind != AnimRowKind.Section && r.KeysAt(t).All(k => ed.IsKeySelected(k.Key));
                DrawDiamond(dl, new Vector2(X(t), cy), radius, selected ? Style.Accent : Style.AggregateKey, false);
            }
        }

        private void DrawTrackLane(ImDrawListPtr dl, AnimEditorController ed, AnimRow r, Func<double, float> X, float cy)
        {
            AnimTrack track = r.Track!;
            for (int i = 0; i + 1 < track.Keys.Count; i++)
            {
                AnimKey a = track.Keys[i], b = track.Keys[i + 1];
                if (!a.IsHold && !track.IsStepped && !a.Value.Equals(b.Value))
                    dl.AddLine(new Vector2(X(a.Time), cy), new Vector2(X(b.Time), cy), Style.TrackSpan, Px(2));
            }
            foreach (AnimKey k in track.Keys)
            {
                uint col = ed.IsKeySelected(k) ? Style.Accent : Style.TrackKey;
                Vector2 c = new Vector2(X(k.Time), cy);
                if (k.IsHold || track.IsStepped)
                {
                    float s = Px(4);
                    dl.AddRectFilled(c - new Vector2(s, s), c + new Vector2(s, s), col);
                    dl.AddRect(c - new Vector2(s, s), c + new Vector2(s, s), 0xFF000000);
                }
                else
                {
                    DrawDiamond(dl, c, Px(5), col, false);
                }
            }
        }

        // major ticks (numbered) at least 60 px apart: frame numbers (1-based) or seconds
        private void DrawTicks(ImDrawListPtr dl, IAnimTimebase tb, Func<double, float> X, float y0, float y1, float xMin, float xMax)
        {
            double pps = Pps;
            double major, minor;
            bool frames = tb.WholeFrames;
            if (frames)
            {
                double frame = 1.0 / tb.Fps;
                int[] steps = { 1, 2, 5, 10, 25, 50, 100, 250, 500, 1000, 2500, 5000 };
                int n = steps.FirstOrDefault(s => s * frame * pps >= Px(60));
                if (n == 0)
                    n = steps[steps.Length - 1];
                major = n * frame;
                minor = frame * pps >= Px(5) ? frame : major / (n >= 5 ? 5 : n);
            }
            else
            {
                double[] steps = { 0.001, 0.002, 0.005, 0.01, 0.02, 0.05, 0.1, 0.2, 0.5, 1, 2, 5, 10, 30, 60, 300 };
                major = steps.FirstOrDefault(s => s * pps >= Px(60));
                if (major == 0)
                    major = steps[steps.Length - 1];
                minor = major / 5;
            }

            double t0 = scroll_ - minor, t1 = scroll_ + (xMax - xMin) / pps + minor;
            long first = (long)Math.Floor(Math.Max(0, t0) / minor);
            for (long i = first; i * minor <= t1; i++)
            {
                double t = i * minor;
                float x = X(t);
                bool isMajor = Math.Abs(t / major - Math.Round(t / major)) < 1e-6;
                dl.AddLine(new Vector2(x, y1 - (isMajor ? Px(10) : Px(4))), new Vector2(x, y1), Style.Tick);
                if (isMajor)
                {
                    string text = frames ? AnimTime.TimeToFrame(t, tb.Fps).ToString() : (Math.Round(t, 3)).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                    dl.AddText(new Vector2(x + Px(3), y0 + Px(2)), Style.Text, text);
                }
            }
        }

        protected void DrawDiamond(ImDrawListPtr dl, Vector2 c, float r, uint color, bool hollow)
        {
            Vector2 a = c + new Vector2(0, -r), b = c + new Vector2(r, 0), d = c + new Vector2(0, r), e = c + new Vector2(-r, 0);
            if (hollow)
            {
                dl.AddQuadFilled(a, b, d, e, Style.Back);
                dl.AddQuad(a, b, d, e, color, 1.5f);
            }
            else
            {
                dl.AddQuadFilled(a, b, d, e, color);
                dl.AddQuad(a, b, d, e, 0xFF000000, 1.0f);
            }
        }

        // ---------------------------------------------------------------- menus

        private void BuildMenus(AnimEditorController ed)
        {
            List<AnimMarker> markers = ed.Host.Markers;
            if (ImGui.BeginPopup("##tlctx"))
            {
                AnimRow? row = FindRow(ctxRow_);
                if (row != null && ed.Host.CanKey && row.Kind != AnimRowKind.Section)
                    BuildTrackMenu(ed, row);
                BuildHostMenu(ed, row);
                if (ed.Host.CanKey && ed.HasCopiedKeys && ImGui.MenuItem(Strings.Get("TlPasteKeys"), "Ctrl+V"))
                    ed.PasteKeys();
                if (ImGui.MenuItem(Strings.Get("TlAddLabel")))
                    ed.AddMarker(ctxTime_);
                if (ctxMarker_ >= 0 && ctxMarker_ < markers.Count)
                {
                    if (ImGui.MenuItem(Strings.Get("TlRenameLabel")))
                    {
                        renameMarker_ = ctxMarker_;
                        renameText_ = markers[ctxMarker_].Name;
                        openRename_ = true;
                    }
                    if (ImGui.MenuItem(Strings.Get("TlDeleteLabel")))
                        ed.DeleteMarker(ctxMarker_);
                }
                ImGui.EndPopup();
            }

            if (openRename_)
            {
                ImGui.OpenPopup("##tlrename");
                openRename_ = false;
            }
            if (ImGui.BeginPopup("##tlrename"))
            {
                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();
                ImGui.SetNextItemWidth(Px(160));
                bool enter = ImGui.InputText("##tlname", ref renameText_, 64, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll);
                ImGui.SameLine();
                if (ImGui.Button("OK") || enter)
                {
                    ed.RenameMarker(renameMarker_, renameText_);
                    ImGui.CloseCurrentPopup();
                }
                ImGui.EndPopup();
            }

            BuildValuePopup(ed);
            BuildHostPopups(ed);
        }

        // keys: the menu of a row of the tree or of a key
        private void BuildTrackMenu(AnimEditorController ed, AnimRow row)
        {
            ed.ValidateSelectedKeys();
            List<AnimTrackEditing.KeyRef> selected = ed.SelectedKeys.ToList();
            if (row.HasKeyButton && row.Target != null)
            {
                bool on = StateOf(ed, row) == AnimKeyState.OnKey;
                string key = row.Kind == AnimRowKind.Property ? (on ? "TlKeyRemove" : "TlKeyAdd") : (on ? "TlKeyRemove" : "TlKeyAll");
                if (ImGui.MenuItem(Strings.Get(key)))
                    ToggleKeyOf(ed, row);
            }
            if (ed.HasPropertyRowsSelected && ImGui.MenuItem(String.Format(Strings.Get("TlKeySelected"), ed.SelectedProperties().Count)))
                ed.KeySelectedRows();
            if (row.Tracks.Count > 0 && ImGui.MenuItem(Strings.Get("TlRemoveAnimation")))
                ed.RemoveAnimation(row.Container, row.Tracks.ToList());

            List<AnimTrack> colors = row.Tracks.Where(t => t.Kind == AnimValueKind.Color).ToList();
            if (colors.Count > 0 && ImGui.BeginMenu(Strings.Get("TlColorSpace")))
            {
                if (ImGui.MenuItem(Strings.Get("TlRgb"), "", colors.All(t => t.ColorSpace == AnimColorSpace.Rgb)))
                    ed.SetColorSpace(colors, AnimColorSpace.Rgb);
                if (ImGui.MenuItem(Strings.Get("TlHsl"), "", colors.All(t => t.ColorSpace == AnimColorSpace.Hsl)))
                    ed.SetColorSpace(colors, AnimColorSpace.Hsl);
                ImGui.EndMenu();
            }
            if (selected.Count > 0)
            {
                ImGui.Separator();
                if (ImGui.BeginMenu(Strings.Get("TlInterp")))
                {
                    string[] eases = EaseNames().TrimEnd('\0').Split('\0');
                    for (int i = 0; i < eases.Length; i++)
                    {
                        if (ImGui.MenuItem(eases[i], "", selected.All(k => k.Key.Interpolation == (AnimInterpolation)i)))
                            ed.SetKeysInterpolation(selected, (AnimInterpolation)i);
                    }
                    if (ImGui.MenuItem(Strings.Get("TlHold"), "", selected.All(k => k.Key.IsHold)))
                        ed.SetKeysInterpolation(selected, AnimInterpolation.Hold);
                    ImGui.EndMenu();
                }
                if (ImGui.MenuItem(Strings.Get("TlCopyKeys"), "Ctrl+C"))
                    ed.CopyKeys();
                if (ImGui.MenuItem(Strings.Get("TlDeleteKeys"), "Delete"))
                    ed.DeleteSelectedKeys();
            }
            ImGui.Separator();
        }

        // the editor of the value of a property (a double click on it in the tree; a color: a click)
        private void BuildValuePopup(AnimEditorController ed)
        {
            if (openValue_)
            {
                ImGui.OpenPopup("##tlvalue");
                openValue_ = false;
            }
            if (!ImGui.BeginPopup("##tlvalue"))
                return;
            AnimRow? row = FindRow(popupRow_);
            if (row == null || row.Property == null)
            {
                ImGui.CloseCurrentPopup();
                ImGui.EndPopup();
                return;
            }
            ImGui.TextDisabled(row.Label);
            IAnimProperty p = row.Property;
            AnimValue v = p.Get();
            if (p.Kind == AnimValueKind.Color)
            {
                Vector4 f = new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W) / 255f;
                ImGui.SetNextItemWidth(Px(240));
                if (ImGui.ColorPicker4("##tlcolor", ref f, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.Uint8))
                {
                    ed.Host.Modify();
                    p.Set(AnimValue.Color(f.X * 255, f.Y * 255, f.Z * 255, f.W * 255));
                }
            }
            else
            {
                double n = v.Number;
                if (ImGui.IsWindowAppearing())
                    ImGui.SetKeyboardFocusHere();
                ImGui.SetNextItemWidth(Px(140));
                if (ImGui.InputDouble("##tlnumber", ref n, 0, 0, "%.3f"))
                {
                    ed.Host.Modify();
                    p.Set(AnimValue.Of(n));
                }
                if (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter))
                    ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
    }
}
