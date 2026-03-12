//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using OpenTK.Graphics.OpenGL4;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.BaseTypes;
using TreeNode = SdfGlueCore.Model.DataNodes.TreeNode;
using OpenTK.Mathematics;
using SdfGlueCore.AbstractRenderer;

namespace SdfGlueEditor.Rendering.SdfGlueRendering
{
    public class RenderingSystem : IDisposable, IRenderingSystem
    {
        private         List<RenderPass>          passes_          = new List<RenderPass>();

        public RenderingSystem()
        {
        }

        public void Reinitialize(RenderingData renderingData, ShaderCodeGenerator codeGenerator, IntCoords previewResolution)
        {
            Dispose();

            RenderPass prevPass = null;

            foreach(TreeNode node in renderingData.Children)
            {
                RenderPassData passData = node as RenderPassData;
                if (passData == null)
                    continue;

                passData.LastErrors = "";

                //if (passData.IsPrimaryPass)
                //{
                //    RenderPass pass = new RenderPass(codeGenerator, passData, previewResolution, "Shaders/shader.vert", "Shaders/Template.frag");
                //    passes_.Add(pass);
                //    passData.RenderingPass = pass;
                //    prevPass = pass;
                //}
                //else
                //{
                //    RenderPass pass = new RenderPass(codeGenerator, passData, previewResolution, "Shaders/shader.vert", "Shaders/TemplateSecondaryPass.frag");
                //    passes_.Add(pass);
                //    passData.RenderingPass = pass;
                //    prevPass = pass;
                //}

                // Unification - Same template for primary/secondary passess
                RenderPass pass = new RenderPass(codeGenerator, passData, previewResolution, "Shaders/Template.vert", "Shaders/Template.frag");
                passes_.Add(pass);
                passData.RenderingPass = pass;
                prevPass = pass;

            }
        }

        public void Dispose()
        {
            foreach(RenderPass pass in passes_)
            {
                pass.Dispose();
            }
            passes_.Clear();
        }

        //Stopwatch swFrame_  = new Stopwatch();
        //Stopwatch swPass_   = new Stopwatch();

        //public void RenderFrame(float deltaTime, DataModel model, int appViewportWidth, int appViewportHeight)
        public void RenderFrame(DataModel model, int appViewportWidth, int appViewportHeight)
        {
            //swFrame_.Restart();

            //if (model.ResetFrameCounterIfNeeded)
            //{
            //    model.ResetFrameCounterIfNeeded = false;
            //    ResetFrameCounter();
            //    foreach(RenderPass pass in passes_)
            //    {
            //        pass.PassData.ForceClear = true;
            //    }
            //}

            // to jest clear całego okna (tło pod oknami ImGui)
            GL.ClearColor(new Color4(0, 32, 48, 255));
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);

            // Ustawiamy viewport dla podglądu w oknie
            IntCoords previewResolution = model.Config.GetPreviewResolution();
            GL.Viewport(0, 0, previewResolution.X, previewResolution.Y);

            RenderPass prevEnabledPass = null;

            foreach(RenderPass pass in passes_)
            {
                //swPass_.Restart();

                if (!pass.PassData.Enabled.Val || !pass.IsAbleToRender())
                {
                    pass.RenderFrameEmpty();
                    //pass.PassData.SetLastRenderTime(swPass_.ElapsedMilliseconds);
                    continue;
                }


                int prevPassTextureId = (prevEnabledPass != null) ? prevEnabledPass.GetTextureId() : 0;

                //pass.RenderFrame(deltaTime, model, model.IsAbleToRender(), appViewportWidth, appViewportHeight, prevPassTextureId);
                pass.RenderFrame(model, model.IsAbleToRender(), prevPassTextureId);
                //swPass_.Stop();
                //pass.PassData.SetLastRenderTime(swPass_.ElapsedMilliseconds);

                prevEnabledPass = pass;
            }

            // Przywracamy viewport dla całego ekranu (z wszystkimi oknami ImGui)
            GL.Viewport(0, 0, appViewportWidth, appViewportHeight);

            //swFrame_.ElapsedMilliseconds;

        }

        public void ReinitializeShader(ShaderCodeGenerator codeGenerator)
        {
            foreach(RenderPass pass in passes_)
            {
                pass.ReinitializeShader(codeGenerator);
            }
        }

        //public void ResetFrameCounter()
        //{
        //    foreach(RenderPass pass in passes_)
        //    {
        //        pass.ResetFrameCounter();
        //    }
        //}

        public void ReinitializePreviewFrameBuffer(IntCoords previewResolution)
        {
            foreach(RenderPass pass in passes_)
            {
                pass.ReinitializePreviewFrameBuffer(previewResolution);
            }
        }

        //internal int GetTextureId()
        //{
        //    return pass0_.GetTextureId();
        //}
        //
        //public Bitmap GetFrameAsBitmap(IntCoords textureSize)
        //{
        //    return pass0_.GetFrameAsBitmap(textureSize);
        //}
    }
}
