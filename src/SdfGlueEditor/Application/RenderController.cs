//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Windows;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Platform;

namespace SdfGlueEditor.Application
{
    // Shaders, preview and image export (image sequence export: AnimationExportController)
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
            ctx_.RenderingSystem.RenderFrame(GetModel(), ctx_.GetFrameRenderParams(), ctx_.Window.Size.X, ctx_.Window.Size.Y);
        }

        // Pass image drawn directly into the application window, under the UI (fullscreen preview, background preview).
        // The rectangle is the same as the one used by WndPreview for the mouse input.
        public void RenderScreenPreview(RenderPassData pass, bool keepAspectRatio)
        {
            if (pass.RenderingPass == null || !String.IsNullOrEmpty(pass.LastErrors))
                return;

            int windowSizeX = ctx_.Window.ClientSize.X;
            int windowSizeY = ctx_.Window.ClientSize.Y;
            IntCoords previewRes = ctx_.GetRenderResolution();

            WndPreview.CalcScreenRect(new System.Numerics.Vector2(windowSizeX, windowSizeY), previewRes, keepAspectRatio,
                                      out System.Numerics.Vector2 pos, out System.Numerics.Vector2 size);

            // ImGui coordinates (origin top-left) -> GL window coordinates (origin bottom-left)
            int x0 = (int)MathF.Round(pos.X);
            int x1 = (int)MathF.Round(pos.X + size.X);
            int y0 = windowSizeY - (int)MathF.Round(pos.Y + size.Y);
            int y1 = windowSizeY - (int)MathF.Round(pos.Y);

            // black bars instead of the application background
            if (keepAspectRatio)
                ctx_.RenderingSystem.ClearWindowToBlack();

            pass.RenderingPass.BlitToWindow(previewRes, x0, y0, x1, y1);
        }

        public void ReloadSdfDefinitions()
        {
            DataModel model = GetModel();

            ctx_.Definitions.Reload();
            model.RefreshDefinitions();
            model.RefreshMaterialDefinition();  // materials refer to the renderer definition (<MaterialDefinition>)

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
            ctx_.ReinitializeShader();
        }

        public void ResetFrameCounter()
        {
            ctx_.Playback.ResetFrameCounter();
        }

        public void PreviewResolutionChanged()
        {
            ctx_.RenderingSystem.ReinitializePreviewFrameBuffer(ctx_.GetRenderResolution());

            ctx_.Playback.ResetFrameCounter();
        }

        public void SaveImage()
        {
            RenderPassData? previewPass = GetModel().GetRPassDataForPreview();
            if (previewPass == null)
            {
                Console.WriteLine("No image to export.");
                return;
            }

            using Bitmap? bmp = previewPass.GetFrameAsBitmap();
            if (bmp == null)
            {
                Console.WriteLine("Unable to get bitmap data.");
                return;
            }

            string? filePath = ctx_.Platform.SaveFileDialog(ImageFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            SaveImageToFile(bmp, filePath);
        }

        // Errors are reported to the user
        public bool SaveImageToFile(Bitmap bmp, string filePath)
        {
            try
            {
                bmp.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Cannot save image: {0}. {1}", filePath, ex.Message);
                ctx_.Platform.ShowErrorMessage("Save image", String.Format("Cannot save image:\n{0}\n\n{1}", filePath, ex.Message));
                return false;
            }
        }
    }
}
