//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppAnimation.Ui;
using System.Numerics;
using Xunit;

namespace SingleDocAppAnimation.Tests
{
    // The timeline window in a headless ImGui context: built with and without a document, then many frames of random
    // clicks, drags, wheel and keys (it must not throw and must leave the model consistent)
    public class WndTimelineTests
    {
        private sealed class Prop : IAnimProperty
        {
            public string               Path        { get; set; } = "";
            public string               Group       { get; set; } = "";
            public string               Label       { get { return Path; } }
            public AnimValueKind        Kind        { get; set; }
            public bool                 Discrete    { get; set; }
            public int                  Order       { get { return 0; } }
            public AnimPropertyHints    Hints       { get; set; } = AnimPropertyHints.Default;
            public AnimValue            Value;

            public AnimValue Get() { return Value; }
            public void Set(AnimValue v) { Value = v; }
        }

        private sealed class Target : IAnimTarget
        {
            public int                          Id          { get; set; }
            public string                       Label       { get; set; } = "";
            public List<Prop>                   Props       = new List<Prop>();
            public List<Target>                 Kids        = new List<Target>();
            public IEnumerable<IAnimProperty>   Properties  { get { return Props; } }
            public IEnumerable<IAnimTarget>     Children    { get { return Kids; } }

            public IAnimProperty? FindProperty(string path) { return Props.FirstOrDefault(p => p.Path == path); }
        }

        private sealed class Container : IAnimTrackContainer
        {
            public string               Id      { get; set; } = "c";
            public List<AnimTrack>      List    = new List<AnimTrack>();
            public IList<AnimTrack>     Tracks  { get { return List; } }
        }

        private sealed class Host : IAnimEditorHost, IAnimTimebase, IAnimRowSource
        {
            public readonly List<Container>     Docs        = new List<Container> { new Container { Id = "d0" }, new Container { Id = "d1" } };
            public readonly List<Target>        Targets     = new List<Target>();
            public readonly List<AnimSection>   List        = new List<AnimSection>();
            public readonly HashSet<IAnimTarget> Selection  = new HashSet<IAnimTarget>();
            public double                       Duration    = 3;
            public int                          Edits, RowSelections;
            private int                         depth_;

            public Host()
            {
                Transport = new AnimTransport(this);
                for (int i = 0; i < 4; i++)
                {
                    Target t = new Target
                    {
                        Id = i + 1, Label = "object " + (i + 1),
                        Props =
                        {
                            new Prop { Path = "x", Group = "Transform/Position", Value = AnimValue.Of(i) },
                            new Prop { Path = "y", Group = "Transform/Position" },
                            new Prop { Path = "color", Group = "Material", Kind = AnimValueKind.Color, Value = AnimValue.Color(200, 100, 50, 255) },
                            new Prop { Path = "mode", Group = "Material", Kind = AnimValueKind.Enum, Hints = new AnimPropertyHints { EnumNames = new[] { "a", "b", "c" } } },
                            new Prop { Path = "visible", Kind = AnimValueKind.Bool, Value = AnimValue.Of(1) },
                        },
                    };
                    if (i == 0)
                        t.Kids.Add(new Target { Id = 10, Label = "child", Props = { new Prop { Path = "radius", Group = "Shape" } } });
                    Targets.Add(t);
                }
                for (int s = 0; s < Docs.Count; s++)
                    List.Add(new AnimSection { Id = "S" + s, Label = "section " + s, Container = Docs[s], Targets = Targets.Skip(s * 2).Take(2).ToList(), Active = s == 0 });
                AnimTrack x = AnimTrackEditing.GetOrAdd(Docs[0], 1, "x", AnimValueKind.Number, false);
                x.SetKey(0, AnimValue.Of(0));
                x.SetKey(1, AnimValue.Of(10));
                Markers.Add(new AnimMarker { Name = "walk", Time = 0.5 });
            }

            public AnimTransport                    Transport   { get; }
            public IEnumerable<IAnimTrackContainer> Containers  { get { return Docs; } }
            public List<AnimMarker>                 Markers     { get; } = new List<AnimMarker>();
            public bool                             CanKey      { get { return true; } }
            public bool                             IsEditing   { get { return depth_ > 0; } }

            public void BeginEdit() { depth_++; }
            public void EndEdit() { if (depth_ > 0) Edits++; depth_ = 0; }
            public void Modify() { BeginEdit(); }
            public void Changed() { }
            public void Invalidate() { }
            public IAnimTarget? FindTarget(IAnimTrackContainer c, int id) { return Targets.FirstOrDefault(t => t.Id == id); }
            public void TimelineSelected() { }
            public void RowsSelected() { RowSelections++; }

            public bool IsAvailable { get { return true; } }
            public double Length { get { return Duration; } }
            public double Fps { get { return 25; } }
            public bool WholeFrames { get; set; }
            public double Snap(double t) { t = Math.Max(0, t); return WholeFrames ? AnimTime.Snap(t, 25) : Math.Round(t / 0.001) * 0.001; }
            public void SetLength(double t) { Duration = Math.Max(0.04, Snap(t)); }

            public IReadOnlyList<AnimSection> Sections { get { return List; } }
            public bool IsSelected(IAnimTarget target) { return Selection.Contains(target); }
        }

        private sealed class Window : WndTimeline
        {
            public AnimEditorController?    Ed;
            public Host?                    Source;

            public Window() : base(null, null) { }

            protected override AnimEditorController? Editor { get { return Ed; } }
            protected override IAnimRowSource? Rows { get { return Source; } }
            protected override string DocumentId { get { return "doc"; } }

