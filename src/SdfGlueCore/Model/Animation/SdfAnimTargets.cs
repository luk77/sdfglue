//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Controller.CodeGen;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueCore.Model.Entities;
using SingleDocAppAnimation;
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppCore.Utils;
using System.Numerics;

namespace SdfGlueCore.Model.Animation
{
    // How the animation drives a parameter now
    public enum SdfAnimState
    {
        Active,     // a uniform: the value of the animation at the playhead
        Baked,      // a constant of the shader (a fixed object) or not used (a disabled operator): the value at 0
        Signal,     // driven by a signal (the signal wins): the value of the animation is kept for the moment it is unbound
    }

    // One animatable channel of a parameter (ExFloat, ExInt, a channel of ExVector2/3/4 or a vec3 color).
    // The value is written to Val and PrevVal (the undo of the property editors stays consistent).
    public sealed class SdfAnimProperty : IAnimProperty
    {
        public  const string                TextFixedObject     = "Fixed - the value is a constant of the shader, the animation is disabled (the value at 0 s)";
        public  const string                TextFixAll          = "\"Fix all objects\" is on - the animation is disabled (the value at 0 s)";
        public  const string                TextDisabledOp      = "The operator is disabled - the animation is disabled (the value at 0 s)";
        public  const string                TextSignal          = "Driven by a signal - the animation is used when the signal is unbound";

        public  readonly ISimpleType        Value;
        public  readonly int                Channel;            // -1: the whole value (a number, a color)
        private readonly bool               degrees_;
        private readonly Func<string?>      fixedReason_;       // null: the owner is not fixed
        private readonly Func<string?>      offReason_;         // null: the owner is used

        public  string                      Path        { get; }
        public  string                      Group       { get; }
        public  string                      Label       { get; }
        public  AnimValueKind               Kind        { get; }
        public  bool                        Discrete    { get { return false; } }
        public  int                         Order       { get { return 0; } }
        public  AnimPropertyHints           Hints       { get; }

        public SdfAnimProperty(string path, string group, string label, AnimValueKind kind, AnimPropertyHints hints, ISimpleType value, int channel, bool degrees, Func<string?> fixedReason, Func<string?> offReason)
        {
            Path            = path;
            Group           = group;
            Label           = label;
            Kind            = kind;
            Hints           = hints;
            Value           = value;
            Channel         = channel;
            degrees_        = degrees;
            fixedReason_    = fixedReason;
            offReason_      = offReason;
        }

        public SdfAnimState State
        {
            get { return GetState(out _); }
        }

        // null: the animation drives the parameter
        public string? DisabledReason
        {
            get
            {
                GetState(out string? reason);
                return reason;
            }
        }

        private SdfAnimState GetState(out string? reason)
        {
            reason = offReason_();
            if (reason != null)
                return SdfAnimState.Baked;
            if (IsSignalBound())
            {
                reason = TextSignal;
                return SdfAnimState.Signal;
            }
            reason = fixedReason_();
            return reason != null ? SdfAnimState.Baked : SdfAnimState.Active;
        }

        private bool IsSignalBound()
        {
            if (!SdfGlueFeatures.UseSignals || Value is not ISignalBindable bindable)
                return false;
            if (Channel >= 0)
                return Channel < bindable.ChannelCount && bindable.GetSignalId(Channel) != 0;
            return SignalBinding.HasBinding(bindable);
        }

        public AnimValue Get()
        {
            return Read(false);
        }

        // the value before the current edit of a property editor (PrevVal)
        public AnimValue GetPrev()
        {
            return Read(true);
        }

        private AnimValue Read(bool prev)
        {
            switch (Value)
            {
                case ExFloat f:     return AnimValue.Of(ToDisplay(prev ? f.PrevVal : f.Val));
                case ExInt i:       return AnimValue.Of(prev ? i.PrevVal : i.Val);
                case ExVector2 v2:
                    Vector2 val2 = prev ? v2.PrevVal : v2.Val;
                    return AnimValue.Of(ToDisplay(Channel == 0 ? val2.X : val2.Y));
                case ExVector3 v3:
                    Vector3 val3 = prev ? v3.PrevVal : v3.Val;
                    if (Kind == AnimValueKind.Color)
                        return AnimValue.Color(val3.X * 255.0, val3.Y * 255.0, val3.Z * 255.0, 255.0);
                    return AnimValue.Of(ToDisplay(Component(val3, Channel)));
                case ExVector4 v4:  return AnimValue.Of(ToDisplay(Component(prev ? v4.PrevVal : v4.Val, Channel)));
                default:            return default;
            }
        }

