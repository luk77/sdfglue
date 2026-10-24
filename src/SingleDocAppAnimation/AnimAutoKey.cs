//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // Auto-key: an edit of an animated property, in any view of the host, is written as a key of
    // its track at the playhead. The host calls it at the end of an edit with the values before and after it, when
    // AnimEditorController.AutoKey is on.
    public static class AnimAutoKey
    {
        // true: the value changed, the track got a key at t
        public static bool Commit(AnimTrack track, AnimValue before, AnimValue after, double t)
        {
            if (AnimValue.Same(after, before, track.Kind))
                return false;
            track.SetKey(t, after);
            return true;
        }

        // the changed properties of a target that have tracks in the container (before: the values by path)
        public static bool Commit(IAnimTrackContainer container, IAnimTarget target, IReadOnlyDictionary<string, AnimValue> before, double t)
        {
            bool changed = false;
            foreach (AnimTrack track in container.TracksOf(target.Id).ToList())
            {
                IAnimProperty? p = target.FindProperty(track.Property);
                if (p != null && before.TryGetValue(track.Property, out AnimValue was))
                    changed |= Commit(track, was, p.Get(), t);
            }
            return changed;
        }

        // the values of the animated properties of a target (what Commit compares after the edit)
        public static Dictionary<string, AnimValue> Snapshot(IAnimTrackContainer container, IAnimTarget target)
        {
            Dictionary<string, AnimValue> values = new Dictionary<string, AnimValue>();
            foreach (AnimTrack track in container.TracksOf(target.Id))
            {
                IAnimProperty? p = target.FindProperty(track.Property);
                if (p != null)
                    values[track.Property] = p.Get();
            }
            return values;
        }
    }
}
