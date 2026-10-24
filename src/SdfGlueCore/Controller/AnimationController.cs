//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.Animation;
using SingleDocAppAnimation;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using System.Numerics;

namespace SdfGlueCore.Controller
{
    // The host of the animation module for the current project: the transport (its own time, not Playback), the editor
    // commands (AnimEditorController), the undo of the animation edits and the evaluation of the tracks into the
    // parameters. The values are applied only when they can change (a seek, the playback, an edit, a rebuild of the
    // shader); the animation never rebuilds the shader itself.
    public class AnimationController : IAnimEditorHost, IAnimTimebase
    {
        // the targets of one node, found once (until Invalidate)
        private class NodeTargets
        {
            public  TreeNode?                   Node;
            public  List<SdfAnimTarget>         Targets     = new List<SdfAnimTarget>();
            public  AnimBindings?               Bindings;
        }

        private readonly SdfGlueAppContextBase              ctx_;
        private readonly Dictionary<string, NodeTargets>    nodes_          = new Dictionary<string, NodeTargets>();
        private          DataModel?                         nodesModel_;

        // a value of a parameter before an edit and where its tracks are
        private struct ValueBefore
        {
            public  string                      ContainerId;
            public  int                         TargetId;
            public  AnimValue                   Value;
        }

        // the running edit: the animation and the values of the parameters before it
        private          ProjectAnimation?                  before_;
        private          Dictionary<SdfAnimProperty, ValueBefore>? valuesBefore_;
        private          bool                               autoEnd_;

        public  AnimTransport                               Transport   { get; }
        public  AnimEditorController                        Editor      { get; }

        // changes with every change of the tracks (the views of the animated values)
        public  int                                         Revision    { get; private set; }

        public AnimationController(SdfGlueAppContextBase ctx)
        {
            ctx_        = ctx;
            Transport   = new AnimTransport(this) { Seeking = EndEdit, Seeked = Apply };
            Editor      = new AnimEditorController(this, new AnimEditorOptions { KeyMode = AnimKeyMode.AlwaysAutoKey, ShowRecordButton = false });
        }

        public ProjectAnimation Data
        {
            get { return ctx_.Document.Animation; }
        }

        // ---------------------------------------------------------------- the frame

        // Once per frame: the playback and the end of an edit made by a widget. While a widget is being edited
        // (uiEditing) the values are not applied, so the animation does not fight the edit.
        public void Update(double deltaTime, bool uiEditing)
        {
            if (autoEnd_ && !uiEditing)
                EndEdit();
            if (Transport.Playing && uiEditing)
                return;
            Transport.Update(deltaTime);
        }

        // a new document: the time at 0, nothing selected
        public void OnDocumentChanged()
        {
            before_         = null;
            valuesBefore_   = null;
            autoEnd_        = false;
            Transport.Reset();
            Editor.Reset();
            Invalidate();
        }

        // The shader is about to be generated (a structural change, Fixed switched...): the parameters get their values
        // (the ones baked into the shader the value at 0 s).
        public void OnShaderGenerating()
        {
            Invalidate();
            Apply();
        }

        // the tracks into the parameters: the active ones at the playhead, the baked ones at 0
        public void Apply()
        {
            double t = Transport.Playhead;
            foreach (AnimNodeTracks c in Data.Containers)
            {
                if (c.Tracks.Count == 0)
                    continue;
                AnimBindings? b = GetNode(c.Id).Bindings;
                if (b == null)
                {
                    b = new AnimBindings(c, id => FindTarget(c, id));
                    GetNode(c.Id).Bindings = b;
                }
                b.Apply(p => SdfAnimTargets.TimeOf(p, t));
            }
        }

        // ---------------------------------------------------------------- the targets

        public IReadOnlyList<SdfAnimTarget> TargetsOf(TreeNode node)
        {
            return GetNode(ProjectAnimation.ContainerId(node)).Targets;
        }

        public AnimNodeTracks TracksOf(TreeNode node)
        {
            return Data.GetOrAdd(ProjectAnimation.ContainerId(node));
        }

        // the node of a container, null: deleted
        public TreeNode? NodeOf(IAnimTrackContainer container)
        {
            return GetNode(container.Id).Node;
        }

        // the property of a parameter (and its channel, -1: the whole value) of a node, null: not animatable
        public (SdfAnimTarget Target, SdfAnimProperty Property)? FindProperty(TreeNode node, ISimpleType value, int channel)
        {
            foreach (SdfAnimTarget t in TargetsOf(node))
            {
                foreach (SdfAnimProperty p in t.Props)
                {
                    if (ReferenceEquals(p.Value, value) && (p.Channel == channel || p.Channel < 0 || channel < 0))
                        return (t, p);
                }
            }
            return null;
        }

