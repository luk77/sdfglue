//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;

namespace SdfGlueEditor.Application
{
    // State shared by the SdfGlue controllers
    public class SdfGlueAppContext
    {
        public  SdAppWindow                 Window;
        public  IPlatformServices           Platform;

        // The code generator owns the current DataModel (replaced on "New project")
        public  ShaderCodeGenerator         CodeGenerator;

        // Created in EditorMainWindow.OnLoad() (requires the OpenGL context)
        public  IRenderingSystem            RenderingSystem         = null!;

        public SdfGlueAppContext(SdAppWindow window, IPlatformServices platform)
        {
            Window          = window;
            Platform        = platform;
            CodeGenerator   = new ShaderCodeGenerator(new DataModel());
        }

        public DataModel GetModel()
        {
            return CodeGenerator.GetModel();
        }

        // Recreates render passes/shaders for the current model (after new/open project etc.)
        public void ReinitializeRenderingSystem()
        {
            RenderingSystem.Reinitialize(GetModel().RenderingSysData, CodeGenerator, GetModel().Config.GetPreviewResolution());
            GetModel().SetDefaultRenderPass();
        }
    }
}
