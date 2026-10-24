//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // The editing of the tracks: keys, tracks, the time, the markers (the model only; an editor wraps the calls in its
    // undo edits)
    public static class AnimTrackEditing
    {
        // a key of a track (the references stay valid while the model is not replaced: undo, redo)
        public readonly struct KeyRef : IEquatable<KeyRef>
        {
            public readonly IAnimTrackContainer Container;
            public readonly AnimTrack           Track;
            public readonly AnimKey             Key;

            public KeyRef(IAnimTrackContainer container, AnimTrack track, AnimKey key)
            {
                Container = container; Track = track; Key = key;
            }

            public bool Equals(KeyRef o) { return ReferenceEquals(Key, o.Key); }
            public override bool Equals(object? obj) { return obj is KeyRef k && Equals(k); }
            public override int GetHashCode() { return Key.GetHashCode(); }
        }

        // ---- keys

        public static AnimTrack GetOrAdd(IAnimTrackContainer container, int targetId, string path, AnimValueKind kind, bool discrete)
        {
            AnimTrack? track = container.FindTrack(targetId, path);
            if (track == null)
            {
                track = new AnimTrack { TargetId = targetId, Property = path, Kind = kind, Discrete = discrete };
                container.Tracks.Add(track);
            }
            return track;
        }

        // A key at t with the current value of the property of the target: the track is made when the property was
        // static. null: the target has no such property (or cannot be animated).
        public static AnimKey? AddKey(IAnimTrackContainer container, IAnimTarget target, string path, double t)
        {
            path = target.ResolvePath(path);
            IAnimProperty? p = target.FindProperty(path);
            if (p == null || target.Id == 0)
                return null;
            return GetOrAdd(container, target.Id, path, p.Kind, p.Discrete).SetKey(t, p.Get());
        }

        // the key is removed; the last key of a track removes the track (the property is static again)
        public static void DeleteKey(IAnimTrackContainer container, AnimTrack track, AnimKey key)
        {
            track.Keys.Remove(key);
            if (track.Keys.Count == 0)
                container.Tracks.Remove(track);
        }

        // The property is static again with its value at t (written by the target, when there is one) and the track is
        // removed.
        public static void MakeStatic(IAnimTrackContainer container, AnimTrack track, double t, IAnimTarget? target)
        {
            if (track.Keys.Count > 0 && target != null)
                target.SetStatic(track.Property, track.ValueAt(t));
            container.Tracks.Remove(track);
        }

        // The keys move by dt from the times they had (original: key -> its time): not before 0, on the grid of the time
        // (snap), a key that lands on another key of its track replaces it.
        public static void MoveKeys(IReadOnlyList<KeyRef> keys, IReadOnlyList<double> original, double dt, Func<double, double> snap)
        {
            if (keys.Count == 0)
                return;
            HashSet<AnimKey> moving = new HashSet<AnimKey>(keys.Select(k => k.Key));
            dt = Math.Max(dt, -original.Min());
            for (int i = 0; i < keys.Count; i++)
                keys[i].Key.Time = snap(original[i] + dt);
            foreach (KeyRef k in keys)
                k.Track.Keys.RemoveAll(o => !moving.Contains(o) && Math.Abs(o.Time - k.Key.Time) < AnimTime.KeyTolerance);
            foreach (AnimTrack t in keys.Select(k => k.Track).Distinct())
                t.Sort();
        }

        // the times of the keys of the tracks (sorted, the keys closer than the tolerance counted once)
        public static IEnumerable<double> KeyTimes(IEnumerable<AnimTrack> tracks)
        {
            double? last = null;
            foreach (double t in tracks.SelectMany(tr => tr.Keys).Select(k => k.Time).OrderBy(t => t))
            {
                if (last == null || t - last.Value >= AnimTime.KeyTolerance)
                    yield return t;
                last = t;
            }
        }

        // the time of the last key (0: none)
        public static double LastKeyTime(IEnumerable<AnimTrack> tracks)
        {
            return tracks.SelectMany(tr => tr.Keys).Select(k => k.Time).DefaultIfEmpty(0).Max();
        }

        // the snap of the playhead: the time of the candidates (keys, markers..) nearest to t, not farther than
        // tolerance; null: none is that near
        public static double? Nearest(IEnumerable<double> candidates, double t, double tolerance)
        {
            double? best = null;
            double distance = tolerance;
            foreach (double c in candidates)
            {
                double d = Math.Abs(c - t);
                if (d <= distance)
                {
                    distance = d;
                    best = c;
                }
            }
            return best;
        }

        // ---- the time

        // dt is inserted at t: the later keys move later
        public static void InsertTime(IEnumerable<AnimTrack> tracks, double t, double dt)
        {
            foreach (AnimKey k in tracks.SelectMany(tr => tr.Keys))
            {
                if (k.Time > t + AnimTime.Epsilon)
                    k.Time += dt;
            }
        }

        // The time t .. t + dt is removed: the keys in it are deleted, the later keys move earlier, the tracks without
        // keys are removed
        public static void RemoveTime(IList<AnimTrack> tracks, double t, double dt)
        {
            double eps = AnimTime.KeyTolerance;
            foreach (AnimTrack track in tracks)
            {
                track.Keys.RemoveAll(k => k.Time > t + eps && k.Time <= t + dt + eps);
                foreach (AnimKey k in track.Keys.Where(k => k.Time > t + dt + eps))
                    k.Time -= dt;
            }
            for (int i = tracks.Count - 1; i >= 0; i--)
            {
                if (tracks[i].Keys.Count == 0)
                    tracks.RemoveAt(i);
            }
        }

        // ---- the markers

        // "label", "label_2".. (baseName) at t; the markers stay sorted by time
        public static AnimMarker AddMarker(List<AnimMarker> markers, double t, string baseName)
        {
            string name = baseName;
            for (int i = 2; markers.Any(m => m.Name == name); i++)
                name = baseName + "_" + i;
            AnimMarker marker = new AnimMarker { Name = name, Time = t };
            markers.Add(marker);
            markers.Sort((a, b) => a.Time.CompareTo(b.Time));
            return marker;
        }

        public static void InsertTime(IEnumerable<AnimMarker> markers, double t, double dt)
        {
            foreach (AnimMarker m in markers)
            {
                if (m.Time > t + AnimTime.Epsilon)
                    m.Time += dt;
            }
        }

        // the markers in the removed time move to its start
        public static void RemoveTime(IEnumerable<AnimMarker> markers, double t, double dt)
        {
            double eps = AnimTime.KeyTolerance;
            foreach (AnimMarker m in markers)
            {
                if (m.Time > t + dt + eps)
                    m.Time -= dt;
                else if (m.Time > t + eps)
                    m.Time = t;
            }
        }
    }
}
