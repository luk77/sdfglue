//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Xml.Linq;
using Xunit;

namespace SingleDocAppAnimation.Tests
{
    // The model of the animation: the values of the tracks, the keys, the time, the markers, XML, the bindings
    public class AnimTrackTests
    {
        private static AnimTrack Track(params (double Time, double Value)[] keys)
        {
            AnimTrack t = new AnimTrack { TargetId = 1, Property = "x" };
            foreach ((double time, double value) in keys)
                t.Keys.Add(new AnimKey { Time = time, Value = AnimValue.Of(value) });
            return t;
        }

        private static AnimValue Rgba(double r, double g, double b, double a = 255)
        {
            return AnimValue.Color(r, g, b, a);
        }

        private static AnimTrack ColorTrack(AnimColorSpace space, params (double Time, AnimValue Color)[] keys)
        {
            AnimTrack t = new AnimTrack { TargetId = 1, Property = "color", Kind = AnimValueKind.Color, ColorSpace = space };
            foreach ((double time, AnimValue color) in keys)
                t.Keys.Add(new AnimKey { Time = time, Value = color });
            return t;
        }

        // the channels as bytes, like a host that stores bytes
        private static string Bytes(AnimValue c)
        {
            return AnimXml.ColorToString(c);
        }

        private sealed class Container : IAnimTrackContainer
        {
            public string               Id      { get; set; } = "c";
            public List<AnimTrack>      List    = new List<AnimTrack>();
            public IList<AnimTrack>     Tracks  { get { return List; } }
        }

        // ---- the values

        [Fact]
        public void Values_NumbersEasingAndHold()
        {
            AnimTrack t = Track((0.5, 10), (1.5, 110));
            Assert.Equal(10, t.ValueAt(0).Number);          // before the first key
            Assert.Equal(60, t.ValueAt(1).Number, 9);
            Assert.Equal(110, t.ValueAt(5).Number);         // after the last one
            Assert.True(t.IsAnimated);

            t.Keys[0].Interpolation = AnimInterpolation.EaseIn;
            Assert.Equal(35, t.ValueAt(1).Number, 9);       // the easing of the span is the one of its first key
            t.Keys[0].Interpolation = AnimInterpolation.EaseOut;
            Assert.Equal(85, t.ValueAt(1).Number, 9);
            t.Keys[0].Interpolation = AnimInterpolation.EaseInOut;
            Assert.Equal(60, t.ValueAt(1).Number, 9);

            t.Keys[0].Interpolation = AnimInterpolation.Hold;
            Assert.Equal(10, t.ValueAt(1.49).Number);
            Assert.Equal(110, t.ValueAt(1.5).Number);
            Assert.Equal(AnimEase.Linear, AnimInterpolation.Hold.Ease());
            Assert.Equal(AnimInterpolation.EaseInOut, AnimInterpolations.Of(AnimEase.InOut));
        }

        [Fact]
        public void Values_SteppedKinds()
        {
            AnimTrack discrete = Track((0, 0), (1, 2));
            discrete.Discrete = true;
            Assert.Equal(0, discrete.ValueAt(0.99).Number);
            Assert.Equal(2, discrete.ValueAt(1).Number);

            foreach (AnimValueKind kind in new[] { AnimValueKind.Bool, AnimValueKind.Int, AnimValueKind.Enum })
            {
                AnimTrack t = Track((0, 0), (1, 1));
                t.Kind = kind;
                Assert.True(t.IsStepped);
                Assert.Equal(0, t.ValueAt(0.5).Number);
            }
            Assert.False(Track((0, 0)).IsStepped);
        }

