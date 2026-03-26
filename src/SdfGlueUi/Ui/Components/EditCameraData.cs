//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Properties;

namespace SdfGlueUi.Ui.Components
{
    public class EditCameraData
    {
        public static void Build(ref CameraData cameraData, float firstColumnWidth)
        {
            UiWindowBase.BeginPropertyGrid(firstColumnWidth);

            int id = 1;

            if (UiVector3.Build(ref id, "Target position"     , ref cameraData.TargetPosition     .Val    , 0.02f))
            {
                cameraData.TargetPositionForSmoothing = cameraData.TargetPosition.Val;
            }

            if (UiFloat.Build(ref id, "Pitch rotation"      , ref cameraData.RotationPitch      .Val    , 0.1f))
            {
                cameraData.RotationPitchForSmoothing = cameraData.RotationPitch.Val;
            }

            if (UiFloat.Build(ref id, "Yaw rotation"        , ref cameraData.RotationYaw        .Val    , 0.1f))
            {
                cameraData.RotationYawForSmoothing = cameraData.RotationYaw.Val;
            }

            UiFloat     .Build(ref id, "Roll rotation"       , ref cameraData.RotationRoll       .Val    , 0.1f);

            if (UiFloat.Build(ref id, "Distance to target"  , ref cameraData.DistanceToTarget   .Val    , 0.02f))
            {
                cameraData.DistanceToTargetForSmoothing = cameraData.DistanceToTarget.Val;
            }

            UiFloat     .Build(ref id, "Zoom"                , ref cameraData.Zoom               .Val    , 0.02f);

            // debug
            //UiVector3.Build(ref id, "Origin"                , ref cameraData.Origin   , 0.02f);
            //UiVector3.Build(ref id, "Forward"               , ref cameraData.Forward  , 0.02f);
            //UiVector3.Build(ref id, "Right"                 , ref cameraData.Right    , 0.02f);
            //UiVector3.Build(ref id, "Up"                    , ref cameraData.Up       , 0.02f);

            UiWindowBase.EndPropertyGrid();
        }
    }
}
