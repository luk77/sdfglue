//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Utils;
using SdfGlueUi.Input;
using SingleDocAppFramework.Input;
using SdfGlueUi.Ui.Components;
using SingleDocAppCore.Model.BaseTypes;
using System.Numerics;
using System.Xml.Linq;

namespace SdfGlueUi.Ui.Windows
{
    public class WndPreview : UiWindowSdfGlue
    {
        private bool                        prevStateRmb_               = false;

        // Camera rotation by mouse
        private MouseInputChannel           mouseInputChannelPan_      = new MouseInputChannel(MouseInputChannel.ChannelType.Pan);
        private MouseInputChannel           mouseInputChannelRotate_   = new MouseInputChannel(MouseInputChannel.ChannelType.Rotation);

        // Camera distance by mouse wheel
        private float                       lastMouseWheelPos_          = 0.0f;

        // preview position and size in global screen coords - needed for correct mouse data calculation (iMouse)
        private Vector2                     previewPos_             = new Vector2();
        private Vector2                     previewSize_            = new Vector2();

        private Vector2                     mousePosForShader_      = new Vector2();
        private Vector2                     clickedPosForShader_    = new Vector2();

        //private int                         selectedPassIndex_      = 0;
        private bool                        constAspectRatio_       = true;
        private MenuRenderPass.PreviewMode  previewMode_            = MenuRenderPass.PreviewMode.LastSelectedPass;
        private RenderPassData?             selectedPass_           = null;

        private string title_ = string.Empty;
        public override string Title
        {
            get
            { 
                return title_;
            }
        }

        public WndPreview(string title)
        {
            title_ = title;
        }

        public override void Build()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            BuildWindow(delegate()
            {
                DataModel model = GetModel();

                int id = 1;

                // resolution (label)
                string[] resolutions = GetModel().Config.ResolutionsAsStrings;
                ImGui.PushID(id++);
                ImGui.SameLine();
                ImGui.Text("Resolution:");
                ImGui.PopID();

                // resolution (combo)
                ImGui.PushID(id++);
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200);
                int lastResolutionIndex = GetModel().Config.CurrentResolutionIndex;
                ImGui.Combo("", ref GetModel().Config.CurrentResolutionIndex, resolutions, resolutions.Length, resolutions.Length);
                if (lastResolutionIndex != GetModel().Config.CurrentResolutionIndex)
                {
                    uiMgr.ActionsExecutor.OnPreviewResolutionChanged();
                }
                ImGui.PopID();

                //ImGui.SameLine();
                //ImGui.Separator();

                ImGui.PushID(id++);
                ImGui.SameLine();
                ImGui.Text(" Pass:");
                ImGui.PopID();

                ImGui.SameLine();

                ImGui.PushID(id++);
                MenuRenderPass.Build(GetModel(), ref previewMode_, ref selectedPass_);
                ImGui.PopID();

                //ImGui.SameLine();
                //ImGui.Separator();

                ImGui.SameLine();
                ImGui.Checkbox("Keep aspect ratio", ref constAspectRatio_);



                //if (!GetModel().Config.PreviewLastSelectedPass)
                //{
                //    List<TreeNode> passesList = GetModel().RenderingSysData.Children.ToList();
                //
                //    if (selectedPassIndex_ >= 0 && selectedPassIndex_ < passesList.Count-1)
                //        renderPass = passesList[selectedPassIndex_] as RenderPassData;
                //}

                //if (previewMode_ == MenuRenderPass.PreviewMode.LastSelectedPass)
                //{
                //    renderPass = GetModel().LastSelectedRPass;
                //}
                //else if (previewMode_ == MenuRenderPass.PreviewMode.FinalPass)
                //{
                //    renderPass = GetModel().GetFinalRPass();
                //}
                //else
                //{
                //    renderPass = selectedPass_;
                //}

                RenderPassData? renderPass = selectedPass_;

                if (renderPass != null)
                {
                    if (!String.IsNullOrEmpty(renderPass.LastErrors))
                    {
                        // error messages
                        ImGui.Text(renderPass.LastErrors);
                    }
                    else
                    {
                        // preview image
                        //Vector2 margin = new Vector2(18.0f, 64.0f);
                        //float additionalOffsetY = 24.0f;
                        Vector2 margin = new Vector2(9.0f, 32.0f) * (1.0f + uiMgr.WindowsScaling);
                        float additionalOffsetY = 12.0f * (1.0f + uiMgr.WindowsScaling);

                        System.Numerics.Vector2 imgSize = ImGui.GetWindowSize();
                        System.Numerics.Vector2 imgSizeOrg = imgSize;
                        imgSize -= margin;

                        if (constAspectRatio_)
                        {
                            //Vector2 destRatioVec = new Vector2(1.6f, 0.9f);
                            Vector2 destRatioVec = new Vector2(imgSize.X, imgSize.Y);
                            float destRatio = destRatioVec.X / destRatioVec.Y;

                            IntCoords previewRes = GetModel().Config.GetPreviewResolution();

                            Vector2 scale = new Vector2(1.0f, 1.0f);
                            float ratio = (previewRes.Y > 0.0f) ? (float)previewRes.X / (float)previewRes.Y : 1.0f;
                            if (ratio > destRatio)
                                scale.Y /=  ratio / destRatio;
                            else
                                scale.X *= ratio / destRatio;

                            Vector2 offset = new Vector2(0.5f * (1.0f - scale.X), 0.5f * (1.0f - scale.Y));
                            offset = offset * imgSizeOrg + 0.5f * margin;
                            offset.Y += additionalOffsetY;

                            ImGui.SetCursorPos(offset);

                            imgSize *= scale;
                        }

                        previewPos_     = ImGui.GetCursorScreenPos();
                        previewSize_    = imgSize;

                        // >> debug
                        //ImGui.GetForegroundDrawList().AddRect( previewPos_, previewPos_ + previewSize_, 0xff0000ff );
                        //IUiInput input = uiMgr.ActionsExecutor;
                        //float mouseX = (float)input.GetMouseStateX();
                        //float mouseY = (float)input.GetMouseStateY();
                        //ImGui.GetForegroundDrawList().AddCircle( new Vector2(mouseX, mouseY), 10.0f, 0xff0000ff );
                        // << debug

                        ImGui.Image((IntPtr)renderPass.GetTextureId(), imgSize
                            ,new System.Numerics.Vector2(0.0f, 1.0f)
                            ,new System.Numerics.Vector2(1.0f, 0.0f)
                            );

                        // >> debug
                        //float mX = (mouseX - previewPos_.X) / previewSize_.X;
                        //float mY = (mouseY - previewPos_.Y) / previewSize_.Y;
                        //ImGui.Text(String.Format("Mouse: {0}, {1}", mX, mY));
                        // << debug
                    }
                }
            });
        }

        public void BuildFullPreview()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

