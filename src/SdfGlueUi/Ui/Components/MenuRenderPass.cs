//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model;
using ImGuiNET;
using System.Numerics;

namespace SdfGlueUi.Ui.Components
{
    public class MenuRenderPass
    {
        private static readonly string      lastSelectedPassLabel_      = "-- Last selected pass --";
        private static readonly string      finalPassLabel_             = "-- Final pass --";

        public enum PreviewMode
        {
            LastSelectedPass,
            FinalPass,
            Custom
        }

        public static void Build(DataModel model, ref PreviewMode previewMode, ref RenderPassData? selectedPass)
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
                // last selected pass
                if (ImGui.MenuItem(lastSelectedPassLabel_))
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
                    if (ImGui.MenuItem(p.Name.Val))
                    {
                        previewMode = PreviewMode.Custom;
                        selectedPass = p;
                    }
                }
                ImGui.Separator();
                // final pass
                if (ImGui.MenuItem(finalPassLabel_))
                {
                    previewMode = PreviewMode.FinalPass;
                    selectedPass = null;
                }
                ImGui.EndPopup();
            }

            if (previewMode == MenuRenderPass.PreviewMode.LastSelectedPass)
            {
                selectedPass = model.LastSelectedRPass;
            }
            else if (previewMode == MenuRenderPass.PreviewMode.FinalPass)
            {
                selectedPass = model.GetFinalRPass();
            }
            else
            {
                //selectedPass = selectedPass_;
            }
        }
    }
}
