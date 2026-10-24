//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // The kinds of the rows of the tree of a timeline
    public enum AnimRowKind
    {
        Section,        // a container of tracks
        Target,
        Group,          // a group of the properties of a target
        Property,
    }

    // A row of the tree of a timeline by stable ids: a container (a section), a target, a group of the properties of a
    // target (Path: the group, IAnimProperty.Group) or a property (Path: its path). The selection of the rows is kept as
    // these, so it survives the rows built again every frame, a reorder of the containers and an undo that replaced the
    // model.
    public readonly struct AnimRowRef : IEquatable<AnimRowRef>
    {
        public readonly AnimRowKind     Kind;
        public readonly string          ContainerId;    // IAnimTrackContainer.Id
        public readonly int             TargetId;       // 0: a section
        public readonly string          Path;           // a group, a property ("": a section, a target)

        private AnimRowRef(AnimRowKind kind, string containerId, int targetId, string path)
        {
            Kind        = kind;
            ContainerId = containerId;
            TargetId    = targetId;
            Path        = path;
        }

        public static AnimRowRef OfSection(string containerId)
        {
            return new AnimRowRef(AnimRowKind.Section, containerId, 0, "");
        }

        public static AnimRowRef OfTarget(string containerId, int targetId)
        {
            return new AnimRowRef(AnimRowKind.Target, containerId, targetId, "");
        }

        public static AnimRowRef OfGroup(string containerId, int targetId, string group)
        {
            return new AnimRowRef(AnimRowKind.Group, containerId, targetId, group);
        }

        public static AnimRowRef OfProperty(string containerId, int targetId, string path)
        {
            return new AnimRowRef(AnimRowKind.Property, containerId, targetId, path);
        }

        // A property of a target (path, in the group) is this row or is under it: the property, its group or a group
        // above it, its target (viaTarget). A section covers nothing (its rows are the keyframes of the host).
        public bool Covers(string containerId, int targetId, string path, string group, bool viaTarget = true)
        {
            if (Kind == AnimRowKind.Section || ContainerId != containerId || TargetId != targetId)
                return false;
            switch (Kind)
            {
                case AnimRowKind.Target:    return viaTarget;
                case AnimRowKind.Group:     return IsInGroup(group, Path);
                default:                    return path == Path;
            }
        }

        // group is the group or one of its subgroups
        public static bool IsInGroup(string group, string of)
        {
            return group == of || (group.Length > of.Length && group[of.Length] == '/' && group.StartsWith(of, StringComparison.Ordinal));
        }

        public bool Equals(AnimRowRef o)
        {
            return Kind == o.Kind && TargetId == o.TargetId && ContainerId == o.ContainerId && Path == o.Path;
        }

        public override bool Equals(object? obj) { return obj is AnimRowRef r && Equals(r); }
        public override int GetHashCode() { return HashCode.Combine(Kind, ContainerId, TargetId, Path); }
        public override string ToString() { return Kind + " " + ContainerId + "/" + TargetId + "/" + Path; }
    }
}