//            ImGui.SetNextWindowPos  (new Vector2(0, 0));
//            ImGui.SetNextWindowSize (new Vector2(uiMgr.MainWindowSizeX, uiMgr.MainWindowSizeY));
//
//            if (!ImGui.Begin(Title, ref IsVisible, 
//                ImGuiWindowFlags.NoTitleBar
//                //| ImGuiWindowFlags.NoResize
//                //| ImGuiWindowFlags.NoMove
//                //| ImGuiWindowFlags.NoScrollbar
//                //| ImGuiWindowFlags.NoCollapse
//                //| ImGuiWindowFlags.NoBackground
//                ))
//            {
//                ImGui.End();
//                return;
//            }

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar
                                    | ImGuiWindowFlags.NoResize
                                    | ImGuiWindowFlags.NoMove
                                    | ImGuiWindowFlags.NoScrollbar
                                    | ImGuiWindowFlags.NoCollapse;

            ImGui.SetNextWindowPos  (new Vector2(0, 0), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize (new Vector2(uiMgr.MainWindowSizeX, uiMgr.MainWindowSizeY), ImGuiCond.FirstUseEver);

            BuildWindow(flags, delegate()
            {
                RenderPassData? renderPass = GetModel().GetRPassDataForPreview();

                float marg = 2.0f;

                // preview image
                //Vector2 margin = new Vector2(18.0f, 64.0f);
                //float additionalOffsetY = 24.0f;
                //Vector2 margin = new Vector2(9.0f, 32.0f) * (1.0f + uiMgr.WindowsScaling);
                Vector2 margin = new Vector2(marg, marg) * (1.0f + uiMgr.WindowsScaling);
                float additionalOffsetY = 0.0f;//12.0f * (1.0f + uiMgr.WindowsScaling);

                System.Numerics.Vector2 imgSize = ImGui.GetWindowSize();
                System.Numerics.Vector2 imgSizeOrg = imgSize;
                imgSize -= margin;

                if (constAspectRatio_)
                {
                    //Vector2 destRatioVec = new Vector2(1.6f, 0.9f);
                    Vector2 destRatioVec = new Vector2(imgSize.X, imgSize.Y);
                    float destRatio = destRatioVec.X / destRatioVec.Y;

                    IntCoords previewRes = GetModel().Config.GetPreviewResolution();

                    Vector2 scale = new Vector2(1.0f, 1.0f);
                    float ratio = (previewRes.Y > 0.0f) ? (float)previewRes.X / (float)previewRes.Y : 1.0f;
                    if (ratio > destRatio)
                        scale.Y /=  ratio / destRatio;
                    else
                        scale.X *= ratio / destRatio;

                    Vector2 offset = new Vector2(0.5f * (1.0f - scale.X), 0.5f * (1.0f - scale.Y));
                    offset = offset * imgSizeOrg + 0.5f * margin;
                    offset.Y += additionalOffsetY;

                    ImGui.SetCursorPos(offset);
    
                    imgSize *= scale;
                }

                previewPos_     = ImGui.GetCursorScreenPos();
                previewSize_    = imgSize;

                // >> debug
                //ImGui.GetForegroundDrawList().AddRect( previewPos_, previewPos_ + previewSize_, 0xff0000ff );
                //IUiInput input = uiMgr.ActionsExecutor;
                //float mouseX = (float)input.GetMouseStateX();
                //float mouseY = (float)input.GetMouseStateY();
                //ImGui.GetForegroundDrawList().AddCircle( new Vector2(mouseX, mouseY), 10.0f, 0xff0000ff );
                // << debug

                if (renderPass != null)
                {
                    ImGui.Image((IntPtr)renderPass.GetTextureId(), imgSize
                        ,new System.Numerics.Vector2(0.0f, 1.0f)
                        ,new System.Numerics.Vector2(1.0f, 0.0f)
                        );
                }

                // >> debug
                //float mX = (mouseX - previewPos_.X) / previewSize_.X;
                //float mY = (mouseY - previewPos_.Y) / previewSize_.Y;
                //ImGui.Text(String.Format("Mouse: {0}, {1}", mX, mY));
                // << debug
            });
        }

        public override void HandleInput(float deltaTime)
        {
            base.HandleInput(deltaTime);

            if (!IsVisible)
                return;

//            bool isAppFocused = ApplicationFocusHelper.ApplicationIsActivated();
//            if (!isAppFocused)
//                return;

            HandleCameraDistanceByMouseWheel();
            HandlePanAndRotationByMouse();
            HandleMovementByWASD(deltaTime);

            GetModel().CameraDat.UpdateSmoothing(deltaTime, GetModel().Config);

            StoreMouseDataForShader();
        }

        private void HandleCameraDistanceByMouseWheel()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            //KeyboardState   input       = currKeyboardState_;
            //MouseState      mouseState  = Mouse.GetCursorState();

            IUiInput input = uiMgr.ActionsExecutor;

            if (IsHovered)
            {
                bool keyDown = GetModel().Config.UseShiftKeyToZoom ? (input.IsKeyDown(UiKey.LeftShift) || input.IsKeyDown(UiKey.RightShift)) : true;
                if (keyDown)
                {
                    float deltaWheel = input.GetWheelPrecise() - lastMouseWheelPos_;
                    float deltaDist = -0.3f * GetModel().Config.MouseWheelSpeed * deltaWheel;

                    if (Math.Abs(deltaWheel) > 0.0001)
                        ResetFrameCounterIfNeeded();

                    GetModel().CameraDat.SetDistanceToTargetByDelta(deltaDist);
                }
            }
            lastMouseWheelPos_ = input.GetWheelPrecise();
        }

        private void HandlePanAndRotationByMouse()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            IUiInput input = uiMgr.ActionsExecutor;

            if (GetModel().Config.UseAltRmbForCameraRotation)
            {
                // "Unity style" camera controller:
                // RMB      - pan
                // Alt+RMB  - rotation
                mouseInputChannelPan_       .Update(input.IsRmbDown(), !input.IsDownAnyAlt(), GetModel().Config, input, GetModel().CameraDat, IsHovered);
                mouseInputChannelRotate_    .Update(input.IsRmbDown(), input.IsDownAnyAlt(),  GetModel().Config, input, GetModel().CameraDat, IsHovered);
            }
            else
            {
                // Default controller:
                // RMB - pan
                // LMB - rotation
                mouseInputChannelPan_       .Update(input.IsRmbDown(), true, GetModel().Config, input, GetModel().CameraDat, IsHovered);
                mouseInputChannelRotate_    .Update(input.IsLmbDown(), true, GetModel().Config, input, GetModel().CameraDat, IsHovered);
            }

            bool stateRmb = input.IsRmbDown();

            if (mouseInputChannelPan_.IsDragging ||
                mouseInputChannelRotate_.IsDragging )
            {
                ResetFrameCounterIfNeeded();
            }

            // Mouse data for shader (Shadertoy compatibility)
            if (stateRmb)
            {
                mousePosForShader_ = CalculateMousePosForShader();

                // RMB clicked
                // clicked pos should be saved only on click
                if (!prevStateRmb_)
                {
                    clickedPosForShader_ = mousePosForShader_;
                }
            }

            prevStateRmb_ = stateRmb;
        }

        private Vector2 CalculateMousePosForShader()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            float mouseX = (float)uiMgr.ActionsExecutor.GetMouseStateX();
            float mouseY = (float)uiMgr.ActionsExecutor.GetMouseStateY();
            float mX = (mouseX - previewPos_.X) / previewSize_.X;
            float mY = (mouseY - previewPos_.Y) / previewSize_.Y;

            return new Vector2(mX, mY);
        }

        private void StoreMouseDataForShader()
        {
            IntCoords resolution = GetModel().Config.GetPreviewResolution();
            float ratioX = resolution.X / (float)previewSize_.X;
            float ratioY = resolution.Y / (float)previewSize_.Y;

            float mX = mousePosForShader_.X;
            float mY = mousePosForShader_.Y;
            float cX = clickedPosForShader_.X;
            float cY = clickedPosForShader_.Y;

            // scale in every frame to correctly handle a change of the preview resolution
            mX *= resolution.X;
            mY *= resolution.Y;
            mY = resolution.Y - mY; // flip Y axis

            cX *= resolution.X;
            cY *= resolution.Y;
            cY = resolution.Y - cY; // flip Y axis

            GetModel().RenderingSysData.MouseData = new Vector4(mX, mY, cX, cY);
        }

        private void HandleMovementByWASD(float deltaTime)
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            IUiInput input = uiMgr.ActionsExecutor;

            //bool stateRmb = input.IsRmbDown();
            
            //if (!IsHovered)
            //    return;

            if (!IsFocused)
                return;

            if (input.IsDownAnyCtrl())
                return;

            float movementSpeed = 5.0f * deltaTime;

            if (input.IsDownAnyShift())
                movementSpeed *= 4.0f;

            if (input.IsDownAnyAlt())
                movementSpeed /= 4.0f;

            Vector3 forwardXZ = GetModel().CameraDat.Forward;
            forwardXZ.Y = 0.0f;
            forwardXZ = Vector3.Normalize(forwardXZ);

            Vector3 rightXZ = GetModel().CameraDat.Right;
            rightXZ.Y = 0.0f;
            rightXZ = Vector3.Normalize(rightXZ);

            bool moved = false;

            Vector3 deltaPos = Vector3.Zero;
            if (input.IsKeyDown(UiKey.W))
            {
                moved = true;
                deltaPos += forwardXZ * movementSpeed;
            }
            if (input.IsKeyDown(UiKey.S))
            {
                moved = true;
                deltaPos -= forwardXZ * movementSpeed;
            }
            if (input.IsKeyDown(UiKey.A))
            {
                moved = true;
                deltaPos -= rightXZ * movementSpeed;
            }
            if (input.IsKeyDown(UiKey.D))
            {
                moved = true;
                deltaPos += rightXZ * movementSpeed;
            }
            if (input.IsKeyDown(UiKey.E))
            {
                moved = true;
                deltaPos += Vector3.UnitY * movementSpeed;
            }
            if (input.IsKeyDown(UiKey.Q))
            {
                moved = true;
                deltaPos -= Vector3.UnitY * movementSpeed;
            }

            if (moved)
                ResetFrameCounterIfNeeded();

            GetModel().CameraDat.SetTargetPosByDelta(deltaPos);
        }

        private void ResetFrameCounterIfNeeded()
        {
            //GetModel().ResetFrameCounterIfNeeded = true;

            RenderPassData? lastSelectedRPass = GetModel().LastSelectedRPass;
            if (lastSelectedRPass == null)
                return;

            if (!lastSelectedRPass.AutoResetFrameCounter.Val)
                return;

            GetModel().ResetFrameCounter();

            //GetModel().LastSelectedRPass.RenderingPass.ResetFrameCounter();
            //GetModel().LastSelectedRPass.ForceClear = true;
        }

    }
}
