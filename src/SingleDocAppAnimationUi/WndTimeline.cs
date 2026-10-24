//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppFramework.Ui;
using System.Globalization;
using System.Numerics;

namespace SingleDocAppAnimation.Ui
{
    // The timeline of the animation module (the layout and the interaction inspired by im-neo-sequencer): a ruler in frames or
    // seconds with the playhead and the end, the markers, and the rows. On the left is the tree (sections -> targets ->
    // groups -> properties, see WndTimeline.Tree), on the right the lanes: a property has its track's keys (diamonds; a
    // square: hold), the groups, the targets and the sections show the keys of what is under them. The key button of a
    // property (a diamond) keys it at the playhead; the value next to it is edited by a drag, or by a double click.
    // Every row can be selected (AnimEditorController.SelectedRows, by stable ids): a click on its name (Ctrl: toggle,
    // Shift: the range from the last click), on the empty part of its lane or on its key; only the selected rows are
    // highlighted, a target the host selected otherwise has a mark at the left edge. The host keys the selected rows
    // (KeySelectedRows) and shows them in its views.
    // Click / drag: the playhead (with Shift held it snaps to the keys and the markers), a key (all the selected ones
    // move), a marker or the end; Shift+drag on the lanes: a box
    // selection of keys; right click: the menu; wheel: scroll, Ctrl+wheel: zoom, middle drag: pan.
    // Keys: Space / Enter play / stop, Delete the selected keys / marker, Ctrl+C / Ctrl+V the keys, Ctrl+A all the rows
    // shown, Escape no rows, , . Home End.
    // The host gives the editor (AnimEditorController) and the rows (IAnimRowSource) every frame and can extend the
    // window (the virtual methods of "the host"): its own toolbar, keys, items in the lanes of the sections (e.g.
    // keyframes of the host), menus and popups.
    public partial class WndTimeline : UiWindowBase
    {
        protected enum DragMode
        {
            None,
            Playhead,
            Host,           // an item of the host (HitSectionLane)
            Marker,
            End,
            TrackKeys,
            Box,
            Value,
            Splitter,
        }

        protected readonly AnimTexts            Strings;
        protected readonly AnimTimelineStyle    Style;

        private double          zoom_           = 200;      // px per second (at the default UI scale)
        private double          scroll_;                    // s at the left edge of the rows
        private float           scrollY_;                   // px
        private float           namesPx_        = 300;      // the width of the tree (at the default UI scale)
        private string          fitted_         = "\0";     // the document the view was fitted to
        private string          expandedDoc_    = "\0";
        protected DragMode      drag_;
        private int             dragMarker_     = -1;
        private double          dragOffset_;
        private double          dragStartTime_;
        private Vector2         boxStart_;
        private string          valueRow_       = "";
        private double          valueStart_;
        private float           valueMouseX_;
        private AnimRowRef?     anchor_;                    // the row of the last click (the start of a Shift range)
        private bool            playheadSnapped_;           // the dragged playhead is snapped (Shift) to a key or a marker

        // the context menu
        private int             ctxMarker_      = -1;
        private string          ctxRow_         = "";
        protected double        ctxTime_;
        private bool            openRename_;
        private int             renameMarker_   = -1;
        private string          renameText_     = "";

        // the editor of a value (double click)
        private string          popupRow_       = "";
        private bool            openValue_;

        public WndTimeline(AnimTexts? texts = null, AnimTimelineStyle? style = null)
        {
            Strings = texts ?? new AnimTexts();
            Style   = style ?? AnimTimelineStyle.Default;
        }

        public override string Title => "Timeline";

        // ---------------------------------------------------------------- the host

        // the editor of the document shown; null: none (NoDocumentText)
        protected virtual AnimEditorController? Editor { get { return null; } }

        protected virtual IAnimRowSource? Rows { get { return null; } }

        // the id of the document shown: a new one is fitted into the width and its tree is collapsed
        protected virtual string DocumentId { get { return ""; } }

        protected virtual string NoDocumentText { get { return Strings.Get("TlNoDocument"); } }

        // shown below the rows (e.g. why there are no tracks), null: nothing
        protected virtual string? RowsFooter { get { return null; } }

        // called before the window is built (its default place and size)
        protected virtual void SetDefaultPlacement()
        {
        }

        // the window has the keyboard focus
        protected virtual void OnFocused()
        {
        }