        public void Set(AnimValue v)
        {
            switch (Value)
            {
                case ExFloat f:
                    f.Val = (float)(degrees_ ? v.X * GMath.DegToRad : v.X);
                    break;
                case ExInt i:
                    i.Val = (int)Math.Round(v.X);
                    break;
                case ExVector2 v2:
                    v2.Val = Channel == 0 ? new Vector2(FromDisplay(v.X), v2.Val.Y) : new Vector2(v2.Val.X, FromDisplay(v.X));
                    break;
                case ExVector3 v3:
                    if (Kind == AnimValueKind.Color)
                        v3.Val = new Vector3((float)(v.X / 255.0), (float)(v.Y / 255.0), (float)(v.Z / 255.0));
                    else
                        v3.Val = WithComponent(v3.Val, Channel, FromDisplay(v.X));
                    break;
                case ExVector4 v4:
                    v4.Val = WithComponent(v4.Val, Channel, FromDisplay(v.X));
                    break;
                default:
                    return;
            }
            Value.ResetPrevVal();
        }

        private double ToDisplay(float val)
        {
            return degrees_ ? val * GMath.RadToDeg : val;
        }

        private float FromDisplay(double val)
        {
            return (float)(degrees_ ? val * GMath.DegToRad : val);
        }

        private static float Component(Vector3 v, int ch)
        {
            return ch == 0 ? v.X : ch == 1 ? v.Y : v.Z;
        }

        private static float Component(Vector4 v, int ch)
        {
            return ch == 0 ? v.X : ch == 1 ? v.Y : ch == 2 ? v.Z : v.W;
        }

        private static Vector3 WithComponent(Vector3 v, int ch, float val)
        {
            if (ch == 0) v.X = val; else if (ch == 1) v.Y = val; else v.Z = val;
            return v;
        }

        private static Vector4 WithComponent(Vector4 v, int ch, float val)
        {
            if (ch == 0) v.X = val; else if (ch == 1) v.Y = val; else if (ch == 2) v.Z = val; else v.W = val;
            return v;
        }
    }

    // A function entity (or the own parameters) of a node as a target of the animation
    public sealed class SdfAnimTarget : IAnimTarget
    {
        public  readonly List<SdfAnimProperty>                  Props       = new List<SdfAnimProperty>();
        private readonly Dictionary<string, SdfAnimProperty>    byPath_     = new Dictionary<string, SdfAnimProperty>();

        public  int                                             Id          { get; }
        public  string                                          Label       { get; }
        public  IEnumerable<IAnimProperty>                      Properties  { get { return Props; } }
        public  IEnumerable<IAnimTarget>                        Children    { get { return Enumerable.Empty<IAnimTarget>(); } }

        public SdfAnimTarget(int id, string label)
        {
            Id      = id;
            Label   = label;
        }

        internal void Add(SdfAnimProperty p)
        {
            if (byPath_.TryAdd(p.Path, p))
                Props.Add(p);
        }

        public IAnimProperty? FindProperty(string path)
        {
            return FindSdfProperty(path);
        }

        public SdfAnimProperty? FindSdfProperty(string path)
        {
            return byPath_.TryGetValue(path, out SdfAnimProperty? p) ? p : null;
        }

        public string? DisabledReason(string path)
        {
            return FindSdfProperty(path)?.DisabledReason;
        }
    }

    // The animatable parameters of the nodes of a project. A node is a container of the tracks (ProjectAnimation.ContainerId),
    // its function entities are the targets (ids below), the parameters are the properties: "<type>:<name>" (the key of
    // ParametersValuesCollection), the channels of a vector "<type>:<name>.x" .. ".w", a vec3 color (editor="color") is one
    // property. Angles (editInDegrees) are animated in degrees. Only the values that are uniforms or CPU-side can be
    // animated: no compilation parameters, no flags that change the generated code.
    public static class SdfAnimTargets
    {
        public const    int             TargetNode          = 1;        // the own parameters of the node (blending)
        public const    int             TargetFunction      = 2;        // SDF function, material, renderer
        public const    int             TargetSecondary     = 3;        // mix operator, backdrop
        public const    int             PositionOpsBase     = 1000;     // + OperatorEntity.Id
        public const    int             DistanceOpsBase     = 2000;
        public const    int             CameraOpsBase       = 3000;

