//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // The kind of an animated value: how it is interpolated and edited. A vector is not a kind: each of its channels is
    // a track of its own (Number).
    public enum AnimValueKind
    {
        Number,
        Color,      // R G B A
        Bool,       // 0 / 1, never interpolated
        Int,        // never interpolated
        Enum,       // the index of a name (AnimPropertyHints.EnumNames), never interpolated
    }

    // How the colors of a track are interpolated
    public enum AnimColorSpace
    {
        Rgb,        // the channels separately
        Hsl,        // hue (the shorter way round the color wheel), saturation, lightness; the alpha linearly
    }

    // An animated value: a number (and the discrete kinds) in X, a color in X Y Z W = R G B A in the range 0..255.
    // The channels are not rounded: a host that stores bytes rounds them (the interpolation stays exact for whole
    // numbers).
    public struct AnimValue : IEquatable<AnimValue>
    {
        public double       X, Y, Z, W;

        public double Number { get { return X; } }

        public static AnimValue Of(double number)
        {
            return new AnimValue { X = number };
        }

        public static AnimValue Color(double r, double g, double b, double a)
        {
            return new AnimValue { X = r, Y = g, Z = b, W = a };
        }

        public bool Equals(AnimValue o) { return X == o.X && Y == o.Y && Z == o.Z && W == o.W; }
        public override bool Equals(object? obj) { return obj is AnimValue v && Equals(v); }
        public override int GetHashCode() { return HashCode.Combine(X, Y, Z, W); }
        public static bool operator ==(AnimValue a, AnimValue b) { return a.Equals(b); }
        public static bool operator !=(AnimValue a, AnimValue b) { return !a.Equals(b); }

        // the same value for an edit (a number within a relative tolerance, a color exactly)
        public static bool Same(AnimValue a, AnimValue b, AnimValueKind kind)
        {
            if (kind == AnimValueKind.Color)
                return a == b;
            return Math.Abs(a.X - b.X) <= 1e-7 * (1 + Math.Abs(a.X));
        }
    }
}
