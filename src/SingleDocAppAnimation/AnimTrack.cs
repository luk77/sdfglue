//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // How the span after a key changes to the next key: interpolated with an easing, or held (a step). The first four
    // are the values of AnimEase.
    public enum AnimInterpolation
    {
        Linear,
        EaseIn,
        EaseOut,
        EaseInOut,
        Hold,
    }

    public static class AnimInterpolations
    {
        public static AnimInterpolation Of(AnimEase ease)
        {
            return (AnimInterpolation)ease;
        }

        // the easing of an interpolated span (Hold: Linear)
        public static AnimEase Ease(this AnimInterpolation i)
        {
            return i == AnimInterpolation.Hold ? AnimEase.Linear : (AnimEase)i;
        }
    }

    // A key of a track: the value from Time on
    public sealed class AnimKey
    {
        public double               Time;               // s
        public AnimValue            Value;
        public AnimInterpolation    Interpolation;      // the span to the next key

        public bool IsHold { get { return Interpolation == AnimInterpolation.Hold; } }

        public AnimKey Clone() { return (AnimKey)MemberwiseClone(); }
    }

    // The keys of one property (one channel) of one target. Property: the path of the property in the target (given by
    // the host, e.g. "rotation", "fill.color"); the target is found by its id. Kind and Discrete come from the property
    // when the track is made (and are stored with it).
    public sealed class AnimTrack
    {
        public int                  TargetId;
        public string               Property    = "";
        public AnimValueKind        Kind;
        public bool                 Discrete;                   // never interpolated (steps)
        public AnimColorSpace       ColorSpace;                 // colors only
        public List<AnimKey>        Keys        = new List<AnimKey>();     // sorted by time, at least one

        // the value changes in time
        public bool IsAnimated { get { return Keys.Count > 1; } }

        // the values are steps: a discrete property, a bool, an int, an enum
        public bool IsStepped { get { return Discrete || Kind == AnimValueKind.Bool || Kind == AnimValueKind.Int || Kind == AnimValueKind.Enum; } }

        public AnimTrack Clone()
        {
            return new AnimTrack { TargetId = TargetId, Property = Property, Kind = Kind, Discrete = Discrete, ColorSpace = ColorSpace, Keys = Keys.Select(k => k.Clone()).ToList() };
        }

        // the key at t (null: none)
        public AnimKey? KeyAt(double t)
        {
            return Keys.FirstOrDefault(k => Math.Abs(k.Time - t) < AnimTime.KeyTolerance);
        }

        // The key at t gets the value, or a new one is made (a new key continues the interpolation of the span it is in).
        public AnimKey SetKey(double t, AnimValue value)
        {
            AnimKey? key = KeyAt(t);
            if (key != null)
            {
                key.Value = value;
                return key;
            }
            int index = 0;
            while (index < Keys.Count && Keys[index].Time < t)
                index++;
            key = new AnimKey { Time = t, Value = value };
            if (index > 0)
                key.Interpolation = Keys[index - 1].Interpolation;
            Keys.Insert(index, key);
            return key;
        }

        public void Sort()
        {
            Keys.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        // The value at the time t: the first key before it, the last key after it, between two keys the interpolation.
        public AnimValue ValueAt(double t)
        {
            if (Keys.Count == 0)
                return default;
            if (t <= Keys[0].Time + AnimTime.Epsilon)
                return Keys[0].Value;
            int i = 0;
            while (i + 1 < Keys.Count && Keys[i + 1].Time <= t + AnimTime.Epsilon)
                i++;
            if (i + 1 >= Keys.Count)
                return Keys[i].Value;

            AnimKey a = Keys[i], b = Keys[i + 1];
            if (a.IsHold || IsStepped)
                return a.Value;
            double span = b.Time - a.Time;
            double u = span > AnimTime.Epsilon ? AnimMath.Ease(a.Interpolation.Ease(), Math.Clamp((t - a.Time) / span, 0, 1)) : 1;
            if (Kind != AnimValueKind.Color)
                return AnimValue.Of(AnimMath.Lerp(a.Value.X, b.Value.X, u));
            return ColorSpace == AnimColorSpace.Hsl ? AnimColors.LerpHsl(a.Value, b.Value, u) : AnimMath.LerpColor(a.Value, b.Value, u);
        }
    }

    // The tracks of one animated document (e.g. one object of a scene, a layer): the keys of the properties of its targets.
    // Id: stable and unique among the containers of the host (not the position of the container): the selection of the
    // timeline finds the container by it, also after an undo that replaced the model.
    public interface IAnimTrackContainer
    {
        string              Id      { get; }
        IList<AnimTrack>    Tracks  { get; }
    }

    public static class AnimTrackContainers
    {
        public static AnimTrack? FindTrack(this IAnimTrackContainer c, int targetId, string property)
        {
            foreach (AnimTrack t in c.Tracks)
            {
                if (t.TargetId == targetId && t.Property == property)
                    return t;
            }
            return null;
        }

        public static IEnumerable<AnimTrack> TracksOf(this IAnimTrackContainer c, int targetId)
        {
            return c.Tracks.Where(t => t.TargetId == targetId);
        }

        public static bool HasTracks(this IAnimTrackContainer c, int targetId)
        {
            foreach (AnimTrack t in c.Tracks)
            {
                if (t.TargetId == targetId)
                    return true;
            }
            return false;
        }

        // a track changes a property in time
        public static bool IsAnimated(this IAnimTrackContainer c)
        {
            return c.Tracks.Any(t => t.IsAnimated);
        }
    }

    // A named time of a timeline (the start / end of a sub-animation)
    public sealed class AnimMarker
    {
        public string               Name        = "";
        public double               Time;               // s

        public AnimMarker Clone() { return (AnimMarker)MemberwiseClone(); }
    }
}
