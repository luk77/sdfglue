//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using Xunit;

namespace SingleDocAppAnimation.Tests
{
    // The selection of the rows of the timeline (AnimEditorController.SelectedRows): stable ids, parents = their subtree,
    // the keying of the selection, the validation after edits and after an undo that replaced the model
    public class AnimRowSelectionTests
    {
        private sealed class Prop : IAnimProperty
        {
            public string               Path        { get; set; } = "";
            public string               Group       { get; set; } = "";
            public string               Label       { get { return Path; } }
            public AnimValueKind        Kind        { get { return AnimValueKind.Number; } }
            public bool                 Discrete    { get { return false; } }
            public int                  Order       { get { return 0; } }
            public AnimPropertyHints    Hints       { get { return AnimPropertyHints.Default; } }
            public AnimValue            Value;

            public AnimValue Get() { return Value; }
            public void Set(AnimValue v) { Value = v; }
        }

        // a target with the properties x, y (Transform/Position), r (Transform) and a "node" without a stable path
        // ("n.@0.x"; ResolvePath gives it "n.a.x" when it is keyed, like the nodes of a path in a vector editor)
        private sealed class Target : IAnimTarget
        {
            public int                          Id          { get; set; }
            public string                       Label       { get { return "t" + Id; } }
            public List<Prop>                   Props       = new List<Prop>();
            public List<Target>                 Kids        = new List<Target>();
            public IEnumerable<IAnimProperty>   Properties  { get { return Props; } }
            public IEnumerable<IAnimTarget>     Children    { get { return Kids; } }

            public IAnimProperty? FindProperty(string path) { return Props.FirstOrDefault(p => p.Path == path); }

            public string ResolvePath(string path)
            {
                Prop? p = Props.FirstOrDefault(o => o.Path == path);
                if (p != null && path == "n.@0.x")
                    p.Path = "n.a.x";
                return p?.Path ?? path;
            }

            public static Target Make(int id)
            {
                return new Target
                {
                    Id = id,
                    Props =
                    {
                        new Prop { Path = "x", Group = "Transform/Position", Value = AnimValue.Of(id) },
                        new Prop { Path = "y", Group = "Transform/Position" },
                        new Prop { Path = "r", Group = "Transform" },
                        new Prop { Path = "n.@0.x", Group = "Geometry" },
                    },
                };
            }
        }

        private sealed class Container : IAnimTrackContainer
        {
            public string               Id      { get; set; } = "";
            public List<AnimTrack>      List    = new List<AnimTrack>();
            public IList<AnimTrack>     Tracks  { get { return List; } }
            public List<Target>         Targets = new List<Target>();

            // a copy (an undo restores a copy of the model: new objects, the same ids)
            public Container Clone()
            {
                return new Container { Id = Id, List = List.Select(t => t.Clone()).ToList(), Targets = Targets };
            }
        }

        private sealed class Host : IAnimEditorHost, IAnimTimebase
        {
            public List<Container>                  Docs        = new List<Container>();
            public int                              Selections;
            private int                             depth_;

            public Host()
            {
                Transport = new AnimTransport(this);
                for (int i = 0; i < 2; i++)
                {
                    Container c = new Container { Id = "layer" + i };
                    c.Targets.Add(Target.Make(10 * i + 1));
                    c.Targets.Add(Target.Make(10 * i + 2));
                    Docs.Add(c);
                }
                Docs[0].Targets[0].Kids.Add(Target.Make(100));
            }

            public AnimTransport                    Transport   { get; }
            public IEnumerable<IAnimTrackContainer> Containers  { get { return Docs; } }
            public List<AnimMarker>                 Markers     { get; } = new List<AnimMarker>();
            public bool                             CanKey      { get; set; } = true;
            public bool                             IsEditing   { get { return depth_ > 0; } }

            public void BeginEdit() { depth_++; }
            public void EndEdit() { depth_ = 0; }
            public void Modify() { BeginEdit(); }
            public void Changed() { }
            public void Invalidate() { }
            public void TimelineSelected() { }
            public void RowsSelected() { Selections++; }

            public IAnimTarget? FindTarget(IAnimTrackContainer c, int id)
            {
                return ((Container)c).Targets.SelectMany(t => t.Kids.Prepend(t)).FirstOrDefault(t => t.Id == id);
            }

            public bool IsAvailable { get { return true; } }
            public double Length { get { return 2; } }
            public double Fps { get { return 10; } }
            public bool WholeFrames { get { return false; } }
            public double Snap(double t) { return Math.Max(0, Math.Round(t / 0.001) * 0.001); }
            public void SetLength(double t) { }
        }

