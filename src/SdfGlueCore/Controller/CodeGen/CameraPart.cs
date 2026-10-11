//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.Entities;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Camera parameters and camera operators (sdfg_setupCamera)
    public class CameraPart : CodeGenPart
    {
        public override string Name { get { return "Camera"; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return ctx.UsesCamera;
        }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;
            CameraData cameraData = ctx.Model.CameraDat;
            OperatorsCollection cameraOperators = ctx.Pass.CameraOperators;

            bool isFixed = ctx.IsCameraFixed();

            sb.AppendLine("#define CAMERA_DEG_TO_RAD            (PI / 180.0)");
            string valueMultiplier = " * CAMERA_DEG_TO_RAD";
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Vec3    , "cameraTargetPosition"       , cameraData.TargetPosition      );
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float   , "cameraRotationPitch"        , cameraData.RotationPitch       , valueMultiplier);
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float   , "cameraRotationYaw"          , cameraData.RotationYaw         , valueMultiplier);
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float   , "cameraRotationRoll"         , cameraData.RotationRoll        , valueMultiplier);
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float   , "cameraDistanceToTarget"     , cameraData.DistanceToTarget    );
            UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float   , "cameraZoom"                 , cameraData.Zoom                );

            if (DataModel.UseCameraControllers && cameraOperators != null)
            {
                Dictionary<string, string> functions = new Dictionary<string, string>();
                cameraOperators.AddCodeToCollection(functions);

                sb.AppendLine("// Camera operators (functions)");
                foreach(var keyVal in functions)
                {
                    sb.Append(keyVal.Value);
                    sb.AppendLine("");
                }

                sb.AppendLine("// Camera operators (parameters)");
                UniformCodeWriter.AppendUniformCodeForOperatorsCollection(ctx, null, isFixed, cameraOperators.Operators);

                sb.AppendLine("// Camera setup function");
                sb.AppendLine("void sdfg_setupCamera(inout vec3 ro, inout vec3 rd, inout vec2 fragCoord)");
                sb.AppendLine("{");
                ctx.Target.AppendCameraSetup(ctx, delegate()
                {
                    UniformCodeWriter.AppendFunctionsCallCodeForOperatorsCollection(sb, null, "ro, rd, fragCoord", cameraOperators.Operators);
                });
                sb.AppendLine("}");
            }
        }
    }
}
