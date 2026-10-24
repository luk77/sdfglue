//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // How a property is shown and edited (the timeline, the key buttons)
    public sealed class AnimPropertyHints
    {
        public static readonly AnimPropertyHints    Default     = new AnimPropertyHints();

        public double           DragStep    = 0.5;      // the change of the value per px of a drag
        public string[]?        EnumNames;              // Enum (and discrete numbers): the names of the values 0, 1..
        public double?          Min, Max;
        public int              Decimals    = 3;        // shown
    }

    // One animatable property (one channel) of a target: where it is in the tree of the timeline, what it is and how
    // it is read and written. The host makes them (a descriptor bound to its object).
    public interface IAnimProperty
    {
        string              Path        { get; }    // the key of the property in the tracks and in the files
        string              Group       { get; }    // the tree above it, '/'-separated ("Transform/Position")
        string              Label       { get; }
        AnimValueKind       Kind        { get; }
        bool                Discrete    { get; }    // never interpolated
        int                 Order       { get; }    // the order of the application of the tracks (lower first)
        AnimPropertyHints   Hints       { get; }

        AnimValue Get();
        void Set(AnimValue value);
    }

    // An object with animatable properties (e.g. an object of a scene). Id: stable in time,
    // the tracks find the target by it (0: the target cannot be animated).
    public interface IAnimTarget
    {
        int                         Id          { get; }
        string                      Label       { get; }
        IEnumerable<IAnimProperty>  Properties  { get; }    // all of them, in the order of the tree
        IEnumerable<IAnimTarget>    Children    { get; }    // the targets below it in the tree

        // a property by its path (also the paths the host makes on demand), null: the target has no such property
        IAnimProperty? FindProperty(string path);

        // The path a track of the property is stored under: the host may give a property a stable path when it is first
        // keyed (called in an edit; it may change the model).
        string ResolvePath(string path)
        {
            return path;
        }

        // The label of a group of the properties (a path of IAnimProperty.Group): by default its last segment; a host can
        // give the groups stable paths (ids) and show them by their positions ("Node 3").
        string GroupLabel(string group)
        {
            int slash = group.LastIndexOf('/');
            return slash >= 0 ? group.Substring(slash + 1) : group;
        }

        // Why the property cannot be animated now (null: it can): its tracks are kept, but no keys are added to it and
        // its value is not edited in the timeline (e.g. the host does not use the value at the moment); shown as a tooltip.
        string? DisabledReason(string path)
        {
            return null;
        }

        // The property is static again with the value (its track is removed): by default the property itself, a host
        // can write all the copies of the target.
        void SetStatic(string path, AnimValue value)
        {
            FindProperty(path)?.Set(value);
        }
    }

    // The tracks of a container evaluated into their targets: the properties are found once (by the ids of the targets
    // and the paths) and reused until Invalidate (or until the number of the tracks changes); Apply only evaluates the
    // tracks and sets the values, in the Order of the properties.
    public sealed class AnimBindings
    {
        private readonly IAnimTrackContainer                    container_;
        private readonly Func<int, IAnimTarget?>                resolve_;
        private readonly List<(AnimTrack Track, IAnimProperty Property)> bound_ = new List<(AnimTrack, IAnimProperty)>();
        private bool                                            valid_;
        private int                                             trackCount_;

        public AnimBindings(IAnimTrackContainer container, Func<int, IAnimTarget?> resolve)
        {
            container_  = container;
            resolve_    = resolve;
        }

        // the properties found (the tracks of missing targets or properties are skipped)
        public int Count { get { Bind(); return bound_.Count; } }

        // a track, a target or a property was added or removed: they are found again
        public void Invalidate()
        {
            valid_ = false;
        }

        public void Apply(double t)
        {
            Apply(_ => t);
        }

        // every property at its own time (e.g. the properties the host does not use now at the start)
        public void Apply(Func<IAnimProperty, double> timeOf)
        {
            Bind();
            foreach ((AnimTrack track, IAnimProperty property) in bound_)
            {
                if (track.Keys.Count > 0)
                    property.Set(track.ValueAt(timeOf(property)));
            }
        }

        private void Bind()
        {
            if (valid_ && trackCount_ == container_.Tracks.Count)
                return;
            bound_.Clear();
            Dictionary<int, IAnimTarget?> targets = new Dictionary<int, IAnimTarget?>();
            foreach (AnimTrack track in container_.Tracks)
            {
                if (!targets.TryGetValue(track.TargetId, out IAnimTarget? target))
                    targets[track.TargetId] = target = resolve_(track.TargetId);
                IAnimProperty? p = target?.FindProperty(track.Property);
                if (p != null)
                    bound_.Add((track, p));
            }
            // a stable sort: the tracks of the same order keep their order
            List<(AnimTrack, IAnimProperty)> sorted = bound_.OrderBy(b => b.Property.Order).ToList();
            bound_.Clear();
            bound_.AddRange(sorted);
            trackCount_ = container_.Tracks.Count;
            valid_ = true;
        }
    }
}
