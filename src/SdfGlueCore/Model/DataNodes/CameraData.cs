//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Utils;
using System.Numerics;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes
{
    public class CameraData : TreeNode
    {
        // serializable
        public  ExVector3               TargetPosition      = new ExVector3(new Vector3(0.0f));
        public  ExFloat                 RotationPitch       = new ExFloatSimple(20.0f);
        public  ExFloat                 RotationYaw         = new ExFloatSimple(0.0f);
        public  ExFloat                 RotationRoll        = new ExFloatSimple(0.0f);
        public  ExFloat                 DistanceToTarget    = new ExFloatSimple(5.0f);
        public  ExFloat                 Zoom                = new ExFloatSimple(1.0f);

        // calculated
        public  Vector3                 Origin              = new Vector3(0.0f);    // calculated
        public  Vector3                 Forward             = new Vector3(0.0f);    // calculated
        public  Vector3                 Right               = new Vector3(0.0f);    // calculated
        public  Vector3                 Up                  = new Vector3(0.0f);    // calculated

        public  bool                    UseSmoothing        = true;

        // helper data for smoothing
        public  Vector3                 TargetPositionForSmoothing  = new Vector3(0.0f);
        public  float                   RotationPitchForSmoothing   = 0.0f;
        public  float                   RotationYawForSmoothing     = 0.0f;
        public  float                   DistanceToTargetForSmoothing= 0.0f;

        public CameraData() : base(DataModel.NodeIdCameraMain, "Camera")
        {
            ResetSmoothing();
        }

//        internal void RecalculateCamera()
//        {
//            OpenTK.Vector3 fromTargetToOrig = -DistanceToTarget * OpenTK.Vector3.UnitZ;
//
//            OpenTK.Matrix3 rotYaw   = OpenTK.Matrix3.CreateRotationY(-GMath.DegToRad * RotationYaw);
//            OpenTK.Matrix3 rotPitch = OpenTK.Matrix3.CreateRotationX(-GMath.DegToRad * RotationPitch);
//
//            fromTargetToOrig = rotYaw * rotPitch * fromTargetToOrig;
//
//            Origin = TargetPosition + MathUtils.ToNumericsVec3(fromTargetToOrig);
//            
//            Forward   = MathUtils.ToNumericsVec3(OpenTK.Vector3.Normalize(-fromTargetToOrig));
//            Right     = Vector3.Normalize(Vector3.Cross(Vector3.UnitY, Forward));
//            Up        = Vector3.Normalize(Vector3.Cross(Forward, Right));
//        }

        internal bool Deserialize(XmlNode nodeThis)
        {
            XmlUtils.DeserializeVector3 (nodeThis, "TargetPosition"     , ref TargetPosition    .Val    );
            XmlUtils.DeserializeFloat   (nodeThis, "RotationPitch"      , ref RotationPitch     .Val    );
            XmlUtils.DeserializeFloat   (nodeThis, "RotationYaw"        , ref RotationYaw       .Val    );
            XmlUtils.DeserializeFloat   (nodeThis, "RotationRoll"       , ref RotationRoll      .Val    );
            XmlUtils.DeserializeFloat   (nodeThis, "DistanceToTarget"   , ref DistanceToTarget  .Val    );
            XmlUtils.DeserializeFloat   (nodeThis, "Zoom"               , ref Zoom              .Val    );

            ResetSmoothing();

            return true;
        }

        public void SetTargetPos(Vector3 pos)
        {
            if (UseSmoothing)
            {
                TargetPositionForSmoothing  = pos;
            }
            else
            {
                TargetPosition.Val          = pos;
                TargetPositionForSmoothing  = TargetPosition.Val;
            }
        }

        public void SetTargetPosByDelta(Vector3 deltaPos)
        {
            if (UseSmoothing)
            {
                TargetPositionForSmoothing  += deltaPos;
            }
            else
            {
                TargetPosition.Val          += deltaPos;
                TargetPositionForSmoothing  = TargetPosition.Val;
            }
        }

        public void SetTargetRotation(float yaw, float pitch)
        {
            if (UseSmoothing)
            {
                RotationPitchForSmoothing   = pitch;
                RotationYawForSmoothing     = yaw;
            }
            else
            {
                RotationPitch.Val           = pitch;
                RotationYaw.Val             = yaw;
                RotationPitchForSmoothing   = pitch;
                RotationYawForSmoothing     = yaw;
            }
        }

        public void SetDistanceToTargetByDelta(float deltaDistanceToTarget)
        {
            float val = UseSmoothing ? DistanceToTargetForSmoothing : DistanceToTarget.Val;

            val += deltaDistanceToTarget;

            if (val < GlobalConfig.MinDistanceToTarget)
                val = GlobalConfig.MinDistanceToTarget;

            if (UseSmoothing)
            {
                DistanceToTargetForSmoothing    = val;
            }
            else
            {
                DistanceToTarget.Val            = val;
                DistanceToTargetForSmoothing    = val;
            }
        }

        internal void ResetSmoothing()
        {
            TargetPositionForSmoothing  = TargetPosition.Val;
            RotationPitchForSmoothing   = RotationPitch.Val;
            RotationYawForSmoothing     = RotationYaw.Val;
            DistanceToTargetForSmoothing= DistanceToTarget.Val;
        }

        public void UpdateSmoothing(float deltaTime, GlobalConfig config)
        {
            if (!UseSmoothing)
                return;

            // Position
            TargetPosition.Val  = GMath.Smooth(
                                            TargetPosition.Val,
                                            TargetPositionForSmoothing,
                                            config.CameraPosDamping, deltaTime);

            // Rotation
            RotationPitch.Val   = GMath.Smooth(
                                            RotationPitch.Val,
                                            RotationPitchForSmoothing,
                                            config.CameraRotDamping, deltaTime);

            RotationYaw.Val     = GMath.Smooth(
                                            RotationYaw.Val,
                                            RotationYawForSmoothing,
                                            config.CameraRotDamping, deltaTime);

            // Distance to target
            DistanceToTarget.Val    = GMath.Smooth(
                                            DistanceToTarget.Val,
                                            DistanceToTargetForSmoothing,
                                            config.CameraDistDamping, deltaTime);
        }

        internal void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "Camera");

            XmlUtils.AddNodeVector3 (xmlDoc, nodeThis, "TargetPosition"     , TargetPosition    .Val    );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "RotationPitch"      , RotationPitch     .Val    );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "RotationYaw"        , RotationYaw       .Val    );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "RotationRoll"       , RotationRoll      .Val    );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "DistanceToTarget"   , DistanceToTarget  .Val    );
            XmlUtils.AddNodeFloat   (xmlDoc, nodeThis, "Zoom"               , Zoom              .Val    );
        }

        public override void ResetPrevVal()
        {
            TargetPosition      .ResetPrevVal();
            RotationPitch       .ResetPrevVal();
            RotationYaw         .ResetPrevVal();
            RotationRoll        .ResetPrevVal();
            DistanceToTarget    .ResetPrevVal();
            Zoom                .ResetPrevVal();
        }

    }
}
