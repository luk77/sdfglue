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

        public  bool                    EnabledDemoMode         = false;

        private bool                    fullPreviewMode_        = false;

        private WndPreview              WindowPreview;

        public UiManagerSdfGlue(IUiExecutorSdfGlue uiActionsExecutor)
            : base(uiActionsExecutor)
        {
            // The first preview is the main one (the only one visible on start, used by full preview mode)
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
                RegisterWindow(new WndSignals()         , null, false);
        }

        // Typed access to the executor passed to the constructor
        public IUiExecutorSdfGlue ActionsExecutor { get { return (IUiExecutorSdfGlue)Executor; } }

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

            WindowPreview.BuildFullPreview();

            return true;
        }
    }
}