        public const    string          PathBlend           = "blend";
        public const    string          PathMaterialBlend   = "material_blend";

        private static readonly string[] ChannelNames       = { "x", "y", "z", "w" };

        // The targets of a node (empty for other nodes). The fixed state is read when it is needed (it can change).
        public static List<SdfAnimTarget> Build(DataModel model, TreeNode node)
        {
            List<SdfAnimTarget> targets = new List<SdfAnimTarget>();
            ShaderExportTarget preview = ShaderExportTarget.Preview;

            if (node is SdfObject obj)
            {
                Func<string?> fixedReason = () => FixedReason(model, UniformRules.IsFixed(model, preview, obj.IsFixed));
                SdfAnimTarget own = new SdfAnimTarget(TargetNode, "Object");
                own.Add(Number(PathBlend, "", "Blending", obj.BlendFactor, Hints(0.01f, LimitsType.Min, 0, 0), fixedReason, NotOff));
                own.Add(Number(PathMaterialBlend, "", "Material Blending", obj.MaterialBlendFactor, Hints(0.01f, LimitsType.Min, 0, 0), fixedReason, NotOff));
                targets.Add(own);
                AddEntity(targets, TargetFunction, "Shape", obj.FunctionSdf, false, fixedReason, NotOff);
                AddEntity(targets, TargetSecondary, "Mix", obj.FunctionMixOp, false, fixedReason, NotOff);
                AddOperators(targets, PositionOpsBase, "Position", obj.PositionOperators, fixedReason);
                AddOperators(targets, DistanceOpsBase, "Distance", obj.DistanceOperators, fixedReason);
            }
            else if (node is MaterialInstance mat)
            {
                Func<string?> fixedReason = () => FixedReason(model, UniformRules.IsMaterialFixed(model, preview, mat));
                AddEntity(targets, TargetFunction, "Material", mat.MaterialProps, true, fixedReason, NotOff);
            }
            else if (node is RenderPassData pass)
            {
                Func<string?> fixedReason = () => FixedReason(model, UniformRules.IsFixed(model, preview, pass.IsFixed));
                Func<string?> cameraFixed = () => FixedReason(model, UniformRules.IsCameraFixed(model, preview));
                AddEntity(targets, TargetFunction, "Renderer", pass.RendererFunc, false, fixedReason, NotOff);
                if (pass.IsPrimaryPass)
                {
                    AddEntity(targets, TargetSecondary, "Backdrop", pass.BackdropFunc, false, fixedReason, NotOff);
                    AddOperators(targets, CameraOpsBase, "Camera", pass.CameraOperators, cameraFixed);
                }
            }
            targets.RemoveAll(t => t.Props.Count == 0);
            return targets;
        }

        // the node of a container id (null: it no longer exists)
        public static TreeNode? FindNode(DataModel model, string containerId)
        {
            TreeNode? found = null;
            if (containerId.StartsWith("sdf:", StringComparison.Ordinal))
            {
                TreeNode.CallRecursive(model.SdfRoot, delegate(TreeNode node)
                {
                    if (found == null && node is SdfObject && ProjectAnimation.ContainerId(node) == containerId)
                        found = node;
                });
                return found;
            }
            IEnumerable<TreeNode> nodes = containerId.StartsWith("mat:", StringComparison.Ordinal) ? model.Materials.Children
                                        : containerId.StartsWith("pass:", StringComparison.Ordinal) ? model.RenderingSysData.Children
                                        : Enumerable.Empty<TreeNode>();
            return nodes.FirstOrDefault(n => ProjectAnimation.ContainerId(n) == containerId);
        }

        // the time a property is evaluated at: the start for a parameter the animation does not drive now
        public static double TimeOf(IAnimProperty property, double playhead)
        {
            return property is SdfAnimProperty p && p.State == SdfAnimState.Baked ? 0.0 : playhead;
        }