        [Fact]
        public void Values_SetKey()
        {
            // replaces the key at the time, inserts in order, a new key continues the span it is in
            AnimTrack s = Track((0, 0), (2, 20));
            s.Keys[0].Interpolation = AnimInterpolation.EaseOut;
            s.SetKey(1, AnimValue.Of(5));
            s.SetKey(2, AnimValue.Of(30));
            Assert.Equal(new[] { 0.0, 1, 2 }, s.Keys.Select(k => k.Time));
            Assert.Equal(30, s.Keys[2].Value.Number);
            Assert.Equal(AnimInterpolation.EaseOut, s.Keys[1].Interpolation);
            Assert.Same(s.Keys[1], s.KeyAt(1 + AnimTime.KeyTolerance * 0.5));
            Assert.Null(s.KeyAt(1.5));

            AnimTrack c = s.Clone();
            c.Keys[0].Value = AnimValue.Of(99);
            Assert.Equal(0, s.Keys[0].Value.Number);
        }

        [Fact]
        public void Values_ColorsRgbAndHsl()
        {
            AnimValue red = Rgba(255, 0, 0), blue = Rgba(0, 0, 255), gray = Rgba(128, 128, 128);

            AnimTrack rgb = ColorTrack(AnimColorSpace.Rgb, (0, red), (1, blue));
            Assert.Equal(Rgba(127.5, 0, 127.5), rgb.ValueAt(0.5));
            Assert.Equal("#800080FF", Bytes(rgb.ValueAt(0.5)));            // the bytes: rounded to even like Math.Round

            // HSL: the hue by the shorter way (red -> blue through magenta, not through green)
            AnimTrack hsl = ColorTrack(AnimColorSpace.Hsl, (0, red), (1, blue));
            Assert.Equal("#FF00FFFF", Bytes(hsl.ValueAt(0.5)));
            Assert.Equal(red, hsl.ValueAt(0));
            Assert.Equal(blue, hsl.ValueAt(1));

            // a color without a hue takes the one of the other
            AnimTrack fromGray = ColorTrack(AnimColorSpace.Hsl, (0, gray), (1, red));
            Assert.Equal("#BF4040FF", Bytes(fromGray.ValueAt(0.5)));

            // the alpha is linear
            AnimTrack alpha = ColorTrack(AnimColorSpace.Hsl, (0, Rgba(255, 0, 0, 0)), (1, Rgba(255, 0, 0, 255)));
            Assert.Equal(127.5, alpha.ValueAt(0.5).W, 9);

            AnimColors.ToHsl(Rgba(0, 255, 0), out double h, out double s, out double l);
            Assert.Equal(120, h, 6);
            Assert.Equal(1, s, 6);
            Assert.Equal(0.5, l, 6);
            AnimColors.ToHsl(Rgba(12, 200, 99), out h, out s, out l);
            Assert.Equal("#0CC863FF", Bytes(AnimColors.FromHsl(h, s, l, 255)));
        }

        [Fact]
        public void Math_AnglesAndTime()
        {
            Assert.Equal(20, AnimMath.AngleDelta(170, -170), 9);
            Assert.Equal(-20, AnimMath.AngleDelta(-170, 170), 9);
            Assert.Equal(180, AnimMath.LerpAngle(170, -170, 0.5), 9);
            Assert.Equal(1, AnimTime.TimeToFrame(0, 25));
            Assert.Equal(2, AnimTime.TimeToFrame(0.04, 25));
            Assert.Equal(0.04, AnimTime.FrameToTime(2, 25), 12);
            Assert.Equal(0.08, AnimTime.Snap(0.07, 25), 12);
        }

        // ---- the editing

