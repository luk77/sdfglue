//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes;
using SingleDocAppAnimation;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppCore.Utils;
using System.Globalization;
using System.Xml;

namespace SdfGlueCore.Model.Animation
{
    // The tracks of one node of the project (an SDF object, a material, a render pass). Id: the kind and the id of the
    // node ("sdf:12", "mat:3", "pass:-1001"), the ids of the nodes are unique only within their kind.
    public class AnimNodeTracks : IAnimTrackContainer
    {
        public  string                  Id      { get; set; } = "";
        public  List<AnimTrack>         List    = new List<AnimTrack>();

        public  IList<AnimTrack>        Tracks  { get { return List; } }

        public AnimNodeTracks Clone()
        {
            return new AnimNodeTracks { Id = Id, List = List.Select(t => t.Clone()).ToList() };
        }
    }

    // The animation of the project (CPU side, independent of the time of the shaders and of the signals): one timeline
    // with its length, frame rate, markers and the tracks of the nodes. The values of the tracks are written to the
    // parameters (SdfAnimTargets); the animation never changes the generated shader.
    public class ProjectAnimation
    {
        public const    double                  DefaultLength   = 10.0;
        public const    double                  DefaultFps      = 30.0;

        public          double                  Length          = DefaultLength;    // s
        public          double                  Fps             = DefaultFps;
        public          List<AnimMarker>        Markers         = new List<AnimMarker>();
        public          List<AnimNodeTracks>    Containers      = new List<AnimNodeTracks>();

        // nothing to save (projects without an animation are saved unchanged)
        public bool IsEmpty
        {
            get
            {
                return Markers.Count == 0 && Containers.All(c => c.Tracks.Count == 0) && Length == DefaultLength && Fps == DefaultFps;
            }
        }

        // a parameter changes in time
        public bool IsAnimated
        {
            get { return Containers.Any(c => c.IsAnimated()); }
        }

        public static string ContainerId(TreeNode node)
        {
            string kind = node is SdfObject ? "sdf" : node is MaterialInstance ? "mat" : node is RenderPassData ? "pass" : "node";
            return kind + ":" + node.Id.ToString(CultureInfo.InvariantCulture);
        }

        public AnimNodeTracks? Find(string containerId)
        {
            return Containers.FirstOrDefault(c => c.Id == containerId);
        }

        public AnimNodeTracks GetOrAdd(string containerId)
        {
            AnimNodeTracks? c = Find(containerId);
            if (c == null)
            {
                c = new AnimNodeTracks { Id = containerId };
                Containers.Add(c);
            }
            return c;
        }

        public ProjectAnimation Clone()
        {
            return new ProjectAnimation
            {
                Length      = Length,
                Fps         = Fps,
                Markers     = Markers.Select(m => m.Clone()).ToList(),
                Containers  = Containers.Select(c => c.Clone()).ToList(),
            };
        }

        public void CopyFrom(ProjectAnimation other)
        {
            ProjectAnimation c = other.Clone();
            Length      = c.Length;
            Fps         = c.Fps;
            Markers     = c.Markers;
            Containers  = c.Containers;
        }

        // the same data (exactly, also the values that are rounded in the file; empty containers do not count)
        public bool SameAs(ProjectAnimation o)
        {
            List<AnimNodeTracks> mine = Containers.Where(c => c.Tracks.Count > 0).ToList();
            List<AnimNodeTracks> other = o.Containers.Where(c => c.Tracks.Count > 0).ToList();
            if (Length != o.Length || Fps != o.Fps || Markers.Count != o.Markers.Count || mine.Count != other.Count)
                return false;
            for (int i = 0; i < Markers.Count; i++)
            {
                if (Markers[i].Name != o.Markers[i].Name || Markers[i].Time != o.Markers[i].Time)
                    return false;
            }
            for (int i = 0; i < mine.Count; i++)
            {
                AnimNodeTracks a = mine[i], b = other[i];
                if (a.Id != b.Id || a.List.Count != b.List.Count)
                    return false;
                for (int j = 0; j < a.List.Count; j++)
                {
                    if (!SameTrack(a.List[j], b.List[j]))
                        return false;
                }
            }
            return true;
        }

