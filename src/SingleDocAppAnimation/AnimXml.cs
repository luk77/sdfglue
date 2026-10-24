//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace SingleDocAppAnimation
{
    // How a host writes the tracks: the name of the attribute of the target and its ids, and what it assumes about a
    // property when a file does not say it (the kind and Discrete are written only when they differ from it)
    public sealed class AnimXmlFormat
    {
        public string                                       TargetAttribute     = "target";
        public Func<int, string>                            TargetToString      = id => id.ToString(CultureInfo.InvariantCulture);
        public Func<string, int>                            TargetFromString    = s => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) ? id : 0;
        public Func<string, (AnimValueKind Kind, bool Discrete)> DefaultKind    = _ => (AnimValueKind.Number, false);
    }

    // A track in XML:
    //   <track target="<id>" property="<path>" [kind="color|bool|int|enum|number"] [discrete="true|false"] [space="hsl"]>
    //     <key time="0.5" value="10" | color="#RRGGBBAA" [hold="true"] [ease="in|out|in-out"]/>
    //   </track>
    // (the numbers with at most 4 decimals)
    public static class AnimXml
    {
        public static readonly AnimXmlFormat    DefaultFormat   = new AnimXmlFormat();

        public static XElement SaveTrack(AnimTrack track, AnimXmlFormat? format = null)
        {
            format ??= DefaultFormat;
            XElement e = new XElement("track", new XAttribute(format.TargetAttribute, format.TargetToString(track.TargetId)), new XAttribute("property", track.Property));
            (AnimValueKind kind, bool discrete) = format.DefaultKind(track.Property);
            if (track.Kind != kind)
                e.Add(new XAttribute("kind", KindName(track.Kind)));
            if (track.Discrete != discrete)
                e.Add(new XAttribute("discrete", track.Discrete ? "true" : "false"));
            bool color = track.Kind == AnimValueKind.Color;
            if (color && track.ColorSpace == AnimColorSpace.Hsl)
                e.Add(new XAttribute("space", "hsl"));
            foreach (AnimKey k in track.Keys)
            {
                XElement ke = new XElement("key", new XAttribute("time", Num(k.Time)));
                ke.Add(color ? new XAttribute("color", ColorToString(k.Value)) : new XAttribute("value", Num(k.Value.X)));
                if (k.IsHold)
                    ke.Add(new XAttribute("hold", "true"));
                else if (k.Interpolation != AnimInterpolation.Linear)
                    ke.Add(new XAttribute("ease", EaseName(k.Interpolation.Ease())));
                e.Add(ke);
            }
            return e;
        }

        public static AnimTrack LoadTrack(XElement e, AnimXmlFormat? format = null)
        {
            format ??= DefaultFormat;
            string property = Attr(e, "property");
            (AnimValueKind kind, bool discrete) = format.DefaultKind(property);
            AnimTrack track = new AnimTrack
            {
                TargetId    = format.TargetFromString(Attr(e, format.TargetAttribute)),
                Property    = property,
                Kind        = ParseKind(Attr(e, "kind")) ?? kind,
                Discrete    = Attr(e, "discrete") switch { "true" => true, "false" => false, _ => discrete },
                ColorSpace  = Attr(e, "space") == "hsl" ? AnimColorSpace.Hsl : AnimColorSpace.Rgb,
            };
            bool color = track.Kind == AnimValueKind.Color;
            foreach (XElement ke in e.Elements("key"))
            {
                AnimKey k = new AnimKey { Time = Math.Max(0, Num(ke, "time", 0)) };
                k.Interpolation = Attr(ke, "hold") == "true" ? AnimInterpolation.Hold : AnimInterpolations.Of(ParseEase(Attr(ke, "ease")));
                k.Value = color ? ParseColor(Attr(ke, "color")) : AnimValue.Of(Num(ke, "value", 0));
                track.Keys.Add(k);
            }
            track.Sort();
            return track;
        }

        // the same in an XmlDocument (hosts that use System.Xml): the element is created in the document, not added
        public static XmlElement SaveTrack(XmlDocument doc, AnimTrack track, AnimXmlFormat? format = null)
        {
            using (XmlReader reader = SaveTrack(track, format).CreateReader())
                return (XmlElement)doc.ReadNode(reader)!;
        }

        public static AnimTrack LoadTrack(XmlNode node, AnimXmlFormat? format = null)
        {
            return LoadTrack(XElement.Parse(node.OuterXml), format);
        }

        // ---- values

        public static string KindName(AnimValueKind kind)
        {
            switch (kind)
            {
                case AnimValueKind.Color:   return "color";
                case AnimValueKind.Bool:    return "bool";
                case AnimValueKind.Int:     return "int";
                case AnimValueKind.Enum:    return "enum";
                default:                    return "number";
            }
        }

        public static AnimValueKind? ParseKind(string s)
        {
            switch (s)
            {
                case "number":  return AnimValueKind.Number;
                case "color":   return AnimValueKind.Color;
                case "bool":    return AnimValueKind.Bool;
                case "int":     return AnimValueKind.Int;
                case "enum":    return AnimValueKind.Enum;
                default:        return null;
            }
        }

        public static string EaseName(AnimEase e)
        {
            return e == AnimEase.In ? "in" : e == AnimEase.Out ? "out" : e == AnimEase.InOut ? "in-out" : "linear";
        }

        public static AnimEase ParseEase(string s)
        {
            return s == "in" ? AnimEase.In : s == "out" ? AnimEase.Out : s == "in-out" ? AnimEase.InOut : AnimEase.Linear;
        }

        // "#RRGGBBAA" (the channels rounded to bytes)
        public static string ColorToString(AnimValue c)
        {
            return String.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", AnimColors.ToByte(c.X), AnimColors.ToByte(c.Y), AnimColors.ToByte(c.Z), AnimColors.ToByte(c.W));
        }

        // "#RRGGBB[AA]" or "0xRRGGBB[AA]"; anything else is opaque black
        public static AnimValue ParseColor(string s)
        {
            s = s.Trim();
            if (s.StartsWith("#"))
                s = s.Substring(1);
            else if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                s = s.Substring(2);
            if ((s.Length != 6 && s.Length != 8) || !uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint v))
                return AnimValue.Color(0, 0, 0, 255);
            if (s.Length == 6)
                v = (v << 8) | 0xFF;
            return AnimValue.Color((byte)(v >> 24), (byte)(v >> 16), (byte)(v >> 8), (byte)v);
        }

        // at most 4 decimals, no "-0"
        public static string Num(double v)
        {
            double r = Math.Round(v, 4);
            if (r == 0)
                r = 0;
            return r.ToString("0.######", CultureInfo.InvariantCulture);
        }

        private static string Attr(XElement e, string name)
        {
            return (string?)e.Attribute(name) ?? "";
        }

        private static double Num(XElement e, string name, double def)
        {
            string? s = (string?)e.Attribute(name);
            return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : def;
        }
    }
}
