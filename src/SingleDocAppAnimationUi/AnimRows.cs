//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation.Ui
{
    // A top row of the timeline: one container of tracks with its targets
    public sealed class AnimSection
    {
        public string                       Id          = "";       // stable (the state of the expansion)
        public string                       Label       = "";
        public IAnimTrackContainer          Container   = null!;
        public IReadOnlyList<IAnimTarget>   Targets     = Array.Empty<IAnimTarget>();     // from the top of the tree
        public bool                         Animated;               // the host animates it otherwise too (kept by the "animated" filter)
        public bool                         Active;                 // the active one (kept by the "selection" filter, highlighted)
        public bool                         Dim;                    // the label is dimmed (a hidden one)
        public object?                      Tag;                    // the host's own data
    }

    // What the tree of the timeline shows, given by the host every frame
    public interface IAnimRowSource
    {
        IReadOnlyList<AnimSection>  Sections    { get; }    // from the top

        // the target is selected in the host (the "selection" filter, highlighted)
        bool IsSelected(IAnimTarget target);
    }

    // A group of properties of a target in the tree (IAnimProperty.Group)
    public sealed class AnimGroup
    {
        public string               Path        = "";           // from the target, e.g. "Transform/Position"
        public string               Label       = "";
        public List<object>         Items       = new List<object>();       // AnimGroup and IAnimProperty, in the order of appearance

        public IEnumerable<IAnimProperty> AllProperties()
        {
            foreach (object o in Items)
            {
                if (o is IAnimProperty p)
                {
                    yield return p;
                }
                else
                {
                    foreach (IAnimProperty q in ((AnimGroup)o).AllProperties())
                        yield return q;
                }
            }
        }

        // the groups of the properties: the '/'-separated paths of IAnimProperty.Group (label: of a group path, by
        // default its last segment)
        public static AnimGroup Build(IEnumerable<IAnimProperty> properties, Func<string, string>? label = null)
        {
            AnimGroup root = new AnimGroup();
            foreach (IAnimProperty p in properties)
                root.GroupFor(p.Group, label).Items.Add(p);
            return root;
        }

        // the groups of the properties of a target (IAnimTarget.GroupLabel)
        public static AnimGroup Build(IAnimTarget target)
        {
            return Build(target.Properties, target.GroupLabel);
        }

        private AnimGroup GroupFor(string path, Func<string, string>? label)
        {
            AnimGroup g = this;
            string full = "";
            foreach (string segment in path.Split('/', StringSplitOptions.RemoveEmptyEntries))
            {
                full = full.Length == 0 ? segment : full + "/" + segment;
                AnimGroup? child = g.Items.OfType<AnimGroup>().FirstOrDefault(c => c.Path == full);
                if (child == null)
                {
                    child = new AnimGroup { Path = full, Label = label?.Invoke(full) ?? segment };
                    g.Items.Add(child);
                }
                g = child;
            }
            return g;
        }

        public bool HasTrack(AnimTrack track)
        {
            foreach (object o in Items)
            {
                if (o is IAnimProperty p ? p.Path == track.Property : ((AnimGroup)o).HasTrack(track))
                    return true;
            }
            return false;
        }
    }

    // A row of the tree: a section, a target, a group of properties or a property
    public sealed class AnimRow
    {
        public AnimRowKind      Kind;
        public int              Depth;
        public string           Id          = "";           // the state of the expansion
        public string           Label       = "";
        public AnimSection      Section     = null!;
        public IAnimTarget?     Target;                     // the target of the row or of its properties
        public AnimGroup?       Group;
        public IAnimProperty?   Property;
        public AnimTrack?       Track;                      // property rows
        public bool             Expandable;
        public bool             Expanded;

        private List<string>?       paths_;
        private List<AnimTrack>?    tracks_;
        private string?             disabled_;
        private bool                disabledKnown_;

        public IAnimTrackContainer Container { get { return Section.Container; } }

        // the row by stable ids (the selection of the rows)
        public AnimRowRef Ref
        {
            get
            {
                switch (Kind)
                {
                    case AnimRowKind.Target:    return AnimRowRef.OfTarget(Container.Id, Target!.Id);
                    case AnimRowKind.Group:     return AnimRowRef.OfGroup(Container.Id, Target!.Id, Group!.Path);
                    case AnimRowKind.Property:  return AnimRowRef.OfProperty(Container.Id, Target!.Id, Property!.Path);
                    default:                    return AnimRowRef.OfSection(Container.Id);
                }
            }
        }

        // Why the row cannot be keyed or edited (IAnimTarget.DisabledReason), null: it can. A group or a target is
        // disabled when all of its properties are.
        public string? Disabled
        {
            get
            {
                if (!disabledKnown_)
                {
                    disabledKnown_ = true;
                    IEnumerable<string> paths = Kind == AnimRowKind.Target ? Target!.Properties.Select(p => p.Path) : KeyPaths;
                    if (Target != null && Kind != AnimRowKind.Section)
                    {
                        foreach (string path in paths)
                        {
                            string? reason = Target.DisabledReason(path);
                            if (reason == null)
                            {
                                disabled_ = null;
                                break;
                            }
                            disabled_ ??= reason;
                        }
                    }
                }
                return disabled_;
            }
        }

        // the key button: properties and groups
        public bool HasKeyButton { get { return Kind == AnimRowKind.Property || Kind == AnimRowKind.Group; } }

        // the paths a key button of the row keys
        public List<string> KeyPaths
        {
            get
            {
                if (paths_ == null)
                {
                    paths_ = Kind == AnimRowKind.Property ? new List<string> { Property!.Path }
                           : Group != null ? Group.AllProperties().Select(p => p.Path).ToList() : new List<string>();
                }
                return paths_;
            }
        }

        // the tracks of the properties under this row
        public List<AnimTrack> Tracks
        {
            get
            {
                if (tracks_ != null)
                    return tracks_;
                switch (Kind)
                {
                    case AnimRowKind.Property:
                        tracks_ = Track != null ? new List<AnimTrack> { Track } : new List<AnimTrack>();
                        break;
                    case AnimRowKind.Group:
                    {
                        HashSet<string> paths = new HashSet<string>(KeyPaths);
                        tracks_ = Container.TracksOf(Target!.Id).Where(t => paths.Contains(t.Property)).ToList();
                        break;
                    }
                    case AnimRowKind.Target:
                        tracks_ = Container.TracksOf(Target!.Id).ToList();
                        break;
                    default:
                        tracks_ = Container.Tracks.ToList();
                        break;
                }
                return tracks_;
            }
        }

        // the keys of the tracks under the row
        public IEnumerable<AnimTrackEditing.KeyRef> Keys()
        {
            foreach (AnimTrack t in Tracks)
            {
                foreach (AnimKey k in t.Keys)
                    yield return new AnimTrackEditing.KeyRef(Container, t, k);
            }
        }

        public IEnumerable<AnimTrackEditing.KeyRef> KeysAt(double time)
        {
            return Keys().Where(k => Math.Abs(k.Key.Time - time) < AnimTime.KeyTolerance);
        }
    }
}
