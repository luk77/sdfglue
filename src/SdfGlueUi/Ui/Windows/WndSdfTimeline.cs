//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.Animation;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppAnimation;
using SingleDocAppAnimation.Ui;
using SingleDocAppCore.Model.DataNodes;
using System.Runtime.CompilerServices;

namespace SdfGlueUi.Ui.Windows
{
    // The keyframe animation of the project (the timeline of the animation module, AnimationController): the sections
    // are the nodes with tracks and the selected node, the targets their function entities, the properties the parameters.
    // Its own time (independent of the Playback window and of iTime). The parameters the animation does not drive now
    // (a fixed object, a disabled operator, a bound signal) are greyed out; their tracks are kept.
    public class WndSdfTimeline : WndTimeline, IAnimRowSource
    {
        private readonly List<AnimSection>  sections_   = new List<AnimSection>();

        private IUiExecutorSdfGlue          Exec        { get { return (IUiExecutorSdfGlue)Executor; } }
        private SdfGlueAppContextBase       Ctx         { get { return Exec.GetAppContext(); } }
        private AnimationController         Anim        { get { return Ctx.Animation; } }

        protected override AnimEditorController? Editor { get { return Anim.Editor; } }

        // a new project: the view fits its length
        protected override string DocumentId { get { return RuntimeHelpers.GetHashCode(Ctx.Document).ToString(); } }

        protected override IAnimRowSource? Rows
        {
            get
            {
                sections_.Clear();
                DataModel model = Ctx.Document;
                TreeNode? selected = Ctx.Selection.SelectedNode;
                if (selected != null && Anim.TargetsOf(selected).Count > 0)
                    Anim.TracksOf(selected);    // the container of the selected node (its rows can be keyed)

                foreach (AnimNodeTracks c in model.Animation.Containers)
                {
                    TreeNode? node = Anim.NodeOf(c);
                    if (node == null || (c.Tracks.Count == 0 && node != selected))
                        continue;
                    IReadOnlyList<SdfAnimTarget> targets = Anim.TargetsOf(node);
                    if (targets.Count == 0)
                        continue;
                    sections_.Add(new AnimSection
                    {
                        Id          = c.Id,
                        Label       = SectionLabel(node),
                        Container   = c,
                        Targets     = targets,
                        Active      = node == selected,
                        Dim         = targets.All(t => t.Props.All(p => p.DisabledReason != null)),
                        Tag         = node,
                    });
                }
                return this;
            }
        }

        IReadOnlyList<AnimSection> IAnimRowSource.Sections { get { return sections_; } }

        bool IAnimRowSource.IsSelected(IAnimTarget target)
        {
            return false;
        }

        // a click on a row selects its node
        protected override void ActivateRow(AnimRow row)
        {
            if (row.Section.Tag is TreeNode node)
                Ctx.Selection.SelectedNode = node;
        }

        private static string SectionLabel(TreeNode node)
        {
            string kind = node is MaterialInstance ? "Material" : node is RenderPassData ? "Pass" : "Object";
            return String.Format("{0}: {1}", kind, node.Name.Val);
        }
    }
}
