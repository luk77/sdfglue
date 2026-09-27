//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
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

        public  int                     ExplorerHeight          = 0;
        public  int                     InspectorPosY           = 0;
        public  int                     InspectorHeight         = 0;
        public  int                     PlaybackHeight          = 0;
        public  int                     PlaybackPosY            = 0;
        public  int                     PreviewWidth            = 0;
        public  int                     PreviewHeight           = 0;
        public  int                     MaterialsHeight         = 0;
        public  int                     RenderParamsPosY        = 0;
        public  int                     RenderParamsHeight      = 0;
        public  int                     LogPosY                 = 0;
        public  int                     LogHeight               = 0;

        public  bool                    EnabledDemoMode         = false;

        private bool                    fullPreviewMode_        = false;

        private UiWindowBase            WindowPreview           = null;
        private UiWindowBase            WindowExplorer          = null;
        private UiWindowBase            WindowInspector         = null;
        private UiWindowBase            WindowGeneratedCode     = null;
        private UiWindowBase            WindowLog               = null;
        private UiWindowBase            WindowSettings          = null;
        private UiWindowBase            WindowDiagnostics       = null;
        private UiWindowBase            WindowPlayback          = null;

        public UiManagerSdfGlue(IUiExecutorSdfGlue uiActionsExecutor, int mainWindowSizeX, int mainWindowSizeY)
            : base(uiActionsExecutor)
        {
            if (DataModel.UseMultiplePreviews)
            {
                for(int i=0; i<DataModel.NumOfPreviews; i++)
                {
                    WndPreview? wndPreview = RegisterWindow(new WndPreview(String.Format("Preview {0}", i+1)), ViewGroupPreviews, i == 0) as WndPreview;

                    if (i == 0)
                        WindowPreview = wndPreview;
                }
            }
            else
            {
                WindowPreview           = RegisterWindow(new WndPreview("Preview"));
            }
            WindowExplorer          = RegisterWindow(new WndExplorer());
            WindowInspector         = RegisterWindow(new WndInspector());
            if (DataModel.UseMultipleCodeViews)
            {
                for(int i=0; i<DataModel.NumOfCodeViews; i++)
                {
                    WndGeneratedCode? wnd = RegisterWindow(new WndGeneratedCode(String.Format("Code view {0}", i+1)), ViewGroupCode, false) as WndGeneratedCode;
                    if (i == 0)
                        WindowGeneratedCode = wnd;
                }
            }
            else
            {
                WindowGeneratedCode     = RegisterWindow(new WndGeneratedCode("Generated Code"), null, false);
            }
            WndLog wndLog           = new WndLog();
            wndLog.PlacementProvider = () => new WindowRect(CenterColPosX, LogPosY, CenterColWidth, LogHeight);
            WindowLog               = RegisterWindow(wndLog                     , null, false);
            WindowSettings          = RegisterWindow(new WndSettings()          , null, false);
            WindowDiagnostics       = RegisterWindow(new WndDiagnostics()       , null, false);
            WindowPlayback          = RegisterWindow(new WndPlayback());


            SetMainWindowClientSize(mainWindowSizeX, mainWindowSizeY);
        }

        // Typed access to the executor passed to the constructor
        public IUiExecutorSdfGlue ActionsExecutor { get { return (IUiExecutorSdfGlue)Executor; } }

        public override void SetMainWindowClientSize(int mainWindowSizeX, int mainWindowSizeY)
        {
            base.SetMainWindowClientSize(mainWindowSizeX, mainWindowSizeY);

            //bool previewExpandWidth = !(WindowMaterials.IsVisible || WindowSceneInspector.IsVisible);
            bool previewExpandWidth = true;//!(WindowMaterials.IsVisible);
            //bool previewExpandHeight = !(WindowRendererParams.IsVisible || WindowLog.IsVisible || WindowDiagnostics.IsVisible);
            bool previewExpandHeight = !(WindowLog.IsVisible || WindowDiagnostics.IsVisible);

            //WindowsScaling      = UiManager.GetWindowsScaling();
            WindowsScaling      = ActionsExecutor.GetWindowsScaling();


            ExplorerHeight      = (int)(0.35 * MainWindowSizeY);
            PlaybackHeight      = (int)(0.16 * MainWindowSizeY);
            InspectorPosY       = BasePosY + ExplorerHeight + DistanceY;
            InspectorHeight     = MainWindowSizeY - ExplorerHeight - PlaybackHeight - 4 * DistanceY - MenuHeight;
            PlaybackPosY        = InspectorPosY + InspectorHeight + DistanceY;
            LogPosY             = PlaybackPosY;
            LogHeight           = PlaybackHeight;
            PreviewWidth        = previewExpandWidth ? (CenterColWidth + RightColWidth + DistanceX) : CenterColWidth;
            PreviewHeight       = previewExpandHeight ? BaseHeight : (int)(BaseHeight - PlaybackHeight - DistanceY);
            MaterialsHeight     = (int)(MainWindowSizeY * 0.5);
            RenderParamsPosY    = BasePosY + PreviewHeight + DistanceY;
            RenderParamsHeight  = 0;//(WindowLog.IsVisible || WindowDiagnostics.IsVisible) ? ((int)(MainWindowSizeY * 0.2)) : (MainWindowSizeY - PreviewHeight - 3 * DistanceY - MenuHeight);
            //LogPosY             = RenderParamsPosY + RenderParamsHeight + DistanceY;
            //LogHeight           = MainWindowSizeY - PreviewHeight - RenderParamsHeight - 4 * DistanceY - MenuHeight;
        }

        public void SetFullPreviewMode(bool fullPreviewMode)
        {
            fullPreviewMode_ = fullPreviewMode;
        }
        public bool GetFullPreviewMode()
        {
            return fullPreviewMode_;
        }

        protected override MainMenuBase CreateMainMenu()
        {
            return new MainMenuSdfGlue(this);
        }

        // In full preview mode only the preview image is shown (no menu, no windows)
        protected override bool SubmitOverrideUI()
        {
            if (!fullPreviewMode_)
                return false;

            if (WindowPreview != null)
                ((WndPreview)WindowPreview).BuildFullPreview();

            return true;
        }
    }
}
