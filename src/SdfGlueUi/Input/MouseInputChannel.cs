//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui;

namespace SdfGlueUi.Input
{
    internal class MouseInputChannel
    {
        public enum ChannelType
        {
            Pan,
            Rotation
        }

        private ChannelType                 channelType_                = ChannelType.Pan;

        private bool                        isDragging_                 = false;
        private bool                        prevButtonState_            = false;
        private int                         mouseDragStartX_            = 0;
        private int                         mouseDragStartY_            = 0;
        private float                       dragStartCameraPitch_       = 0.0f;
        private float                       dragStartCameraYaw_         = 0.0f;
        private System.Numerics.Vector3     dragStartCameraTarget_      = new System.Numerics.Vector3(0.0f);

        public MouseInputChannel(ChannelType type)
        {
            channelType_ = type;
        }

        public bool IsDragging { get { return isDragging_; } }

        public void Update(bool buttonCurrentState, bool isRequiredKeyPressed, GlobalConfig config, IUiActionsExecutor input, CameraData cameraData, bool isHovered)
        {
            bool dragStart              = !prevButtonState_ && buttonCurrentState && isRequiredKeyPressed;
            bool dragEnd                = prevButtonState_ && !buttonCurrentState;

            if (isHovered)
            {
                if (dragStart)
                {
                    isDragging_ = true;
                    mouseDragStartX_            = input.GetMouseStateX();
                    mouseDragStartY_            = input.GetMouseStateY();
                    dragStartCameraPitch_       = cameraData.RotationPitch.Val;
                    dragStartCameraYaw_         = cameraData.RotationYaw.Val;
                    dragStartCameraTarget_      = cameraData.TargetPosition.Val;
                }
            }

            if (dragEnd)
            {
                isDragging_ = false;
            }

            if (isDragging_)
            {
                if (channelType_ == ChannelType.Pan)
                {
                    // pan
                    float dragSpeed = 0.002f * config.MouseCameraPanSpeed * cameraData.DistanceToTarget.Val;
                    float mouseDragDeltaX = input.GetMouseStateX() - mouseDragStartX_;
                    float mouseDragDeltaY = input.GetMouseStateY() - mouseDragStartY_;
                    cameraData.TargetPosition.Val = dragStartCameraTarget_ - dragSpeed * mouseDragDeltaX * cameraData.Right + dragSpeed * mouseDragDeltaY * cameraData.Up;
                    // reset this value - no smoothing in that case
                    cameraData.TargetPositionForSmoothing = cameraData.TargetPosition.Val;
                }
                else if (channelType_ == ChannelType.Rotation)
                {
                    // rotation
                    float mouseDragDeltaX = input.GetMouseStateX() - mouseDragStartX_;
                    float mouseDragDeltaY = input.GetMouseStateY() - mouseDragStartY_;

                    if (config.MouseCameraRotationInvPitch)
                        mouseDragDeltaX *= -1.0f;

                    if (config.MouseCameraRotationInvYaw)
                        mouseDragDeltaY *= -1.0f;

                    //cameraData.RotationPitch.Val  = dragStartCameraPitch_ + 0.5f * config.MouseCameraRotationSpeedPitch * mouseDragDeltaY;
                    //cameraData.RotationYaw.Val    = dragStartCameraYaw_   + 0.5f * config.MouseCameraRotationSpeedYaw   * mouseDragDeltaX;

                    cameraData.RotationPitchForSmoothing    = dragStartCameraPitch_ + 0.5f * config.MouseCameraRotationSpeedPitch * mouseDragDeltaY;
                    cameraData.RotationYawForSmoothing      = dragStartCameraYaw_   + 0.5f * config.MouseCameraRotationSpeedYaw   * mouseDragDeltaX;
                }
            }

            prevButtonState_ = buttonCurrentState;
        }
    }

}