        private static bool SameTrack(AnimTrack a, AnimTrack b)
        {
            if (a.TargetId != b.TargetId || a.Property != b.Property || a.Kind != b.Kind || a.Discrete != b.Discrete || a.ColorSpace != b.ColorSpace || a.Keys.Count != b.Keys.Count)
                return false;
            for (int i = 0; i < a.Keys.Count; i++)
            {
                AnimKey ka = a.Keys[i], kb = b.Keys[i];
                if (ka.Time != kb.Time || ka.Value != kb.Value || ka.Interpolation != kb.Interpolation)
                    return false;
            }
            return true;
        }

        // <Animation length fps><Marker name time/>..<Tracks node="sdf:12"><track ../>..</Tracks>..</Animation>
        // (only the tracks of the nodes that exist)
        public void Serialize(XmlDocument xmlDoc, XmlNode parent, Func<string, bool> exists)
        {
            XmlElement nodeAnim = XmlUtils.AddNode(xmlDoc, parent, "Animation");
            XmlUtils.AddAtributeString(nodeAnim, "length", AnimXml.Num(Length));
            XmlUtils.AddAtributeString(nodeAnim, "fps", AnimXml.Num(Fps));
            foreach (AnimMarker m in Markers)
            {
                XmlElement nodeMarker = XmlUtils.AddNode(xmlDoc, nodeAnim, "Marker");
                XmlUtils.AddAtributeString(nodeMarker, "name", m.Name);
                XmlUtils.AddAtributeString(nodeMarker, "time", AnimXml.Num(m.Time));
            }
            foreach (AnimNodeTracks c in Containers)
            {
                if (c.Tracks.Count == 0 || !exists(c.Id))
                    continue;
                XmlElement nodeTracks = XmlUtils.AddNode(xmlDoc, nodeAnim, "Tracks");
                XmlUtils.AddAtributeString(nodeTracks, "node", c.Id);
                foreach (AnimTrack t in c.Tracks)
                    nodeTracks.AppendChild(AnimXml.SaveTrack(xmlDoc, t));
            }
        }

        public void Deserialize(XmlNode nodeAnim)
        {
            Length  = Math.Max(0.0, ParseDouble(XmlUtils.LoadAttributeAsString(nodeAnim, "length", null), DefaultLength));
            Fps     = ParseDouble(XmlUtils.LoadAttributeAsString(nodeAnim, "fps", null), DefaultFps);
            if (Fps <= 0)
                Fps = DefaultFps;
            Markers.Clear();
            Containers.Clear();
            foreach (XmlNode node in nodeAnim.ChildNodes)
            {
                if (node.Name == "Marker")
                {
                    Markers.Add(new AnimMarker
                    {
                        Name = XmlUtils.LoadAttributeAsString(node, "name", null) ?? "",
                        Time = Math.Max(0.0, ParseDouble(XmlUtils.LoadAttributeAsString(node, "time", null), 0.0)),
                    });
                }
                else if (node.Name == "Tracks")
                {
                    string? id = XmlUtils.LoadAttributeAsString(node, "node", null);
                    if (String.IsNullOrEmpty(id))
                        continue;
                    AnimNodeTracks c = GetOrAdd(id);
                    foreach (XmlNode nodeTrack in node.ChildNodes)
                    {
                        if (nodeTrack.Name != "track")
                            continue;
                        AnimTrack t = AnimXml.LoadTrack(nodeTrack);
                        if (t.Keys.Count > 0 && c.FindTrack(t.TargetId, t.Property) == null)
                            c.Tracks.Add(t);
                    }
                }
            }
        }

        private static double ParseDouble(string? s, double def)
        {
            return s != null && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : def;
        }
    }
}
