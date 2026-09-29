//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppFramework.Ui.Components;
using SingleDocAppFramework.Ui.Menu;

namespace SdfGlueUi.Ui
{
    // SdfGlue additions to the framework main menu
    public class MainMenuSdfGlue : MainMenuBase
    {
        private UiManagerSdfGlue        uiMgrSdfGlue_;

        public MainMenuSdfGlue(UiManagerSdfGlue uiMgr)
            : base(uiMgr)
        {
            uiMgrSdfGlue_ = uiMgr;
        }

        private IUiExecutorSdfGlue ExecutorSdfGlue { get { return uiMgrSdfGlue_.ActionsExecutor; } }

        protected override void BuildFileMenuExtras()
        {
            ImGui.Separator();
            if (ImGui.MenuItem("Save image as", "CTRL+I"))                      { ExecutorSdfGlue.OnSaveImage(); }
            if (ImGui.MenuItem("Export image sequence", ""))                    { ExecutorSdfGlue.OnExportAnimation(); }

            ImGui.Separator();
            if (Directory.Exists("Examples"))
            {
                if (ImGui.BeginMenu("Examples"))
                {
                    string exampleToOpen = MenuFileTree.Build("Examples", "*.xml");
                    if (!String.IsNullOrEmpty(exampleToOpen))
                    {
                        ExecutorSdfGlue.OnOpenDocument(exampleToOpen);
                    }
                    ImGui.EndMenu();
                }
            }
        }

        protected override void BuildEditMenuExtras()
        {
            TreeNode? selectedNode = ExecutorSdfGlue.GetModel().SelectedNode;

            ImGui.Separator();
            if (selectedNode is RenderPassData || selectedNode is RenderingData)
            {
                RenderPassData? selectedPass = selectedNode as RenderPassData;

                if (ImGui.MenuItem("Cut", "CTRL+X", false, selectedPass != null))   { ExecutorSdfGlue.OnCutRenderPass   (selectedPass); }
                if (ImGui.MenuItem("Copy", "CTRL+C", false, selectedPass != null))  { ExecutorSdfGlue.OnCopyRenderPass  (selectedPass); }
                if (ImGui.MenuItem("Paste", "CTRL+V"))                              { ExecutorSdfGlue.OnPasteRenderPass (selectedNode); }
            }
            else if (selectedNode is MaterialInstance || selectedNode is MaterialsCollection)
            {
                MaterialInstance? selectedMaterial = selectedNode as MaterialInstance;

                if (ImGui.MenuItem("Cut", "CTRL+X", false, selectedMaterial != null))   { ExecutorSdfGlue.OnCutMaterial     (selectedMaterial); }
                if (ImGui.MenuItem("Copy", "CTRL+C", false, selectedMaterial != null))  { ExecutorSdfGlue.OnCopyMaterial    (selectedMaterial); }
                if (ImGui.MenuItem("Paste", "CTRL+V"))                                  { ExecutorSdfGlue.OnPasteMaterial   (selectedNode); }
            }
            else
            {
                SdfObject? selectedObject = selectedNode as SdfObject;

                if (ImGui.MenuItem("Cut", "CTRL+X"))    { ExecutorSdfGlue.OnCutObject   (selectedObject); }
                if (ImGui.MenuItem("Copy", "CTRL+C"))   { ExecutorSdfGlue.OnCopyObject  (selectedObject); }
                if (ImGui.MenuItem("Paste", "CTRL+V"))  { ExecutorSdfGlue.OnPasteObject (selectedObject); }
            }
        }

        protected override void BuildAppMenus()
        {
            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Reload SDF Definitions", "CTRL+E"))         { ExecutorSdfGlue.OnReloadSdfDefinitions(); }
                if (ImGui.MenuItem("Compile shader", "CTRL+R, CTRL+ENTER"))     { ExecutorSdfGlue.OnRebuildShader(); }
                if (ImGui.MenuItem("Reset frame counter", ""))                  { ExecutorSdfGlue.OnResetFrameCounter(); }

                ImGui.Separator();
                ImGui.Checkbox("Demo mode", ref uiMgrSdfGlue_.EnabledDemoMode);

                ImGui.EndMenu();
            }
        }

        protected override void BuildDevelopmentMenuExtras()
        {
            if (ImGui.MenuItem("Batch process all projects", ""))               { ExecutorSdfGlue.OnBatchProcessAllProjects(); }
        }

        protected override void BuildViewMenuExtras()
        {
            bool fullPreviewMode = uiMgrSdfGlue_.GetFullPreviewMode();
            if (ImGui.Checkbox("Full screen preview", ref fullPreviewMode))
                uiMgrSdfGlue_.SetFullPreviewMode(fullPreviewMode);
        }
    }
}