        // the toolbar of the host after the transport
        protected virtual void BuildHostToolbar(AnimEditorController ed)
        {
        }

        // the keys that move the playhead (also in the host's other views): , . the frames, Home / End
        protected virtual void HandleTransportKeys(AnimEditorController ed)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            if (io.KeyCtrl || io.KeyAlt)
                return;
            if (ImGui.IsKeyPressed(ImGuiKey.Comma))
                ed.Transport.StepFrame(-1);
            if (ImGui.IsKeyPressed(ImGuiKey.Period))
                ed.Transport.StepFrame(+1);
            if (ImGui.IsKeyPressed(ImGuiKey.Home, false))
                ed.Transport.GoTo(0);
            if (ImGui.IsKeyPressed(ImGuiKey.End, false))
                ed.Transport.GoTo(ed.Transport.Timebase.Length);
        }

        // Delete with no keys nor a marker selected: the host deletes what it selected in the timeline
        protected virtual void DeleteHostSelection(AnimEditorController ed)
        {
        }

        // a press on a row (the names or the lane): the row becomes the active one in the host
        protected virtual void ActivateRow(AnimRow row)
        {
        }

        // a click on the timeline that deselects: the host deselects what it selects in the timeline
        protected virtual void ClearHostSelection()
        {
        }

        // the times of the items of the host the playhead snaps to (Shift+drag)
        protected virtual IEnumerable<double> HostSnapTimes(AnimEditorController ed)
        {
            return Enumerable.Empty<double>();
        }

        // An item of the host in the lane of a section row at the mouse, -1: none
        protected virtual int HitSectionLane(AnimRow row, Func<double, float> x, float mouseX)
        {
            return -1;
        }

        // the left button on an item of the host: true starts a drag (DragHost .. EndHostDrag)
        protected virtual bool PressHostItem(AnimEditorController ed, AnimRow row, int item, double t)
        {
            return false;
        }

        protected virtual void DragHost(AnimEditorController ed, double t)
        {
        }

        protected virtual void EndHostDrag(AnimEditorController ed)
        {
        }

        protected virtual string? HostItemTooltip(AnimRow row, int item)
        {
            return null;
        }

        // draws the lane of a section row (under the keys of its tracks)
        protected virtual void DrawSectionLane(ImDrawListPtr dl, AnimEditorController ed, AnimRow row, Func<double, float> x, float y0, float y1, float cy)
        {
        }

        // the right button: what the host's menu needs (row: under the mouse, item: HitSectionLane)
        protected virtual void OnContextMenu(AnimEditorController ed, AnimRow? row, int item)
        {
        }

        // the items of the host in the context menu (after the ones of the keys)
        protected virtual void BuildHostMenu(AnimEditorController ed, AnimRow? row)
        {
        }

        // the popups of the host
        protected virtual void BuildHostPopups(AnimEditorController ed)
        {
        }

        // ---------------------------------------------------------------- the window

        // sizes for the default 13 px font, scaled with the UI font (high DPI)
        protected static float Px(float v)
        {
            return v * ImGui.GetFontSize() / 13.0f;
        }

