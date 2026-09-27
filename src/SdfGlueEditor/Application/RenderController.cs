//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Platform;

namespace SdfGlueEditor.Application
{
    // Shaders, preview and image/animation export
    public class RenderController
    {
        private static readonly FileFilter  ImageFileFilter     = new FileFilter("PNG file", "*.png");

        private SdfGlueAppContext       ctx_;

        public RenderController(SdfGlueAppContext ctx)
        {
            ctx_ = ctx;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }

        public void RenderFrame()
        {
            ctx_.RenderingSystem.RenderFrame(GetModel(), ctx_.Window.Size.X, ctx_.Window.Size.Y);
        }

        public void ReloadSdfDefinitions()
        {
            DataModel model = GetModel();

            model.ReloadDefinitions();
            model.RefreshDefinitions();

            CompileShader();
        }

        // Refreshes material definitions and compiles the shader
        public void RebuildShader()
        {
            GetModel().RefreshMaterialDefinition();

            CompileShader();
        }

        public void CompileShader()
        {
            ctx_.RenderingSystem.ReinitializeShader(ctx_.CodeGenerator);
        }

        public void ResetFrameCounter()
        {
            GetModel().ResetFrameCounter();
        }

        public void PreviewResolutionChanged()
        {
            ctx_.RenderingSystem.ReinitializePreviewFrameBuffer(GetModel().Config.GetPreviewResolution());

            GetModel().ResetFrameCounter();
        }

        public void SaveImage()
        {
            if (GetModel().GetRPassDataForPreview() == null)
            {
                Console.WriteLine("No image to export.");
                return;
            }

            IntCoords textureSize = GetModel().Config.GetPreviewResolution();

            Bitmap bmp = GetModel().GetRPassDataForPreview().GetFrameAsBitmap(textureSize);
            if (bmp == null)
            {
                Console.WriteLine("Unable to get bitmap data.");
                return;
            }

            string? filePath = ctx_.Platform.SaveFileDialog(ImageFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
        }

        public void ExportAnimation()
        {
            if (GetModel().GetRPassDataForPreview() == null)
            {
                Console.WriteLine("No image to export.");
                return;
            }

            IntCoords textureSize = GetModel().Config.GetPreviewResolution();

            string? baseDir = ctx_.Platform.SelectFolderDialog();
            if (String.IsNullOrEmpty(baseDir))
                return;

            double deltaTime = 1.0 / GetModel().Config.ExportAnimSettings.Fps;

            // prerender (no save)
            for (int f=0; f<GetModel().Config.ExportAnimSettings.NumOfFramesToPrerender; f++)
            {
                // render
                RenderFrame();

                // update time
                GetModel().Update(deltaTime);
            }

            // actual render and save
            for (int f=0; f<GetModel().Config.ExportAnimSettings.NumOfFramesToExport; f++)
            {
                // render
                RenderFrame();

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
    }
}
