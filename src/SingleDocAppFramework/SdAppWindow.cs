using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SingleDocAppCore.Input;
using SingleDocAppCore.Settings;
using SingleDocAppFramework.Input;
using SingleDocAppFramework.Layouts;
using SingleDocAppFramework.Platform;
using SingleDocAppFramework.Rendering.OpenTk.ImGuiRendering;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Styles;
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
        private bool                titleDocumentModified_      = false;

        private SdAppSettings       appSettings_;
        private UserSettingsBase    userSettings_;
        private IPlatformServices   platform_;
        private float               dpiScaling_                 = 1.0f;
        private string?             appliedUiStyle_             = null;     // UserSettings.UiStyle applied to ImGui
        private InputSystem?        inputs_                     = null;     // only with SdAppSettings.EnableInputSystem

        // userSettings: UserSettingsBase or an application-specific derived class;
        // loaded here, so it is ready in the constructor of the derived window
        public SdAppWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings, SdAppSettings appSettings, UserSettingsBase userSettings, IPlatformServices platform)
            : base(gameWindowSettings, nativeWindowSettings)

        {
            appSettings_    = appSettings;
            userSettings_   = userSettings;
            platform_       = platform;
            dpiScaling_     = platform.ObtainDpiScaling();

            LoadUserSettings();
        }

        public SdAppSettings            AppSettings     { get { return appSettings_; } }
        public UserSettingsBase         UserSettings    { get { return userSettings_; } }
        public IPlatformServices        Platform        { get { return platform_; } }
        public UiExecutorFrameworkBase  Executor        { get { return executor_; } }

        // null if the input system is disabled (SdAppSettings.EnableInputSystem)
        public InputSystem?             Inputs          { get { return inputs_; } }

        // User settings: defaults, then the file (if present). A missing file is created with defaults,
        // an invalid one is left untouched (defaults are used).
        private void LoadUserSettings()
        {
            string filePath = appSettings_.GetUserSettingsPath();

            userSettings_.InitDefault();

            if (File.Exists(filePath))
            {
                if (userSettings_.LoadFromFile(filePath))
                    Console.WriteLine("User settings loaded: {0}", filePath);
                else
                    Console.WriteLine("WARNING: Default user settings are used");
            }
            else
            {
                if (userSettings_.SaveToFile(filePath))
                    Console.WriteLine("User settings file created with default values: {0}", filePath);
            }
        }

        public void SaveUserSettings()
        {
            string filePath = appSettings_.GetUserSettingsPath();

            if (userSettings_.SaveToFile(filePath))
                Console.WriteLine("User settings saved: {0}", filePath);
        }

        // Defaults are set in memory only - saving is explicit
        public void RestoreDefaultUserSettings()
        {
            userSettings_.RestoreDefaults();
        }

        // Applies settings that are not read directly where they are used.
        // Called in OnLoad() and every frame, so changes in the settings window take effect immediately.
        protected virtual void ApplyUserSettings()
        {
            //RenderFrequency = userSettings_.UseRenderFrequencyLimit ? userSettings_.RenderFrequencyLimit : 0;
            UpdateFrequency = userSettings_.UseUpdateFrequencyLimit ? userSettings_.UpdateFrequencyLimit : 0;
        }

        // Factories implemented by the application.
        // Called from OnLoad(), so the derived class may rely on its own OnLoad() initialization
        // done before calling base.OnLoad().
        protected abstract UiExecutorFrameworkBase  CreateExecutor  ();
        protected abstract UiManagerBase            CreateUiManager (UiExecutorFrameworkBase executor);

        protected override void OnLoad()
        {
            // Note: derived classes should call base.OnLoad() at the end of their OnLoad()!

            ApplyUserSettings();

            InitInputSystem();

            executor_          = CreateExecutor();
            uiMgr_              = CreateUiManager(executor_);
            uiMgr_.AppSettings  = appSettings_;

            ReinitializeImGuiController(GetStartupLayoutPath());

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

        //-------------------------------------------------------------------
        // Optional input system (SdAppSettings.EnableInputSystem)
        //-------------------------------------------------------------------

        private void InitInputSystem()
        {
            if (!appSettings_.EnableInputSystem || inputs_ != null)
                return;

            inputs_ = new InputSystem(CreateInputDevices(), appSettings_.GetInputSettingsPath(), InitDefaultInputChannels);
            RegisterReservedInputKeys(inputs_.ReservedKeys);
            inputs_.Load();
        }

        // Input devices used by the input system. Override to add devices (e.g. MIDI) or to skip some of them.
        protected virtual IEnumerable<IInputDevice> CreateInputDevices()
        {
            yield return new KeyboardInputDevice(this, IsKeyboardInputActive);
            yield return new GamepadInputDevice();
        }

        // Keys are reported to input channels only when the application is focused, no ImGui text field
        // is active and no shortcut modifier (Ctrl/Alt) is pressed
        protected virtual bool IsKeyboardInputActive()
        {
            return imguiInitialized_ && IsApplicationFocused() && !ImGui.GetIO().WantTextInput && !IsDownAnyCtrl() && !IsDownAnyAlt();
        }

        // Default input channels (no input settings file, "Restore defaults")
        protected virtual void InitDefaultInputChannels(InputChannelsCollection channels)
        {
        }

        // Keys that can't be bound to input channels (UiKey names). Applications add their own shortcuts.
        protected virtual void RegisterReservedInputKeys(HashSet<string> reservedKeys)
        {
            UiKey[] keys =
            {
                UiKey.Escape, UiKey.Enter, UiKey.KeyPadEnter, UiKey.Tab, UiKey.Space, UiKey.Backspace, UiKey.Delete,
                UiKey.Up, UiKey.Down, UiKey.Left, UiKey.Right,
                UiKey.LeftShift, UiKey.RightShift, UiKey.LeftControl, UiKey.RightControl,
                UiKey.LeftAlt, UiKey.RightAlt, UiKey.LeftSuper, UiKey.RightSuper, UiKey.Menu,
            };
            foreach(UiKey key in keys)
                reservedKeys.Add(key.ToString());
        }

        // Saves pending changes of the input channels and closes the devices
        private void ShutdownInputSystem()
        {
            if (inputs_ == null)
                return;

            inputs_.Dispose();
            inputs_ = null;
        }

        protected override void OnUnload()
        {
            ShutdownInputSystem();

            base.OnUnload();
        }

        // Window title: "<app title><extra info>, <document name>[*]" (* - unsaved changes)
        public void RefreshWindowTitle()
        {
            string title = GetAppTitle() + GetTitleExtraInfo();

            string? documentName = GetDocumentDisplayName();
            if (!String.IsNullOrEmpty(documentName))
                title += ", " + documentName;

            titleDocumentModified_ = executor_ != null && executor_.IsDocumentModified();
            if (titleDocumentModified_)
                title += "*";

            Title = title;
        }

        // The modified state changes with every edit/undo/redo, so the title is checked every frame
        private void RefreshWindowTitleIfModifiedChanged()
        {
            if (executor_.IsDocumentModified() != titleDocumentModified_)
                RefreshWindowTitle();
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

            ApplyUserSettings();

            currMouseClientPos_ = this.PointToClient(new Vector2i((int)MouseState.X, (int)MouseState.Y));

            // this is required when docking for proper positioning of preview image
            uiMgr_.WindowsScaling   = GetWindowsScaling();
            uiMgr_.MainWindowSizeX  = this.ClientSize.X;
            uiMgr_.MainWindowSizeY  = this.ClientSize.Y;

            ImGui.GetIO().FontGlobalScale = GetWindowsScaling();

            ApplyUiStyleIfChanged();

            if (IsApplicationFocused())
            {
                uiMgr_.HandleInput((float)e.Time);

                HandleFrameworkShortcuts();
                HandleAppShortcuts();
            }

            RefreshWindowTitleIfModifiedChanged();
        }

        public float GetWindowsScaling()
        {
            return dpiScaling_ * GetUiTextScaleFactor();
        }

        // Additional, user-configurable UI scale (multiplied by the OS DPI scaling)
        protected virtual float GetUiTextScaleFactor()
        {
            return userSettings_.UiTextScaleFactor;
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

            appliedUiStyle_ = null;
            ApplyUiStyleIfChanged();

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

        // UserSettings.UiStyle is checked every frame, so a style chosen in the settings window or the View menu
        // takes effect immediately. Called outside ImGui.NewFrame() / ImGui.Render().
        private void ApplyUiStyleIfChanged()
        {
            string styleName = userSettings_.UiStyle;
            if (styleName == appliedUiStyle_)
                return;

            appliedUiStyle_ = styleName;

            if (!UiStyles.Apply(styleName))
                Console.WriteLine("WARNING: Unknown UI style: {0}, the default style is used", styleName);

            // platform windows (viewports) must be opaque and look like OS windows
            if ((ImGui.GetIO().ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                ImGuiStylePtr style = ImGui.GetStyle();
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }
        }

        // Layout remembered in the user settings, or the default one if it is not set or does not exist
        private string GetStartupLayoutPath()
        {
            if (String.IsNullOrEmpty(userSettings_.LayoutFile))
                return appSettings_.GetDefaultLayoutPath();

            string layoutFilePath = Path.Combine(appSettings_.LayoutsDirectory, userSettings_.LayoutFile);
            if (!File.Exists(layoutFilePath))
            {
                Console.WriteLine("WARNING: Layout from user settings not found: {0}, the default layout is used", layoutFilePath);
                return appSettings_.GetDefaultLayoutPath();
            }

            return layoutFilePath;
        }

        // Remembers the current layout in the user settings (saved together with the other settings)
        private void SetCurrentLayout(string layoutFilePath)
        {
            userSettings_.LayoutFile = Path.GetRelativePath(appSettings_.LayoutsDirectory, layoutFilePath);
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

            SetCurrentLayout(filePath);
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

            SetCurrentLayout(requestedLayoutFilePath_);
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

            // input channels are updated before the UI and the application logic of this frame
            inputs_?.Update(e.Time);

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

        // Closing the window (Alt+F4, close button) - asks about unsaved changes
        protected override void OnClosing(CancelEventArgs e)
        {
            if (executor_ != null && !executor_.ConfirmDiscardChanges())
            {
                e.Cancel = true;
                return;
            }

            base.OnClosing(e);

            ShutdownInputSystem();
            ShutdownImGui();
        }

        // called from the "Exit" menu item - asks about unsaved changes
        public virtual void OnExitApp()
        {
            if (!executor_.ConfirmDiscardChanges())
                return;

            ShutdownInputSystem();
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
