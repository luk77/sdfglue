//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Controller;
using SdfGlueCore.Model.Animation;
using SingleDocAppAnimation;
using SingleDocAppAnimation.Ui;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppFramework.Ui.Properties;
using System.Numerics;

namespace SdfGlueUi.Ui.Properties
{
    // The animation of the parameters of one node in the property editors (Details): the key button next to a
    // parameter (or a channel of a vector), the frame colors of an animated value and the undo of an edit of an
    // animated value (auto-key: the edit is a key at the playhead, one undo step with the tracks).
    public sealed class SdfAnimKeys
    {
        private readonly AnimationController    anim_;
        private readonly TreeNode               node_;

        public SdfAnimKeys(AnimationController anim, TreeNode node)
        {
            anim_   = anim;
            node_   = node;
        }

        // the node can be animated (null: not)
        public static SdfAnimKeys? For(AnimationController anim, TreeNode? node)
        {
            return node != null && anim.TargetsOf(node).Count > 0 ? new SdfAnimKeys(anim, node) : null;
        }

        // The key button of a parameter (channel -1: all its channels), followed by SameLine. Nothing is drawn for
        // a value that is not animatable or that the animation does not drive now (fixed, a bound signal).
        public void BuildKeyButton(ref int id, ISimpleType value, int channel = -1)
        {
            ImGui.PushID(id++);
            if (Find(value, channel, out SdfAnimTarget? target, out List<string> paths, out string? disabled) && disabled == null)
            {
                AnimNodeTracks tracks = anim_.TracksOf(node_);
                AnimKeyState state = AnimEditorController.StateOf(tracks, target!.Id, paths, anim_.Transport.KeyTime);
                if (AnimKeyButton.Button("##key", state, state == AnimKeyState.OnKey ? "Remove the key at the playhead" : "Add a key at the playhead (Timeline)"))
                    anim_.Editor.ToggleKeys(tracks, target, paths);
                ImGui.SameLine();
            }
            ImGui.PopID();
        }

        // the frame colors of an animated value (call PopColors after the widget)
        public int PushColors(ISimpleType value, int channel = -1)
        {
            if (!Find(value, channel, out SdfAnimTarget? target, out List<string> paths, out string? disabled) || disabled != null)
                return 0;
            AnimKeyState state = AnimEditorController.StateOf(anim_.TracksOf(node_), target!.Id, paths, anim_.Transport.KeyTime);
            return AnimKeyButton.PushColors(state);
        }

        public static void PopColors(int pushed)
        {
            AnimKeyButton.PopColors(pushed);
        }

        // The undo of the widget just built: an animated value is keyed by the edit (true), otherwise the caller
        // saves its own action (false).
        public bool HandleUndo(ISimpleType value)
        {
            AnimNodeTracks tracks = anim_.TracksOf(node_);
            bool animated = anim_.FindProperties(node_, value).Any(tp => tp.Property.DisabledReason == null && tracks.FindTrack(tp.Target.Id, tp.Property.Path) != null);
            if (!animated)
                return false;
            UndoEditTracker.HandleLastItem(() => IsChanged(value), () => anim_.CommitParameterEdit(node_, value));
            return true;
        }

        // the properties of the value (of a channel) the animation drives; disabled: none, the reason of the first one
        private bool Find(ISimpleType value, int channel, out SdfAnimTarget? target, out List<string> paths, out string? disabled)
        {
            target      = null;
            paths       = new List<string>();
            disabled    = null;
            foreach ((SdfAnimTarget t, SdfAnimProperty p) in anim_.FindProperties(node_, value))
            {
                if (channel >= 0 && p.Channel >= 0 && p.Channel != channel)
                    continue;
                target = t;
                string? reason = p.DisabledReason;
                if (reason == null)
                    paths.Add(p.Path);
                else
                    disabled ??= reason;
            }
            if (paths.Count > 0)
                disabled = null;
            return target != null;
        }

        private static bool IsChanged(ISimpleType value)
        {
            switch (value)
            {
                case ExSimpleType<float> f:     return !f.PrevVal.Equals(f.Val);
                case ExSimpleType<int> i:       return !i.PrevVal.Equals(i.Val);
                case ExSimpleType<Vector2> v2:  return !v2.PrevVal.Equals(v2.Val);
                case ExSimpleType<Vector3> v3:  return !v3.PrevVal.Equals(v3.Val);
                case ExSimpleType<Vector4> v4:  return !v4.PrevVal.Equals(v4.Val);
                default:                        return false;
            }
        }
    }
}
