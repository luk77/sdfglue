//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // How the edits of the animated properties become keys
    public enum AnimKeyMode
    {
        AlwaysAutoKey,      // every edit of an animated property is a key at the playhead
        RecordToggle,       // only while recording (the record button of the timeline); otherwise the edits are dropped
    }

    // The configuration of the animation editor, given by the host when it is made
    public sealed class AnimEditorOptions
    {
        public AnimKeyMode      KeyMode             = AnimKeyMode.RecordToggle;
        public bool             ShowRecordButton    = true;     // RecordToggle: the timeline shows the record button
        public string           MarkerName          = "marker"; // the name of a new marker ("marker", "marker_2"..)
    }

    // The state of the key button of a property (or of a group): not animated, animated, a key at the playhead
    public enum AnimKeyState
    {
        None,
        Animated,
        OnKey,
    }

    // What the animation editor needs from its host: the time, the edited tracks and markers, the undo edits. The
    // module does not implement undo: every command is BeginEdit .. EndEdit of the host (e.g. a snapshot of the document).
    public interface IAnimEditorHost
    {
        AnimTransport                       Transport   { get; }
        IEnumerable<IAnimTrackContainer>    Containers  { get; }    // the edited ones
        List<AnimMarker>                    Markers     { get; }
        bool                                CanKey      { get; }    // the tracks can be edited
        bool                                IsEditing   { get; }

        void BeginEdit();
        void EndEdit();

        // a widget is about to change the model: the host starts an edit (it ends it when no widget is active)
        void Modify();

        // the model changed during an edit (redraw)
        void Changed();

        // the animated values were changed directly (the keys were moved): the views of them are built again
        void Invalidate();

        // the target of a container by its id, null: none (the pasted keys, a property made static)
        IAnimTarget? FindTarget(IAnimTrackContainer container, int targetId);

        // keys or a marker of the timeline were selected: the host deselects what it selects in the timeline
        void TimelineSelected();

        // the selection of the rows of the timeline was changed by the user (AnimEditorController.SelectedRows): the host
        // selects their targets in its views
        void RowsSelected()
        {
        }
    }

    // The commands of the animation editor (the timeline window, the key buttons): the selection and the clipboard of
    // the keys, the selection of the rows of the timeline (properties, groups, targets, containers), keying,
    // interpolation, the drag of the keys, the markers. Each command is one undo edit of the host.
    public sealed class AnimEditorController
    {
        // a key copied to the clipboard
        private sealed class CopiedKey
        {
            public IAnimTrackContainer  Container   = null!;
            public int                  TargetId;
            public string               Property    = "";
            public AnimValueKind        Kind;
            public bool                 Discrete;
            public AnimColorSpace       ColorSpace;
            public double               Offset;             // s from the first copied key
            public AnimKey              Key         = null!;
        }

        public readonly IAnimEditorHost                 Host;
        public readonly AnimEditorOptions               Options;
        public readonly List<AnimTrackEditing.KeyRef>   SelectedKeys    = new List<AnimTrackEditing.KeyRef>();
        public readonly List<AnimRowRef>                SelectedRows    = new List<AnimRowRef>();     // in the order of selection
        public int                                      SelectedMarker  = -1;
        public bool                                     Recording;      // AnimKeyMode.RecordToggle

        private List<CopiedKey>                         clipboard_      = new List<CopiedKey>();
        private List<double>                            dragTimes_      = new List<double>();

        public AnimEditorController(IAnimEditorHost host, AnimEditorOptions options)
        {
            Host    = host;
            Options = options;
        }

        public AnimTransport Transport { get { return Host.Transport; } }

        // the edits of the animated properties are keys now
        public bool AutoKey { get { return Options.KeyMode == AnimKeyMode.AlwaysAutoKey || Recording; } }

        // a command in one edit (inside a running edit it stays a part of it)
        public void Edit(Action change)
        {
            bool outer = Host.IsEditing;
            Host.BeginEdit();
            change();
            if (!outer)
                Host.EndEdit();
            else
                Host.Changed();
        }

        // a new document: nothing selected
        public void Reset()
        {
            SelectedKeys.Clear();
            SelectedMarker = -1;
            if (SelectedRows.Count > 0)
            {
                SelectedRows.Clear();
                RowSelectionVersion++;
            }
        }

        private IAnimTrackContainer? FindContainer(string id)
        {
            return id.Length > 0 ? Host.Containers.FirstOrDefault(c => c.Id == id) : null;
        }

        // ---------------------------------------------------------------- the selection of the keys

        // The keys that are still in the model (an edit may have removed some). A key of a container that was replaced
        // (undo, redo) is found again in the container of the same id: the track of its property, the key at its time.
        public void ValidateSelectedKeys()
        {
            HashSet<IAnimTrackContainer> containers = new HashSet<IAnimTrackContainer>(Host.Containers);
            for (int i = SelectedKeys.Count - 1; i >= 0; i--)
            {
                AnimTrackEditing.KeyRef k = SelectedKeys[i];
                if (containers.Contains(k.Container) && k.Container.Tracks.Contains(k.Track) && k.Track.Keys.Contains(k.Key))
                    continue;
                IAnimTrackContainer? c = containers.Contains(k.Container) ? null : FindContainer(k.Container.Id);
                AnimTrack? track = c?.FindTrack(k.Track.TargetId, k.Track.Property);
                AnimKey? key = track?.KeyAt(k.Key.Time);
                if (c != null && track != null && key != null && !IsKeySelected(key))
                    SelectedKeys[i] = new AnimTrackEditing.KeyRef(c, track, key);
                else
                    SelectedKeys.RemoveAt(i);
            }
        }

        public bool IsKeySelected(AnimKey key)
        {
            return SelectedKeys.Any(k => ReferenceEquals(k.Key, key));
        }

        public void SelectKeys(IEnumerable<AnimTrackEditing.KeyRef> keys, bool add)
        {
            if (!add)
                SelectedKeys.Clear();
            foreach (AnimTrackEditing.KeyRef k in keys)
            {
                if (!SelectedKeys.Contains(k))
                    SelectedKeys.Add(k);
            }
            if (SelectedKeys.Count > 0)
            {
                SelectedMarker = -1;
                Host.TimelineSelected();
            }
        }

        public void ToggleKeySelection(AnimTrackEditing.KeyRef key)
        {
            if (!SelectedKeys.Remove(key))
                SelectedKeys.Add(key);
        }

        // ---------------------------------------------------------------- the selection of the rows

        // changes with every change of SelectedRows (the views that follow it, e.g. a property panel that scrolls to it)
        public int RowSelectionVersion { get; private set; }

        public bool IsRowSelected(AnimRowRef row)
        {
            return SelectedRows.Contains(row);
        }

        // rows of targets, groups or properties are selected (the keying of the selection; sections alone: none)
        public bool HasPropertyRowsSelected { get { return SelectedRows.Any(r => r.Kind != AnimRowKind.Section); } }

        // the rows are selected (add: to the selected ones); the host is told (IAnimEditorHost.RowsSelected)
        public void SelectRows(IEnumerable<AnimRowRef> rows, bool add)
        {
            List<AnimRowRef> before = SelectedRows.ToList();
            if (!add)
                SelectedRows.Clear();
            foreach (AnimRowRef r in rows)
            {
                if (!SelectedRows.Contains(r))
                    SelectedRows.Add(r);
            }
            RowsChanged(before);
        }

        public void ToggleRow(AnimRowRef row)
        {
            List<AnimRowRef> before = SelectedRows.ToList();
            if (!SelectedRows.Remove(row))
                SelectedRows.Add(row);
            RowsChanged(before);
        }

        public void ClearRows()
        {
            SelectRows(Enumerable.Empty<AnimRowRef>(), false);
        }

        private void RowsChanged(List<AnimRowRef> before)
        {
            if (before.SequenceEqual(SelectedRows))
                return;
            RowSelectionVersion++;
            Host.RowsSelected();
        }

        // The host changed the selection of its targets (e.g. a click in a view): the rows of the targets that are no
        // longer selected are deselected, a selected target without a selected row gets the row of the target (not of its
        // properties). The host is not told (it made the change).
        public void SyncSelectedTargets(IEnumerable<(string ContainerId, int TargetId)> targets)
        {
            HashSet<(string, int)> set = new HashSet<(string, int)>(targets);
            List<AnimRowRef> before = SelectedRows.ToList();
            SelectedRows.RemoveAll(r => r.Kind != AnimRowKind.Section && !set.Contains((r.ContainerId, r.TargetId)));
            foreach ((string c, int t) in set)
            {
                if (!SelectedRows.Any(r => r.ContainerId == c && r.TargetId == t))
                    SelectedRows.Add(AnimRowRef.OfTarget(c, t));
            }
            if (!before.SequenceEqual(SelectedRows))
                RowSelectionVersion++;
        }

        // the rows whose container, target, group or property is still in the model (an edit, undo or redo may have
        // removed some)
        public void ValidateSelectedRows()
        {
            if (SelectedRows.Count > 0 && SelectedRows.RemoveAll(r => !Exists(r)) > 0)
                RowSelectionVersion++;
        }

        private bool Exists(AnimRowRef r)
        {
            IAnimTrackContainer? c = FindContainer(r.ContainerId);
            if (c == null)
                return false;
            if (r.Kind == AnimRowKind.Section)
                return true;
            IAnimTarget? target = Host.FindTarget(c, r.TargetId);
            switch (r.Kind)
            {
                case AnimRowKind.Target:    return target != null;
                case AnimRowKind.Group:     return target != null && target.Properties.Any(p => AnimRowRef.IsInGroup(p.Group, r.Path));
                default:                    return target?.FindProperty(r.Path) != null;
            }
        }

        // A property is selected in the timeline: its row or a group above it (or its target: viaTarget). The groups are
        // matched by the group of the property (IAnimTarget.FindProperty).
        public bool IsPropertySelected(IAnimTrackContainer container, int targetId, string path, bool viaTarget = true)
        {
            if (SelectedRows.Count == 0 || targetId == 0)
                return false;
            string group = "";
            if (SelectedRows.Any(r => r.Kind == AnimRowKind.Group && r.TargetId == targetId && r.ContainerId == container.Id))
                group = Host.FindTarget(container, targetId)?.FindProperty(path)?.Group ?? "";
            return SelectedRows.Any(r => r.Covers(container.Id, targetId, path, group, viaTarget));
        }

        // The properties under the selected rows, each once: a property, the properties of a group (and of its subgroups),
        // all the properties of a target and of its children. Sections add nothing.
        public List<(IAnimTrackContainer Container, IAnimTarget Target, string Path)> SelectedProperties()
        {
            List<(IAnimTrackContainer, IAnimTarget, string)> list = new List<(IAnimTrackContainer, IAnimTarget, string)>();
            HashSet<(string, int, string)> seen = new HashSet<(string, int, string)>();
            void Add(IAnimTrackContainer c, IAnimTarget t, string path)
            {
                if (seen.Add((c.Id, t.Id, path)))
                    list.Add((c, t, path));
            }
            void AddAll(IAnimTrackContainer c, IAnimTarget t)
            {
                foreach (IAnimProperty p in t.Properties)
                    Add(c, t, p.Path);
                foreach (IAnimTarget child in t.Children)
                {
                    if (child.Id != 0)
                        AddAll(c, child);
                }
            }

            foreach (AnimRowRef r in SelectedRows)
            {
                if (r.Kind == AnimRowKind.Section)
                    continue;
                IAnimTrackContainer? c = FindContainer(r.ContainerId);
                IAnimTarget? t = c != null ? Host.FindTarget(c, r.TargetId) : null;
                if (c == null || t == null || t.Id == 0)
                    continue;
                if (r.Kind == AnimRowKind.Target)
                {
                    AddAll(c, t);
                }
                else if (r.Kind == AnimRowKind.Group)
                {
                    foreach (IAnimProperty p in t.Properties.Where(p => AnimRowRef.IsInGroup(p.Group, r.Path)))
                        Add(c, t, p.Path);
                }
                else if (t.FindProperty(r.Path) != null)
                {
                    Add(c, t, r.Path);
                }
            }
            return list;
        }

        // The Keyframe of the selected rows: a key at the playhead in every property under them (SelectedProperties; a key
        // that is there gets the current value), one edit. A property the host gives a stable path when it is keyed
        // (IAnimTarget.ResolvePath) stays selected under that path. Returns the number of the properties keyed.
        public int KeySelectedRows()
        {
            if (!Host.CanKey)
                return 0;
            List<(IAnimTrackContainer Container, IAnimTarget Target, string Path)> props = SelectedProperties();
            if (props.Count == 0)
                return 0;
            int keyed = 0;
            Edit(delegate
            {
                double t = Transport.KeyTime;
                foreach ((IAnimTrackContainer c, IAnimTarget target, string path) in props)
                {
                    if (target.DisabledReason(path) != null)
                        continue;
                    string resolved = target.ResolvePath(path);
                    int i = SelectedRows.IndexOf(AnimRowRef.OfProperty(c.Id, target.Id, path));
                    if (resolved != path && i >= 0)
                        SelectedRows[i] = AnimRowRef.OfProperty(c.Id, target.Id, resolved);
                    if (AnimTrackEditing.AddKey(c, target, resolved, t) != null)
                        keyed++;
                }
            });
            return keyed;
        }

        // ---------------------------------------------------------------- keying

        // the key button of the properties (paths) of a target at t
        public static AnimKeyState StateOf(IAnimTrackContainer container, int targetId, IReadOnlyCollection<string> paths, double t)
        {
            List<AnimTrack?> tracks = paths.Select(p => container.FindTrack(targetId, p)).ToList();
            if (tracks.All(tr => tr == null))
                return AnimKeyState.None;
            return paths.Count > 0 && tracks.All(tr => tr?.KeyAt(t) != null) ? AnimKeyState.OnKey : AnimKeyState.Animated;
        }

        public AnimKeyState StateOf(IAnimTrackContainer container, int targetId, IReadOnlyCollection<string> paths)
        {
            return StateOf(container, targetId, paths, Transport.KeyTime);
        }

        // The key button of properties of a target (a property, or all of a group): a property that is not animated gets
        // a track with a key with its current value; when all have a key at the playhead, the keys are removed (the last
        // key of a track makes the property static again); otherwise the ones without a key there get one.
        public void ToggleKeys(IAnimTrackContainer container, IAnimTarget target, IEnumerable<string> paths)
        {
            if (!Host.CanKey || target.Id == 0)
                return;
            List<string> list = paths.Where(p => target.DisabledReason(p) == null).ToList();
            if (list.Count == 0)
                return;
            Edit(delegate
            {
                double t = Transport.KeyTime;
                List<string> resolved = list.Select(target.ResolvePath).ToList();
                bool all = resolved.All(p => container.FindTrack(target.Id, p)?.KeyAt(t) != null);
                foreach (string path in resolved)
                {
                    AnimTrack? track = container.FindTrack(target.Id, path);
                    AnimKey? key = track?.KeyAt(t);
                    if (all && track != null && key != null)
                    {
                        SelectedKeys.RemoveAll(k => ReferenceEquals(k.Key, key));
                        AnimTrackEditing.DeleteKey(container, track, key);
                    }
                    else if (key == null)
                    {
                        AnimTrackEditing.AddKey(container, target, path, t);
                    }
                }
            });
        }

        // the properties are static again with their values at the playhead (all their keys are removed)
        public void RemoveAnimation(IAnimTrackContainer container, IEnumerable<AnimTrack> tracks)
        {
            List<AnimTrack> list = tracks.ToList();
            if (list.Count == 0)
                return;
            Edit(delegate
            {
                foreach (AnimTrack track in list)
                    AnimTrackEditing.MakeStatic(container, track, Transport.Playhead, Host.FindTarget(container, track.TargetId));
                ValidateSelectedKeys();
            });
        }

        public void SetColorSpace(IEnumerable<AnimTrack> tracks, AnimColorSpace space)
        {
            List<AnimTrack> list = tracks.Where(t => t.Kind == AnimValueKind.Color).ToList();
            if (list.Count > 0)
                Edit(delegate { foreach (AnimTrack t in list) t.ColorSpace = space; });
        }

        // holds or interpolates the spans after the keys
        public void SetKeysInterpolation(IEnumerable<AnimTrackEditing.KeyRef> keys, AnimInterpolation interpolation)
        {
            List<AnimTrackEditing.KeyRef> list = keys.ToList();
            if (list.Count > 0)
                Edit(delegate { foreach (AnimTrackEditing.KeyRef k in list) k.Key.Interpolation = interpolation; });
        }

        public void DeleteSelectedKeys()
        {
            ValidateSelectedKeys();
            if (SelectedKeys.Count == 0)
                return;
            List<AnimTrackEditing.KeyRef> list = SelectedKeys.ToList();
            Edit(delegate
            {
                foreach (AnimTrackEditing.KeyRef k in list)
                    AnimTrackEditing.DeleteKey(k.Container, k.Track, k.Key);
                SelectedKeys.Clear();
            });
        }

        // ---------------------------------------------------------------- moving keys

        // a drag of the selected keys: BeginKeyDrag, DragKeys (the offset from the start), EndKeyDrag
        public void BeginKeyDrag()
        {
            ValidateSelectedKeys();
            Host.BeginEdit();
            dragTimes_ = SelectedKeys.Select(k => k.Key.Time).ToList();
        }

        public void DragKeys(double dt)
        {
            if (!Transport.Timebase.IsAvailable || SelectedKeys.Count != dragTimes_.Count)
                return;
            AnimTrackEditing.MoveKeys(SelectedKeys, dragTimes_, dt, Transport.Timebase.Snap);
            Host.Invalidate();
            Host.Changed();
        }

        public void EndKeyDrag()
        {
            Host.EndEdit();
            ValidateSelectedKeys();
        }

        // ---------------------------------------------------------------- clipboard

        public bool HasCopiedKeys { get { return clipboard_.Count > 0; } }

        public void CopyKeys()
        {
            ValidateSelectedKeys();
            if (SelectedKeys.Count == 0)
                return;
            double first = SelectedKeys.Min(k => k.Key.Time);
            clipboard_ = SelectedKeys.Select(k => new CopiedKey
            {
                Container = k.Container, TargetId = k.Track.TargetId, Property = k.Track.Property, Kind = k.Track.Kind, Discrete = k.Track.Discrete,
                ColorSpace = k.Track.ColorSpace, Offset = k.Key.Time - first, Key = k.Key.Clone(),
            }).ToList();
        }

        // the copied keys at the playhead, into the tracks they were copied from (made again when they are gone)
        public void PasteKeys()
        {
            if (!Transport.Timebase.IsAvailable || !Host.CanKey || clipboard_.Count == 0)
                return;
            HashSet<IAnimTrackContainer> containers = new HashSet<IAnimTrackContainer>(Host.Containers);
            List<AnimTrackEditing.KeyRef> pasted = new List<AnimTrackEditing.KeyRef>();
            Edit(delegate
            {
                double t0 = Transport.KeyTime;
                foreach (CopiedKey c in clipboard_.Where(c => containers.Contains(c.Container)))
                {
                    if (Host.FindTarget(c.Container, c.TargetId) == null)
                        continue;
                    bool created = c.Container.FindTrack(c.TargetId, c.Property) == null;
                    AnimTrack track = AnimTrackEditing.GetOrAdd(c.Container, c.TargetId, c.Property, c.Kind, c.Discrete);
                    if (created)
                        track.ColorSpace = c.ColorSpace;
                    AnimKey key = track.SetKey(Transport.Timebase.Snap(t0 + c.Offset), c.Key.Value);
                    key.Interpolation = c.Key.Interpolation;
                    pasted.Add(new AnimTrackEditing.KeyRef(c.Container, track, key));
                }
                SelectedKeys.Clear();
            });
            SelectKeys(pasted, false);
        }

        // ---------------------------------------------------------------- markers

        public void AddMarker(double t)
        {
            if (!Transport.Timebase.IsAvailable)
                return;
            Edit(delegate
            {
                AnimMarker marker = AnimTrackEditing.AddMarker(Host.Markers, Transport.Timebase.Snap(t), Options.MarkerName);
                SelectedMarker = Host.Markers.IndexOf(marker);
                Host.TimelineSelected();
            });
        }

        // false: the name is empty or used by another marker
        public bool RenameMarker(int index, string name)
        {
            List<AnimMarker> markers = Host.Markers;
            name = name.Trim();
            if (index < 0 || index >= markers.Count || name.Length == 0 || markers.Any(m => m.Name == name))
                return false;
            Edit(delegate { markers[index].Name = name; });
            return true;
        }

        public void DeleteMarker(int index)
        {
            List<AnimMarker> markers = Host.Markers;
            if (index < 0 || index >= markers.Count)
                return;
            Edit(delegate
            {
                markers.RemoveAt(index);
                SelectedMarker = -1;
            });
        }

        // call in an edit (a drag)
        public void MoveMarker(int index, double t)
        {
            List<AnimMarker> markers = Host.Markers;
            if (index < 0 || index >= markers.Count)
                return;
            markers[index].Time = Transport.Timebase.Snap(t);
            Host.Changed();
        }

        // call in an edit (a drag of the end)
        public void SetLength(double t)
        {
            if (!Transport.Timebase.IsAvailable)
                return;
            Transport.Timebase.SetLength(t);
            Host.Changed();
        }
    }
}
