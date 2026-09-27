using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Rendering.OpenTk.ImGuiRendering;
using SingleDocAppFramework.Ui;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace SingleDocAppFramework
{
    public abstract class SdAppWindow : GameWindow, IUiInput
    {
        private float               mouseScroll_                = 0.0f;
        private Vector2i            currMouseClientPos_         = new Vector2i(0, 0);

        // created in OnLoad() by CreateExecutor() / CreateUiManager()
        protected UiManagerBase             uiMgr_              = null!;
        protected UiExecutorFrameworkBase   executor_           = null!;

        private bool                imguiInitialized_           = false;

        private SdAppSettings       appSettings_;
        private IPlatformServices   platform_;
        private float               dpiScaling_                 = 1.0f;

        public SdAppWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, SdAppSettings appSettings, IPlatformServices platform)
            : base(gameWindowSettings, nativeWindowSettings)

        {
            appSettings_    = appSettings;
            platform_       = platform;
            dpiScaling_     = platform.ObtainDpiScaling();
        }

        public SdAppSettings            AppSettings     { get { return appSettings_; } }
        public IPlatformServices        Platform        { get { return platform_; } }
        public UiExecutorFrameworkBase  Executor        { get { return executor_; } }

        // Factories implemented by the application.
        // Called from OnLoad(), so the derived class may rely on its own OnLoad() initialization
        // done before calling base.OnLoad().
        protected abstract UiExecutorFrameworkBase  CreateExecutor  ();
        protected abstract UiManagerBase            CreateUiManager (UiExecutorFrameworkBase executor);

        protected override void OnLoad()
        {
            // Note: derived classes should call base.OnLoad() at the end of their OnLoad()!

            executor_           = CreateExecutor();
            uiMgr_              = CreateUiManager(executor_);
            uiMgr_.AppSettings  = appSettings_;

            ReinitializeImGuiController(appSettings_.GetDefaultLayoutPath());

            // Windows can be moved only by their title bar
            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

            // deactivate imgui.ini
            unsafe
            {
                ImGui.GetIO().NativePtr->IniFilename = (byte*)null;
            }

            RefreshWindowTitle();

            base.OnLoad();
        }

        // Window title: "<app title><extra info>, <document name>"
        public void RefreshWindowTitle()
        {
            string title = GetAppTitle() + GetTitleExtraInfo();

            string? documentName = GetDocumentDisplayName();
            if (!String.IsNullOrEmpty(documentName))
                title += ", " + documentName;

            Title = title;
        }

        protected virtual string GetAppTitle()
        {
            return appSettings_.AppName;
        }

        protected virtual string GetTitleExtraInfo()
        {
            return "";
        }

        protected virtual string? GetDocumentDisplayName()
        {
            return null;
        }

        protected bool IsApplicationFocused()
        {
            return platform_.IsApplicationFocused(this);
        }

        protected bool IsImGuiInitialized()
        {
            return imguiInitialized_;
        }

        public string GetImGuiVersion()
        {
            return ImGui.GetVersion();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            mouseScroll_ += e.OffsetY;
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            GL.Viewport(0, 0, e.Width, e.Height);
        }

        // IUiInput
        public bool IsKeyDown(UiKey uiKey)
        {
            return IsKeyDown(UiKeyToOpenTkKey(uiKey));
        }

        public bool IsKeyPressed(UiKey uiKey)
        {
            return IsKeyPressed(UiKeyToOpenTkKey(uiKey));
        }

        public bool IsDownAnyCtrl()
        {
            return KeyboardState.IsKeyDown(Keys.LeftControl)  || KeyboardState.IsKeyDown(Keys.RightControl);
        }

        public bool IsDownAnyShift()
        {
            return KeyboardState.IsKeyDown(Keys.LeftShift)    || KeyboardState.IsKeyDown(Keys.RightShift);
        }

        public bool IsDownAnyAlt()
        {
            return KeyboardState.IsKeyDown(Keys.LeftAlt)      || KeyboardState.IsKeyDown(Keys.RightAlt);
        }

        // UiKey values are copied from OpenTK Keys, so a plain cast is enough
        private static Keys UiKeyToOpenTkKey(UiKey key)
        {
            return (Keys)key;
        }

        public float GetWheelPrecise()
        {
            return mouseScroll_;
        }

        public bool IsRmbDown()
        {
            return MouseState.IsButtonDown(MouseButton.Right);
        }

        public bool IsLmbDown()
        {
            return MouseState.IsButtonDown(MouseButton.Left);
        }

        public int GetMouseStateX()
        {
            return currMouseClientPos_.X;
        }

        public int GetMouseStateY()
        {
            return currMouseClientPos_.Y;
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            currMouseClientPos_ = this.PointToClient(new Vector2i((int)MouseState.X, (int)MouseState.Y));

            if (uiMgr_.AutoLayoutWindows)
            {
                uiMgr_.SetMainWindowClientSize(this.ClientSize.X, this.ClientSize.Y);
            }
            else
            {
                // this is required when docking for proper positioning of preview image
                uiMgr_.WindowsScaling   = GetWindowsScaling();
                uiMgr_.MainWindowSizeX  = this.ClientSize.X;
                uiMgr_.MainWindowSizeY  = this.ClientSize.Y;
            }

            ImGui.GetIO().FontGlobalScale = GetWindowsScaling();

            if (IsApplicationFocused())
            {
                uiMgr_.HandleInput((float)e.Time);

                HandleFrameworkShortcuts();
                HandleAppShortcuts();
            }
        }

        public float GetWindowsScaling()
        {
            return dpiScaling_ * GetUiTextScaleFactor();
        }

        // Additional, user-configurable UI scale (multiplied by the OS DPI scaling)
        protected virtual float GetUiTextScaleFactor()
        {
            return 1.0f;
        }

        private static readonly Keys[] WindowToggleKeys = { Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0 };

        // Shortcuts common to all applications: New/Open/Save/Save as, Undo/Redo,
        // close focused window (Ctrl+W) and toggle windows (Ctrl+1..Ctrl+0)
        private void HandleFrameworkShortcuts()
        {
            if (!appSettings_.EnableFrameworkShortcuts)
                return;

            if (!IsDownAnyCtrl())
                return;

            bool isShiftKeyDown = IsDownAnyShift();

            if (IsKeyPressed(Keys.N))
            {
                executor_.OnNewDocument();
            }
            if (IsKeyPressed(Keys.O))
            {
                executor_.OnOpenDocument();
            }
            if (IsKeyPressed(Keys.S))
            {
                if (isShiftKeyDown || !executor_.CanSaveDocument())
                    executor_.OnSaveDocumentAs();
                else
                    executor_.OnSaveDocument();
            }
            // While an ImGui text field is active, Ctrl+Z/Y are handled by ImGui's own text undo
            bool imguiTextInput = ImGui.GetIO().WantTextInput;
            if (IsKeyPressed(Keys.Z) && !imguiTextInput)
            {
                if (isShiftKeyDown)
                    executor_.OnRedo();
                else
                    executor_.OnUndo();
            }
            if (IsKeyPressed(Keys.Y) && !imguiTextInput)
            {
                executor_.OnRedo();
            }
            if (IsKeyPressed(Keys.W))
            {
                uiMgr_.CloseCurrentWindow();
            }

            for (int i = 0; i < WindowToggleKeys.Length; i++)
            {
                if (IsKeyPressed(WindowToggleKeys[i]))
                {
                    uiMgr_.ToggleWindow(i);
                    break;
                }
            }
        }

        // Application-specific shortcuts (called only when the application is focused)
        protected virtual void HandleAppShortcuts()
        {
        }


        protected void ReinitializeImGuiController(string layoutFilePath)
        {
            ImGui.CreateContext();
            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
            if (appSettings_.EnableDocking)
                io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            if (appSettings_.EnableViewports)
                io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;     // This allows drag-out windows

            ImGui.StyleColorsDark();

            ImGuiStylePtr style = ImGui.GetStyle();
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }

            ImguiImplOpenTK4.Init(this);
            ImguiImplOpenGL3.Init();

            imguiInitialized_ = true;

            // init layout (a missing layout file does not block application start)
            LayoutData? layoutData = LoadLayoutFile(layoutFilePath);
            if (layoutData == null)
            {
                Console.WriteLine("Layout file not found or invalid: {0}", layoutFilePath);
                return;
            }

            ImGui.LoadIniSettingsFromMemory(layoutData.ImguiLayoutSettingsTxt);
            uiMgr_.ApplyLayout(layoutData);
        }

        private string? requestedLayoutFilePath_ = null;
        public void OnLoadLayout(string layoutFilePath)
        {
            requestedLayoutFilePath_ = layoutFilePath;
        }


        public void OnSaveCurrentLayout(WindowsVisibilityCollection windowsVisibility)
        {
            Directory.CreateDirectory(appSettings_.LayoutsDirectory);

            string? filePath = platform_.SaveFileDialog(appSettings_.LayoutFileFilter, appSettings_.LayoutsDirectory);

            OnSaveCurrentLayout(filePath, windowsVisibility);
        }

        public void OnSaveCurrentLayout(string? filePath, WindowsVisibilityCollection windowsVisibility)
        {
            if (String.IsNullOrEmpty(filePath))
                return;

            string iniSettings = ImGui.SaveIniSettingsToMemory();

            LayoutData layoutData = new LayoutData();
            layoutData.DisplayName = Path.GetFileNameWithoutExtension(filePath);
            layoutData.WindowsVisibility = windowsVisibility;
            layoutData.ImguiLayoutSettingsTxt = iniSettings;

            XmlDocument xmlDoc = layoutData.Serialize();
            xmlDoc.Save(filePath);
        }


        private static LayoutData? LoadLayoutFile(string layoutFilePath)
        {
            if (!File.Exists(layoutFilePath))
                return null;

            try
            {
                LayoutData layoutData = new LayoutData();

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(layoutFilePath);

                XmlNode? rootNode = xmlDoc.SelectSingleNode("LayoutData");
                if (rootNode == null)
                    return null;

                layoutData.Deserialize(rootNode);

                return layoutData;
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        private void HandleLayoutsSwitching()
        {
            if (requestedLayoutFilePath_ == null)
                return;

            LayoutData? layoutData = LoadLayoutFile(requestedLayoutFilePath_);
            if (layoutData == null)
            {
                requestedLayoutFilePath_ = null;
                return;
            }

            requestedLayoutFilePath_ = null;


            ImGui.LoadIniSettingsFromMemory(layoutData.ImguiLayoutSettingsTxt);

            uiMgr_.ApplyLayout(layoutData);
        }

        protected abstract void OnRender3d();

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (!imguiInitialized_)
                return;

            HandleLayoutsSwitching();

            ImguiImplOpenGL3.NewFrame();
            ImguiImplOpenTK4.NewFrame();
            ImGui.NewFrame();

            OnRender3d();

            uiMgr_.SubmitUI();      // includes DockSpaceOverViewport()

            // This call accumulates the whole frame rendering time
            ImGui.Render();

            ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
                Context.MakeCurrent();
            }

            CheckGLError("End of frame");

            SwapBuffers();


        }

        public static void CheckGLError(string title)
        {
            var error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
            {
                Debug.Print($"{title}: {error}");
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            ShutdownImGui();
        }

        // called from the "Exit" menu item
        public virtual void OnExitApp()
        {
            ShutdownImGui();

            Environment.Exit(0);
        }

        private void ShutdownImGui()
        {
            if (!imguiInitialized_)
                return;

            ImguiImplOpenGL3.Shutdown();
            ImguiImplOpenTK4.Shutdown();
            imguiInitialized_ = false;
        }

    }
}
