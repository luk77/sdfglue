//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppAnimation.Ui;
using Xunit;

namespace SingleDocAppAnimation.Tests
{
    // The module stays independent of the applications that use it (an optional module of the SingleDocApp framework);
    // the parts of the UI that need no ImGui frame
    public class ModuleTests
    {
        [Fact]
        public void Module_DoesNotDependOnApplications()
        {
            // only the runtime, the framework and ImGui (no application)
            string[] allowed = { "System", "Microsoft", "netstandard", "mscorlib", "ImGui", "SingleDocApp" };
            foreach (System.Reflection.Assembly a in new[] { typeof(AnimTrack).Assembly, typeof(WndTimeline).Assembly })
                Assert.All(a.GetReferencedAssemblies(), r => Assert.Contains(allowed, p => r.Name != null && r.Name.StartsWith(p, StringComparison.Ordinal)));
            // the model has no UI
            Assert.DoesNotContain(typeof(AnimTrack).Assembly.GetReferencedAssemblies(), r => r.Name != null && (r.Name.StartsWith("ImGui", StringComparison.Ordinal) || r.Name.StartsWith("OpenTK", StringComparison.Ordinal) || r.Name.StartsWith("SingleDocApp", StringComparison.Ordinal)));
        }

        [Fact]
        public void Texts_DefaultsAndOverride()
        {
            AnimTexts texts = new AnimTexts();
            Assert.Equal("Loop", texts.Get("TlLoop"));
            Assert.Equal("NoSuchKey", texts.Get("NoSuchKey"));
            Assert.Equal("Contour 2", texts.Property("Contour 2"));

            texts.Override = key => key == "TlLoop" ? "Repeat" : key == "P.Contour" ? "Outline" : null;
            Assert.Equal("Repeat", texts.Get("TlLoop"));
            Assert.Equal("Play from the start", texts.Get("TlFromStart"));       // not overridden: the default
            Assert.Equal("Outline 2", texts.Property("Contour 2"));               // a numbered group by its name
            Assert.Equal("Rotation", texts.Property("Rotation"));
        }

        private sealed class Prop : IAnimProperty
        {
            public string               Path        { get; set; } = "";
            public string               Group       { get; set; } = "";
            public string               Label       { get { return Path; } }
            public AnimValueKind        Kind        { get { return AnimValueKind.Number; } }
            public bool                 Discrete    { get { return false; } }
            public int                  Order       { get { return 0; } }
            public AnimPropertyHints    Hints       { get { return AnimPropertyHints.Default; } }

            public AnimValue Get() { return default; }
            public void Set(AnimValue v) { }
        }

        [Fact]
        public void Groups_OfTheTree()
        {
            AnimGroup root = AnimGroup.Build(new[]
            {
                new Prop { Path = "x", Group = "Transform/Position" }, new Prop { Path = "rotation", Group = "Transform" },
                new Prop { Path = "y", Group = "Transform/Position" }, new Prop { Path = "alpha" },
                new Prop { Path = "n.x", Group = "Geometry/Contour 1/Node 1" },
            });
            Assert.Equal(new[] { "x", "y", "rotation", "alpha", "n.x" }, root.AllProperties().Select(p => p.Path));
            AnimGroup transform = (AnimGroup)root.Items[0];
            Assert.Equal("Transform", transform.Label);
            Assert.Equal("Transform/Position", ((AnimGroup)transform.Items[0]).Path);
            Assert.True(transform.HasTrack(new AnimTrack { Property = "y" }));
            Assert.False(transform.HasTrack(new AnimTrack { Property = "alpha" }));
        }
    }
}
