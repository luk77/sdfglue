//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using SdfGlueCore;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SdfGlueEditor.Application;
using SdfGlueEditor.Rendering.SdfGlueRendering;
using SdfGlueUi.Ui;
using SingleDocAppCore.Input;
using SingleDocAppCore.Settings;
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Ui;
using SingleDocAppMidiWin;
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
        private AnimationExportController       animExport_             = null!;
        private ShaderExportController          shaderExport_           = null!;
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
            settings.EnableInputSystem  = SdfGlueFeatures.UseSignals;     // input channels are used by signals only
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

            return String.Format("{0} v.{1} {2}", AppSettings.AppName, AppVersion.GetAppDisplayVersion(), debugVersion ? " (debug)" : "");
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
            return new UiExecutorSdfGlue(ctx_, documents_, render_, animExport_, shaderExport_, camera_, hierarchy_);
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
            animExport_ = new AnimationExportController(ctx_, render_);
            camera_     = new CameraController(ctx_);
            demoMode_   = new DemoModeController(documents_);
            shaderExport_ = new ShaderExportController(ctx_, documents_);
            hierarchy_  = new ProjectHierarchyController(ctx_);

            base.OnLoad();

            // the input system is created in base.OnLoad() (only with SdAppSettings.EnableInputSystem)
            if (Inputs != null)
                SignalInputs.Provider = new InputSystemSignalProvider(Inputs);
        }

        // input system (only with SdfGlueFeatures.UseSignals)

        protected override IEnumerable<IInputDevice> CreateInputDevices()
        {
            foreach(IInputDevice device in base.CreateInputDevices())
                yield return device;
            yield return new MidiInputDevice();
        }

        // 8 channels bound to the bottom row of the keyboard (not used by shortcuts)
        protected override void InitDefaultInputChannels(InputChannelsCollection channels)
        {
            string[] keys = { "Z", "X", "C", "V", "B", "N", "M", "Comma" };
            foreach(string key in keys)
            {
                InputChannel channel = channels.AddChannel();
                channel.Bindings.Add(new InputBindingKey() { Key = key });
            }
        }

        // camera movement in the preview (WASD, Q/E) and the maximized / fullscreen preview modes (F12)
        protected override void RegisterReservedInputKeys(HashSet<string> reservedKeys)
        {
            base.RegisterReservedInputKeys(reservedKeys);

            string[] keys = { "W", "A", "S", "D", "Q", "E", "F12" };
            foreach(string key in keys)
                reservedKeys.Add(key);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            // Note: UI input, shortcuts and user settings (update frequency) are handled in base.OnUpdateFrame()
            base.OnUpdateFrame(e);

            // after the shortcuts (base.OnUpdateFrame); the mode can also be changed from the View menu
            SyncFullscreenWindow();

            camera_.RecalculateCamera();

            // monitor file system changes (a change is kept until the image sequence export is finished)
            if (fileChangeMonitor_ != null && !animExport_.IsActive && fileChangeMonitor_.ConsumeChange())
            {
                if (ctx_.Settings.MonitorFileSystemChanges)
                {
                    render_.ReloadSdfDefinitions();
                }
            }

            if (!animExport_.IsActive)
                demoMode_.Update(e.Time, ref GetUiManager().EnabledDemoMode);
        }

        // Called only after the user settings were changed (settings dialog, revert, restore defaults)
        protected override void ApplyUserSettings(UserSettingsBase? previous)
        {
            base.ApplyUserSettings(previous);

            UserSettingsSdfGlue? prev = previous as UserSettingsSdfGlue;
            if (prev == null)
                return;

            // identifiers in the generated code
            if (prev.UseNamesAsIds != ctx_.Settings.UseNamesAsIds)
                render_.RebuildShader();
        }

        // windows are (semi-)transparent only while the background preview is visible
        protected override float GetWindowOpacity()
        {
            return GetUiManager().IsBackgroundPreviewActive() ? UserSettings.WindowOpacity : 1.0f;
        }

        // borderless fullscreen OS window only in the fullscreen preview mode
        private void SyncFullscreenWindow()
        {
            bool fullscreen = GetUiManager().GetPreviewViewMode() == PreviewViewMode.Fullscreen;
            if (fullscreen != IsBorderlessFullscreen())
                SetBorderlessFullscreen(fullscreen);
        }

        protected override void OnRender3d()
        {
            // the image sequence export renders its frames instead (and advances the time itself)
            if (animExport_.IsActive)
                animExport_.Step();
            else
                render_.RenderFrame();

            // fullscreen / background preview: drawn directly into the window (under the UI); the mode is from the previous UI frame
            RenderPassData? screenPass = GetUiManager().GetScreenPreviewPass(out bool keepAspectRatio);
            if (screenPass != null)
                render_.RenderScreenPreview(screenPass, keepAspectRatio);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (!IsImGuiInitialized())
                return;

            double deltaTime = ctx_.Playback.UseConstTimeStep ? PlaybackState.ConstTimeStep : e.Time;

            if (!animExport_.IsActive)
            {
                ctx_.Update(deltaTime);
                // the keyframe animation has its own time (real time, not the constant step of the shader time)
                ctx_.Animation.Update(e.Time, ImGui.IsAnyItemActive());
            }

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

            // Editor -> Maximized -> Fullscreen -> Editor
            if (IsKeyPressed(Keys.F12))
            {
                uiMgr.CyclePreviewViewMode();
            }

            if (IsKeyPressed(Keys.Escape))
            {
                // back to the editor from maximized / fullscreen preview
                if (uiMgr.GetPreviewViewMode() != PreviewViewMode.Editor)
                {
                    uiMgr.SetPreviewViewMode(PreviewViewMode.Editor);
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
