//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.Utils;
using SdfGlueEditor.Application;
using SdfGlueEditor.Platform;
using SdfGlueEditor.Rendering.OpenTk;
using SdfGlueEditor.Rendering.OpenTk.ImGuiRendering;
using SdfGlueEditor.Rendering.SdfGlueRendering;
using SdfGlueEditor.Utils;
using SdfGlueUi.Input;
using SdfGlueUi.Layouts;
using SdfGlueUi.Ui;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;
using TreeNode = SdfGlueCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor
{
    public class EditorMainWindow : GameWindow, IUiActionsExecutor
    {
        public static readonly bool     EnableOpenGlDebug           = true;

        public static readonly float    MinDeltaTime            = 0.0f;
        public static readonly float    MaxDeltaTime            = 0.5f;  // 2 fps'y

        public static readonly float    CameraAnimThreshold     = 0.005f;

        public static readonly int      ApplicationWindowSizeX      = 1600;
        public static readonly int      ApplicationWindowSizeY      = 900;


        private UiManager                       uiMgr_                  = null;
        private ShaderCodeGenerator             codeGenerator_          = null;
        //private ImGuiController                 _controller             = null;
        private bool                            imguiInitialized_       = false;
        private IRenderingSystem                renderingSystem_        = null;
        private ProjectHierarchyController      phc_                    = null;

        // Camera focus animation (key [F])
        private System.Numerics.Vector3     cameraAnimTo_               = new System.Numerics.Vector3(0.0f);
        private bool                        isAnimatingCamera_          = false;

        private Vector2i            currMouseClientPos_         = new Vector2i(0, 0);
        private float               mouseScroll_                = 0.0f;

        private float               windowsScaling_             = 1.0f;


        public EditorMainWindow(GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
            //: base(ApplicationWindowSizeX, ApplicationWindowSizeY, new OpenTK.Graphics.GraphicsMode(new OpenTK.Graphics.ColorFormat(8, 8, 8, 8), 3, 3, 4), 
            //    "", 
            //    GameWindowFlags.Default, 
            //    DisplayDevice.Default, 
            //    3, 0, 
            //    OpenTK.Graphics.GraphicsContextFlags.ForwardCompatible)
            : base(gameWindowSettings, nativeWindowSettings)

        {
            string sdfGlueVersion = GetSdfGlueVersion();

            // Testy z vsync - na flickering przy dużych częstotliwościach raczej nie pomaga
            //Context.SwapInterval = 1;


            RefreshWindowTitle();

            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine(sdfGlueVersion);
            Console.WriteLine("-----------------------------------------------------------------------------");
            Console.WriteLine("ImGui v.{0}", ImGui.GetVersion());
            Console.WriteLine("OpenGL v.{0}", GL.GetString(StringName.Version));
            Console.WriteLine("-----------------------------------------------------------------------------");

            codeGenerator_      = new ShaderCodeGenerator();
            uiMgr_              = new UiManager(this, this.ClientSize.X, this.ClientSize.Y);

            FileSystemWatcherUtils.Initialize(".", new string[] {"*.xml", "*.glsl", "*.vert", "*.frag", "*.shader"});
        }

        private string GetSdfGlueVersion()
        {
            bool debugVersion = false;
#if DEBUG
            debugVersion = true;
#endif

            //return String.Format("SDF Glue v.{0}.{1}{2}", DataModel.VersionMajor, DataModel.VersionMinor, debugVersion ? " (debug)" : "");
            return String.Format("SDF Glue v.{0} {1}", DataModel.GetAppVersion(), debugVersion ? " (debug)" : "");
        }

        private void RefreshWindowTitle()
        {
            string projectPath = "<Unnamed project>";
            DataModel model = GetModel();
            if (model != null && !String.IsNullOrEmpty(model.ProjectFilePath))
                projectPath = model.ProjectFilePath;

            string additionalVersionInfo = "";

            additionalVersionInfo += String.Format(" [Api: {0}, Profile: {1}, Debug:{2}]", API, Profile, EnableOpenGlDebug);

            if (EnableOpenGlDebug)
            {
                GL.Enable(EnableCap.DebugOutput);
                //additionalVersionInfo += " [OpenGl debug enabled]";
            }

            // .Net Framework:
            //string openGlVersion = GL.GetString(StringName.Version);
            // .Net Core:
            // to działa, ale pod warunkiem, ze wcześniej ustawi się nativeWindowSettings.APIVersion = ...
            //int glMajor = GL.GetInteger(GetPName.MajorVersion);
            //int glMinor = GL.GetInteger(GetPName.MinorVersion);
            //string openGlVersion = glMajor+"."+glMinor;
            // j.w.
            string openGlVersion = APIVersion.ToString();

            Title = String.Format("{0}, ImGui v.{1}, OpenGL v.{2}{3}, Project: {4}", GetSdfGlueVersion(), ImGui.GetVersion(), openGlVersion, additionalVersionInfo, projectPath);
        }

        protected override void OnLoad()
        {
            InitWindowsScaling();

            float windowsScaling = GetWindowsScaling();
            ReinitializeImGuiController("Layouts\\Default.xml");

            // Wyłączenie "chwytania" okien za wnętrze
            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;

            // deactivate imgui.ini
            unsafe
            {
                ImGui.GetIO().NativePtr->IniFilename = (byte*)null;
            }

            ApplySettings();

            renderingSystem_ = new RenderingSystem();
            renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            GetModel().SetDefaultRenderPass();

            phc_ = new ProjectHierarchyController(renderingSystem_, codeGenerator_);

            //uiMgr_.RenderingTextureId = renderingSystem_.GetTextureId();

            //uiMgr_.DisplayDeviceWidth  = defaultScreenRes.X;
            //uiMgr_.DisplayDeviceHeight = defaultScreenRes.Y;

            base.OnLoad();
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            // TODO: temporary: przerobić na eventy z UI
            ApplySettings();

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

            bool isAppFocused = PlatformAndOs.IsApplicationFocused();

//            prevKeyboardState_ = currKeyboardState_;
//            currKeyboardState_ = KeyboardState;

            currMouseClientPos_ = this.PointToClient(new Vector2i((int)MouseState.X, (int)MouseState.Y));

            //_controller.SetTextScaling(GetWindowsScaling());
            ImGui.GetIO().FontGlobalScale = GetWindowsScaling();


            //GetModel().CameraDat.RecalculateCamera();
            RecalculateCamera(GetModel().CameraDat);

            HandleCameraFocusAnimation(isAppFocused);
            if (isAppFocused)
            {
                uiMgr_.HandleInput();

                UpdateGlobalKeyboardShortcuts();
            }

            // monitor file system changes
            if (FileSystemWatcherUtils.ChangeDetected)
            {
                FileSystemWatcherUtils.ChangeDetected = false;
                if (GetModel().Config.MonitorFileSystemChanges)
                {
                    OnReloadSdfDefinitions();
                }
            }

            UpdateDemoMode(e.Time);
        }

        //Stopwatch swFrame_  = new Stopwatch();

        // experiments with FPS limitting
        //private double timeAccumulator_ = 0.0f;
        //private static readonly double FpsLimit = 10.0;

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);

            if (!imguiInitialized_)
                return;

            double deltaTime = GetModel().UseConstTimeStep ? GlobalConfig.ConstTimeStep : e.Time;

            //timeAccumulator_ += deltaTime;
            //double timeLimit = 1.0 / FpsLimit;

            GetModel().Update(deltaTime);

            HandleLayoutsSwitching();

            //_controller.Update(this, (float)e.Time);
            ImguiImplOpenGL3.NewFrame();
            ImguiImplOpenTK4.NewFrame();
            ImGui.NewFrame();


            //if (timeAccumulator_ > timeLimit)
            //{
                //renderingSystem_.RenderFrame((float)e.Time, GetModel(), Width, Height);
                renderingSystem_.RenderFrame(GetModel(), Size.X, Size.Y);

            //    timeAccumulator_ = 0.0;//timeLimit;
            //}

            uiMgr_.SubmitUI();
            //ImGui.DockSpaceOverViewport();    // to się już woła wewnątrz SubmitUI()

            //swFrame_.Restart();

            // Ta metoda kumuluje cały czas renderowania klatki
            //_controller.Render();
            ImGui.Render();


            //GetModel().DiagnosticTime = swFrame_.ElapsedMilliseconds;

            //GL.Viewport(0, 0, FramebufferSize.X, FramebufferSize.Y);
            //GL.ClearColor(new Color4(255, 32, 48, 255));
            //GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData());

            if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable))
            {
                ImGui.UpdatePlatformWindows();
                ImGui.RenderPlatformWindowsDefault();
                Context.MakeCurrent();
            }

            CheckGLError("End of frame");

            SwapBuffers();


            // Te akcje muszą być wykonane poza pętlą generowania UI 
            // (ze wzgl. na modyfikowanie iterowanej kolekcji)
            phc_.HandleDeleteNode();
            phc_.HandleMoveNodeUp();
            phc_.HandleMoveNodeDown();
        }

        public static void CheckGLError(string title)
        {
            var error = GL.GetError();
            if (error != OpenTK.Graphics.OpenGL4.ErrorCode.NoError)
            {
                Debug.Print($"{title}: {error}");
            }
        }

        protected override void OnResize(ResizeEventArgs e)
        {
            base.OnResize(e);

            // Update the opengl viewport
            GL.Viewport(0, 0, e.Width, e.Height);

            // Tell ImGui of the new size

            // TODO: IMGUI_UPGRADE verify!
            //if (_controller != null)
            //    _controller.WindowResized(e.Width, e.Height);
        }

        protected override void OnUnload()
        {
            if (renderingSystem_ != null)
            {
                renderingSystem_.Dispose();
                renderingSystem_ = null;
            }

            base.OnUnload();
        }

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);

            // TODO: IMGUI_UPGRADE verify!
            //if (_controller != null)
            //    _controller.PressChar((char)e.Unicode);
        }

        public void OnReloadSdfDefinitions()
        {
            DataModel model = GetModel();

            //model.ReloadCommonFunctions();
            //model.ReloadRenderers(false);
            model.ReloadDefinitions();

            model.RefreshDefinitions();

            renderingSystem_.ReinitializeShader(codeGenerator_);
        }

        public void OnRebuildShader()
        {
            GetModel().RefreshMaterialDefinition();

            renderingSystem_.ReinitializeShader(codeGenerator_);

        }

        public void OnResetFrameCounter()
        {
            //renderingSystem_.ResetFrameCounter();
            GetModel().ResetFrameCounter();
        }

        //public void OnRendererChanged()
        //{
        //    renderingSystem_.ReinitializeShader(codeGenerator_);
        //}

        public void OnRebuildPreviewTexture()
        {
            OnPreviewResolutionChanged();
        }

        public void OnPreviewResolutionChanged()
        {
            renderingSystem_.ReinitializePreviewFrameBuffer(GetModel().Config.GetPreviewResolution());

            //renderingSystem_.ResetFrameCounter();
            GetModel().ResetFrameCounter();

            //uiMgr_.RenderingTextureId = renderingSystem_.GetTextureId();
        }

        public DataModel GetModel()
        {
            if (codeGenerator_ == null)
                return null;

            return codeGenerator_.GetModel();
        }

        public void ToggleFullPreviewMode()
        {
            uiMgr_.SetFullPreviewMode(!uiMgr_.GetFullPreviewMode());
        }

        public void OnNewProject()
        {
            codeGenerator_.CreateNewModel();
            //renderingSystem_.ReinitializeShader(codeGenerator_);

            renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            GetModel().SetDefaultRenderPass();
        }

        public void OnOpenProject()
        {
            string? filePath = WinFormsUtils.OpenProjectFilePath();
            if (String.IsNullOrEmpty(filePath))
                return;

            OnOpenProject(filePath);
        }

        public void OnOpenProject(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return;

            GetModel().ProjectFilePath = filePath;
            //WinFormsUtils.ShowMessageBox("Open file path: " + filePath, "Info");
            LoadProject(GetModel().ProjectFilePath);

            //renderingSystem_.ReinitializeShader(codeGenerator_);

            //GetModel().LastErrorMessage = "";
            GetModel().SelectedNode = null;

            renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            GetModel().SetDefaultRenderPass();

            RefreshWindowTitle();
        }

        public void OnSaveProject()
        {
            if (String.IsNullOrEmpty(GetModel().ProjectFilePath))
                return;

            //WinFormsUtils.ShowMessageBox("Saving to: " + GetModel().ProjectFilePath, "Info");
            SaveProject(GetModel().ProjectFilePath);

            RefreshWindowTitle();
        }

        public void OnSaveProjectAs()
        {
            string filePath = WinFormsUtils.SaveAsProjectFilePath();
            if (String.IsNullOrEmpty(filePath))
                return;

            GetModel().ProjectFilePath = filePath;
            //WinFormsUtils.ShowMessageBox("Save as file path: " + filePath, "Info");
            SaveProject(GetModel().ProjectFilePath);

            RefreshWindowTitle();
        }

        public void OnSaveImage()
        {
            if (GetModel().GetRPassDataForPreview() == null)
            {
                Console.WriteLine("No image to export.");
                return;
            }

            IntCoords textureSize = GetModel().Config.GetPreviewResolution();

            //Bitmap bmp = renderingSystem_.GetFrameAsBitmap(textureSize);
            Bitmap bmp = GetModel().GetRPassDataForPreview().GetFrameAsBitmap(textureSize);
            if (bmp == null)
            {
                Console.WriteLine("Unable to get bitmap data.");
                return;
            }

            string filePath = WinFormsUtils.SaveAsImageFilePath();
            if (String.IsNullOrEmpty(filePath))
                return;

            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void OnExportAnimation()
        {
            if (GetModel().GetRPassDataForPreview() == null)
            {
                Console.WriteLine("No image to export.");
                return;
            }

            IntCoords textureSize = GetModel().Config.GetPreviewResolution();

            string baseDir = WinFormsUtils.GetExportAnimationDirectory();
            if (String.IsNullOrEmpty(baseDir))
                return;

            double deltaTime = 1.0 / GetModel().Config.ExportAnimSettings.Fps;

            // prerender (no save)
            for (int f=0; f<GetModel().Config.ExportAnimSettings.NumOfFramesToPrerender; f++)
            {
                // render
                renderingSystem_.RenderFrame(GetModel(), Size.X, Size.Y);

                // update time
                GetModel().Update(deltaTime);
            }

            // actual render and save
            for (int f=0; f<GetModel().Config.ExportAnimSettings.NumOfFramesToExport; f++)
            {
                // render
                renderingSystem_.RenderFrame(GetModel(), Size.X, Size.Y);

                // update time
                GetModel().Update(deltaTime);

                // get image
                Bitmap bmp = GetModel().GetRPassDataForPreview().GetFrameAsBitmap(textureSize);
                if (bmp == null)
                {
                    Console.WriteLine("Unable to get bitmap data.");
                    break;
                }

                // save image
                string filePath = Path.Combine(baseDir, String.Format("frame_{0}.png", f.ToString("D8")));
                bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
            }

        }


        private string? requestedLayoutFilePath_ = null;
        public void OnLoadLayout(string layoutFilePath)
        {
            requestedLayoutFilePath_ = layoutFilePath;
        }


        public void OnSaveCurrentLayout(WindowsVisibilityCollection windowsVisibility)
        {
            string? filePath = WinFormsUtils.SaveAsLayoutFilePath();
            if (String.IsNullOrEmpty(filePath))
                return;

            string iniSettings = ImGui.SaveIniSettingsToMemory();

            //File.WriteAllText(filePath, iniSettings);

            LayoutData layoutData = new LayoutData();
            layoutData.DisplayName = Path.GetFileNameWithoutExtension(filePath);
            layoutData.WindowsVisibility = windowsVisibility;
            layoutData.ImguiLayoutSettingsTxt = iniSettings;

            XmlDocument xmlDoc = layoutData.Serialize();
            xmlDoc.Save(filePath);
        }



        // gdyby była potrzeba dodania potwierdzenia przy wyjściu z aplikacji
        // to trzeba to dodać w OnClosing() oraz w OnExitApp()
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (imguiInitialized_)
            {
                ImguiImplOpenGL3.Shutdown();
                ImguiImplOpenTK4.Shutdown();
                imguiInitialized_ = false;
            }
        }

        // to jest wołane jako opcja z menu
        public void OnExitApp()
        {
            if (imguiInitialized_)
            {
                ImguiImplOpenGL3.Shutdown();
                ImguiImplOpenTK4.Shutdown();
                imguiInitialized_ = false;
            }

            Environment.Exit(0);
        }

        private void LoadProject(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            GetModel().Deserialize(xmlDoc);
            GetModel().CreateEmptyProjectHierarchy();
            GetModel().FixMaterialsReferences();
            GetModel().RenderingSysData.RefreshDefinitionReference(GetModel().Renderers, GetModel().BackdropsDefinitions, GetModel().CameraControllers);
            GetModel().RefreshMaterialDefinition();
            GetModel().ResetPrevValRecursive();
            UndoManager.Instance.ClearAll();
        }

        private void SaveProject(string filePath)
        {
            XmlDocument xmlDoc = GetModel().Serialize();
            xmlDoc.Save(filePath);
        }






        private void UpdateGlobalKeyboardShortcuts()
        {
            var input = KeyboardState;

            bool isControlKeyDown   = input.IsKeyDown(Keys.LeftControl)  || input.IsKeyDown(Keys.RightControl);
            bool isShiftKeyDown     = input.IsKeyDown(Keys.LeftShift)    || input.IsKeyDown(Keys.RightShift);
            bool isAltKeyDown       = input.IsKeyDown(Keys.LeftAlt)      || input.IsKeyDown(Keys.RightAlt);

// Na .NetCore wydaje się zbędne
//            if (isAltKeyDown)
//            {
//                if (input.IsKeyDown(Keys.F4))
//                {
//                    OnExitApp();
//                }
//            }

            if (IsKeyPressed(Keys.F12))
            {
                ToggleFullPreviewMode();
            }

            if (IsKeyPressed(Keys.Escape))
            {
                // turn off full preview mode
                if (uiMgr_.GetFullPreviewMode())
                {
                    uiMgr_.SetFullPreviewMode(false);
                }
            }

            if (isControlKeyDown)
            {
                // New, open, save
                if (IsKeyPressed(Keys.N))
                {
                    OnNewProject();
                }
                if (IsKeyPressed(Keys.O))
                {
                    OnOpenProject();
                }
                if (IsKeyPressed(Keys.S))
                {
                    bool neverSaved = String.IsNullOrEmpty(GetModel().ProjectFilePath);
                    if (isShiftKeyDown || neverSaved)
                        OnSaveProjectAs();
                    else
                        OnSaveProject();
                }
                if (IsKeyPressed(Keys.I))
                {
                    OnSaveImage();
                }
                if (IsKeyPressed(Keys.W))
                {
                    uiMgr_.CloseCurrentWindow();
                }

                // Rebuild shader
                if (IsKeyPressed(Keys.Enter))
                {
                    renderingSystem_.ReinitializeShader(codeGenerator_);
                }
                if (IsKeyPressed(Keys.R))
                {
                    renderingSystem_.ReinitializeShader(codeGenerator_);
                }

                if (IsKeyPressed(Keys.E))
                {
                    OnReloadSdfDefinitions();
                }
                if (IsKeyPressed(Keys.Z))
                {
                    if (isShiftKeyDown)
                    {
                        UndoManager.Instance.DoRedo();
                    }
                    else
                    {
                        UndoManager.Instance.DoUndo();
                    }
                }

                // Windows shortcuts
                     if (IsKeyPressed(Keys.D1))  { uiMgr_.ToggleWindow(0); }
                else if (IsKeyPressed(Keys.D2))  { uiMgr_.ToggleWindow(1); }
                else if (IsKeyPressed(Keys.D3))  { uiMgr_.ToggleWindow(2); }
                else if (IsKeyPressed(Keys.D4))  { uiMgr_.ToggleWindow(3); }
                else if (IsKeyPressed(Keys.D5))  { uiMgr_.ToggleWindow(4); }
                else if (IsKeyPressed(Keys.D6))  { uiMgr_.ToggleWindow(5); }
                else if (IsKeyPressed(Keys.D7))  { uiMgr_.ToggleWindow(6); }
                else if (IsKeyPressed(Keys.D8))  { uiMgr_.ToggleWindow(7); }
                else if (IsKeyPressed(Keys.D9))  { uiMgr_.ToggleWindow(8); }
                else if (IsKeyPressed(Keys.D0))  { uiMgr_.ToggleWindow(9); }
            }
        }

        public void OnFocusObject(SdfObject node)
        {
            if (node == null)
                return;

            cameraAnimTo_ = node.GetPosition();
            isAnimatingCamera_ = true;
        }

        private void HandleCameraFocusAnimation(bool isAppFocused)
        {
            if (isAppFocused)
            {
                KeyboardState input = KeyboardState;

                bool isControlKeyDown   = input.IsKeyDown(Keys.LeftControl)  || input.IsKeyDown(Keys.RightControl);
                //bool isShiftKeyDown     = input.IsKeyDown(Keys.LeftShift)    || input.IsKeyDown(Keys.RightShift);
                //bool isAltKeyDown       = input.IsKeyDown(Keys.LeftAlt)      || input.IsKeyDown(Keys.RightAlt);

                if (isControlKeyDown)
                {
                    if (IsKeyPressed(Keys.F))
                    {
                        OnFocusObject(GetModel().SelectedNode as SdfObject);
                    }
                }
            }

            if (isAnimatingCamera_)
            {
                if (IsConditionForAnimatingCamera())
                {
                    GetModel().CameraDat.TargetPosition.Val.X = GMath.Lerp(GetModel().CameraDat.TargetPosition.Val.X, cameraAnimTo_.X, 0.2f);
                    GetModel().CameraDat.TargetPosition.Val.Y = GMath.Lerp(GetModel().CameraDat.TargetPosition.Val.Y, cameraAnimTo_.Y, 0.2f);
                    GetModel().CameraDat.TargetPosition.Val.Z = GMath.Lerp(GetModel().CameraDat.TargetPosition.Val.Z, cameraAnimTo_.Z, 0.2f);

                    if (!IsConditionForAnimatingCamera())
                    {
                        GetModel().CameraDat.TargetPosition.Val = cameraAnimTo_;
                        isAnimatingCamera_ = false;
                    }
                }
            }
        }

        public bool IsConditionForAnimatingCamera()
        {
            float distToTarget = (cameraAnimTo_ - GetModel().CameraDat.TargetPosition.Val).Length();
            return distToTarget > CameraAnimThreshold;
        }

