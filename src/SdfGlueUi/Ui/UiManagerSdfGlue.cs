//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Windows;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Menu;
using SingleDocAppFramework.Ui.Windows;

namespace SdfGlueUi.Ui
{
    public class UiManagerSdfGlue : UiManagerBase
    {
        public  const string            ViewGroupPreviews       = "Previews";
        public  const string            ViewGroupCode           = "Code";

        public  bool                    EnabledDemoMode         = false;

        private PreviewViewMode         previewViewMode_        = PreviewViewMode.Editor;

        private WndPreview              WindowPreview;

        // Pass drawn under all windows (View -> Background preview), visible in the empty central area of the dockspace.
        // Not a registered window (no ImGui window, not in layouts); the choice is not saved.
        private WndPreview              backgroundPreview_;

        public UiManagerSdfGlue(IUiExecutorSdfGlue uiActionsExecutor)
            : base(uiActionsExecutor)
        {
            backgroundPreview_      = new WndPreview("Background preview", true);
            backgroundPreview_.SetUiManager(this);

            // The first preview is the main one (the only one visible on start, used by maximized / fullscreen preview)
            WindowPreview           = new WndPreview("Preview 1");
            RegisterWindow(WindowPreview, ViewGroupPreviews, true);
            for(int i=1; i<DataModel.NumOfPreviews; i++)
            {
                RegisterWindow(new WndPreview(String.Format("Preview {0}", i+1)), ViewGroupPreviews, false);
            }
            RegisterWindow(new WndExplorer());
            RegisterWindow(new WndInspector());
            RegisterWindow(new WndGeneratedCode("Code view 1"), ViewGroupCode, false);
            for(int i=1; i<DataModel.NumOfCodeViews; i++)
            {
                RegisterWindow(new WndGeneratedCode(String.Format("Code view {0}", i+1)), ViewGroupCode, false);
            }
            RegisterWindow(new WndLog()                 , null, false);
            RegisterWindow(new WndSettings()            , null, false);
            RegisterWindow(new WndDiagnostics()         , null, false);
            RegisterWindow(new WndPlayback());
            if (DataModel.UseSignals)
            {
                RegisterWindow(new WndSignals()         , null, false);
                RegisterWindow(new WndInputsSdfGlue()   , null, false);
            }
        }

        // Typed access to the executor passed to the constructor
        public IUiExecutorSdfGlue ActionsExecutor { get { return (IUiExecutorSdfGlue)Executor; } }

        // Fullscreen mode is applied to the OS window by the application (EditorMainWindow)
        public void SetPreviewViewMode(PreviewViewMode previewViewMode)
        {
            previewViewMode_ = previewViewMode;
        }
        public PreviewViewMode GetPreviewViewMode()
        {
            return previewViewMode_;
        }

        // Editor -> Maximized -> Fullscreen -> Editor
        public void CyclePreviewViewMode()
        {
            switch (previewViewMode_)
            {
                case PreviewViewMode.Editor:    previewViewMode_ = PreviewViewMode.Maximized;   break;
                case PreviewViewMode.Maximized: previewViewMode_ = PreviewViewMode.Fullscreen;  break;
                default:                        previewViewMode_ = PreviewViewMode.Editor;      break;
            }
        }

        protected override MainMenuBase CreateMainMenu()
        {
            return new MainMenuSdfGlue(this);
        }

        // Pass drawn by the application directly into the window, under the UI (null - nothing).
        // Fullscreen preview: the pass of the main preview, without any ImGui window.
        // Editor: the background preview (if a pass is chosen).
        public RenderPassData? GetScreenPreviewPass(out bool keepAspectRatio)
        {
            if (previewViewMode_ == PreviewViewMode.Fullscreen)
            {
                keepAspectRatio = WindowPreview.KeepAspectRatio;
                return WindowPreview.GetScreenPreviewPass();
            }

            keepAspectRatio = backgroundPreview_.KeepAspectRatio;

            if (previewViewMode_ == PreviewViewMode.Editor)
                return backgroundPreview_.GetScreenPreviewPass();

            return null;
        }

        public bool IsBackgroundPreviewActive()
        {
            return previewViewMode_ == PreviewViewMode.Editor && backgroundPreview_.GetScreenPreviewPass() != null;
        }

        // View -> Background preview
        public void BuildBackgroundPreviewMenuItems()
        {
            backgroundPreview_.BuildBackgroundMenuItems();
        }

        // Fullscreen: nothing of the dockspace is displayed, but the docked windows stay docked.
        // Background preview: the empty central area of the dockspace is transparent (and passes the mouse input).
        protected override ImGuiDockNodeFlags GetDockSpaceFlags()
        {
            if (previewViewMode_ == PreviewViewMode.Fullscreen)
                return ImGuiDockNodeFlags.KeepAliveOnly | ImGuiDockNodeFlags.PassthruCentralNode;

            if (IsBackgroundPreviewActive())
                return ImGuiDockNodeFlags.PassthruCentralNode;

            return ImGuiDockNodeFlags.None;
        }

        // In maximized / fullscreen preview mode only the preview image is shown (no menu, no windows)
        protected override bool SubmitOverrideUI()
        {
            if (previewViewMode_ == PreviewViewMode.Editor)
            {
                // input state of the background preview (the image is drawn by the application)
                if (IsBackgroundPreviewActive())
                    backgroundPreview_.BuildScreenPreview(false);
                return false;
            }

            if (previewViewMode_ == PreviewViewMode.Fullscreen)
                WindowPreview.BuildScreenPreview(true);     // the image is drawn by the application (no ImGui window)
            else
                WindowPreview.BuildFullPreview();

            return true;
        }

        // The background preview controls the camera only when a pass is chosen
        public override void HandleInput(float deltaTime)
        {
            base.HandleInput(deltaTime);

            if (IsBackgroundPreviewActive())
            {
                backgroundPreview_.HandleInput(deltaTime);
            }
            else
            {
                backgroundPreview_.IsHovered = false;
                backgroundPreview_.IsFocused = false;
            }
        }
    }
}
