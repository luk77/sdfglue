//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using ImGuiNET;
using System.Numerics;
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueUi.Ui.Components
{
    public class MenuRenderPass
    {
        private static readonly string      lastSelectedPassLabel_      = "-- Last selected pass --";
        private static readonly string      finalPassLabel_             = "-- Final pass --";
        private static readonly string      noneLabel_                  = "-- None --";

        public enum PreviewMode
        {
            LastSelectedPass,
            FinalPass,
            Custom,
            None        // nothing is displayed (background preview)
        }

        public static void Build(SdfGlueAppContextBase ctx, ref PreviewMode previewMode, ref RenderPassData? selectedPass)
        {
            /*
            ImGui.SetNextItemWidth(300);
            List<string> passes = new List<string>();
            foreach (TreeNode tn in renderingSysData.Children)
            {
                RenderPassData? p = tn as RenderPassData;
                if (p == null)
                    continue;

                passes.Add(p.Name.Val);
            }
            string[] arrPasses = passes.ToArray();

            ImGui.Combo("", ref selectedPassIndex_, arrPasses, arrPasses.Length, arrPasses.Length);
            */

            string? buttonLabel = lastSelectedPassLabel_;    // PreviewMode.LastSelectedPass
            if (previewMode == PreviewMode.FinalPass)
                buttonLabel = finalPassLabel_;
            if (previewMode == PreviewMode.Custom)
            {
                if (selectedPass != null)
                    buttonLabel = selectedPass.Name.Val;
            }

            if (ImGui.Button(buttonLabel, new Vector2(450, 0)))
            {
                ImGui.OpenPopup("menu_render_passes");
            }
            if (ImGui.BeginPopup("menu_render_passes"))
            {
                BuildMenuItems(ctx.Document, ref previewMode, ref selectedPass, false);
                ImGui.EndPopup();
            }

            ResolveSelectedPass(ctx, previewMode, ref selectedPass);
        }

        // Menu items: [none], last selected pass, all passes, final pass (inside a popup or a menu)
        public static void BuildMenuItems(DataModel model, ref PreviewMode previewMode, ref RenderPassData? selectedPass, bool withNone)
        {
            if (withNone)
            {
                if (ImGui.MenuItem(noneLabel_, "", previewMode == PreviewMode.None))
                {
                    previewMode = PreviewMode.None;
                    selectedPass = null;
                }
                ImGui.Separator();
            }
            // last selected pass
            if (ImGui.MenuItem(lastSelectedPassLabel_, "", previewMode == PreviewMode.LastSelectedPass))
            {
                previewMode = PreviewMode.LastSelectedPass;
                selectedPass = null;
            }
            ImGui.Separator();
            // all, one by one
            foreach (TreeNode tn in model.RenderingSysData.Children)
            {
                RenderPassData? p = tn as RenderPassData;
                if (p == null)
                    continue;
                if (ImGui.MenuItem(p.Name.Val, "", previewMode == PreviewMode.Custom && selectedPass == p))
                {
                    previewMode = PreviewMode.Custom;
                    selectedPass = p;
                }
            }
            ImGui.Separator();
            // final pass
            if (ImGui.MenuItem(finalPassLabel_, "", previewMode == PreviewMode.FinalPass))
            {
                previewMode = PreviewMode.FinalPass;
                selectedPass = null;
            }
        }

        // The pass to display for the mode (LastSelectedPass / FinalPass follow the selection / model, None - null)
        public static void ResolveSelectedPass(SdfGlueAppContextBase ctx, PreviewMode previewMode, ref RenderPassData? selectedPass)
        {
            if (previewMode == PreviewMode.None)
            {
                selectedPass = null;
            }
            else if (previewMode == MenuRenderPass.PreviewMode.LastSelectedPass)
            {
                selectedPass = ctx.Selection.LastSelectedRPass;
            }
            else if (previewMode == MenuRenderPass.PreviewMode.FinalPass)
            {
                selectedPass = ctx.Document.GetFinalRPass();
            }
            else
            {
                //selectedPass = selectedPass_;
            }
        }
    }
}
