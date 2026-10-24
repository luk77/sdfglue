//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Dialogs;
using SdfGlueUi.Ui.Settings;
using SdfGlueUi.Ui.Windows;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Menu;
using SingleDocAppFramework.Ui.Settings;
using SingleDocAppFramework.Ui.Windows;

namespace SdfGlueUi.Ui
{
    public class UiManagerSdfGlue : UiManagerBase
    {
        public  const string            ViewGroupPreviews       = "Previews";
        public  const string            ViewGroupCode           = "Code";

        public  const int               NumOfPreviews           = 4;
        public  const int               NumOfCodeViews          = 4;

        public  bool                    EnabledDemoMode         = false;

        private PreviewViewMode         previewViewMode_        = PreviewViewMode.Editor;

        private WndPreview              WindowPreview;

        // Pass drawn under all windows (View -> Background preview), visible in the empty central area of the dockspace.
        // Not a registered window (no ImGui window, not in layouts); the choice is not saved.
        private WndPreview              backgroundPreview_;

        // File > Export image sequence... (modal)
        private DlgExportAnimation      exportAnimationDialog_;

        public UiManagerSdfGlue(IUiExecutorSdfGlue uiActionsExecutor)
            : base(uiActionsExecutor)
        {
            backgroundPreview_      = new WndPreview("Background preview", true);
            backgroundPreview_.SetUiManager(this);

            exportAnimationDialog_  = new DlgExportAnimation(uiActionsExecutor);

            // The first preview is the main one (the only one visible on start, used by maximized / fullscreen preview)
            WindowPreview           = new WndPreview("Preview 1");
            RegisterWindow(WindowPreview, ViewGroupPreviews, true);
            for(int i=1; i<NumOfPreviews; i++)
            {
                RegisterWindow(new WndPreview(String.Format("Preview {0}", i+1)), ViewGroupPreviews, false);
            }
            RegisterWindow(new WndExplorer());
            RegisterWindow(new WndInspector());
            RegisterWindow(new WndGeneratedCode("Code view 1"), ViewGroupCode, false);
            for(int i=1; i<NumOfCodeViews; i++)
            {
                RegisterWindow(new WndGeneratedCode(String.Format("Code view {0}", i+1)), ViewGroupCode, false);
            }
            RegisterWindow(new WndLog()                 , null, false);
            RegisterWindow(new WndDiagnostics()         , null, false);
            RegisterWindow(new WndPlayback());
            if (SdfGlueFeatures.UseSignals)
            {
                RegisterWindow(new WndSignals()         , null, false);
                RegisterWindow(new WndInputsSdfGlue()   , null, false);
            }
            // the keyframe animation (appended: the order of the windows gives their shortcuts)
            RegisterWindow(new WndSdfTimeline()         , null, false);
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

        public void OpenExportAnimationDialog()
        {
            exportAnimationDialog_.Open();
        }

        protected override void BuildAppModalDialogs()
        {
            exportAnimationDialog_.Build();
        }

        protected override bool IsAppModalDialogOpen()
        {
            return exportAnimationDialog_.IsOpen;
        }

        protected override MainMenuBase CreateMainMenu()
        {
            return new MainMenuSdfGlue(this);
        }

        protected override void RegisterSettingsCategories(SettingsRegistry registry)
        {
            base.RegisterSettingsCategories(registry);

            SettingsCategoriesSdfGlue.Register(registry);
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