// wygląda na to że jest to zbędne
//        public bool IsKeyDown(Keys key)
//        {
//            return KeyboardState.IsKeyDown(key);
//        }
//
//        public bool IsKeyPressed(Keys key)
//        {
////            return !prevKeyboardState_[key] && currKeyboardState_[key];
//            return !KeyboardState.WasKeyDown(key) && KeyboardState.IsKeyDown(key);
//        }

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
            bool isControlKeyDown   = KeyboardState.IsKeyDown(Keys.LeftControl)  || KeyboardState.IsKeyDown(Keys.RightControl);
            return isControlKeyDown;
        }

        public bool IsDownAnyShift()
        {
            bool isShiftKeyDown     = KeyboardState.IsKeyDown(Keys.LeftShift)    || KeyboardState.IsKeyDown(Keys.RightShift);
            return isShiftKeyDown;
        }

        public bool IsDownAnyAlt()
        {
            bool isAltKeyDown       = KeyboardState.IsKeyDown(Keys.LeftAlt)      || KeyboardState.IsKeyDown(Keys.RightAlt);
            return isAltKeyDown;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            mouseScroll_ += e.OffsetY;

            // TODO: IMGUI_UPGRADE verify!
            //if (_controller != null)
            //    _controller.MouseScroll(e.Offset);
        }

        public float GetWheelPrecise()
        {
            //return currMouseState_.WheelPrecise;
            //return MouseState.Scroll;
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

        public void RecalculateCamera(CameraData cameraData)
        {
            Vector3 fromTargetToOrig = -cameraData.DistanceToTarget.Val * Vector3.UnitZ;

            Matrix3 rotYaw   = Matrix3.CreateRotationY(-GMath.DegToRad * cameraData.RotationYaw.Val);
            Matrix3 rotPitch = Matrix3.CreateRotationX(-GMath.DegToRad * cameraData.RotationPitch.Val);

            fromTargetToOrig = rotYaw * rotPitch * fromTargetToOrig;

            cameraData.Origin = cameraData.TargetPosition.Val + MathUtils.ToNumericsVec3(fromTargetToOrig);
            
            cameraData.Forward   = MathUtils.ToNumericsVec3(Vector3.Normalize(-fromTargetToOrig));
            cameraData.Right     = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(System.Numerics.Vector3.UnitY, cameraData.Forward));
            cameraData.Up        = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(cameraData.Forward, cameraData.Right));
        }

        public void CopyTextToClipboard(string text)
        {
            //new SetClipboardHelper(System.Windows.Forms.DataFormats.Text, text).Go();
            ClipboardHelper.SetTextToClipboard(text);
        }

        private void InitWindowsScaling()
        {
            //return 1.0f;  // DEBUG

            windowsScaling_ = PlatformAndOs.ObtainWindowsScaling();
        }

        public float GetWindowsScaling()
        {
            return windowsScaling_ * GetModel().Config.UiTextScaleFactor;
        }

        private static Keys UiKeyToOpenTkKey(UiKey key)
        {
            return (Keys)key;
        }

        private int BatchProcessAllProjects(string rootPath, string searchPattern)
        {
            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);

            int count = 0;
            foreach (string path in files)
            {
                Console.WriteLine("Loading project: {0}", path);
                LoadProject(path);

                SaveProject(path);
                count++;
            }
            return count;
        }

        public void OnBatchProcessAllProjects()
        {
            int countP = BatchProcessAllProjects("Projects", "*.xml");
            int countE = BatchProcessAllProjects("Examples", "*.xml");

            Console.WriteLine("Batch processing finished. Projects:{0}, examples:{1}", countP, countE);

            renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            GetModel().SetDefaultRenderPass();
        }

        private void HandleLayoutsSwitching()
        {
            if (requestedLayoutFilePath_ == null)
                return;

            //ReinitializeImGuiController(requestedLayoutFilePath_);

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

        private void ReinitializeImGuiController(string layoutFilePath)
        {
            LayoutData? layoutData = LoadLayoutFile(layoutFilePath);
            if (layoutData == null)
                return;

            //if (_controller != null)
            //{
            //    _controller.Dispose();
            //    _controller = null;
            //}
            //if (imguiInitialized_)
            //{
            //     ImguiImplOpenGL3.Shutdown();
            //     ImguiImplOpenTK4.Shutdown();
            //    imguiInitialized_ = false;
            //}

            //ImGui.LoadIniSettingsFromDisk(layoutFilePath);

            float windowsScaling = GetWindowsScaling();
            //_controller = new ImGuiController(Size.X, Size.Y, windowsScaling, layoutData.ImguiLayoutSettingsTxt);

            ImGui.CreateContext();
            ImGuiIOPtr io = ImGui.GetIO();
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            io.ConfigFlags |= ImGuiConfigFlags.NavEnableGamepad;
            io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
            //io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;       // This allows drag-out windows

            ImGui.StyleColorsDark();

            ImGuiStylePtr style = ImGui.GetStyle();
            if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
            {
                style.WindowRounding = 0.0f;
                style.Colors[(int)ImGuiCol.WindowBg].W = 1.0f;
            }

            ImguiImplOpenTK4.Init(this);
            ImguiImplOpenGL3.Init();
            // init layout
            ImGui.LoadIniSettingsFromMemory(layoutData.ImguiLayoutSettingsTxt);

            imguiInitialized_ = true;

            uiMgr_.ApplyLayout(layoutData);
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

        public void ApplySettings()
        {
            //RenderFrequency = GetModel().Config.UseRenderFrequencyLimit ? GetModel().Config.RenderFrequencyLimit : 0;
            UpdateFrequency = GetModel().Config.UseUpdateFrequencyLimit ? GetModel().Config.UpdateFrequencyLimit : 0;
        }


        public void OnAddChildObject(SdfObject node, FunctionDefinition definition)
        {
            phc_.OnAddChildObject(node, definition);
        }

        public void OnDeleteNode(TreeNode node)
        {
            phc_.OnDeleteNode(node);
        }

        public void OnCopyObject(SdfObject node)
        {
            phc_.OnCopyObject(node);
        }

        public void OnCutObject(SdfObject node)
        {
            phc_.OnCutObject(node);
        }

        public void OnPasteObject(SdfObject pasteTarget)
        {
            phc_.OnPasteObject(pasteTarget);
        }

        public void OnMoveNodeUp(TreeNode node)
        {
            phc_.OnMoveNodeUp(node);
        }

        public void OnMoveNodeDown(TreeNode node)
        {
            phc_.OnMoveNodeDown(node);
        }

        public void OnAddRenderPass(RenderingData parent, FunctionDefinition definition)
        {
            phc_.OnAddRenderPass(parent, definition);
        }

        public void OnCutRenderPass(RenderPassData node)
        {
            phc_.OnCutRenderPass(node);
        }

        public void OnCopyRenderPass(RenderPassData node)
        {
            phc_.OnCopyRenderPass(node);
        }

        public void OnPasteRenderPass()
        {
            phc_.OnPasteRenderPass();
        }

        // Demo mode
        private double projectDemoDuration_     = 2.0;
        private double demoModeTimer_           = 0.0;
        private Random rndDemoMode_             = new Random();
        private void LoadRandomProject(string rootPath, string searchPattern)
        {
            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
            if (files.Length == 0)
                return;

            int rndIndex = rndDemoMode_.Next(files.Length);
            string path = files[rndIndex];

            Console.WriteLine("Loading project: {0}", path);
            OnOpenProject(path);
        }
        private void UpdateDemoMode(double deltaTime)
        {
            if (!uiMgr_.EnabledDemoMode)
                return;

            demoModeTimer_ += deltaTime;

            if (demoModeTimer_ > projectDemoDuration_)
            {
                demoModeTimer_ -= projectDemoDuration_;
                //LoadRandomProject("Projects", "*.xml");
                LoadRandomProject("Examples", "*.xml");
            }
        }

    }
}