        [Fact]
        public void Editing_KeysAndTracks()
        {
            Container c = new Container();
            AnimTrack t = AnimTrackEditing.GetOrAdd(c, 3, "fill.color", AnimValueKind.Color, false);
            Assert.Same(t, AnimTrackEditing.GetOrAdd(c, 3, "fill.color", AnimValueKind.Color, false));
            Assert.Equal(AnimValueKind.Color, t.Kind);
            Assert.Same(t, c.FindTrack(3, "fill.color"));
            Assert.True(c.HasTracks(3));
            Assert.False(c.HasTracks(4));

            t.SetKey(0, Rgba(1, 2, 3));
            t.SetKey(1, Rgba(4, 5, 6));
            Assert.True(c.IsAnimated());
            AnimTrackEditing.DeleteKey(c, t, t.Keys[0]);
            Assert.Single(c.Tracks);
            AnimTrackEditing.DeleteKey(c, t, t.Keys[0]);
            Assert.Empty(c.Tracks);

            // moving keys: on the grid, not before 0, over another key of the track
            AnimTrack m = Track((0, 0), (1, 10), (1.5, 15));
            c.List.Add(m);
            List<AnimTrackEditing.KeyRef> keys = new List<AnimTrackEditing.KeyRef> { new AnimTrackEditing.KeyRef(c, m, m.Keys[1]) };
            Func<double, double> snap = x => Math.Round(Math.Max(0, x) / 0.001) * 0.001;
            AnimTrackEditing.MoveKeys(keys, new[] { 1.0 }, 0.5004, snap);
            Assert.Equal(new[] { 0.0, 1.5 }, m.Keys.Select(k => k.Time));
            Assert.Equal(10, m.Keys[1].Value.Number);
            AnimTrackEditing.MoveKeys(keys, new[] { 1.5 }, -5, snap);
            Assert.Equal(0.0, keys[0].Key.Time);
            Assert.Single(m.Keys);

            // the key times of several tracks, merged
            Assert.Equal(new[] { 0.0, 1, 2 }, AnimTrackEditing.KeyTimes(new[] { Track((0, 0), (1, 1)), Track((1 + AnimTime.KeyTolerance * 0.1, 0), (2, 0)) }));
            Assert.Equal(2, AnimTrackEditing.LastKeyTime(new[] { Track((0, 0), (2, 1)) }));
        }

        private sealed class Prop : IAnimProperty
        {
            public string               Path        { get; set; } = "";
            public string               Group       { get; set; } = "";
            public string               Label       { get; set; } = "";
            public AnimValueKind        Kind        { get; set; }
            public bool                 Discrete    { get; set; }
            public int                  Order       { get; set; }
            public AnimPropertyHints    Hints       { get { return AnimPropertyHints.Default; } }
            public AnimValue            Value;
            public List<string>         Log         = new List<string>();

            public AnimValue Get() { return Value; }
            public void Set(AnimValue v) { Value = v; Log.Add(Path); }
        }

        private sealed class Target : IAnimTarget
        {
            public int                  Id          { get; set; }
            public string               Label       { get { return "t" + Id; } }
            public List<Prop>           Props       = new List<Prop>();
            public IEnumerable<IAnimProperty>   Properties  { get { return Props; } }
            public IEnumerable<IAnimTarget>     Children    { get { return Enumerable.Empty<IAnimTarget>(); } }
            public int                  Finds;

            public IAnimProperty? FindProperty(string path)
            {
                Finds++;
                return Props.FirstOrDefault(p => p.Path == path);
            }
        }

        [Fact]
        public void Editing_AddKeyAndMakeStatic()
        {
            Container c = new Container();
            Target target = new Target { Id = 7, Props = { new Prop { Path = "a", Value = AnimValue.Of(5) }, new Prop { Path = "b", Kind = AnimValueKind.Int, Value = AnimValue.Of(2) } } };
            AnimKey k = AnimTrackEditing.AddKey(c, target, "a", 0.5)!;
            Assert.Equal(5, k.Value.Number);
            Assert.Null(AnimTrackEditing.AddKey(c, target, "zzz", 0.5));
            Assert.Equal(2, AnimTrackEditing.AddKey(c, target, "b", 0)!.Value.Number);
            Assert.Equal(AnimValueKind.Int, c.FindTrack(7, "b")!.Kind);         // the kind of the property
            Assert.Null(AnimTrackEditing.AddKey(c, new Target { Id = 0, Props = { new Prop { Path = "a" } } }, "a", 0));

            AnimTrack a = c.FindTrack(7, "a")!;
            a.SetKey(1.5, AnimValue.Of(15));
            AnimTrackEditing.MakeStatic(c, a, 1, target);
            Assert.Equal(10, target.Props[0].Value.Number, 9);
            Assert.Null(c.FindTrack(7, "a"));
        }

