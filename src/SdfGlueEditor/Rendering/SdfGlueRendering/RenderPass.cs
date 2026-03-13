//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
// References:
// - https://github.com/opentk/LearnOpenTK
// - https://learnopengl.com/Advanced-OpenGL/Framebuffers

using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueEditor.Rendering.OpenTk;
using SdfGlueEditor.Utils;
using SdfGlueCore.Utils;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.Entities;
using System.Text;
using OpenTK.Mathematics;
using SdfGlueCore.AbstractRenderer;
using System.Drawing;
using TreeNode = SdfGlueCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor.Rendering.SdfGlueRendering
{
    public class RenderPass : IDisposable, IRenderingPass
    {
        public static readonly      PixelFormat         ColorBufferPixelFormat      = PixelFormat.Rgba;
        public static readonly      PixelType           ColorBufferPixelType        = PixelType.Float;
        public static readonly      Color4              DisabledShaderClearColor    = new Color4(0, 0, 0, 255);

        // GL resources
        private     RenderPassData              passData_;
        private     OpenTkShader                shader_;
        private     int                         elementBufferObject_            = 0;
        private     int                         vertexBufferObject_             = 0;
        private     int                         vertexArrayObject_              = 0;
        private     int                         windowFrameBuffer_              = 0;
        private     int                         windowFrameBufferTexture_       = 0;

        private     Vector3                     resolution_                     = new Vector3(0.0f, 0.0f, 1.0f);
        private     string                      pathShaderVert_                 = null;
        private     string                      pathShaderFrag_                 = null;

        private readonly float[] vertices_ =
        {
            // Position               UV
             1.0f,  1.0f, 0.0f,     1.0f, 1.0f, // top right
             1.0f, -1.0f, 0.0f,     1.0f, 0.0f, // bottom right
            -1.0f, -1.0f, 0.0f,     0.0f, 0.0f, // bottom left
            -1.0f,  1.0f, 0.0f,     0.0f, 1.0f  // top left
        };

        private readonly uint[] indices_ =
        {
            0, 1, 3,
            1, 2, 3
        };

        public RenderPassData PassData { get { return passData_; } }

        public RenderPass(ShaderCodeGenerator codeGenerator, RenderPassData passData, IntCoords previewResolution, string pathShaderVert, string pathShaderFrag)
        {
            passData_               = passData;
            pathShaderVert_         = pathShaderVert;
            pathShaderFrag_         = pathShaderFrag;

            ReinitializePreviewFrameBuffer(previewResolution);

            vertexBufferObject_ = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject_);
            GL.BufferData(BufferTarget.ArrayBuffer, vertices_.Length * sizeof(float), vertices_, BufferUsageHint.StaticDraw);

            elementBufferObject_ = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject_);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indices_.Length * sizeof(uint), indices_, BufferUsageHint.StaticDraw);

            shader_ = new OpenTkShader();

            vertexArrayObject_ = GL.GenVertexArray();

            ReinitializeShader(codeGenerator);
        }

        public void Dispose()
        {
            // unbind resources
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            // delete resources
            if (vertexBufferObject_ != 0)
            {
                GL.DeleteBuffer(vertexBufferObject_);
                vertexBufferObject_ = 0;
            }

            if (elementBufferObject_ != 0)
            {
                GL.DeleteBuffer(elementBufferObject_);
                elementBufferObject_ = 0;
            }

            if (vertexArrayObject_ != 0)
            {
                GL.DeleteVertexArray(vertexArrayObject_);
                vertexArrayObject_ = 0;
            }

            shader_.ReleaseProgram();

            if (windowFrameBufferTexture_ != 0)
            {
                GL.DeleteTexture(windowFrameBufferTexture_);
                windowFrameBufferTexture_ = 0;
            }

            if (windowFrameBuffer_ != 0)
            {
                GL.DeleteFramebuffer(windowFrameBuffer_);
                windowFrameBuffer_ = 0;
            }
        }

        public void ReinitializePreviewFrameBuffer(IntCoords previewResolution)
        {
            if (windowFrameBufferTexture_ != 0)
            {
                GL.DeleteTexture(windowFrameBufferTexture_);
                windowFrameBufferTexture_ = 0;
            }

            if (windowFrameBuffer_ != 0)
            {
                GL.DeleteFramebuffer(windowFrameBuffer_);
                windowFrameBuffer_ = 0;
            }

            windowFrameBuffer_ = GeneratePreviewWindowFramebuffer(out windowFrameBufferTexture_, previewResolution);
        }

        public int GetTextureId()
        {
            return windowFrameBufferTexture_;
        }

        private int GeneratePreviewWindowFramebuffer(out int textureColorBuffer, IntCoords previewResolution)
        {
            // https://learnopengl.com/Advanced-OpenGL/Framebuffers

            //Generate framebuffer
            int framebuffer = GL.GenFramebuffer();
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer);

            //GL.ClampColor(ClampColorTarget.ClampReadColor, ClampColorMode.False);
            //GL.ClampColor(ClampColorTarget.ClampFragmentColor, ClampColorMode.False);
            //GL.ClampColor(ClampColorTarget.ClampVertexColor, ClampColorMode.False);

            // create a RGBA color texture
            GL.GenTextures(1, out textureColorBuffer);
            GL.BindTexture(TextureTarget.Texture2D, textureColorBuffer);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f,
                                previewResolution.X, previewResolution.Y,
                                0, ColorBufferPixelFormat, ColorBufferPixelType,
                                IntPtr.Zero);

            //GL.Enable(EnableCap.FramebufferSrgb);
            //GL.Disable(EnableCap.FramebufferSrgb);

            TextureMinFilter filtering = passData_.UseTextureFiltering.Val ? TextureMinFilter.Linear : TextureMinFilter.Nearest;

            // Set the texture filtering parameters
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)filtering);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)filtering);

            GL.BindTexture(TextureTarget.Texture2D, 0);

            //Create color attachment texture
            GL.FramebufferTexture(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, textureColorBuffer, 0);

            DrawBuffersEnum[] buffers = new DrawBuffersEnum[1] { (DrawBuffersEnum)FramebufferAttachment.ColorAttachment0 };
            GL.DrawBuffers(buffers.Length, buffers);

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);

            return framebuffer;
        }

        private void CleanupShader()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            shader_.ReleaseProgram();
        }


        public void ReinitializeShader(ShaderCodeGenerator codeGenerator)
        {
            CleanupShader();

            StringBuilder sbErrors = new StringBuilder(2048);

            // TODO:
            // Tymczasowo generowanie kodu Unity wpięte na sztywno tutaj.
            // Docelowo można by to robić tylko przy eksporcie.
            //codeGenerator.GetModel().LastGenCodeUnity = codeGenerator.GenerateUnityCode(passData_);
            passData_.LastGenCodeUnity = codeGenerator.GenerateUnityCode(sbErrors, passData_);

            string shaderVert = File.ReadAllText(pathShaderVert_);
            string shaderFrag = File.ReadAllText(pathShaderFrag_);
            shaderFrag = codeGenerator.ModifyFragShaderSource(sbErrors, shaderFrag, passData_.LastGenCode, false, passData_);

            shader_.Reinitialize(sbErrors, shaderVert, shaderFrag, passData_.Name.Val);
            shader_.Use();

            passData_.LastErrors = sbErrors.ToString();

            GL.BindVertexArray(vertexArrayObject_);

            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBufferObject_);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, elementBufferObject_);

            // Single vertex data description:
            int vertexDataSize              = 5 * sizeof(float);   // 3 for position and 2 for UV
            int vertexDataPositionOffset    = 0;                   // position starts at the beginning of the vertex data
            int vertexDataPositionSize      = 3;                   // 3 floats for position
            int vertexDataTexCoordOffset    = 3 * sizeof(float);   // texture coordinates start after position data (3 floats for position)
            int vertexDataTexCoordSize      = 2;                   // 2 floats for texture coordinates

            int vertexLocation = shader_.GetAttribLocation("inPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, vertexDataPositionSize, VertexAttribPointerType.Float, false, vertexDataSize, vertexDataPositionOffset);

            int texCoordLocation = shader_.GetAttribLocation("inUvCoord");
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, vertexDataTexCoordSize, VertexAttribPointerType.Float, false, vertexDataSize, vertexDataTexCoordOffset);
        }

        public bool IsAbleToRender()
        {
            if (!String.IsNullOrEmpty(passData_.LastErrors))
                return false;

            return true;
        }

        public void RenderFrameEmpty()
        {
            // Bind the vertex array object (VAO)
            GL.BindVertexArray(vertexArrayObject_);

            // Switch to rendering to the window texture
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, windowFrameBuffer_);

            // This is clearing when shader is disabled
            GL.ClearColor(DisabledShaderClearColor);

            // Clear - only color buffer is used
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // Switch back to rendering to the default framebuffer (the screen)
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        }

        public void RenderFrame(DataModel model, bool renderEnabled, int prevPassTextureId)
        {
            IntCoords resolution = model.Config.GetPreviewResolution();
            resolution_.X = resolution.X;
            resolution_.Y = resolution.Y;

            if (renderEnabled)
            {
                // Bind the vertex array object (VAO)
                GL.BindVertexArray(vertexArrayObject_);

                // Switch to rendering to the window texture
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, windowFrameBuffer_);

                // Clear the framebuffer (if needed)
                if (passData_.ClearOnEveryFrame.Val || passData_.ForceClear)
                {
                    passData_.ForceClear = false;

                    Color4 clearColor = new Color4( passData_.ClearColor.Val.X, 
                                                    passData_.ClearColor.Val.Y, 
                                                    passData_.ClearColor.Val.Z, 
                                                    passData_.ClearOpacity.Val);

                    GL.ClearColor(clearColor);

                    // Only color buffer is used
                    GL.Clear(ClearBufferMask.ColorBufferBit);
                }

                // Render the shader
                RenderShader(model, prevPassTextureId);

                // Switch back to rendering to the default framebuffer (the screen)
                GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            }
        }

        private void RenderShader(DataModel model, int prevPassTextureId)
        {
            if (!passData_.Enabled.Val)
                return;

            shader_.Use();

            // Shadertoy compatible uniforms
            shader_.SetFloat("iTime", (float)model.CurrentTime);
            shader_.SetInt("iFrame", model.CurrentFrame);
            shader_.SetVector3("iResolution", resolution_);
            shader_.SetVector4("iMouse", MathUtils.ToGlVec4(model.RenderingSysData.MouseData));

            // Output z tego przebiegu jako kanał 0
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, GetTextureId());

            // Output z poprzedniego przebiegu jako kanał 1
            GL.ActiveTexture(TextureUnit.Texture1);
            GL.BindTexture(TextureTarget.Texture2D, prevPassTextureId);

            // To jest istotne, bez tego nie działa:
            // From: https://opentk.net/learn/chapter1/6-multiple-textures.html
            shader_.SetInt("iChannel0", 0); // W 0 jest output z tego przebiegu
            shader_.SetInt("iChannel1", 1); // W 1 jest output z poprzedniego przebiegu
            shader_.SetInt("iChannel2", 2); // na razie nie używane
            shader_.SetInt("iChannel3", 3); // na razie nie używane

            // Camera
            shader_.SetVector3  ("cameraTargetPosition"     , MathUtils.ToGlVec3(model.CameraDat.TargetPosition.Val)          );
            shader_.SetFloat    ("cameraRotationPitch"      , model.CameraDat.RotationPitch.Val * GMath.DegToRad    );
            shader_.SetFloat    ("cameraRotationYaw"        , model.CameraDat.RotationYaw.Val   * GMath.DegToRad    );
            shader_.SetFloat    ("cameraRotationRoll"       , model.CameraDat.RotationRoll.Val  * GMath.DegToRad    );
            shader_.SetFloat    ("cameraDistanceToTarget"   , model.CameraDat.DistanceToTarget.Val                  );
            shader_.SetFloat    ("cameraZoom"               , model.CameraDat.Zoom.Val                              );

            // Materials
            foreach(MaterialInstance mat in model.Materials.Children)
            {
                bool isFixed = model.ProjSettings.FixAllObjects.Val || mat.IsFixed.Val;
                if (isFixed)
                    continue;

                string materialPrefix = String.Format("material_{0}.", mat.Id);

                foreach(FunctionDefParameter p in mat.MaterialProps.Definition.MaterialParameters)
                {
                    string uniformFullName = String.Format("{0}{1}", materialPrefix, p.ParameterName);
                    ISimpleType paramVal = mat.MaterialProps.ParametersValues[p.ParameterName];
                    SetShaderParameterByType(paramVal, uniformFullName);
                }
            }

            // Renderer
            SetShaderParametersForFunctionEntity(passData_.RendererFunc, null);

            // Backdrop
            SetShaderParametersForFunctionEntity(passData_.BackdropFunc, null);

            if (DataModel.UseCameraControllers)
            {
                SetShaderParametersForOperatorsCollection(passData_.CameraOperators, null);
            }

            // Objects
            TreeNode.CallRecursive(model.SdfRoot, delegate(TreeNode node)
            {
                string nodeId   = model.Config.UseNamesAsIds ? node.Name.Val : node.Id.ToString();

                SdfObject sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                // Większość uniform'ów jest współdzielona między funkcjami distance i material.
                // Dla tego ich ustawianie można pominąć tylko wtedy, gdy obie flagi są wyłączone.
                if (!sdfObj.CanBeUsedInDistanceFunction() && !sdfObj.CanBeUsedInMaterialsFunction())
                    return;

                SetShaderParameterByType(sdfObj.BlendFactor             , String.Format("g_obj_{0}_blendF"      , nodeId));
                SetShaderParameterByType(sdfObj.MaterialId              , String.Format("g_obj_{0}_matId"       , nodeId));
                SetShaderParameterByType(sdfObj.MaterialBlendFactor     , String.Format("g_obj_{0}_matBlendF"   , nodeId));

                SetShaderParametersForFunctionEntity(sdfObj.FunctionMixOp, nodeId);

                if (sdfObj.UseShape.Val)
                {
                    SetShaderParametersForFunctionEntity(sdfObj.FunctionSdf, nodeId);
                }

                SetShaderParametersForOperatorsCollection(sdfObj.PositionOperators, nodeId);
                SetShaderParametersForOperatorsCollection(sdfObj.DistanceOperators, nodeId);
            });

            GL.DrawElements(PrimitiveType.Triangles, indices_.Length, DrawElementsType.UnsignedInt, 0);
        }

        private void SetShaderParametersForOperatorsCollection(OperatorsCollection collection, string nodeId)
        {
            for(int i=0; i<collection.Operators.Count; i++)
            {
                OperatorEntity opent = collection.Operators[i];
                SetShaderParametersForFunctionEntity(opent, nodeId, i);
            }
        }

        private void SetShaderParameterByType(ISimpleType paramVal, string uniformName)
        {
            if (paramVal is ExFloat)
            {
                if (paramVal is ExFloatWithSignal)
                {
                    ExFloatWithSignal exObjF = paramVal as ExFloatWithSignal;
                    float currVal = exObjF.Val;
                    if (exObjF.SignalRef != null)
                    {
                        currVal = exObjF.SignalRef.GetCurrentValue();
                    }

                    shader_.SetFloat(uniformName, currVal);
                }
                else
                {
                    ExFloat exObjF = paramVal as ExFloat;
                    shader_.SetFloat(uniformName, exObjF.Val);
                }
            }
            else if (paramVal is ExInt)
            {
                ExInt exObjI = paramVal as ExInt;
                shader_.SetInt(uniformName, exObjI.Val);
            }
            else if (paramVal is ExVector2)
            {
                ExVector2 exObjV2 = paramVal as ExVector2;
                shader_.SetVector2(uniformName, MathUtils.ToGlVec2(exObjV2.Val));
            }
            else if (paramVal is ExVector3)
            {
                ExVector3 exObjV3 = paramVal as ExVector3;
                shader_.SetVector3(uniformName, MathUtils.ToGlVec3(exObjV3.Val));
            }
            else if (paramVal is ExVector4)
            {
                ExVector4 exObjV4 = paramVal as ExVector4;
                shader_.SetVector4(uniformName, MathUtils.ToGlVec4(exObjV4.Val));
            }
            //else if (paramVal is ExBool)
            //{
            //    ExBool exObjB = paramVal as ExBool;
            //    shader_.SetBool(uniformName, exObjB.Val);
            //}
        }

        private void SetShaderParametersForFunctionEntity(FunctionEntity functionEntity, string nodeId, int opEntityIndex = -1)
        {
            bool simpleParamsFormat = String.IsNullOrEmpty(nodeId);

            for(int i=0; i<functionEntity.Definition.Parameters.Count; i++)
            {
                FunctionDefParameter p = functionEntity.Definition.Parameters[i];

                string? fullNameId = null;
                if (opEntityIndex == -1)
                {
                    if (simpleParamsFormat)
                        fullNameId = String.Format("{0}_{1}"                , functionEntity.ParamPrefix, p.ParameterName);
                    else
                        fullNameId = String.Format("g_obj_{0}_{1}_{2}"      , nodeId, functionEntity.ParamPrefix, p.ParameterName);
                }
                else
                {
                    if (simpleParamsFormat)
                        fullNameId = String.Format("{0}{1}_{2}"                , functionEntity.ParamPrefix, opEntityIndex + 1, p.ParameterName);
                    else
                        fullNameId = String.Format("g_obj_{0}_{1}{2}_{3}"      , nodeId, functionEntity.ParamPrefix, opEntityIndex + 1, p.ParameterName);
                }

                ISimpleType paramVal = functionEntity.ParametersValues[p.ParameterName];
                SetShaderParameterByType(paramVal, fullNameId);
            }
        }


        public Bitmap GetFrameAsBitmap(IntCoords textureSize)
        {
            byte[] pixels = new byte[textureSize.X * textureSize.Y * 4];
            int stride = textureSize.X * 4;

            //texture.Bind();
            GL.BindTexture(TextureTarget.Texture2D, windowFrameBufferTexture_);

            // To dziwne, ale trzeba użyć formatu PixelFormat.Bgr...
            //GL.GetTexImage<byte>(TextureTarget.Texture2D, 0, (PixelFormat)PixelInternalFormat.Rgba, PixelType.UnsignedByte, pixels);
            GL.GetTexImage<byte>(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

            // konwerja rgb -> bgr
            //int pixelIndex = 0;
            //byte offR = 0;
            //byte offG = 0;
            //byte offB = 0;
            //byte offA = 0;
            //for(int y=0; y<textureSize.Y; y++)
            //{
            //    for(int x=0; x<textureSize.X; x++)
            //    {
            //        pixelIndex = x + textureSize.X * y;
            //        offR = pixels[pixelIndex * 4 + 0];
            //        offG = pixels[pixelIndex * 4 + 1];
            //        offB = pixels[pixelIndex * 4 + 2];
            //        offA = pixels[pixelIndex * 4 + 3];
            //        pixels[pixelIndex * 4 + 0] = offB;
            //        pixels[pixelIndex * 4 + 1] = offG;
            //        pixels[pixelIndex * 4 + 2] = offR;
            //        pixels[pixelIndex * 4 + 3] = offA;
            //    }
            //}

            IntPtr pixelAddress = Marshal.UnsafeAddrOfPinnedArrayElement(pixels, 0);

            Bitmap bmp = new Bitmap(textureSize.X, textureSize.Y, stride, 
                System.Drawing.Imaging.PixelFormat.Format32bppArgb, pixelAddress);
            bmp.RotateFlip(RotateFlipType.Rotate180FlipX);
            //System.Drawing.Imaging.BitmapData data = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            //    System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            //IntPtr ptr = data.Scan0;

            return bmp;
        }
    }
}