        public override void Build()
        {
            SetDefaultPlacement();
            BuildWindow(ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse, delegate ()
            {
                bool focused = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows);
                if (focused)
                    OnFocused();

                AnimEditorController? ed = Editor;
                IAnimRowSource? rows = Rows;
                if (ed == null || rows == null || !ed.Transport.Timebase.IsAvailable)
                {
                    ImGui.TextDisabled(NoDocumentText);
                    return;
                }

                ed.ValidateSelectedKeys();
                ed.ValidateSelectedRows();
                BuildToolbar(ed);
                if (focused && !ImGui.GetIO().WantTextInput)
                    HandleKeys(ed);
                BuildTimeline(ed, rows);
                BuildMenus(ed);
            });
        }

        // ---------------------------------------------------------------- toolbar

        private void BuildToolbar(AnimEditorController ed)
        {
            AnimTransport tr = ed.Transport;
            if (IconButton("tlstart", Icon.PlayFromStart))
                tr.PlayFromStart();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.Get("TlFromStart"));
            ImGui.SameLine();
            if (IconButton("tlplay", tr.Playing ? Icon.Stop : Icon.Play))
                tr.TogglePlay();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(Strings.Get(tr.Playing ? "TlStop" : "TlPlay"));
            ImGui.SameLine();
            ImGui.Checkbox(Strings.Get("TlLoop"), ref tr.Loop);
            if (ed.Options.KeyMode == AnimKeyMode.RecordToggle && ed.Options.ShowRecordButton)
            {
                ImGui.SameLine();
                bool on = ed.Recording;
                if (on)
                    ImGui.PushStyleColor(ImGuiCol.Button, 0xFF2020C0);
                if (IconButton("tlrecord", Icon.Record, Strings.Get("TlRecord")))
                    ed.Recording = !ed.Recording;
                if (on)
                    ImGui.PopStyleColor();
                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip(Strings.Get("TlRecordTip"));
            }
            ImGui.SameLine();
            IAnimTimebase tb = tr.Timebase;
            if (tb.WholeFrames)
                ImGui.Text(String.Format(Strings.Get("TlFrameOf"), AnimTime.TimeToFrame(tr.Playhead, tb.Fps), Math.Max(1, (int)Math.Round(tb.Length * tb.Fps))));
            else
                ImGui.Text(String.Format(Strings.Get("TlTimeOf"), tr.Playhead, tb.Length));

            BuildHostToolbar(ed);

            ImGui.SameLine();
            if (ImGui.Button(Strings.Get("TlAddLabel")))
                ed.AddMarker(tr.Playhead);
            ImGui.SameLine();
            if (ImGui.Button("-##tlzoomout"))
                zoom_ /= 1.5;
            ImGui.SameLine();
            if (ImGui.Button("+##tlzoomin"))
                zoom_ *= 1.5;
            ImGui.SameLine();
            if (ImGui.Button(Strings.Get("TlFit")))
                fitted_ = "\0";

            // the tree: what it shows
            ImGui.SameLine(0, Px(20));
            int filter = (int)filter_;
            ImGui.SetNextItemWidth(Px(100));
            if (ImGui.Combo(Strings.Get("TlFilter") + "##tlfilter", ref filter, Strings.Get("TlFilterAll") + "\0" + Strings.Get("TlFilterAnimated") + "\0" + Strings.Get("TlFilterSelected") + "\0"))
                filter_ = (KeyFilter)filter;
            ImGui.SameLine();
            ImGui.SetNextItemWidth(Px(110));
            ImGui.InputTextWithHint("##tlsearch", Strings.Get("TlSearch"), ref search_, 64);
            ImGui.SameLine();
            HelpMarker(Strings.Get("TlTip"));
        }

        protected string EaseNames()
        {
            return Strings.Get("TlEaseLinear") + "\0" + Strings.Get("TlEaseIn") + "\0" + Strings.Get("TlEaseOut") + "\0" + Strings.Get("TlEaseInOut") + "\0";
        }

        protected enum Icon
        {
            Play,
            PlayFromStart,
            Stop,
            Record,
        }

        // a button with a small icon drawn in front of the label (the default ImGui font has no symbols)
        protected static bool IconButton(string id, Icon icon, string label = "")
        {
            Vector2 p = ImGui.GetCursorScreenPos();
            float h = ImGui.GetFrameHeight();
            bool pressed = ImGui.Button("      " + label + "##" + id);
            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            p += new Vector2(h * 0.35f, h * 0.2f);
            float s = h * 0.6f;
            switch (icon)
            {
                case Icon.Play:
                    dl.AddTriangleFilled(p, p + new Vector2(0, s), p + new Vector2(s * 0.9f, s * 0.5f), 0xFF40D040);
                    break;
                case Icon.PlayFromStart:
                    dl.AddRectFilled(p, p + new Vector2(s * 0.2f, s), 0xFF40D040);
                    dl.AddTriangleFilled(p + new Vector2(s * 0.3f, 0), p + new Vector2(s * 0.3f, s), p + new Vector2(s * 1.1f, s * 0.5f), 0xFF40D040);
                    break;
                case Icon.Stop:
                    dl.AddRectFilled(p + new Vector2(s * 0.1f, s * 0.1f), p + new Vector2(s * 0.9f, s * 0.9f), 0xFF4040E0);
                    break;
                case Icon.Record:
                    dl.AddCircleFilled(p + new Vector2(s * 0.5f, s * 0.5f), s * 0.4f, 0xFF4040F0);
                    break;
            }
            return pressed;
        }

        // the keys of this window: the transport keys, Space and Enter play / stop, Delete removes the selected keys /
        // marker / the host's selection, Ctrl+C / Ctrl+V copy and paste the keys of the tracks, Ctrl+A selects the rows
        // shown, Escape deselects the rows
        private void HandleKeys(AnimEditorController ed)
        {
            HandleTransportKeys(ed);
            ImGuiIOPtr io = ImGui.GetIO();
            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.C, false))
                ed.CopyKeys();
            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.V, false))
                ed.PasteKeys();
            if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.A, false))
                ed.SelectRows(rows_.Select(r => r.Ref), false);
            if (ImGui.IsKeyPressed(ImGuiKey.Escape, false))
            {
                ed.ClearRows();
                anchor_ = null;
            }
            if (ImGui.IsKeyPressed(ImGuiKey.Space, false) || ImGui.IsKeyPressed(ImGuiKey.Enter, false) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter, false))
                ed.Transport.TogglePlay();
            if (ImGui.IsKeyPressed(ImGuiKey.Delete, false) || ImGui.IsKeyPressed(ImGuiKey.Backspace, false))
            {
                if (ed.SelectedKeys.Count > 0)
                    ed.DeleteSelectedKeys();
                else if (ed.SelectedMarker >= 0)
                    ed.DeleteMarker(ed.SelectedMarker);
                else
                    DeleteHostSelection(ed);
            }
        }

        // ---------------------------------------------------------------- timeline

        private float NamesWidth    { get { return Px(namesPx_); } }
        protected float RowHeight   { get { return Px(20); } }
        private float RulerHeight   { get { return Px(22); } }
        private float LabelsHeight  { get { return Px(18); } }
        private float IndentWidth   { get { return Px(12); } }
        private float KeyButtonX(float areaX) { return areaX - Px(14); }
        private float ValueRight(float areaX) { return areaX - Px(26); }
        private float ValueWidth     { get { return Px(86); } }

        private double Pps { get { return zoom_ * Px(1); } }

        private void BuildTimeline(AnimEditorController ed, IAnimRowSource source)
        {
            AnimTransport transport = ed.Transport;
            List<AnimMarker> markers = ed.Host.Markers;
            Vector2 origin = ImGui.GetCursorScreenPos();
            Vector2 size = Vector2.Max(ImGui.GetContentRegionAvail(), new Vector2(Px(200), Px(60)));
            float areaX = origin.X + Math.Min(NamesWidth, size.X * 0.7f), areaW = size.X - (areaX - origin.X);
            float rowsY = origin.Y + RulerHeight + LabelsHeight;
            double length = transport.Timebase.Length;

            // a new document is fitted into the width, its tree starts collapsed
            string doc = DocumentId;
            if (fitted_ != doc)
            {
                fitted_ = doc;
                zoom_ = Math.Clamp(areaW * 0.92 / Math.Max(length, 0.04) / Px(1), 5, 50000);
                scroll_ = -Px(8) / (zoom_ * Px(1));
                scrollY_ = 0;
            }
            if (expandedDoc_ != doc)
            {
                expandedDoc_ = doc;
                expanded_.Clear();
            }
            BuildRows(ed, source);

            float X(double t) { return areaX + (float)((t - scroll_) * Pps); }
            double TimeAt(float x) { return scroll_ + (x - areaX) / Pps; }
            float RowY(int row) { return rowsY + row * RowHeight - scrollY_; }
            int rowCount = rows_.Count;

            ImGui.InvisibleButton("##timeline", size, ImGuiButtonFlags.MouseButtonLeft | ImGuiButtonFlags.MouseButtonRight | ImGuiButtonFlags.MouseButtonMiddle);
            bool hovered = ImGui.IsItemHovered();
            bool active = ImGui.IsItemActive();
            Vector2 mouse = ImGui.GetMousePos();
            ImGuiIOPtr io = ImGui.GetIO();

            // ---- view
            float maxScrollY = Math.Max(0, rowCount * RowHeight - (size.Y - RulerHeight - LabelsHeight));
            if (hovered && io.MouseWheel != 0)
            {
                if (io.KeyCtrl)
                {
                    double tm = TimeAt(mouse.X);
                    zoom_ = Math.Clamp(zoom_ * Math.Pow(1.2, io.MouseWheel), 5, 50000);
                    scroll_ = tm - (mouse.X - areaX) / Pps;
                }
                else if (io.KeyShift || mouse.X < areaX)
                {
                    scrollY_ -= io.MouseWheel * RowHeight;
                }
                else
                {
                    scroll_ -= io.MouseWheel * Px(60) / Pps;
                }
            }
            if (active && ImGui.IsMouseDragging(ImGuiMouseButton.Middle, 0))
            {
                scroll_ -= io.MouseDelta.X / Pps;
                scrollY_ -= io.MouseDelta.Y;
            }
            scroll_ = Math.Max(scroll_, -Px(8) / Pps);     // a margin before 0
            scrollY_ = Math.Clamp(scrollY_, 0, maxScrollY);

            // ---- hit tests
            int hitIndex = mouse.Y >= rowsY ? (int)Math.Floor((mouse.Y - rowsY + scrollY_) / RowHeight) : -1;
            AnimRow? hitRow = hitIndex >= 0 && hitIndex < rowCount ? rows_[hitIndex] : null;
            bool inNames = mouse.X < areaX;
            bool inRuler = mouse.Y < origin.Y + RulerHeight;
            bool inLabels = !inRuler && mouse.Y < rowsY;

            // an item of the host in a section row
            int hitItem = -1;
            if (hitRow != null && hitRow.Kind == AnimRowKind.Section && !inNames && !inRuler && !inLabels)
                hitItem = HitSectionLane(hitRow, X, mouse.X);
            // the keys of the tracks under a row (the nearest one in time)
            double? hitTrackTime = null;
            if (hitRow != null && !inNames && !inRuler && !inLabels && hitItem < 0 && hitRow.Tracks.Count > 0)
            {
                float best = Px(6);
                foreach (AnimTrackEditing.KeyRef k in hitRow.Keys())
                {
                    float d = Math.Abs(X(k.Key.Time) - mouse.X);
                    if (d <= best)
                    {
                        best = d;
                        hitTrackTime = k.Key.Time;
                    }
                }
            }
            int hitMarker = -1;
            if (inLabels)
            {
                for (int i = 0; i < markers.Count; i++)
                {
                    float lx = X(markers[i].Time);
                    if (mouse.X >= lx - Px(4) && mouse.X <= lx + Px(10))
                        hitMarker = i;
                }
            }
            bool onEnd = inRuler && Math.Abs(X(length) - mouse.X) <= Px(5);
            bool onSplitter = !inRuler && Math.Abs(mouse.X - areaX) <= Px(3);
            if (hovered && onSplitter && drag_ == DragMode.None)
                ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeEW);

            // ---- mouse
            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                MousePressed(ed, hitRow, mouse, areaX, origin, length, hitItem, hitTrackTime, hitMarker, inRuler, inLabels, inNames, onEnd, onSplitter, TimeAt(mouse.X));
            if (drag_ != DragMode.None && ImGui.IsMouseDown(ImGuiMouseButton.Left))
            {
                double t = TimeAt(mouse.X);
                switch (drag_)
                {
                    case DragMode.Playhead:
                        transport.GoTo(Math.Clamp(SnapPlayhead(ed, t, io.KeyShift), 0, length));
                        break;
                    case DragMode.Host:
                        DragHost(ed, t);
                        break;
                    case DragMode.Marker:
                        ed.MoveMarker(dragMarker_, t - dragOffset_);
                        break;
                    case DragMode.End:
                        ed.SetLength(t);
                        break;
                    case DragMode.TrackKeys:
                        ed.DragKeys(t - dragStartTime_);
                        break;
                    case DragMode.Splitter:
                        namesPx_ = Math.Clamp((mouse.X - origin.X) / Px(1), 150, Math.Max(150, size.X / Px(1) * 0.7f));
                        break;
                    case DragMode.Value:
                        DragValue(ed, mouse.X);
                        break;
                }
            }
            else if (drag_ != DragMode.None)
            {
                if (drag_ == DragMode.Box)
                    SelectBox(ed, boxStart_, mouse, X, RowY, areaX);
                else if (drag_ == DragMode.TrackKeys)
                    ed.EndKeyDrag();
                else if (drag_ == DragMode.Host)
                    EndHostDrag(ed);
                else if (drag_ != DragMode.Playhead && drag_ != DragMode.Splitter)
                    ed.Host.EndEdit();
                drag_ = DragMode.None;
                playheadSnapped_ = false;
            }
            if (hovered && ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                ctxMarker_ = hitMarker;
                ctxRow_ = hitRow?.Id ?? "";
                ctxTime_ = transport.Timebase.Snap(TimeAt(mouse.X));
                OnContextMenu(ed, hitRow, hitItem);
                if (hitRow != null && hitTrackTime != null)
                {
                    List<AnimTrackEditing.KeyRef> at = hitRow.KeysAt(hitTrackTime.Value).ToList();
                    if (!at.Any(k => ed.IsKeySelected(k.Key)))
                        ed.SelectKeys(at, false);
                }
                // the menu of a row that is not selected: the row is selected (the commands of the selected rows)
                else if (hitRow != null && inNames && !ed.IsRowSelected(hitRow.Ref))
                {
                    SelectRow(ed, hitRow, false, false);
                }
                ImGui.OpenPopup("##tlctx");
            }
            if (hovered && drag_ == DragMode.None)
            {
                if (hitRow != null && hitItem >= 0 && HostItemTooltip(hitRow, hitItem) is string tip)
                    ImGui.SetTooltip(tip);
                else if (hitRow != null && hitTrackTime != null)
                    ImGui.SetTooltip(TrackKeyTooltip(hitRow, hitTrackTime.Value));
                else if (hitRow != null && inNames && hitRow.Disabled is string reason)
                    ImGui.SetTooltip(reason);
                else if (hitRow != null && inNames && hitRow.HasKeyButton && Math.Abs(mouse.X - KeyButtonX(areaX)) <= Px(8))
                    ImGui.SetTooltip(Strings.Get(hitRow.Kind == AnimRowKind.Property ? "TlKeyTip" : "TlKeyGroupTip"));
                else if (hitRow != null && inNames && hitRow.Kind == AnimRowKind.Property && hitRow.Track != null && mouse.X > areaX - Px(26) - ValueWidth && mouse.X < ValueRight(areaX))
                    ImGui.SetTooltip(Strings.Get("TlAnimatedBy"));
            }

            Draw(ed, source, origin, size, areaX, rowsY, X, RowY);
            if (drag_ == DragMode.Box)
            {
                ImDrawListPtr dl = ImGui.GetWindowDrawList();
                dl.AddRect(boxStart_, mouse, Style.Box);
                dl.AddRectFilled(boxStart_, mouse, (Style.Box & 0x00FFFFFF) | 0x28000000);
            }
        }

        // the left button went down
        private void MousePressed(AnimEditorController ed, AnimRow? hitRow, Vector2 mouse, float areaX, Vector2 origin, double length,
                                  int hitItem, double? hitTrackTime, int hitMarker, bool inRuler, bool inLabels, bool inNames, bool onEnd, bool onSplitter, double t)
        {
            ImGuiIOPtr io = ImGui.GetIO();
            AnimTransport transport = ed.Transport;
            List<AnimMarker> markers = ed.Host.Markers;
            if (onSplitter)
            {
                drag_ = DragMode.Splitter;
            }
            else if (inRuler)
            {
                if (onEnd)
                {
                    transport.Playing = false;
                    ed.Host.BeginEdit();
                    drag_ = DragMode.End;
                }
                else
                {
                    transport.GoTo(Math.Min(t, length));
                    drag_ = DragMode.Playhead;
                }
            }
            else if (inLabels)
            {
                if (hitMarker >= 0)
                {
                    ClearHostSelection();
                    ed.SelectedMarker = hitMarker;
                    transport.GoTo(markers[hitMarker].Time);
                    ed.Host.BeginEdit();
                    drag_ = DragMode.Marker;
                    dragMarker_ = hitMarker;
                    dragOffset_ = t - markers[hitMarker].Time;
                }
                else
                {
                    transport.GoTo(Math.Min(t, length));
                    drag_ = DragMode.Playhead;
                }
            }
            else if (hitRow != null && inNames)
            {
                PressedNames(ed, hitRow, mouse, origin.X, areaX);
            }
            else if (hitRow != null && hitItem >= 0)
            {
                ed.SelectedKeys.Clear();
                if (PressHostItem(ed, hitRow, hitItem, t))
                    drag_ = DragMode.Host;
            }
            else if (hitRow != null && hitTrackTime != null)
            {
                // a key of a track (or the aggregated keys of a row): selected, dragged with the other selected keys
                // its row is selected too (Shift / Ctrl: added to the selected rows)
                List<AnimTrackEditing.KeyRef> at = hitRow.KeysAt(hitTrackTime.Value).ToList();
                if (io.KeyShift || io.KeyCtrl)
                {
                    if (at.All(k => ed.IsKeySelected(k.Key)))
                        at.ForEach(ed.ToggleKeySelection);
                    else
                        ed.SelectKeys(at, true);
                    SelectRow(ed, hitRow, true, false);
                }
                else if (!at.Any(k => ed.IsKeySelected(k.Key)))
                {
                    ed.SelectKeys(at, false);
                    SelectRow(ed, hitRow, false, false);
                }
                if (ed.SelectedKeys.Count > 0)
                {
                    ActivateRow(hitRow);
                    ed.BeginKeyDrag();
                    drag_ = DragMode.TrackKeys;
                    dragStartTime_ = t;
                }
            }
            else if (mouse.X >= areaX && io.KeyShift && hitRow != null && ed.Host.CanKey)
            {
                boxStart_ = mouse;
                drag_ = DragMode.Box;
            }
            else if (hitRow != null)
            {
                // the empty part of a lane: the row is selected, the playhead moves there
                ClearHostSelection();
                ed.SelectedKeys.Clear();
                ActivateRow(hitRow);
                SelectRow(ed, hitRow, io.KeyCtrl, false);
                transport.GoTo(Math.Min(t, length));
                drag_ = DragMode.Playhead;
            }
            else if (mouse.X >= areaX)
            {
                ed.SelectedKeys.Clear();
                transport.GoTo(Math.Min(t, length));
                drag_ = DragMode.Playhead;
            }
        }

        // a click in the tree
        private void PressedNames(AnimEditorController ed, AnimRow row, Vector2 mouse, float left, float areaX)
        {
            float arrowX = left + Px(4) + row.Depth * IndentWidth;
            bool onArrow = row.Expandable && mouse.X >= arrowX - Px(2) && mouse.X <= arrowX + Px(12);
            bool enabled = row.Disabled == null;
            bool onKey = enabled && row.HasKeyButton && ed.Host.CanKey && Math.Abs(mouse.X - KeyButtonX(areaX)) <= Px(8);
            bool onValue = enabled && row.Kind == AnimRowKind.Property && mouse.X >= ValueRight(areaX) - ValueWidth && mouse.X <= ValueRight(areaX) + Px(2);

            ActivateRow(row);
            if (onKey)
            {
                ToggleKeyOf(ed, row);
            }
            else if (onValue && row.Property != null)
            {
                if (row.Property.Kind == AnimValueKind.Color || ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    popupRow_ = row.Id;
                    openValue_ = true;
                }
                else
                {
                    ed.Host.BeginEdit();
                    drag_ = DragMode.Value;
                    valueRow_ = row.Id;
                    valueStart_ = row.Property.Get().Number;
                    valueMouseX_ = mouse.X;
                }
            }
            else if (onArrow || (row.Expandable && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)))
            {
                if (!expanded_.Remove(row.Id))
                    expanded_.Add(row.Id);
            }
            else
            {
                ClearHostSelection();
                SelectRow(ed, row, ImGui.GetIO().KeyCtrl, ImGui.GetIO().KeyShift);
            }
        }

        // A click on a row: it is the selected one; toggle (Ctrl): added / removed; range (Shift): the rows shown from the
        // row of the last click to this one (Ctrl+Shift: added to the selection).
        private void SelectRow(AnimEditorController ed, AnimRow row, bool toggle, bool range)
        {
            AnimRowRef r = row.Ref;
            int from = anchor_ is AnimRowRef a ? rows_.FindIndex(o => o.Ref.Equals(a)) : -1;
            int to = rows_.IndexOf(row);
            if (range && from >= 0 && to >= 0)
            {
                ed.SelectRows(rows_.GetRange(Math.Min(from, to), Math.Abs(to - from) + 1).Select(o => o.Ref), toggle);
                return;
            }
            if (toggle)
                ed.ToggleRow(r);
            else
                ed.SelectRows(new[] { r }, false);
            anchor_ = r;
        }

        private static void ToggleKeyOf(AnimEditorController ed, AnimRow row)
        {
            if (row.Target != null)
                ed.ToggleKeys(row.Container, row.Target, row.KeyPaths);
        }

        // the drag of a value in the tree: the property as the host shows it (an animated one is keyed by the edit)
        private void DragValue(AnimEditorController ed, float mouseX)
        {
            AnimRow? row = FindRow(valueRow_);
            if (row == null || row.Property == null)
                return;
            ImGuiIOPtr io = ImGui.GetIO();
            double step = row.Property.Hints.DragStep * (io.KeyShift ? 0.1 : io.KeyCtrl ? 10 : 1);
            row.Property.Set(AnimValue.Of(valueStart_ + (mouseX - valueMouseX_) / Px(1) * step));
            ed.Host.Changed();
        }

        // The playhead dragged with Shift snaps to the nearest key of the tracks, item of the host
        // (HostSnapTimes), marker, the start or the end within a few px of the mouse; otherwise it follows the mouse.
        private double SnapPlayhead(AnimEditorController ed, double t, bool snap)
        {
            playheadSnapped_ = false;
            if (!snap)
                return t;
            IEnumerable<double> candidates = ed.Host.Containers.SelectMany(c => c.Tracks).SelectMany(tr => tr.Keys).Select(k => k.Time)
                .Concat(ed.Host.Markers.Select(m => m.Time))
                .Concat(HostSnapTimes(ed))
                .Append(0).Append(ed.Transport.Timebase.Length);
            double? near = AnimTrackEditing.Nearest(candidates, t, Px(10) / Pps);
            playheadSnapped_ = near != null;
            return near ?? t;
        }

        // the keys in the rectangle (over the lanes) are selected
        private void SelectBox(AnimEditorController ed, Vector2 a, Vector2 b, Func<double, float> X, Func<int, float> RowY, float areaX)
        {
            float x0 = Math.Min(a.X, b.X), x1 = Math.Max(a.X, b.X), y0 = Math.Min(a.Y, b.Y), y1 = Math.Max(a.Y, b.Y);
            List<AnimTrackEditing.KeyRef> inside = new List<AnimTrackEditing.KeyRef>();
            for (int i = 0; i < rows_.Count; i++)
            {
                float ry0 = RowY(i), ry1 = ry0 + RowHeight;
                if (ry1 < y0 || ry0 > y1 || rows_[i].Tracks.Count == 0)
                    continue;
                foreach (AnimTrackEditing.KeyRef k in rows_[i].Keys())
                {
                    float kx = X(k.Key.Time);
                    if (kx >= x0 && kx <= x1 && kx >= areaX)
                        inside.Add(k);
                }
            }
            ed.SelectKeys(inside, ImGui.GetIO().KeyShift);
        }

        private string TrackKeyTooltip(AnimRow row, double time)
        {
            List<AnimTrackEditing.KeyRef> keys = row.KeysAt(time).ToList();
            string s = String.Format(CultureInfo.InvariantCulture, Strings.Get("TlKeyTime"), time);
            if (keys.Count == 1)
            {
                AnimKey k = keys[0].Key;
                string[] eases = EaseNames().Split('\0');
                AnimTrack track = keys[0].Track;
                s += "\n" + FormatValue(k.Value, track.Kind, FindProperty(row, track)?.Hints) + "\n"
                   + (k.IsHold ? Strings.Get("TlHold") : Strings.Get("TlInterp") + ": " + eases[(int)k.Interpolation]);
                if (track.Kind == AnimValueKind.Color)
                    s += ", " + (track.ColorSpace == AnimColorSpace.Hsl ? "HSL" : "RGB");
            }
            else
            {
                s += "\n" + String.Format(Strings.Get("TlKeysN"), keys.Count);
            }
            return s;
        }

        // the property of a track of a row (its hints)
        private static IAnimProperty? FindProperty(AnimRow row, AnimTrack track)
        {
            if (row.Property != null && row.Property.Path == track.Property)
                return row.Property;
            return row.Target != null && row.Target.Id == track.TargetId ? row.Target.FindProperty(track.Property) : null;
        }

        // ---------------------------------------------------------------- values

        protected static string FormatValue(AnimValue v, AnimValueKind kind, AnimPropertyHints? hints)
        {
            if (kind == AnimValueKind.Color)
                return AnimXml.ColorToString(v);
            string[]? names = hints?.EnumNames;
            if (names != null && names.Length > 0)
                return names[Math.Clamp((int)Math.Round(v.Number), 0, names.Length - 1)];
            int decimals = hints?.Decimals ?? 3;
            return Math.Round(v.Number, decimals).ToString("0." + new string('#', decimals), CultureInfo.InvariantCulture);
        }

        protected static uint ToImColor(AnimValue c)
        {
            return ((uint)AnimColors.ToByte(c.W) << 24) | ((uint)AnimColors.ToByte(c.Z) << 16) | ((uint)AnimColors.ToByte(c.Y) << 8) | AnimColors.ToByte(c.X);
        }
    }
}
