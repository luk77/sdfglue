//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using Xunit;

namespace SingleDocAppAnimation.Tests
{
    // The transport and the commands of the animation editor with a host of one container
    public class AnimEditorTests
    {
        private sealed class Prop : IAnimProperty
        {
            public string               Path        { get; set; } = "";
            public string               Group       { get { return ""; } }
            public string               Label       { get { return Path; } }
            public AnimValueKind        Kind        { get; set; }
            public bool                 Discrete    { get { return false; } }
            public int                  Order       { get { return 0; } }
            public AnimPropertyHints    Hints       { get { return AnimPropertyHints.Default; } }
            public AnimValue            Value;

            public AnimValue Get() { return Value; }
            public void Set(AnimValue v) { Value = v; }
        }

        private sealed class Target : IAnimTarget
        {
            public int                          Id          { get; set; }
            public string                       Label       { get { return "t"; } }
            public List<Prop>                   Props       = new List<Prop>();
            public HashSet<string>              Disabled    = new HashSet<string>();
            public IEnumerable<IAnimProperty>   Properties  { get { return Props; } }
            public IEnumerable<IAnimTarget>     Children    { get { return Enumerable.Empty<IAnimTarget>(); } }

            public IAnimProperty? FindProperty(string path) { return Props.FirstOrDefault(p => p.Path == path); }
            public string? DisabledReason(string path) { return Disabled.Contains(path) ? "off" : null; }
        }

        private sealed class Container : IAnimTrackContainer
        {
            public string               Id      { get; set; } = "c";
            public List<AnimTrack>      List    = new List<AnimTrack>();
            public IList<AnimTrack>     Tracks  { get { return List; } }
        }

        // a document of 2 s, the time in ms or in frames of 10 fps
        private sealed class Host : IAnimEditorHost, IAnimTimebase
        {
            public readonly Container       Doc         = new Container();
            public readonly Target          Item        = new Target { Id = 5, Props = { new Prop { Path = "x", Value = AnimValue.Of(3) }, new Prop { Path = "y" } } };
            public double                   Duration    = 2;
            public bool                     Frames;
            public bool                     Available   = true;
            public int                      Begins, Ends, Changes, Invalidates, Selected;
            private int                     depth_;

            public Host() { Transport = new AnimTransport(this); }

            public AnimTransport                    Transport   { get; }
            public IEnumerable<IAnimTrackContainer> Containers  { get { return new[] { Doc }; } }
            public List<AnimMarker>                 Markers     { get; } = new List<AnimMarker>();
            public bool                             CanKey      { get { return true; } }
            public bool                             IsEditing   { get { return depth_ > 0; } }

            public void BeginEdit() { if (depth_++ == 0) Begins++; }
            public void EndEdit() { depth_ = 0; Ends++; }
            public void Modify() { BeginEdit(); }
            public void Changed() { Changes++; }
            public void Invalidate() { Invalidates++; }
            public IAnimTarget? FindTarget(IAnimTrackContainer c, int id) { return c == Doc && id == Item.Id ? Item : null; }
            public void TimelineSelected() { Selected++; }

            public bool IsAvailable { get { return Available; } }
            public double Length { get { return Duration; } }
            public double Fps { get { return 10; } }
            public bool WholeFrames { get { return Frames; } }
            public double Snap(double t) { t = Math.Max(0, t); return Frames ? AnimTime.Snap(t, 10) : Math.Round(t / 0.001) * 0.001; }
            public void SetLength(double t) { Duration = Snap(t); }
        }

        [Fact]
        public void Keys_DisabledPropertyIsNotKeyed()
        {
            Host host = new Host();
            AnimEditorController ed = new AnimEditorController(host, new AnimEditorOptions());
            host.Item.Disabled.Add("y");
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x", "y" });
            Assert.NotNull(host.Doc.FindTrack(5, "x"));
            Assert.Null(host.Doc.FindTrack(5, "y"));

            ed.SelectRows(new[] { AnimRowRef.OfTarget("c", 5) }, false);
            host.Transport.GoTo(1);
            Assert.Equal(1, ed.KeySelectedRows());
            Assert.Null(host.Doc.FindTrack(5, "y"));

            // the bindings: every property at its own time
            AnimTrack x = host.Doc.FindTrack(5, "x")!;
            x.SetKey(1, AnimValue.Of(10));
            AnimBindings b = new AnimBindings(host.Doc, id => host.FindTarget(host.Doc, id));
            b.Apply(p => p.Path == "x" ? 0 : 1);
            Assert.Equal(3, host.Item.Props[0].Value.X);
            b.Apply(1);
            Assert.Equal(10, host.Item.Props[0].Value.X);
        }