        private static string? NotOff()
        {
            return null;
        }

        private static string? FixedReason(DataModel model, bool isFixed)
        {
            if (!isFixed)
                return null;
            return model.ProjSettings.FixAllObjects.Val ? SdfAnimProperty.TextFixAll : SdfAnimProperty.TextFixedObject;
        }

        private static AnimPropertyHints Hints(float speed, LimitsType limits, float min, float max)
        {
            AnimPropertyHints h = new AnimPropertyHints { DragStep = speed };
            if (limits != LimitsType.None)
                h.Min = min;
            if (limits == LimitsType.MinMax)
                h.Max = max;
            return h;
        }

        private static SdfAnimProperty Number(string path, string group, string label, ExFloat value, AnimPropertyHints hints, Func<string?> fixedReason, Func<string?> offReason)
        {
            return new SdfAnimProperty(path, group, label, AnimValueKind.Number, hints, value, -1, false, fixedReason, offReason);
        }

        private static void AddOperators(List<SdfAnimTarget> targets, int idBase, string kind, OperatorsCollection operators, Func<string?> fixedReason)
        {
            foreach (OperatorEntity op in operators.Operators)
            {
                OperatorEntity o = op;
                AddEntity(targets, idBase + op.Id, kind, op, false, fixedReason, () => o.Enabled.Val ? null : SdfAnimProperty.TextDisabledOp);
            }
        }

        private static void AddEntity(List<SdfAnimTarget> targets, int id, string kind, FunctionEntity? entity, bool materialParams,
                                      Func<string?> fixedReason, Func<string?> offReason)
        {
            FunctionDefinition? def = entity?.Definition;
            if (entity == null || def == null)
                return;

            string name = def.DisplayName ?? def.FunctionName ?? "";
            SdfAnimTarget target = new SdfAnimTarget(id, String.IsNullOrEmpty(name) ? kind : kind + ": " + name);
            foreach (FunctionDefParameter p in materialParams ? def.MaterialParameters : def.Parameters)
            {
                if (!p.IsEditable() || !entity.ParametersValues.TryGetValue(p.ParameterKey, out ISimpleType? value))
                    continue;

                string label = p.DisplayName ?? p.ParameterName ?? p.ParameterKey;
                AnimPropertyHints hints = Hints(p.ValSpeed, p.LimitsType, p.MinVal, p.MaxVal);
                if (p.EditInDegrees)
                    hints = Hints(p.ValSpeed, LimitsType.None, 0, 0);
                switch (value)
                {
                    case ExFloat:
                        target.Add(new SdfAnimProperty(p.ParameterKey, "", label, AnimValueKind.Number, hints, value, -1, p.EditInDegrees, fixedReason, offReason));
                        break;
                    case ExInt:
                        AnimValueKind kindInt = p.EditorType == ParamEditorType.Toggle ? AnimValueKind.Bool : p.EditorType == ParamEditorType.Combo ? AnimValueKind.Enum : AnimValueKind.Int;
                        if (kindInt == AnimValueKind.Enum)
                            hints.EnumNames = p.Options;
                        hints.Decimals = 0;
                        target.Add(new SdfAnimProperty(p.ParameterKey, "", label, kindInt, hints, value, -1, false, fixedReason, offReason));
                        break;
                    case ExVector3 when p.EditorType == ParamEditorType.Color:
                        target.Add(new SdfAnimProperty(p.ParameterKey, "", label, AnimValueKind.Color, hints, value, -1, false, fixedReason, offReason));
                        break;
                    case ExVector2:
                    case ExVector3:
                    case ExVector4:
                        int channels = value is ExVector2 ? 2 : value is ExVector3 ? 3 : 4;
                        for (int ch = 0; ch < channels; ch++)
                        {
                            target.Add(new SdfAnimProperty(p.ParameterKey + "." + ChannelNames[ch], label, ChannelNames[ch].ToUpperInvariant(), AnimValueKind.Number,
                                                           hints, value, ch, p.EditInDegrees, fixedReason, offReason));
                        }
                        break;
                }
            }
            targets.Add(target);
        }
    }
}