        [Fact]
        public void Editing_TimeAndMarkers()
        {
            List<AnimTrack> tracks = new List<AnimTrack> { Track((0, 0), (1, 10), (2, 20)) };
            AnimTrackEditing.InsertTime(tracks, 0.5, 0.25);
            Assert.Equal(new[] { 0.0, 1.25, 2.25 }, tracks[0].Keys.Select(k => k.Time));
            AnimTrackEditing.RemoveTime(tracks, 1.0, 0.5);
            Assert.Equal(new[] { 0.0, 1.75 }, tracks[0].Keys.Select(k => k.Time));
            AnimTrackEditing.RemoveTime(tracks, -1, 5);
            Assert.Empty(tracks);

            List<AnimMarker> markers = new List<AnimMarker>();
            AnimTrackEditing.AddMarker(markers, 1, "label");
            AnimTrackEditing.AddMarker(markers, 0.5, "label");
            Assert.Equal(new[] { "label_2", "label" }, markers.Select(m => m.Name));
            AnimTrackEditing.InsertTime(markers, 0.7, 0.1);
            Assert.Equal(new[] { 0.5, 1.1 }, markers.Select(m => Math.Round(m.Time, 9)));
            AnimTrackEditing.RemoveTime(markers, 0.4, 0.3);
            Assert.Equal(new[] { 0.4, 0.8 }, markers.Select(m => Math.Round(m.Time, 9)));
        }

        // ---- XML

        [Fact]
        public void Xml_RoundTrip()
        {
            AnimTrack t = Track((0, 0), (1.5, 90.5), (2, 1.23456));
            t.TargetId = 12;
            t.Keys[0].Interpolation = AnimInterpolation.EaseInOut;
            t.Keys[1].Interpolation = AnimInterpolation.Hold;
            string xml = AnimXml.SaveTrack(t).ToString();
            Assert.Contains("<track target=\"12\" property=\"x\">", xml);
            Assert.Contains("<key time=\"0\" value=\"0\" ease=\"in-out\" />", xml);
            Assert.Contains("<key time=\"1.5\" value=\"90.5\" hold=\"true\" />", xml);
            Assert.Contains("value=\"1.2346\"", xml);
            Assert.Equal(xml, AnimXml.SaveTrack(AnimXml.LoadTrack(XElement.Parse(xml))).ToString());

            // the same through an XmlDocument
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            System.Xml.XmlElement root = doc.CreateElement("tracks");
            doc.AppendChild(root);
            root.AppendChild(AnimXml.SaveTrack(doc, t));
            Assert.Equal(xml, AnimXml.SaveTrack(AnimXml.LoadTrack(root.FirstChild!)).ToString());

            // colors, kinds the format does not assume
            AnimTrack c = ColorTrack(AnimColorSpace.Hsl, (0, Rgba(255, 0, 0)), (1, Rgba(0, 0, 255, 128)));
            AnimTrack b = Track((0, 0), (1, 1));
            b.Kind = AnimValueKind.Bool;
            b.Discrete = true;
            AnimXmlFormat format = new AnimXmlFormat { TargetAttribute = "element", TargetToString = id => "e" + id, TargetFromString = s => int.Parse(s.Substring(1)) };
            XElement ce = AnimXml.SaveTrack(c, format), be = AnimXml.SaveTrack(b, format);
            Assert.Equal("<track element=\"e1\" property=\"color\" kind=\"color\" space=\"hsl\"><key time=\"0\" color=\"#FF0000FF\" /><key time=\"1\" color=\"#0000FF80\" /></track>", ce.ToString(SaveOptions.DisableFormatting));
            Assert.Contains("kind=\"bool\" discrete=\"true\"", be.ToString());
            AnimTrack c2 = AnimXml.LoadTrack(ce, format), b2 = AnimXml.LoadTrack(be, format);
            Assert.Equal(1, c2.TargetId);
            Assert.Equal(AnimValueKind.Color, c2.Kind);
            Assert.Equal(AnimColorSpace.Hsl, c2.ColorSpace);
            Assert.Equal(Rgba(0, 0, 255, 128), c2.Keys[1].Value);
            Assert.Equal(AnimValueKind.Bool, b2.Kind);
            Assert.True(b2.Discrete);

            // what the host assumes is not written
            AnimXmlFormat colors = new AnimXmlFormat { DefaultKind = p => (p == "color" ? AnimValueKind.Color : AnimValueKind.Number, false) };
            Assert.DoesNotContain("kind=", AnimXml.SaveTrack(c, colors).ToString());
            Assert.Equal(AnimValueKind.Color, AnimXml.LoadTrack(AnimXml.SaveTrack(c, colors), colors).Kind);

            // keys out of order are sorted, a bad color is black
            AnimTrack d = AnimXml.LoadTrack(XElement.Parse("<track target=\"1\" property=\"x\" kind=\"color\"><key time=\"2\" color=\"zz\"/><key time=\"1\" color=\"0x10203040\"/></track>"));
            Assert.Equal(new[] { 1.0, 2 }, d.Keys.Select(k => k.Time));
            Assert.Equal(Rgba(0x10, 0x20, 0x30, 0x40), d.Keys[0].Value);
            Assert.Equal(Rgba(0, 0, 0, 255), d.Keys[1].Value);
        }

