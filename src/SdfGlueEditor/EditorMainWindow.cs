//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SdfGlueCore.Model;
using SdfGlueEditor.Application;
using SdfGlueEditor.Rendering.SdfGlueRendering;
using SdfGlueUi.Ui;
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Ui;
using System.ComponentModel;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace SdfGlueEditor
{
    // SdfGlue main window: window lifecycle, frame loop and app-specific shortcuts.
    // Application logic lives in the controllers (SdfGlueEditor.Application),
    // UI actions are routed through UiExecutorSdfGlue.
    public class EditorMainWindow : SdAppWindow
    {
        public static readonly bool     EnableOpenGlDebug           = true;

        private SdfGlueAppContext               ctx_;
        private FileChangeMonitor?              fileChangeMonitor_      = null;

        // created in OnLoad() (require the OpenGL context / rendering system)
        private DocumentController              documents_              = null!;
        private RenderController                render_                 = null!;
        private CameraController                camera_                 = null!;
        private DemoModeController              demoMode_               = null!;
        private ProjectHierarchyController      hierarchy_              = null!;


        public EditorMainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, IPlatformServices platform)
            : base(gameWindowSettings, nativeWindowSettings, CreateAppSettings(), new UserSettingsSdfGlue(), platform)
        {
            // Vsync tests - does not really help with flickering at high frequencies
            //Context.SwapInterval = 1;

            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine(GetSdfGlueVersion());
            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine("ImGui v.{0}", GetImGuiVersion());
            Console.WriteLine("OpenGL v.{0}", GL.GetString(StringName.Version));
            Console.WriteLine("-----------------------------------------------------------------------------");

            // user settings are already loaded by the base constructor
            ctx_                = new SdfGlueAppContext(this, platform, (UserSettingsSdfGlue)UserSettings);

            fileChangeMonitor_  = new FileChangeMonitor(".", new string[] {"*.xml", "*.glsl", "*.vert", "*.frag", "*.shader"}, 300);
        }

        private static SdAppSettings CreateAppSettings()
        {
            SdAppSettings settings = new SdAppSettings();
            settings.AppName            = "SDF Glue";
            settings.DocumentFileFilter = new FileFilter("Project files", "*.xml");
            settings.EnableDocking      = true;
            return settings;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }

        private UiManagerSdfGlue GetUiManager()
        {
            return (UiManagerSdfGlue)uiMgr_;
        }

        // window title

        private string GetSdfGlueVersion()
        {
            bool debugVersion = false;
#if DEBUG
            debugVersion = true;
#endif

            return String.Format("{0} v.{1} {2}", AppSettings.AppName, DataModel.GetAppDisplayVersion(), debugVersion ? " (debug)" : "");
        }

        protected override string GetAppTitle()
        {
            return GetSdfGlueVersion();
        }

        protected override string GetTitleExtraInfo()
        {
            // Note: APIVersion is the version requested in nativeWindowSettings.APIVersion
            string openGlVersion = APIVersion.ToString();

            return String.Format(", ImGui v.{0}, OpenGL v.{1} [Api: {2}, Profile: {3}, Debug:{4}]", GetImGuiVersion(), openGlVersion, API, Profile, EnableOpenGlDebug);
        }

        protected override string? GetDocumentDisplayName()
        {
            return documents_.GetDocumentDisplayName();
        }

        // framework factories (called from base.OnLoad())

        protected override UiExecutorFrameworkBase CreateExecutor()
        {
            return new UiExecutorSdfGlue(ctx_, documents_, render_, camera_, hierarchy_);
        }

        protected override UiManagerBase CreateUiManager(UiExecutorFrameworkBase executor)
        {
            return new UiManagerSdfGlue((IUiExecutorSdfGlue)executor);
        }

        // lifecycle

        protected override void OnLoad()
        {
            // Note: base.OnLoad() is called at the end (it creates the executor and the UI manager)

            if (EnableOpenGlDebug)
            {
                GL.Enable(EnableCap.DebugOutput);
            }

            ctx_.RenderingSystem = new RenderingSystem();
            ctx_.ReinitializeRenderingSystem();

            documents_  = new DocumentController(ctx_);
            render_     = new RenderController(ctx_);
            camera_     = new CameraController(ctx_);
            demoMode_   = new DemoModeController(documents_);
            hierarchy_  = new ProjectHierarchyController(ctx_.RenderingSystem, ctx_.CodeGenerator);

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Note: UI input, shortcuts and user settings (update frequency) are handled in base.OnUpdateFrame()
            base.OnUpdateFrame(e);

            camera_.RecalculateCamera();

            // monitor file system changes
            if (fileChangeMonitor_ != null && fileChangeMonitor_.ConsumeChange())
            {
                if (GetModel().Config.MonitorFileSystemChanges)
                {
                    render_.ReloadSdfDefinitions();
                }
            }

            demoMode_.Update(e.Time, ref GetUiManager().EnabledDemoMode);
        }

        protected override void OnRender3d()
        {
            render_.RenderFrame();
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (!IsImGuiInitialized())
                return;

            double deltaTime = GetModel().UseConstTimeStep ? DataModel.ConstTimeStep : e.Time;

            GetModel().Update(deltaTime);

            // These actions have to be executed outside of the UI building loop
            // (they modify the collections iterated there)
            hierarchy_.HandleDeleteNode();
            hierarchy_.HandleMoveNodeUp();
            hierarchy_.HandleMoveNodeDown();
        }

        protected override void OnUnload()
        {
            if (ctx_.RenderingSystem != null)
            {
                ctx_.RenderingSystem.Dispose();
                ctx_.RenderingSystem = null!;
            }

            if (fileChangeMonitor_ != null)
            {
                fileChangeMonitor_.Dispose();
                fileChangeMonitor_ = null;
            }

            base.OnUnload();
        }

        // Confirmation of unsaved changes on exit is done by SdAppWindow (OnClosing() and OnExitApp())
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
        }

        // called from the "Exit" menu item
        public override void OnExitApp()
        {
            base.OnExitApp();
        }

        // shortcuts

        // Generic shortcuts (New/Open/Save, Undo/Redo, Ctrl+W, Ctrl+1..0) are handled by SdAppWindow
        protected override void HandleAppShortcuts()
        {
            UiManagerSdfGlue uiMgr = GetUiManager();

            if (IsKeyPressed(Keys.F12))
            {
                uiMgr.SetFullPreviewMode(!uiMgr.GetFullPreviewMode());
            }

            if (IsKeyPressed(Keys.Escape))
            {
                // turn off full preview mode
                if (uiMgr.GetFullPreviewMode())
                {
                    uiMgr.SetFullPreviewMode(false);
                }
            }

            if (IsDownAnyCtrl())
            {
                if (IsKeyPressed(Keys.I))
                {
                    render_.SaveImage();
                }

                // Compile shader
                if (IsKeyPressed(Keys.Enter) || IsKeyPressed(Keys.R))
                {
                    render_.CompileShader();
                }

                if (IsKeyPressed(Keys.E))
                {
                    render_.ReloadSdfDefinitions();
                }

                // Focus camera on the selected object
                if (IsKeyPressed(Keys.F))
                {
                    camera_.FocusSelectedObject();
                }
            }
        }
    }
}
