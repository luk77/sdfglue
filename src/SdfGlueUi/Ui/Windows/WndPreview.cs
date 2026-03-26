//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Utils;
using SdfGlueUi.Input;
using SdfGlueUi.Ui.Components;
using System.Numerics;
using System.Xml.Linq;

namespace SdfGlueUi.Ui.Windows
{
    public class WndPreview : UiWindowBase
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
            BuildWindow(uiMgr_.CenterColPosX, uiMgr_.BasePosY, uiMgr_.PreviewWidth, uiMgr_.PreviewHeight, delegate()
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
                    uiMgr_.ActionsExecutor.OnPreviewResolutionChanged();
                }
                ImGui.PopID();

                if (DataModel.UseMultiplePreviews)
                {
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
                }

                //ImGui.SameLine();
                //ImGui.Separator();

                if (DataModel.UseMultiplePreviews)
                {
                    ImGui.SameLine();
                    ImGui.Checkbox("Keep aspect ratio", ref constAspectRatio_);
                }
                else
                {
                    ImGui.SameLine();
                    ImGui.Checkbox("Keep aspect ratio", ref GetModel().Config.ConstAspectRatioPreview);

                    ImGui.SameLine();
                    ImGui.Checkbox("Show last selected pass", ref GetModel().Config.PreviewLastSelectedPass);
                }



                RenderPassData? renderPass = GetModel().GetRPassDataForPreview();

                if (DataModel.UseMultiplePreviews)
                {
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

                    renderPass = selectedPass_;
                }

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
                        Vector2 margin = new Vector2(9.0f, 32.0f) * (1.0f + uiMgr_.WindowsScaling);
                        float additionalOffsetY = 12.0f * (1.0f + uiMgr_.WindowsScaling);

                        System.Numerics.Vector2 imgSize = ImGui.GetWindowSize();
                        System.Numerics.Vector2 imgSizeOrg = imgSize;
                        imgSize -= margin;

                        bool constAspectRatio = DataModel.UseMultiplePreviews ? constAspectRatio_ : GetModel().Config.ConstAspectRatioPreview;
                        if (constAspectRatio)
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
                        //IUiActionsExecutor input = uiMgr_.ActionsExecutor;
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
//            ImGui.SetNextWindowPos  (new Vector2(0, 0));
//            ImGui.SetNextWindowSize (new Vector2(uiMgr_.MainWindowSizeX, uiMgr_.MainWindowSizeY));
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

            BuildWindow(0, 0, uiMgr_.MainWindowSizeX, uiMgr_.MainWindowSizeY, flags, delegate()
            {
                RenderPassData renderPass = GetModel().GetRPassDataForPreview();

                float marg = 2.0f;

                // preview image
                //Vector2 margin = new Vector2(18.0f, 64.0f);
                //float additionalOffsetY = 24.0f;
                //Vector2 margin = new Vector2(9.0f, 32.0f) * (1.0f + uiMgr_.WindowsScaling);
                Vector2 margin = new Vector2(marg, marg) * (1.0f + uiMgr_.WindowsScaling);
                float additionalOffsetY = 0.0f;//12.0f * (1.0f + uiMgr_.WindowsScaling);

                System.Numerics.Vector2 imgSize = ImGui.GetWindowSize();
                System.Numerics.Vector2 imgSizeOrg = imgSize;
                imgSize -= margin;

                bool constAspectRatio = DataModel.UseMultiplePreviews ? constAspectRatio_ : GetModel().Config.ConstAspectRatioPreview;
                if (constAspectRatio)
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
                //IUiActionsExecutor input = uiMgr_.ActionsExecutor;
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
            //KeyboardState   input       = currKeyboardState_;
            //MouseState      mouseState  = Mouse.GetCursorState();

            IUiActionsExecutor input = uiMgr_.ActionsExecutor;

            if (IsHovered)
            {
                bool keyDown = GetModel().Config.UseShiftKeyToZoom ? (input.IsKeyDown(UiKey.LeftShift) || input.IsKeyDown(UiKey.RightShift)) : true;
                if (keyDown)
                {
                    //float distanceToTarget = GetModel().CameraDat.DistanceToTarget.Val;
                    float distanceToTarget = GetModel().CameraDat.DistanceToTargetForSmoothing;

                    float deltaWheel = input.GetWheelPrecise() - lastMouseWheelPos_;

                    float deltaDist = -0.3f * GetModel().Config.MouseWheelSpeed * deltaWheel;
                    if (Math.Abs(deltaDist) > 0.0001)
                    {
                        deltaDist = deltaDist;
                    }

                    distanceToTarget += deltaDist;

                    if (Math.Abs(deltaWheel) > 0.0001)
                        ResetFrameCounterIfNeeded();

                    if (distanceToTarget < GlobalConfig.MinDistanceToTarget)
                        distanceToTarget = GlobalConfig.MinDistanceToTarget;

                    //GetModel().CameraDat.DistanceToTarget.Val = distanceToTarget;
                    GetModel().CameraDat.DistanceToTargetForSmoothing = distanceToTarget;
                }
            }
            lastMouseWheelPos_ = input.GetWheelPrecise();
        }

        private void HandlePanAndRotationByMouse()
        {
            IUiActionsExecutor input = uiMgr_.ActionsExecutor;

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
            float mouseX = (float)uiMgr_.ActionsExecutor.GetMouseStateX();
            float mouseY = (float)uiMgr_.ActionsExecutor.GetMouseStateY();
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

            // skalowanie robimy w każdej klatce, aby poprawnie obsłużyć zmianę rozdzielczości podglądu
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
            IUiActionsExecutor input = uiMgr_.ActionsExecutor;

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

            //Vector3 pos = GetModel().CameraDat.TargetPosition.Val;
            //pos += deltaPos;
            //GetModel().CameraDat.TargetPosition.Val = pos;

            GetModel().CameraDat.TargetPositionForSmoothing += deltaPos;
        }

        private void ResetFrameCounterIfNeeded()
        {
            //GetModel().ResetFrameCounterIfNeeded = true;

            if (GetModel().LastSelectedRPass == null)
                return;

            if (!GetModel().LastSelectedRPass.AutoResetFrameCounter.Val)
                return;

            GetModel().ResetFrameCounter();

            //GetModel().LastSelectedRPass.RenderingPass.ResetFrameCounter();
            //GetModel().LastSelectedRPass.ForceClear = true;
        }

    }
}