        // the properties of a parameter (all its channels)
        public List<(SdfAnimTarget Target, SdfAnimProperty Property)> FindProperties(TreeNode node, ISimpleType value)
        {
            List<(SdfAnimTarget, SdfAnimProperty)> list = new List<(SdfAnimTarget, SdfAnimProperty)>();
            foreach (SdfAnimTarget t in TargetsOf(node))
            {
                foreach (SdfAnimProperty p in t.Props)
                {
                    if (ReferenceEquals(p.Value, value))
                        list.Add((t, p));
                }
            }
            return list;
        }

        private NodeTargets GetNode(string containerId)
        {
            DataModel model = ctx_.Document;
            if (!ReferenceEquals(model, nodesModel_))
            {
                nodes_.Clear();
                nodesModel_ = model;
            }
            if (!nodes_.TryGetValue(containerId, out NodeTargets? n))
            {
                n = new NodeTargets { Node = SdfAnimTargets.FindNode(model, containerId) };
                if (n.Node != null)
                    n.Targets = SdfAnimTargets.Build(model, n.Node);
                nodes_[containerId] = n;
            }
            return n;
        }

        // ---------------------------------------------------------------- IAnimTimebase

        public bool     IsAvailable     { get { return true; } }
        public double   Length          { get { return Data.Length; } }
        public double   Fps             { get { return Data.Fps; } }
        public bool     WholeFrames     { get { return false; } }

        // the keys are on the frames
        public double Snap(double t)
        {
            return Math.Max(0.0, AnimTime.Snap(t, Data.Fps));
        }

        public void SetLength(double t)
        {
            Data.Length = Math.Max(1.0 / Data.Fps, Snap(t));
        }

        // ---------------------------------------------------------------- IAnimEditorHost

        public IEnumerable<IAnimTrackContainer> Containers  { get { return Data.Containers; } }
        public List<AnimMarker>                 Markers     { get { return Data.Markers; } }
        public bool                             CanKey      { get { return true; } }
        public bool                             IsEditing   { get { return before_ != null; } }

        public void BeginEdit()
        {
            if (before_ != null)
                return;
            before_         = Data.Clone();
            valuesBefore_   = SnapshotValues();
        }

        // The end of an edit: the changed values of the animated parameters become keys at the playhead (auto-key),
        // one undo step for the tracks and the values.
        public void EndEdit()
        {
            if (before_ == null || valuesBefore_ == null)
                return;
            ProjectAnimation before = before_;
            Dictionary<SdfAnimProperty, ValueBefore> valuesBefore = valuesBefore_;
            before_         = null;
            valuesBefore_   = null;
            autoEnd_        = false;

            // the edited values: keys of the animated ones
            List<SdfAnimProperty> changed = new List<SdfAnimProperty>();
            foreach (KeyValuePair<SdfAnimProperty, ValueBefore> kv in valuesBefore)
            {
                if (!AnimValue.Same(kv.Key.Get(), kv.Value.Value, kv.Key.Kind))
                    changed.Add(kv.Key);
            }
            CommitKeys(changed, valuesBefore);

            Dictionary<SdfAnimProperty, AnimValue> valuesAfter = changed.ToDictionary(p => p, p => p.Get());
            ProjectAnimation after = Data.Clone();
            if (!before.SameAs(after) || changed.Count > 0)
                SaveUndo(before, after, changed.ToDictionary(p => p, p => valuesBefore[p].Value), valuesAfter);
            Invalidate();
            Apply();
        }

        public void Modify()
        {
            BeginEdit();
            autoEnd_ = true;
        }

        public void Changed()
        {
            Revision++;
        }

        public void Invalidate()
        {
            Revision++;
            nodes_.Clear();
        }

        public IAnimTarget? FindTarget(IAnimTrackContainer container, int targetId)
        {
            return GetNode(container.Id).Targets.FirstOrDefault(t => t.Id == targetId);
        }

        public void TimelineSelected()
        {
        }

        // the rows selected in the timeline select their node (the last one)
        public void RowsSelected()
        {
            for (int i = Editor.SelectedRows.Count - 1; i >= 0; i--)
            {
                TreeNode? node = GetNode(Editor.SelectedRows[i].ContainerId).Node;
                if (node != null)
                {
                    ctx_.Selection.SelectedNode = node;
                    return;
                }
            }
        }

        // ---------------------------------------------------------------- an edit in a property editor

