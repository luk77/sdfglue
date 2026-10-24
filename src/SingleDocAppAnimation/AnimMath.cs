//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    public enum AnimEase
    {
        Linear,
        In,         // slow start
        Out,        // slow end
        InOut,
    }

    // The interpolation of numbers, angles and colors
    public static class AnimMath
    {
        // u: 0..1 -> 0..1 (quadratic)
        public static double Ease(AnimEase ease, double u)
        {
            switch (ease)
            {
                case AnimEase.In:       return u * u;
                case AnimEase.Out:      return 1 - (1 - u) * (1 - u);
                case AnimEase.InOut:    return u < 0.5 ? 2 * u * u : 1 - 2 * (1 - u) * (1 - u);
                default:                return u;
            }
        }

        public static double Lerp(double a, double b, double u)
        {
            return a + (b - a) * u;
        }

        // degrees
        public static double LerpAngle(double a, double b, double u)
        {
            return a + AngleDelta(a, b) * u;
        }

        // the shorter way from the angle a to b (degrees, -180 .. 180)
        public static double AngleDelta(double a, double b)
        {
            double d = (b - a) % 360.0;
            if (d > 180)
                d -= 360;
            else if (d < -180)
                d += 360;
            return d;
        }

        // the channels of a color separately
        public static AnimValue LerpColor(AnimValue a, AnimValue b, double u)
        {
            return AnimValue.Color(Lerp(a.X, b.X, u), Lerp(a.Y, b.Y, u), Lerp(a.Z, b.Z, u), Lerp(a.W, b.W, u));
        }
    }

    // Colors (R G B A 0..255) in the HSL space (hue 0..360 degrees, saturation and lightness 0..1)
    public static class AnimColors
    {
        public static void ToHsl(AnimValue c, out double h, out double s, out double l)
        {
            double r = c.X / 255.0, g = c.Y / 255.0, b = c.Z / 255.0;
            double max = Math.Max(r, Math.Max(g, b)), min = Math.Min(r, Math.Min(g, b));
            double d = max - min;
            l = (max + min) * 0.5;
            h = 0;
            s = 0;
            if (d < 1e-12)
                return;
            s = d / (1 - Math.Abs(2 * l - 1));
            if (max == r)
                h = 60 * (((g - b) / d) % 6);
            else if (max == g)
                h = 60 * ((b - r) / d + 2);
            else
                h = 60 * ((r - g) / d + 4);
            if (h < 0)
                h += 360;
        }

        public static AnimValue FromHsl(double h, double s, double l, double alpha)
        {
            h = ((h % 360) + 360) % 360;
            s = Math.Clamp(s, 0, 1);
            l = Math.Clamp(l, 0, 1);
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c * 0.5;
            double r, g, b;
            if (h < 60)         { r = c; g = x; b = 0; }
            else if (h < 120)   { r = x; g = c; b = 0; }
            else if (h < 180)   { r = 0; g = c; b = x; }
            else if (h < 240)   { r = 0; g = x; b = c; }
            else if (h < 300)   { r = x; g = 0; b = c; }
            else                { r = c; g = 0; b = x; }
            return AnimValue.Color((r + m) * 255.0, (g + m) * 255.0, (b + m) * 255.0, alpha);
        }

        // a .. b at u in the HSL space: the hue by the shorter way (a color without a hue - gray, black, white - takes the
        // hue of the other one), the alpha linearly
        public static AnimValue LerpHsl(AnimValue a, AnimValue b, double u)
        {
            if (u <= 0 || a == b)
                return a;
            if (u >= 1)
                return b;
            ToHsl(a, out double ha, out double sa, out double la);
            ToHsl(b, out double hb, out double sb, out double lb);
            if (sa < 1e-9)
                ha = hb;
            else if (sb < 1e-9)
                hb = ha;
            double h = AnimMath.LerpAngle(ha, hb, u);
            double alpha = a.W + (b.W - a.W) * u;
            return FromHsl(h, AnimMath.Lerp(sa, sb, u), AnimMath.Lerp(la, lb, u), alpha);
        }

        // a channel as a byte (rounded, clamped)
        public static byte ToByte(double channel)
        {
            return (byte)Math.Clamp((int)Math.Round(channel), 0, 255);
        }
    }

    // The time of a timeline (s) and its frames
    public static class AnimTime
    {
        public const double     Epsilon         = 1e-9;
        public const double     KeyTolerance    = Epsilon * 1000;      // the keys closer in time are at the same time

        // frame: 1-based
        public static double FrameToTime(int frame, double fps)
        {
            return (frame - 1) / fps;
        }

        // the frame (1-based) shown at the time t
        public static int TimeToFrame(double t, double fps)
        {
            return (int)Math.Floor(t * fps + 1e-6) + 1;
        }

        // the nearest whole frame
        public static double Snap(double t, double fps)
        {
            return Math.Round(t * fps) / fps;
        }
    }
}