        [Fact]
        public void Transport_PlaybackAndSteps()
        {
            Host host = new Host();
            AnimTransport tr = host.Transport;
            int seeking = 0, seeked = 0;
            tr.Seeking = () => seeking++;
            tr.Seeked = () => seeked++;

            tr.Playhead = 0.5;
            Assert.Equal(1, seeking);
            Assert.Equal(1, seeked);
            tr.Playhead = 0.5;                  // no change: no notification
            Assert.Equal(1, seeking);
            tr.Playhead = -1;
            Assert.Equal(0, tr.Playhead);

            // a loop
            tr.PlayFromStart();
            tr.Update(0.75);
            Assert.Equal(0.75, tr.Playhead, 9);
            tr.Update(1.5);
            Assert.Equal(0.25, tr.Playhead, 9);
            Assert.True(tr.Playing);
            // without a loop the playback stops at the end
            tr.Loop = false;
            tr.Update(5);
            Assert.False(tr.Playing);
            Assert.Equal(2, tr.Playhead, 9);
            // from the end (after a frame stopped): from the start again
            tr.Update(0.016);
            tr.TogglePlay();
            tr.Update(0.1);
            Assert.Equal(0.1, tr.Playhead, 9);

            // whole frames
            host.Frames = true;
            tr.Update(0.07);
            Assert.Equal(0.1, tr.Playhead, 9);  // frame 2 still (0.17 s)
            tr.GoTo(0.33);
            Assert.False(tr.Playing);
            Assert.Equal(0.3, tr.Playhead, 9);
            tr.StepFrame(1);
            Assert.Equal(0.4, tr.Playhead, 9);
            tr.GoTo(7);
            Assert.Equal(2, tr.Playhead, 9);    // within the document

            // the continuous time: the steps go to the frames of the fps
            host.Frames = false;
            tr.GoTo(0.25);
            tr.StepFrame(1);
            Assert.Equal(0.3, tr.Playhead, 9);
            tr.StepFrame(-1);
            Assert.Equal(0.2, tr.Playhead, 9);
            Assert.Equal(0.2, tr.KeyTime, 9);

            // no document: stopped
            host.Available = false;
            tr.Playing = true;
            tr.Update(0.1);
            Assert.False(tr.Playing);
            tr.Reset();
            Assert.Equal(0, tr.Playhead);
        }