        // The undo action of an edit of a parameter by a property editor (instead of its own action): the changed
        // channels with tracks become keys at the playhead (the parameter has PrevVal = the value before the edit).
        public UndoAction CommitParameterEdit(TreeNode node, ISimpleType value, OnValueChanged? onChanged = null)
        {
            ProjectAnimation before = Data.Clone();
            string containerId = ProjectAnimation.ContainerId(node);
            Dictionary<SdfAnimProperty, ValueBefore> valuesBefore = FindProperties(node, value).ToDictionary(
                tp => tp.Property, tp => new ValueBefore { ContainerId = containerId, TargetId = tp.Target.Id, Value = tp.Property.GetPrev() });
            ISimpleType oldValue = CopyPrev(value), newValue = value.Copy();
            CommitKeys(valuesBefore.Keys.Where(p => !AnimValue.Same(p.Get(), valuesBefore[p].Value, p.Kind)).ToList(), valuesBefore);
            value.ResetPrevVal();
            ProjectAnimation after = Data.Clone();
            Revision++;

            return new ActionDelegates(
                delegate
                {
                    Data.CopyFrom(before);
                    SetValue(value, oldValue);
                    Invalidate();
                    Apply();
                    onChanged?.Invoke();
                },
                delegate
                {
                    Data.CopyFrom(after);
                    SetValue(value, newValue);
                    Invalidate();
                    Apply();
                    onChanged?.Invoke();
                });
        }

        // ---------------------------------------------------------------- undo

        // the values of the parameters of the nodes the views have shown (the timeline edits only these)
        private Dictionary<SdfAnimProperty, ValueBefore> SnapshotValues()
        {
            Dictionary<SdfAnimProperty, ValueBefore> values = new Dictionary<SdfAnimProperty, ValueBefore>();
            foreach (KeyValuePair<string, NodeTargets> n in nodes_)
            {
                foreach (SdfAnimTarget t in n.Value.Targets)
                {
                    foreach (SdfAnimProperty p in t.Props)
                        values[p] = new ValueBefore { ContainerId = n.Key, TargetId = t.Id, Value = p.Get() };
                }
            }
            return values;
        }

        // the changed properties with tracks get keys at the playhead (only the ones the animation drives)
        private void CommitKeys(List<SdfAnimProperty> changed, Dictionary<SdfAnimProperty, ValueBefore> valuesBefore)
        {
            if (!Editor.AutoKey)
                return;
            double t = Transport.KeyTime;
            foreach (SdfAnimProperty p in changed)
            {
                if (p.DisabledReason != null)
                    continue;
                ValueBefore v = valuesBefore[p];
                AnimTrack? track = Data.Find(v.ContainerId)?.FindTrack(v.TargetId, p.Path);
                if (track != null)
                    AnimAutoKey.Commit(track, v.Value, p.Get(), t);
            }
        }

        private void SaveUndo(ProjectAnimation before, ProjectAnimation after, Dictionary<SdfAnimProperty, AnimValue> valuesBefore, Dictionary<SdfAnimProperty, AnimValue> valuesAfter)
        {
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    Data.CopyFrom(before);
                    foreach (KeyValuePair<SdfAnimProperty, AnimValue> kv in valuesBefore)
                        kv.Key.Set(kv.Value);
                    Invalidate();
                    Apply();
                },
                delegate
                {
                    Data.CopyFrom(after);
                    foreach (KeyValuePair<SdfAnimProperty, AnimValue> kv in valuesAfter)
                        kv.Key.Set(kv.Value);
                    Invalidate();
                    Apply();
                }));
        }

        // a copy of the value before the edit
        private static ISimpleType CopyPrev(ISimpleType value)
        {
            ISimpleType copy = value.Copy();
            switch (value)
            {
                case ExSimpleType<float> f:     ((ExSimpleType<float>)copy).Val = f.PrevVal;    break;
                case ExSimpleType<int> i:       ((ExSimpleType<int>)copy).Val = i.PrevVal;      break;
                case ExSimpleType<bool> b:      ((ExSimpleType<bool>)copy).Val = b.PrevVal;     break;
                case ExSimpleType<Vector2> v2:  ((ExSimpleType<Vector2>)copy).Val = v2.PrevVal; break;
                case ExSimpleType<Vector3> v3:  ((ExSimpleType<Vector3>)copy).Val = v3.PrevVal; break;
                case ExSimpleType<Vector4> v4:  ((ExSimpleType<Vector4>)copy).Val = v4.PrevVal; break;
            }
            return copy;
        }

        private static void SetValue(ISimpleType value, ISimpleType from)
        {
            switch (value)
            {
                case ExSimpleType<float> f:     f.Val = ((ExSimpleType<float>)from).Val;        break;
                case ExSimpleType<int> i:       i.Val = ((ExSimpleType<int>)from).Val;          break;
                case ExSimpleType<bool> b:      b.Val = ((ExSimpleType<bool>)from).Val;         break;
                case ExSimpleType<Vector2> v2:  v2.Val = ((ExSimpleType<Vector2>)from).Val;     break;
                case ExSimpleType<Vector3> v3:  v3.Val = ((ExSimpleType<Vector3>)from).Val;     break;
                case ExSimpleType<Vector4> v4:  v4.Val = ((ExSimpleType<Vector4>)from).Val;     break;
            }
            value.ResetPrevVal();
        }
    }
}