        private static (Host, AnimEditorController) Make()
        {
            Host host = new Host();
            return (host, new AnimEditorController(host, new AnimEditorOptions { KeyMode = AnimKeyMode.AlwaysAutoKey }));
        }

        [Fact]
        public void Select_ToggleAndTheHost()
        {
            (Host host, AnimEditorController ed) = Make();
            AnimRowRef x = AnimRowRef.OfProperty("layer0", 1, "x");
            AnimRowRef group = AnimRowRef.OfGroup("layer0", 2, "Transform");
            ed.SelectRows(new[] { x }, false);
            Assert.True(ed.IsRowSelected(x));
            Assert.Equal(1, host.Selections);
            int version = ed.RowSelectionVersion;
            ed.SelectRows(new[] { x }, false);                  // no change: no notification
            Assert.Equal(1, host.Selections);
            Assert.Equal(version, ed.RowSelectionVersion);
            ed.ToggleRow(group);
            Assert.Equal(new[] { x, group }, ed.SelectedRows);
            ed.ToggleRow(x);
            Assert.Equal(new[] { group }, ed.SelectedRows);
            Assert.True(ed.HasPropertyRowsSelected);
            ed.SelectRows(new[] { AnimRowRef.OfSection("layer1") }, false);
            Assert.False(ed.HasPropertyRowsSelected);
            ed.ClearRows();
            Assert.Empty(ed.SelectedRows);
            Assert.Equal(5, host.Selections);
            // equal by value (the rows are built again every frame)
            Assert.Equal(AnimRowRef.OfGroup("layer0", 2, "Transform"), group);
            Assert.NotEqual(AnimRowRef.OfGroup("layer1", 2, "Transform"), group);
        }

        [Fact]
        public void Parents_MeanTheirSubtree()
        {
            (Host host, AnimEditorController ed) = Make();
            ed.SelectRows(new[] { AnimRowRef.OfGroup("layer0", 1, "Transform") }, false);
            Assert.Equal(new[] { "x", "y", "r" }, ed.SelectedProperties().Select(p => p.Path));
            Assert.True(ed.IsPropertySelected(host.Docs[0], 1, "x"));          // a subgroup
            Assert.True(ed.IsPropertySelected(host.Docs[0], 1, "r"));
            Assert.False(ed.IsPropertySelected(host.Docs[0], 1, "n.@0.x"));
            Assert.False(ed.IsPropertySelected(host.Docs[0], 2, "x"));
            Assert.False(ed.IsPropertySelected(host.Docs[1], 1, "x"));

            // "Transform/Pos" is not a group above "Transform/Position"
            ed.SelectRows(new[] { AnimRowRef.OfGroup("layer0", 1, "Transform/Pos") }, false);
            Assert.Empty(ed.SelectedProperties());

            // a target: all its properties and the ones of its children, each once
            ed.SelectRows(new[] { AnimRowRef.OfTarget("layer0", 1), AnimRowRef.OfProperty("layer0", 1, "x") }, false);
            Assert.Equal(8, ed.SelectedProperties().Count);
            Assert.Contains(ed.SelectedProperties(), p => p.Target.Id == 100 && p.Path == "y");
            Assert.True(ed.IsPropertySelected(host.Docs[0], 1, "y"));
            Assert.False(ed.IsPropertySelected(host.Docs[0], 1, "y", viaTarget: false));
            Assert.True(ed.IsPropertySelected(host.Docs[0], 1, "x", viaTarget: false));

            // a section: nothing to key
            ed.SelectRows(new[] { AnimRowRef.OfSection("layer0") }, false);
            Assert.Empty(ed.SelectedProperties());
            Assert.False(ed.IsPropertySelected(host.Docs[0], 1, "x"));
        }

        [Fact]
        public void KeySelectedRows_KeysExactlyTheSelection()
        {
            (Host host, AnimEditorController ed) = Make();
            host.Transport.Playhead = 0.5;
            ed.SelectRows(new[] { AnimRowRef.OfGroup("layer0", 1, "Transform/Position"), AnimRowRef.OfProperty("layer1", 12, "r"), AnimRowRef.OfSection("layer1") }, false);
            Assert.Equal(3, ed.KeySelectedRows());
            Assert.Equal(new[] { "x", "y" }, host.Docs[0].List.Select(t => t.Property));
            Assert.All(host.Docs[0].List, t => Assert.Equal(1, t.TargetId));
            Assert.Equal(1, host.Docs[0].List[0].Keys[0].Value.Number);         // the current value
            Assert.Equal(0.5, host.Docs[0].List[0].Keys[0].Time, 9);
            AnimTrack r = Assert.Single(host.Docs[1].List);
            Assert.Equal(12, r.TargetId);

            // again at another time: one more key in each, the current value
            host.Transport.Playhead = 1;
            host.Docs[0].Targets[0].Props[0].Value = AnimValue.Of(7);
            ed.KeySelectedRows();
            Assert.Equal(2, host.Docs[0].List[0].Keys.Count);
            Assert.Equal(7, host.Docs[0].List[0].Keys[1].Value.Number);

            // nothing to key / no keying
            ed.SelectRows(new[] { AnimRowRef.OfSection("layer0") }, false);
            Assert.Equal(0, ed.KeySelectedRows());
            ed.SelectRows(new[] { AnimRowRef.OfProperty("layer0", 2, "x") }, false);
            host.CanKey = false;
            Assert.Equal(0, ed.KeySelectedRows());
            Assert.Null(host.Docs[0].FindTrack(2, "x"));
        }

