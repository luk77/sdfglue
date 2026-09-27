//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Mathematics;
using SdfGlueCore.Model.DataNodes;
using SdfGlueEditor.Utils;
using SingleDocAppCore.Utils;

namespace SdfGlueEditor.Application
{
    // Orbit camera of the preview (target, distance, yaw, pitch -> origin and basis vectors)
    public class CameraController
    {
        private SdfGlueAppContext       ctx_;

        public CameraController(SdfGlueAppContext ctx)
        {
            ctx_ = ctx;
        }

        public void FocusObject(SdfObject? node)
        {
            if (node == null)
                return;

            ctx_.GetModel().CameraDat.SetTargetPos(node.GetPosition());
        }

        public void FocusSelectedObject()
        {
            FocusObject(ctx_.GetModel().SelectedNode as SdfObject);
        }

        public void RecalculateCamera()
        {
            CameraData cameraData = ctx_.GetModel().CameraDat;

            Vector3 fromTargetToOrig = -cameraData.DistanceToTarget.Val * Vector3.UnitZ;

            Matrix3 rotYaw   = Matrix3.CreateRotationY(-GMath.DegToRad * cameraData.RotationYaw.Val);
            Matrix3 rotPitch = Matrix3.CreateRotationX(-GMath.DegToRad * cameraData.RotationPitch.Val);

            fromTargetToOrig = rotYaw * rotPitch * fromTargetToOrig;

            cameraData.Origin = cameraData.TargetPosition.Val + MathUtils.ToNumericsVec3(fromTargetToOrig);

            cameraData.Forward   = MathUtils.ToNumericsVec3(Vector3.Normalize(-fromTargetToOrig));
            cameraData.Right     = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(System.Numerics.Vector3.UnitY, cameraData.Forward));
            cameraData.Up        = System.Numerics.Vector3.Normalize(System.Numerics.Vector3.Cross(cameraData.Forward, cameraData.Right));
        }
    }
}
