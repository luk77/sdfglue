//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SingleDocAppCore.Controller;
using SingleDocAppCore.Model.BaseTypes;

namespace SdfGlueCore.Controller
{
    // State of the SdfGlue application shared by the controllers and the UI:
    // the current project (Document, replaced by SetDocument()) and everything which is not saved in it.
    // The editor derives SdfGlueAppContext (window, platform services) from it.
    public class SdfGlueAppContextBase : SdAppContextBase<DataModel>
    {
        // User settings (loaded by the application), shared by consecutive documents
        public  UserSettingsSdfGlue         Settings;

        // Function definitions shared by all models (current and temporary ones)
        public  FunctionLibrary             Definitions;

        public  ShaderCodeGenerator         CodeGenerator;

        // Created by the application when the graphics context is ready
        public  IRenderingSystem            RenderingSystem         = null!;

        // Time of the session (iTime, iFrame), FPS
        public  PlaybackState               Playback                = new PlaybackState();

        public  EditorSelection             Selection               = new EditorSelection();

        // Keyframe animation of the parameters (its own time, not Playback)
        public  AnimationController         Animation;

        // Resolution used instead of the preview resolution (image sequence export)
        public  IntCoords?                  RenderResolutionOverride = null;

        public SdfGlueAppContextBase(UserSettingsSdfGlue settings)
            : base(new DataModel(LoadDefinitions()))
        {
            Settings        = settings;
            Definitions     = Document.Definitions;
            CodeGenerator   = new ShaderCodeGenerator(settings);
            Animation       = new AnimationController(this);
        }

        private static FunctionLibrary LoadDefinitions()
        {
            FunctionLibrary definitions = new FunctionLibrary();
            definitions.Reload();
            return definitions;
        }

        // New (default) project using the function definitions of the application
        public DataModel CreateModel()
        {
            return new DataModel(Definitions);
        }

        public DataModel GetModel()
        {
            return Document;
        }

        // Recreates render passes/shaders for the current document (after new/open project etc.)
        public void ReinitializeRenderingSystem()
        {
            ReinitializeRenderPasses();
            Selection.SelectDefaultRenderPass(Document);
        }

        // Recreates render passes/shaders after the list of passes has changed
        public void ReinitializeRenderPasses()
        {
            Animation.OnShaderGenerating();
            RenderingSystem.Reinitialize(Document, CodeGenerator, GetRenderResolution());
        }

        // Regenerates and compiles the shaders of all passes
        public void ReinitializeShader()
        {
            // the animated values before the code is generated (the fixed parameters get the value at 0 s)
            Animation.OnShaderGenerating();
            RenderingSystem.ReinitializeShader(Document, CodeGenerator);
        }

        // Resolution of the rendered frames: the preview resolution or RenderResolutionOverride
        public IntCoords GetRenderResolution()
        {
            return RenderResolutionOverride ?? Settings.GetPreviewResolution();
        }

        public bool IsAbleToRender()
        {
            return Settings.EnableRendering;
        }

        public FrameRenderParams GetFrameRenderParams()
        {
            return new FrameRenderParams(Playback.CurrentTime, Playback.CurrentFrame, GetRenderResolution(), IsAbleToRender());
        }

        // Called once per frame (not during the image sequence export)
        public void Update(double deltaTime)
        {
            Playback.Update(deltaTime);
            UpdateSignals(deltaTime);
        }

        // Advances the time also when paused, without the FPS statistics (image sequence export)
        public void AdvanceTime(double deltaTime)
        {
            Playback.AdvanceTime(deltaTime);
            UpdateSignals(deltaTime);
        }

        private void UpdateSignals(double deltaTime)
        {
            if (SdfGlueFeatures.UseSignals)
                Document.Signals.Update(Playback.CurrentTime, deltaTime);
        }

        protected override void OnDocumentChanged()
        {
            Playback.Reset();
            Selection.Clear();
            Animation.OnDocumentChanged();

            ReinitializeRenderingSystem();
        }
    }
}