        [Fact]
        public void KeySelectedRows_ResolvedPathStaysSelected()
        {
            (Host host, AnimEditorController ed) = Make();
            ed.SelectRows(new[] { AnimRowRef.OfProperty("layer0", 1, "n.@0.x") }, false);
            Assert.Equal(1, ed.KeySelectedRows());
            Assert.NotNull(host.Docs[0].FindTrack(1, "n.a.x"));
            Assert.Equal(new[] { AnimRowRef.OfProperty("layer0", 1, "n.a.x") }, ed.SelectedRows);
            ed.ValidateSelectedRows();
            Assert.Single(ed.SelectedRows);
        }

        [Fact]
        public void Validation_AfterEditsAndAnUndo()
        {
            (Host host, AnimEditorController ed) = Make();
            AnimRowRef x = AnimRowRef.OfProperty("layer1", 11, "x");
            AnimRowRef group = AnimRowRef.OfGroup("layer1", 11, "Transform");
            AnimRowRef section = AnimRowRef.OfSection("layer0");
            ed.SelectRows(new[] { x, group, section }, false);
            AnimTrack track = AnimTrackEditing.GetOrAdd(host.Docs[1], 11, "x", AnimValueKind.Number, false);
            track.SetKey(0, AnimValue.Of(1));
            track.SetKey(1, AnimValue.Of(2));
            ed.SelectKeys(new[] { new AnimTrackEditing.KeyRef(host.Docs[1], track, track.Keys[1]) }, false);

            // the containers are reordered and replaced by copies (an undo): the rows and the keys are found by the ids
            host.Docs = new List<Container> { host.Docs[1].Clone(), host.Docs[0].Clone() };
            ed.ValidateSelectedRows();
            ed.ValidateSelectedKeys();
            Assert.Equal(new[] { x, group, section }, ed.SelectedRows);
            AnimTrackEditing.KeyRef key = Assert.Single(ed.SelectedKeys);
            Assert.Same(host.Docs[0], key.Container);
            Assert.Same(host.Docs[0].List[0].Keys[1], key.Key);

            // a property, a group, a target, a container that are gone
            host.Docs[0].Targets[0].Props.RemoveAll(p => p.Path == "x");
            ed.ValidateSelectedRows();
            Assert.Equal(new[] { group, section }, ed.SelectedRows);
            host.Docs[0].Targets[0].Props.RemoveAll(p => p.Group.StartsWith("Transform"));
            ed.ValidateSelectedRows();
            Assert.Equal(new[] { section }, ed.SelectedRows);
            host.Docs.RemoveAt(1);
            int version = ed.RowSelectionVersion;
            ed.ValidateSelectedRows();
            Assert.Empty(ed.SelectedRows);
            Assert.NotEqual(version, ed.RowSelectionVersion);
        }

        [Fact]
        public void SyncSelectedTargets_FollowsTheHost()
        {
            (Host host, AnimEditorController ed) = Make();
            AnimRowRef x = AnimRowRef.OfProperty("layer0", 1, "x");
            ed.SelectRows(new[] { x, AnimRowRef.OfTarget("layer0", 2), AnimRowRef.OfSection("layer1") }, false);
            int selections = host.Selections;

            // the target 1 stays (its property row stays), 2 is no longer selected, 12 is new: its target row
            ed.SyncSelectedTargets(new[] { ("layer0", 1), ("layer1", 12) });
            Assert.Equal(new[] { x, AnimRowRef.OfSection("layer1"), AnimRowRef.OfTarget("layer1", 12) }, ed.SelectedRows);
            Assert.Equal(selections, host.Selections);          // the host made the change
            ed.SyncSelectedTargets(Array.Empty<(string, int)>());
            Assert.Equal(new[] { AnimRowRef.OfSection("layer1") }, ed.SelectedRows);
        }
    }
}