        // ---- the bindings

        [Fact]
        public void Bindings_ApplyInOrderAndFindOnce()
        {
            List<string> log = new List<string>();
            Target a = new Target { Id = 1, Props = { new Prop { Path = "color", Kind = AnimValueKind.Color, Log = log }, new Prop { Path = "alpha", Order = 1, Log = log } } };
            Target b = new Target { Id = 2, Props = { new Prop { Path = "x", Log = log } } };
            Container c = new Container();
            AnimTrack alpha = Track((0, 0), (1, 1));
            alpha.Property = "alpha";
            AnimTrack x = Track((0, 0), (1, 100));
            x.TargetId = 2;
            AnimTrack missing = Track((0, 0));
            missing.TargetId = 9;
            c.List.AddRange(new[] { alpha, ColorTrack(AnimColorSpace.Rgb, (0, Rgba(0, 0, 0)), (1, Rgba(200, 100, 0))), x, missing });
            int resolves = 0;
            AnimBindings bindings = new AnimBindings(c, id => { resolves++; return id == 1 ? a : id == 2 ? b : null; });

            bindings.Apply(0.5);
            Assert.Equal(3, bindings.Count);
            Assert.Equal(new[] { "color", "x", "alpha" }, log);          // the order of the properties, then of the tracks
            Assert.Equal(Rgba(100, 50, 0), a.Props[0].Value);
            Assert.Equal(0.5, a.Props[1].Value.Number, 9);
            Assert.Equal(50, b.Props[0].Value.Number, 9);
            int found = a.Finds + b.Finds;

            bindings.Apply(1);
            Assert.Equal(found, a.Finds + b.Finds);                 // found once
            Assert.Equal(3, resolves);
            Assert.Equal(100, b.Props[0].Value.Number, 9);

            // a new track: found again
            AnimTrack y = Track((0, 1));
            y.TargetId = 2;
            y.Property = "x";
            c.List.Remove(x);
            c.List.Add(x);
            c.List.Add(y);
            bindings.Apply(1);
            Assert.Equal(1, b.Props[0].Value.Number);               // the later track of the same property wins
            bindings.Invalidate();
            Assert.Equal(4, bindings.Count);
        }

        [Fact]
        public void Nearest_TheSnapOfThePlayhead()
        {
            double[] keys = { 0, 0.5, 1.2, 1.25 };
            Assert.Equal(0.5, AnimTrackEditing.Nearest(keys, 0.53, 0.05));
            Assert.Equal(1.25, AnimTrackEditing.Nearest(keys, 1.24, 0.05));     // the nearest one
            Assert.Equal(1.2, AnimTrackEditing.Nearest(keys, 1.2, 0.05));
            Assert.Null(AnimTrackEditing.Nearest(keys, 0.8, 0.05));             // none is that near: the time stays
            Assert.Null(AnimTrackEditing.Nearest(Array.Empty<double>(), 0.8, 1));
        }
    }
}