            protected override void SetDefaultPlacement()
            {
                ImGui.SetNextWindowPos(new Vector2(0, 0), ImGuiCond.Always);
                ImGui.SetNextWindowSize(new Vector2(1200, 500), ImGuiCond.Always);
            }
        }

        private static void Frame(Window window)
        {
            ImGui.GetIO().DeltaTime = 1 / 60f;
            ImGui.NewFrame();
            window.Build();
            ImGui.Render();
        }

        [Fact]
        public void Timeline_BuildsAndSurvivesRandomInput()
        {
            IntPtr context = ImGui.CreateContext();
            try
            {
                ImGui.SetCurrentContext(context);
                ImGuiIOPtr io = ImGui.GetIO();
                io.DisplaySize = new Vector2(1280, 720);
                io.Fonts.AddFontDefault();
                io.Fonts.GetTexDataAsRGBA32(out IntPtr _, out int _, out int _, out int _);
                io.Fonts.SetTexID(new IntPtr(1));

                // no document
                Window window = new Window();
                Frame(window);

                Host host = new Host();
                AnimEditorController ed = new AnimEditorController(host, new AnimEditorOptions { KeyMode = AnimKeyMode.RecordToggle });
                window.Ed = ed;
                window.Source = host;
                Random random = new Random(1234);
                int seeks = 0;
                host.Transport.Seeked = () => seeks++;
                ImGuiKey[] keys = { ImGuiKey.Space, ImGuiKey.Delete, ImGuiKey.Comma, ImGuiKey.Period, ImGuiKey.Home, ImGuiKey.End, ImGuiKey.C, ImGuiKey.V, ImGuiKey.A, ImGuiKey.Escape };
                // a point: mostly the tree and the first rows (the arrows, the key buttons, the values)
                Vector2 Point()
                {
                    int where = random.Next(4);
                    float px = where == 0 ? random.Next(0, 300) : random.Next(0, 1210);
                    float py = where <= 1 ? random.Next(20, 300) : random.Next(0, 510);
                    return new Vector2(px, py);
                }
                void Click(Vector2 p, int button)
                {
                    io.AddMousePosEvent(p.X, p.Y);
                    Frame(window);
                    io.AddMouseButtonEvent(button, true);
                    Frame(window);
                    io.AddMouseButtonEvent(button, false);
                    Frame(window);
                }
                int maxTracks = 0;
                bool selected = false;
                host.Selection.Add(host.Targets[1]);        // selected in the host (the mark of its row)
                for (int step = 0; step < 3000; step++)
                {
                    if (step == 1500)
                        host.WholeFrames = true;
                    int action = random.Next(20);
                    if (action < 8)
                    {
                        Click(Point(), 0);
                        if (random.Next(4) == 0)
                            Click(Point(), 0);
                    }
                    else if (action < 10)
                    {
                        // a drag
                        Vector2 p = Point(), q = Point();
                        io.AddMousePosEvent(p.X, p.Y);
                        Frame(window);
                        io.AddMouseButtonEvent(0, true);
                        Frame(window);
                        for (int i = 1; i <= 5; i++)
                        {
                            Vector2 m = Vector2.Lerp(p, q, i / 5f);
                            io.AddMousePosEvent(m.X, m.Y);
                            Frame(window);
                        }
                        io.AddMouseButtonEvent(0, false);
                        Frame(window);
                    }
                    else if (action < 13)
                    {
                        // the menu and a click into it
                        Vector2 p = Point();
                        Click(p, 1);
                        Click(p + new Vector2(random.Next(5, 150), random.Next(5, 200)), 0);
                    }
                    else if (action == 13)
                    {
                        io.AddMouseWheelEvent(0, random.Next(-2, 3));
                        Frame(window);
                    }
                    else if (action < 17)
                    {
                        ImGuiKey key = keys[random.Next(keys.Length)];
                        bool ctrl = random.Next(2) == 0;
                        io.AddKeyEvent(ImGuiKey.ModCtrl, ctrl);
                        io.AddKeyEvent(key, true);
                        Frame(window);
                        io.AddKeyEvent(key, false);
                        io.AddKeyEvent(ImGuiKey.ModCtrl, false);
                        Frame(window);
                    }
                    else
                    {
                        io.AddKeyEvent(ImGuiKey.ModShift, random.Next(3) == 0);
                        Frame(window);
                    }
                    if (step % 100 == 0)
                        host.Transport.Update(0.1);

                    maxTracks = Math.Max(maxTracks, host.Docs.Sum(d => d.List.Count));
                    selected |= ed.SelectedRows.Count > 1;

                    // the model stays consistent
                    foreach (AnimTrack t in host.Docs.SelectMany(d => d.List))
                    {
                        Assert.NotEmpty(t.Keys);
                        Assert.Equal(t.Keys.OrderBy(k => k.Time).Select(k => k.Time), t.Keys.Select(k => k.Time));
                        Assert.All(t.Keys, k => Assert.True(k.Time >= 0));
                    }
                }
                // what the input did
                Assert.True(seeks > 100, "seeks " + seeks);
                Assert.True(host.Edits > 50, "edits " + host.Edits);
                Assert.True(maxTracks > 2, "tracks " + maxTracks);         // keyed by the key buttons of the tree
                Assert.True(selected);                                      // Ctrl / Shift clicks on the rows
                Assert.True(host.RowSelections > 50, "row selections " + host.RowSelections);
            }
            finally
            {
                ImGui.DestroyContext(context);
            }
        }
    }
}
