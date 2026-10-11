//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.BaseTypes;
using System.Globalization;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Material structs, material instances, initMaterials() and blendMaterials()
    public class MaterialsPart : CodeGenPart
    {
        public override string Name { get { return "Materials"; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return ctx.UsesMaterials;
        }

        public override void Generate(CodeGenContext ctx)
        {
            FunctionDefinition? materialDefinition = ctx.GetMaterialDefinition();
            if (materialDefinition == null)
                return;

            StringBuilder sb = ctx.Code;

            sb.AppendLine   ("struct Material"              );
            sb.AppendLine   ("{"                            );

            foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
            {
                if (p.Type == SdfParamType.Float)
                {
                    sb.AppendLine   (String.Format("    float {0};", p.ParameterName        ));
                }
                else if (p.Type == SdfParamType.Int)
                {
                    sb.AppendLine   (String.Format("    int   {0};", p.ParameterName        ));
                }
                else if (p.Type == SdfParamType.Vec2)
                {
                    sb.AppendLine   (String.Format("    vec2  {0};", p.ParameterName        ));
                }
                else if (p.Type == SdfParamType.Vec3)
                {
                    sb.AppendLine   (String.Format("    vec3  {0};", p.ParameterName        ));
                }
                else if (p.Type == SdfParamType.Vec4)
                {
                    sb.AppendLine   (String.Format("    vec4  {0};", p.ParameterName        ));
                }
                //else if (p.Type == SdfParamType.Bool)
                //{
                //    sb.AppendLine   (String.Format("    bool  {0};", p.ParameterName        ));
                //}
            }

            sb.AppendLine   ("};"                           );
            sb.AppendLine   (""                             );
            sb.AppendLine   ("struct MaterialDesc"          );
            sb.AppendLine   ("{"                            );
            sb.AppendLine   ("    float       distance;"    );
            sb.AppendLine   ("    float       materialId;"  );
            sb.AppendLine   ("    float       cellIndex;"   );
            sb.AppendLine   ("    Material    material;"    );
            sb.AppendLine   ("};"                           );
            sb.AppendLine   (""                             );

            // Material instances. Struct constructors are not used - they do not work in Unity (HLSL).
            foreach(MaterialInstance mat in ctx.Model.Materials.Children)
            {
                bool isFixed = ctx.IsMaterialFixed(mat);
                if (isFixed)
                    sb.AppendLine(String.Format(        "Material material_{0};", FormatInt(mat.Id)));
                else
                    sb.AppendLine(String.Format("uniform Material material_{0};", FormatInt(mat.Id)));
            }
            sb.AppendLine   ();

            sb.AppendLine   ("void initMaterials()");
            sb.AppendLine   ("{");
            foreach(MaterialInstance mat in ctx.Model.Materials.Children)
            {
                // Assignments to uniforms are not allowed
                bool isFixed = ctx.IsMaterialFixed(mat);
                if (!isFixed)
                    continue;

                string matId = FormatInt(mat.Id);

                foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
                {
                    if (p == null)
                        continue;
                    if (p.ParameterName == null)
                        continue;

                    ISimpleType paramVal = mat.MaterialProps.ParametersValues[p.ParameterKey];
                    string paramValAsString = paramVal.FormatAsStringForUniform();
                    sb.AppendLine(String.Format("    material_{0}.{1}     = {2};", matId, p.ParameterName, paramValAsString));
                }

                sb.AppendLine   ();
            }
            sb.Append       ("}");
            sb.AppendLine   ();

            sb.AppendLine   ();
            sb.AppendLine   ("Material blendMaterials(Material mt1, Material mt2, float blend)");
            sb.AppendLine   ("{"                                                               );
            sb.AppendLine   ("    Material mtOut;"                                             );
            sb.AppendLine   ();

            foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
            {
                if (p.Type == SdfParamType.Vec3)
                {
                    // TODO: temporarily every vec3 is treated as a color
                    sb.AppendLine   (String.Format("    mtOut.{0}    = MIX_COLORS  (mt1.{0}   , mt2.{0}   , blend);", p.ParameterName));
                }
                else if (p.Type == SdfParamType.Float || p.Type == SdfParamType.Int || p.Type == SdfParamType.Vec2 || p.Type == SdfParamType.Vec4)
                {
                    // TODO: add different kinds of value interpolation (e.g. smoothstep). Tested - works:
                    //sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , smoothstep(0.0, 1.0, blend) );", p.ParameterName));
                    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                }
            }

            sb.AppendLine   ();
            sb.AppendLine   ("    return mtOut;");
            sb.AppendLine   ("}");
        }

        private static string FormatInt(int val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }
    }
}