        [Fact]
        public void Editor_KeysAndUndoEdits()
        {
            Host host = new Host();
            AnimEditorController ed = new AnimEditorController(host, new AnimEditorOptions());
            host.Transport.Playhead = 0.5;

            // a key: one edit
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x" });
            Assert.Equal(1, host.Begins);
            Assert.Equal(1, host.Ends);
            AnimTrack x = host.Doc.FindTrack(5, "x")!;
            Assert.Equal(3, x.Keys.Single().Value.Number);
            Assert.Equal(AnimKeyState.OnKey, ed.StateOf(host.Doc, 5, new[] { "x" }));
            Assert.Equal(AnimKeyState.Animated, ed.StateOf(host.Doc, 5, new[] { "x", "y" }));
            Assert.Equal(AnimKeyState.None, ed.StateOf(host.Doc, 5, new[] { "y" }));

            // a group: the ones without a key get one; when all have one, they are removed
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x", "y" });
            Assert.Equal(2, host.Doc.Tracks.Count);
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x", "y" });
            Assert.Empty(host.Doc.Tracks);

            // inside a running edit a command stays a part of it
            host.BeginEdit();
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x" });
            Assert.Equal(3, host.Ends);
            Assert.True(host.IsEditing);
            host.EndEdit();

            // a second key, the selection, the interpolation, the drag
            x = host.Doc.FindTrack(5, "x")!;
            host.Item.Props[0].Value = AnimValue.Of(13);
            host.Transport.Playhead = 1.5;
            ed.ToggleKeys(host.Doc, host.Item, new[] { "x" });
            Assert.Equal(new[] { 0.5, 1.5 }, x.Keys.Select(k => k.Time));
            ed.SelectKeys(new[] { new AnimTrackEditing.KeyRef(host.Doc, x, x.Keys[0]) }, false);
            Assert.Equal(1, host.Selected);
            Assert.True(ed.IsKeySelected(x.Keys[0]));
            ed.SetKeysInterpolation(ed.SelectedKeys, AnimInterpolation.Hold);
            Assert.True(x.Keys[0].IsHold);
            ed.BeginKeyDrag();
            ed.DragKeys(-0.2004);
            Assert.Equal(1, host.Invalidates);
            ed.EndKeyDrag();
            Assert.Equal(new[] { 0.3, 1.5 }, x.Keys.Select(k => Math.Round(k.Time, 9)));

            // copy and paste at the playhead (the same track)
            ed.CopyKeys();
            host.Transport.Playhead = 1;
            ed.PasteKeys();
            Assert.Equal(new[] { 0.3, 1, 1.5 }, x.Keys.Select(k => Math.Round(k.Time, 9)));
            Assert.True(x.Keys[1].IsHold);
            Assert.Single(ed.SelectedKeys);

            // the selected keys that are gone are deselected
            x.Keys.RemoveAt(1);
            ed.ValidateSelectedKeys();
            Assert.Empty(ed.SelectedKeys);

            // static again: the value at the playhead, the track removed
            host.Transport.Playhead = 1;
            ed.RemoveAnimation(host.Doc, new[] { x });
            Assert.Empty(host.Doc.Tracks);
            Assert.Equal(3, host.Item.Props[0].Value.Number);       // the hold of the first key
        }

        [Fact]
        public void Editor_MarkersAndLength()
        {
            Host host = new Host();
            AnimEditorController ed = new AnimEditorController(host, new AnimEditorOptions());
            ed.AddMarker(0.5004);
            ed.AddMarker(0.2);
            Assert.Equal(new[] { "marker_2", "marker" }, host.Markers.Select(m => m.Name));
            Assert.Equal(0, ed.SelectedMarker);
            Assert.Equal(0.5, host.Markers[1].Time, 9);
            Assert.False(ed.RenameMarker(0, "marker"));
            Assert.True(ed.RenameMarker(0, " walk "));
            Assert.Equal("walk", host.Markers[0].Name);
            ed.MoveMarker(0, 0.7777);
            Assert.Equal(0.778, host.Markers[0].Time, 9);
            ed.DeleteMarker(1);
            Assert.Single(host.Markers);
            Assert.Equal(-1, ed.SelectedMarker);

            ed.SetLength(3.0001);
            Assert.Equal(3, host.Duration, 9);
        }

        [Fact]
        public void Editor_AutoKeyModes()
        {
            Host host = new Host();
            AnimEditorController record = new AnimEditorController(host, new AnimEditorOptions());
            Assert.False(record.AutoKey);
            record.Recording = true;
            Assert.True(record.AutoKey);
            Assert.True(new AnimEditorController(host, new AnimEditorOptions { KeyMode = AnimKeyMode.AlwaysAutoKey }).AutoKey);

            // an edit of an animated property in any view of the host: a key at the playhead
            AnimTrack x = AnimTrackEditing.GetOrAdd(host.Doc, 5, "x", AnimValueKind.Number, false);
            x.SetKey(0, AnimValue.Of(3));
            Dictionary<string, AnimValue> before = AnimAutoKey.Snapshot(host.Doc, host.Item);
            Assert.False(AnimAutoKey.Commit(host.Doc, host.Item, before, 1));
            host.Item.Props[0].Value = AnimValue.Of(8);
            Assert.True(AnimAutoKey.Commit(host.Doc, host.Item, before, 1));
            Assert.Equal(8, x.KeyAt(1)!.Value.Number);
        }
    }
}
