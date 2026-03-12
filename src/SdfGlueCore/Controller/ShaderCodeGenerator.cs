//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.Entities;
using System.Globalization;
using System.Text;

namespace SdfGlueCore.Controller
{
    public class CodeGenResult
    {
        public  string?                 Includes             = null;
        public  string?                 Definitions          = null;
        public  string?                 Materials            = null;
        public  string?                 DistanceFunctions    = null;
        public  string?                 MixOpFunctions       = null;
        public  string?                 PosOpFunctions       = null;
        public  string?                 DistOpFunctions      = null;
        public  string?                 MapUniforms          = null;
        public  string?                 MapFunction          = null;
        public  string?                 MaterialsFunction    = null;
        public  string?                 FullShader           = null;

        public void Clean()
        {
            Includes             = null;
            Definitions          = null;
            Materials            = null;
            DistanceFunctions    = null;
            MixOpFunctions       = null;
            PosOpFunctions       = null;
            DistOpFunctions      = null;
            MapUniforms          = null;
            MapFunction          = null;
            MaterialsFunction    = null;
            FullShader           = null;
        }
    }

    public class ShaderCodeGenerator
    {
        private enum FunctionsCollectionType
        {
            Sdf,
            MixOp,
            PosOp,
            DistOp,
        }



        private DataModel                   model_ = new DataModel();

        public ShaderCodeGenerator()
        {
            CreateNewModel();
        }

        public void CreateNewModel()
        {
            model_ = new DataModel();
        }

        public DataModel GetModel()
        {
            return model_;
        }

        private string GetMainVectorType()
        {
            return model_.ProjSettings.Use4d.Val ? "vec4" : "vec3";
        }

        public string ModifyFragShaderSource(StringBuilder sbErrors, string shaderSource, CodeGenResult result, bool forUnity, RenderPassData renderPassData)
        {
            result.Clean();

            CameraData cameraData = GetModel().CameraDat;

            bool isUsingBackdrop  = renderPassData.IsPrimaryPass;
            bool isUsingCamera    = renderPassData.IsPrimaryPass;
            bool isUsingMaterials = renderPassData.IsPrimaryPass && IsCurrentShaderUsingMaterials();
            bool isUsingSdfTree   = renderPassData.IsPrimaryPass;

            // common functions (includes)
            shaderSource = InsertCommonFunctions        (sbErrors, shaderSource, "//__common_functions__"             , ref result.Includes       , renderPassData);

            // backdrop
            if (isUsingBackdrop)
            {
                shaderSource = GenerateBackdropCode         (sbErrors, shaderSource, "//__generated_backdrop_function__"  , renderPassData);
            }

            // camera
            if (isUsingCamera)
            {
                shaderSource = GenerateCameraUniforms       (sbErrors, shaderSource, "//__generated_camera_data__"        , ref result.Definitions    , cameraData, renderPassData.CameraOperators);
            }

            // materials
            if (isUsingMaterials)
            {
                shaderSource = GenerateMaterials        (sbErrors, shaderSource, "//__generated_materials__"          , ref result.Materials      , false, forUnity);
                shaderSource = GenerateMapFuction       (sbErrors, shaderSource, "//__generated_materials_function__" , true  , out result.MaterialsFunction);
                shaderSource = GenerateCallInitMaterials(sbErrors, shaderSource, "//__generated_call_init_materials__" , renderPassData);
            }
            else
            {
                shaderSource = shaderSource.Replace("//__generated_materials__", "");
                shaderSource = shaderSource.Replace("//__generated_materials_function__", "");
                shaderSource = shaderSource.Replace("//__generated_call_init_materials__", "");
            }

            // sdf tree
            if (isUsingSdfTree)
            {
                shaderSource = GenerateDefinitions          (sbErrors, shaderSource, "//__generated_definitions__"        , ref result.Definitions    , renderPassData);
                shaderSource = GenerateFunctions            (sbErrors, shaderSource, "//__generated_distance_functions__" , FunctionsCollectionType.Sdf       , ref result.DistanceFunctions   );
                shaderSource = GenerateFunctions            (sbErrors, shaderSource, "//__generated_mixop_functions__"    , FunctionsCollectionType.MixOp     , ref result.MixOpFunctions      , isUsingMaterials ? "MaterialsBlending" : null);
                shaderSource = GenerateFunctions            (sbErrors, shaderSource, "//__generated_posop_functions__"    , FunctionsCollectionType.PosOp     , ref result.PosOpFunctions      );
                shaderSource = GenerateFunctions            (sbErrors, shaderSource, "//__generated_distop_functions__"   , FunctionsCollectionType.DistOp    , ref result.DistOpFunctions     );
                shaderSource = GenerateMapUniforms          (sbErrors, shaderSource, "//__generated_map_uniforms__"       , ref result.MapUniforms);
                shaderSource = GenerateMapFuction           (sbErrors, shaderSource, "//__generated_map_function__"       , false , out result.MapFunction);
            }

            // renderer
            shaderSource = GenerateRendererUniforms     (sbErrors, shaderSource, "//__generated_renderer_uniforms__"  , renderPassData);
            shaderSource = GenerateRendererCode         (sbErrors, shaderSource, "//__generated_renderer_code__"      , renderPassData);

            result.FullShader = shaderSource;

            return shaderSource;
        }

        private int FindTagIndex(StringBuilder sbErrors, string shaderSource, string tag)
        {
            int index = shaderSource.IndexOf(tag);
            if (index == -1)
            {
                // TODO: ten error można przywrócić gdy będzie już sensowne przekazywanie do shadera
                // które rzeczy ma zawierać a które nie.
                // Obecnie ten error jest raportowany dla secondary pass'ów,
                // które nie mają wielu z funkcjonalności primary pass'ów.
                // Dodatkowym problemem jest generowanie kodu Unity,
                // które jest wpięte na sztywno i używa swojego template'a (bez: __generated_call_init_materials__).
                //sbErrors.AppendLine(String.Format("Tag: '{0}' not found. Code modification not possible.", tag));
                return -1;
            }

            return index;
        }

        private string InsertCommonFunctions(StringBuilder sbErrors, string shaderSource, string tag, ref string? generatedCode, RenderPassData renderPassData)
        {
            int commonFunctIndex = FindTagIndex(sbErrors, shaderSource, tag);
            if (commonFunctIndex == -1)
                return shaderSource;

            generatedCode = renderPassData.CollectIncludes(model_);

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private string GenerateCallInitMaterials(StringBuilder sbErrors, string shaderSource, string tag, RenderPassData renderPassData)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            string generatedCode = "initMaterials();";

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private string GenerateRendererUniforms(StringBuilder sbErrors, string shaderSource, string tag, RenderPassData renderPassData)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

            bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || renderPassData.IsFixed.Val;
            AppendUniformCodeForFunctionEntity(sb, null, isFixed, renderPassData.RendererFunc);

            string generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private string GenerateFunction(StringBuilder sbErrors, string shaderSource, string tag, FunctionEntity? functEntity)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            string? code = functEntity?.Definition?.FunctionCode;

            shaderSource = shaderSource.Replace(tag, code);

            return shaderSource;
        }

        private string GenerateRendererCode(StringBuilder sbErrors, string shaderSource, string tag, RenderPassData renderPassData)
        {
            return GenerateFunction(sbErrors, shaderSource, tag, renderPassData?.RendererFunc);
        }

        //private string GenerateBackdropFunction(StringBuilder sbErrors, string shaderSource, string tag, RenderPassData renderPassData)
        //{
        //    return GenerateFunction(sbErrors, shaderSource, tag, renderPassData?.BackdropFunc);
        //}


        private string GenerateBackdropCode(StringBuilder sbErrors, string shaderSource, string tag, RenderPassData renderPassData)//, ref string? generatedCode)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

            bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || renderPassData.IsFixed.Val;

            BackdropEntity? backdropEntity = renderPassData?.BackdropFunc;

            string? backdropFunctionCode = backdropEntity?.Definition?.FunctionCode;

            sb.AppendLine();
            sb.AppendLine("//---------------------------------------------------------------------------");
            sb.AppendLine("// Backdrop");
            sb.AppendLine("//---------------------------------------------------------------------------");

            sb.AppendLine("// Backdrop function");
            sb.Append(backdropFunctionCode);

            sb.AppendLine();
            sb.AppendLine("// Backdrop uniforms");
            AppendUniformCodeForFunctionEntity(sb, null, isFixed, backdropEntity);

            sb.AppendLine();
            sb.AppendLine("// Backdrop wrapper function");
            sb.AppendLine("vec3 sdfg_getBackdropColor(in vec3 dir)");
            sb.AppendLine("{");
            sb.AppendFormat("    return {0}(dir", backdropEntity?.DefinitionName);
            AppendFunctionAdditionalParameters(sb, null, backdropEntity);
            sb.AppendFormat(");");
            sb.AppendLine();

            sb.AppendLine("}");

            sb.AppendLine();
            sb.AppendLine("//---------------------------------------------------------------------------");
            sb.AppendLine("// Backdrop (end)");
            sb.AppendLine("//---------------------------------------------------------------------------");
            sb.AppendLine();

            string generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }


        private string GenerateCameraUniforms(StringBuilder sbErrors, string shaderSource, string tag, ref string? generatedCode, CameraData cameraData, OperatorsCollection cameraOperators)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

            bool isFixed = GetModel().ProjSettings.FixAllObjects.Val;// || cameraData.IsFixed.Val;

            sb.AppendLine("#define CAMERA_DEG_TO_RAD            (PI / 180.0)");
            string valueMultiplier = " * CAMERA_DEG_TO_RAD";
            AppendUniformCode(sb, isFixed, SdfParamType.Vec3    , "cameraTargetPosition"       , cameraData.TargetPosition      );
            AppendUniformCode(sb, isFixed, SdfParamType.Float   , "cameraRotationPitch"        , cameraData.RotationPitch       , valueMultiplier);
            AppendUniformCode(sb, isFixed, SdfParamType.Float   , "cameraRotationYaw"          , cameraData.RotationYaw         , valueMultiplier);
            AppendUniformCode(sb, isFixed, SdfParamType.Float   , "cameraRotationRoll"         , cameraData.RotationRoll        , valueMultiplier);
            AppendUniformCode(sb, isFixed, SdfParamType.Float   , "cameraDistanceToTarget"     , cameraData.DistanceToTarget    );
            AppendUniformCode(sb, isFixed, SdfParamType.Float   , "cameraZoom"                 , cameraData.Zoom                );

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
                AppendUniformCodeForOperatorsCollection(sb, null, isFixed, cameraOperators.Operators);

                sb.AppendLine("// Camera setup function");
                sb.AppendLine("void sdfg_setupCamera(inout vec3 ro, inout vec3 rd, inout vec2 fragCoord)");
                sb.AppendLine("{");
                AppendFunctionsCallCodeForOperatorsCollection(sb, null, "ro, rd, fragCoord", cameraOperators.Operators);
                sb.AppendLine("}");
            }

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private string GenerateDefinitions(StringBuilder sbErrors, string shaderSource, string tag, ref string? generatedCode, RenderPassData renderPassData)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

            sb.AppendLine(String.Format("#define MAX_STEPS                ({0})", model_.MarchingMaxSteps.ToString(CultureInfo.InvariantCulture)));
            sb.AppendLine(String.Format("#define MIN_DIST                 ({0})", model_.MarchingMinDist.ToString("F6", CultureInfo.InvariantCulture)));
            sb.AppendLine(String.Format("#define MAX_DIST                 ({0})", model_.MarchingMaxDist.ToString("F6", CultureInfo.InvariantCulture)));

            sb.AppendLine(String.Format("#define SDFG_VEC                 {0}", GetMainVectorType() ));

            // Compilation parameters
            FunctionDefParametersCollection? compParameters = renderPassData?.RendererFunc?.Definition?.CompilationParameters;
            if (compParameters != null)
            {
                foreach(FunctionDefParameter p in compParameters)
                {
                    if (p == null)
                        continue;
                    if (p.ParameterName == null)
                        continue;
                    //object paramVal = renderPassData.RendererFunc.CompilationParametersValues[p.ParameterName].GetValueAsObject();
                    //int paramValAsInt = (int)paramVal;
                    ISimpleType? paramVal = renderPassData?.RendererFunc?.CompilationParametersValues[p.ParameterName];
                    sb.AppendLine(String.Format("#define {0}                  ({1})", p.ParameterName, paramVal?.FormatAsStringForUniform()));
                }
            }

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private bool IsCurrentShaderUsingMaterials()
        {
            if (GetModel().Materials?.GetChildrenCount() == 0)
                return false;

            // This is required only to access current material definition.
            // It could also be done by GetModel().GetFirstPrimaryRPass() ...
            MaterialInstance? firstMaterial = GetModel().Materials?.GetChildrenAt(0) as MaterialInstance;
            if (firstMaterial == null)
                return false;

            FunctionDefinition? materialDefinition = firstMaterial?.MaterialProps?.Definition;

            // If materialDefinition is empty it means render pass do not use materials system at all.
            if (materialDefinition == null)
                return false;
            if (materialDefinition.MaterialParameters.Count == 0)
                return false;

            return true;
        }

        private string GenerateMaterials(StringBuilder sbErrors, string shaderSource, string tag, ref string? generatedCode, bool useStructConstructors, bool forUnity)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            if (!IsCurrentShaderUsingMaterials())
                return shaderSource;

            if (GetModel().Materials?.GetChildrenCount() == 0)
                return shaderSource;

            // This is required only to access current material definition.
            // It could also be done by GetModel().GetFirstPrimaryRPass() ...
            MaterialInstance? firstMaterial = GetModel().Materials?.GetChildrenAt(0) as MaterialInstance;
            if (firstMaterial == null)
                return shaderSource;

            FunctionDefinition? materialDefinition = firstMaterial.MaterialProps.Definition;
            if (materialDefinition == null)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

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

            // Doklejamy polę "zaślepkę" aby uniknąć błędów kompilacji shadera (gdy struktura nie ma żadnych pól)
            if (materialDefinition.MaterialParameters.Count == 0)
            {
                sb.AppendLine   (String.Format("    float dummy_param;"));
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


            // Stare podejście - z użyciem konstruktorów struktur.
            // Niestety nie działa w Unity
            if (useStructConstructors)
            {
                sb.Append       ("//");
                foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
                {
                    sb.Append(" " + p.ParameterName);
                }
                sb.AppendLine   ();

                foreach(MaterialInstance mat in GetModel().Materials.Children)
                {
                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || mat.IsFixed.Val;
                    if (isFixed)
                        sb.AppendFormat (        "Material material_{0}      = Material( ", FormatInt(mat.Id));
                    else
                        sb.AppendFormat ("uniform Material material_{0}      = Material( ", FormatInt(mat.Id));

                    for (int i=0; i<materialDefinition?.MaterialParameters.Count; i++)
                    {
                        FunctionDefParameter p = materialDefinition.MaterialParameters[i];
                        if (p == null)
                            continue;
                        if (p.ParameterName == null)
                            continue;
                        bool isLast = i == materialDefinition.MaterialParameters.Count-1;
                        ISimpleType paramVal = mat.MaterialProps.ParametersValues[p.ParameterName];
                        sb.Append(paramVal.FormatAsStringForUniform());
                        if (!isLast)
                            sb.Append(", ");
                    }

                    sb.Append       (");"                                   );
                    sb.AppendLine   ();
                }
            }
            else
            {
                foreach(MaterialInstance mat in GetModel().Materials.Children)
                {
                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || mat.IsFixed.Val;
                    if (isFixed)
                        sb.AppendLine(String.Format(        "Material material_{0};", FormatInt(mat.Id)));
                    else
                        sb.AppendLine(String.Format("uniform Material material_{0};", FormatInt(mat.Id)));
                }
                sb.AppendLine   ();

                sb.AppendLine   ("void initMaterials()");
                sb.AppendLine   ("{");
                foreach(MaterialInstance mat in GetModel().Materials.Children)
                {
                    // Nie można robić przypisań do uniformów,
                    // chyba że eksportujemy do Unity 
                    // (wtedy chcemy mieć wszystkie materiały)
                    bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || mat.IsFixed.Val;
                    if (!forUnity && !isFixed)
                        continue;

                    string matId = FormatInt(mat.Id);

                    foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
                    {
                        if (p == null)
                            continue;
                        if (p.ParameterName == null)
                            continue;

                        ISimpleType paramVal = mat.MaterialProps.ParametersValues[p.ParameterName];
                        string paramValAsString = paramVal.FormatAsStringForUniform();
                        sb.AppendLine(String.Format("    material_{0}.{1}     = {2};", matId, p.ParameterName, paramValAsString));
                    }

                    sb.AppendLine   ();
                }
                sb.Append       ("}");
                sb.AppendLine   ();
            }

            sb.AppendLine   ();
            sb.AppendLine   ("Material blendMaterials(Material mt1, Material mt2, float blend)");
            sb.AppendLine   ("{"                                                               );
            sb.AppendLine   ("    Material mtOut;"                                             );
            sb.AppendLine   ();

            foreach(FunctionDefParameter p in materialDefinition.MaterialParameters)
            {
                if (p.Type == SdfParamType.Float)
                {
                    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                    // TODO: dodać różne rodzaje interpolacji wartości (np. smoothstep). Sprawdzone - działa:
                    //sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , smoothstep(0.0, 1.0, blend) );", p.ParameterName));
                }
                else if (p.Type == SdfParamType.Int)
                {
                    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                }
                else if (p.Type == SdfParamType.Vec2)
                {
                    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                }
                else if (p.Type == SdfParamType.Vec3)
                {
                    // TODO: tymczasowo każdy vec3 traktujemy jako kolor
                    //sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                    sb.AppendLine   (String.Format("    mtOut.{0}    = MIX_COLORS  (mt1.{0}   , mt2.{0}   , blend);", p.ParameterName));
                }
                else if (p.Type == SdfParamType.Vec4)
                {
                    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                }
                //else if (p.Type == SdfParamType.Bool)
                //{
                //    sb.AppendLine   (String.Format("    mtOut.{0}     = mix         (mt1.{0}    , mt2.{0}    , blend);", p.ParameterName));
                //}
            }

            sb.AppendLine   ();
            sb.AppendLine   ("    return mtOut;");
            sb.AppendLine   ("}");

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private string GenerateFunctions(StringBuilder sbErrors, string shaderSource, string tag, FunctionsCollectionType collectionType, ref string? generatedCode, string? featureName = null)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            Dictionary<string, string> functions = new Dictionary<string, string>();

            TreeNode.CallRecursive(model_.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (collectionType == FunctionsCollectionType.Sdf)
                    sdfObj.FunctionSdf.AddCodeToCollection(functions);
                else if (collectionType == FunctionsCollectionType.MixOp)
                {
                    sdfObj.FunctionMixOp.AddCodeToCollection(functions, featureName);
                }
                else if (collectionType == FunctionsCollectionType.PosOp)
                    sdfObj.PositionOperators.AddCodeToCollection(functions);
                else if (collectionType == FunctionsCollectionType.DistOp)
                    sdfObj.DistanceOperators.AddCodeToCollection(functions);
            });

            StringBuilder sb = new StringBuilder(5000);

            foreach(var keyVal in functions)
            {
                sb.Append(keyVal.Value);
                sb.AppendLine("");
            }

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private static void AppendUniformCode(StringBuilder sb, bool isFixed, SdfParamType type, string paramName, ISimpleType paramVal, string? valuePrefix = null)
        {
            string paramType       = FunctionDefParameter.SdfTypeToString(type);

            string paramCurrValAsString = paramVal.FormatAsStringForUniform();

            if (!String.IsNullOrEmpty(valuePrefix))
                paramCurrValAsString += valuePrefix;

            bool useFixedDefines = true;

            if (isFixed)
            {
                if (useFixedDefines)
                    sb.Append("#define ");
                else
                    sb.Append("        ");
            }
            else
                sb.Append("uniform ");

            if (isFixed && useFixedDefines)
            {
                for(int i=0; i<16; i++)
                    sb.Append(" ");
            }
            else
            {
                sb.Append(paramType);
                for(int i=0; i<1 + Math.Max(15 - paramType.Length, 0); i++)
                    sb.Append(" ");
            }

            sb.Append(paramName);
            for(int i=0; i<1 + Math.Max(31 - paramName.Length, 0); i++)
                sb.Append(" ");

            if (isFixed)
            {
                if (useFixedDefines)
                    sb.Append(" (");
                else
                    sb.Append("= ");
            }
            else
                sb.Append("= ");

            sb.Append(paramCurrValAsString);

            if (isFixed)
            {
                if (useFixedDefines)
                    sb.Append(")");
                else
                    sb.Append(";");
            }
            else
            {
                sb.Append(";");
            }

            sb.AppendLine();
        }

        private string GenerateMapUniforms(StringBuilder sbErrors, string shaderSource, string tag, ref string? generatedCode)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
                return shaderSource;

            StringBuilder sb = new StringBuilder(5000);

            // Parameters (uniforms)
            TreeNode.CallRecursive(model_.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                // Większość uniform'ów jest współdzielona między funkcjami distance i material.
                // Dla tego ich generowanie można pominąć tylko wtedy, gdy obie flagi są wyłączone.
                if (!sdfObj.CanBeUsedInDistanceFunction() && !sdfObj.CanBeUsedInMaterialsFunction())
                    return;

                string? nodeId = GetModel().Config.UseNamesAsIds ? node.Name.Val : node.Id.ToString();

                int parentId = (sdfObj.ParentAsSdf != null) ? sdfObj.ParentAsSdf.Id : 0;

                string varNameBase = "g_obj_" + nodeId +"_";

                sb.AppendLine(GetNodeComment(node));

                bool isFixed = GetModel().ProjSettings.FixAllObjects.Val || sdfObj.IsFixed.Val;
                AppendUniformCode(sb, isFixed, SdfParamType.Float, varNameBase+"blendF"      , sdfObj.BlendFactor);
                AppendUniformCode(sb, isFixed, SdfParamType.Float, varNameBase+"matId"       , sdfObj.MaterialId);
                AppendUniformCode(sb, isFixed, SdfParamType.Float, varNameBase+"matBlendF"   , sdfObj.MaterialBlendFactor);

                AppendUniformCodeForFunctionEntity(sb, nodeId, isFixed, sdfObj.FunctionMixOp);

                if (sdfObj.UseShape.Val)
                {
                    AppendUniformCodeForFunctionEntity(sb, nodeId, isFixed, sdfObj.FunctionSdf);
                }

                AppendUniformCodeForOperatorsCollection(sb, nodeId, isFixed, sdfObj.PositionOperators.Operators);
                AppendUniformCodeForOperatorsCollection(sb, nodeId, isFixed, sdfObj.DistanceOperators.Operators);

                sb.AppendLine("");
            });

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;

        }

        private void AppendUniformCodeForOperatorsCollection(StringBuilder sb, string? nodeId, bool isFixed, List<OperatorEntity> operators)
        {
            for(int i=0; i<operators.Count; i++)
            {
                OperatorEntity opent = operators[i];
                if (!opent.Enabled.Val)
                    continue;
                AppendUniformCodeForFunctionEntity(sb, nodeId, isFixed, opent, i);
            }
        }

        private string GenerateMapFuction(StringBuilder sbErrors, string shaderSource, string tag, bool isMaterialsBlendingFunction, out string generatedCode)
        {
            int index = FindTagIndex(sbErrors, shaderSource, tag);
            if (index == -1)
            {
                generatedCode = "";
                return shaderSource;
            }

            string mainVectorType = GetMainVectorType();

            StringBuilder sb = new StringBuilder(5000);

            // distance function
            sb.AppendLine("");
            if (isMaterialsBlendingFunction)
            {
                if (DataModel.MaterialsBlendingEnabled)
                    sb.AppendFormat("MaterialDesc gen_getMaterial({0} p)", mainVectorType);
                else
                    sb.AppendFormat("vec2 gen_getMaterial({0} p)", mainVectorType);
            }
            else
            {
                sb.AppendFormat("float gen_getDist({0} p)", mainVectorType);
            }
            sb.AppendLine();
            sb.AppendLine("{");

            TreeNode.CallRecursive(model_.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (isMaterialsBlendingFunction)
                {
                    if (!sdfObj.CanBeUsedInMaterialsFunction())
                        return;
                }
                else
                {
                    if (!sdfObj.CanBeUsedInDistanceFunction())
                        return;
                }

                string? nodeId   = GetModel().Config.UseNamesAsIds ? node.Name.Val : node.Id.ToString();
                string? parentId = (sdfObj.ParentAsSdf != null) ? (GetModel().Config.UseNamesAsIds ? sdfObj.ParentAsSdf.Name.Val : sdfObj.ParentAsSdf.Id.ToString()) : "";

                string parentPosStr = "p";
                string parentCellIndexStr = "0.0";
                if (sdfObj.ParentAsSdf != null)
                {
                    parentPosStr        = String.Format("calcPos_{0}", parentId);
                    parentCellIndexStr  = String.Format("cellIndex_{0}", parentId);
                }

                sb.AppendLine("    " + GetNodeComment(node));
                sb.AppendLine(String.Format("    {2}       calcPos_{0}     = {1};"  , nodeId, parentPosStr, mainVectorType));
                sb.AppendLine(String.Format("    float      cellIndex_{0}   = {1};"  , nodeId, parentCellIndexStr));

                // position operators
                AppendFunctionsCallCodeForOperatorsCollection(sb, nodeId, "calcPos_", sdfObj.PositionOperators.Operators);

                // sdf function
                if (sdfObj.UseShape.Val)
                {
                    // visible
                    sb.Append(String.Format("    float      dist_{0} = {1}(calcPos_{0}", nodeId, sdfObj.FunctionSdf?.Definition?.FunctionName));
                    AppendFunctionAdditionalParameters(sb, nodeId, sdfObj.FunctionSdf);
                    sb.Append(");");
                    sb.AppendLine("");
                }
                else
                {
                    // not visible
                    sb.AppendLine(String.Format("    float      dist_{0} = MAX_DIST;", nodeId));
                }

                // distance operators
                AppendFunctionsCallCodeForOperatorsCollection(sb, nodeId, "dist_", sdfObj.DistanceOperators.Operators);

                // opakowanie w vec2, materiał
                if (isMaterialsBlendingFunction)
                {
                    if (DataModel.MaterialsBlendingEnabled)
                    {
                        string materialObjectName = String.Format("material_{0}", (((int)sdfObj.MaterialId.Val)).ToString(CultureInfo.InvariantCulture));

                        // Stare podejście - konstruktor struktury:
                        //sb.AppendLine(String.Format("    MaterialDesc   obj_{0}         = MaterialDesc(dist_{0}, g_obj_{0}_matId, {1});", nodeId, materialObjectName));
                        // Bez konstruktora struktury:
                        sb.AppendLine(String.Format("    MaterialDesc   obj_{0};"                   , nodeId));
                        sb.AppendLine(String.Format("    obj_{0}.distance    = dist_{0};"           , nodeId));
                        sb.AppendLine(String.Format("    obj_{0}.materialId  = g_obj_{0}_matId;"    , nodeId));
                        sb.AppendLine(String.Format("    obj_{0}.cellIndex   = cellIndex_{0};"      , nodeId));
                        sb.AppendLine(String.Format("    obj_{0}.material    = {1};"                , nodeId, materialObjectName));
                    }
                    else
                        sb.AppendLine(String.Format("    vec2           obj_{0}         = vec2(dist_{0}, g_obj_{0}_matId);", nodeId));
                }
                else
                    sb.AppendLine(String.Format("    float      obj_{0}         = dist_{0};", nodeId));
                sb.AppendLine("");
            });

            sb.AppendLine("    // Mixing objects");
            sb.AppendLine("");

            // Operator łączenia jest zawsze stosowany w stosunku do parenta.
            // W związku z tym składanie obiektów musi się odbywać od najniższego poziomu w górę.
            TreeNode.CallRecursiveChildrenFirst(model_.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (isMaterialsBlendingFunction)
                {
                    if (!sdfObj.CanBeUsedInMaterialsFunction())
                    {
                        if (sdfObj.ParentAsSdf == null)
                        {
                            sb.AppendLine(String.Format("    MaterialDesc emptyMaterial;"));
                            sb.AppendLine(String.Format("    return emptyMaterial;"));
                        }
                        return;
                    }
                }
                else
                {
                    if (!sdfObj.CanBeUsedInDistanceFunction())
                    {
                        if (sdfObj.ParentAsSdf == null)
                            sb.AppendLine(String.Format("    return MAX_DIST;"));
                        return;
                    }
                }

                string? nodeId   = GetModel().Config.UseNamesAsIds ? node.Name.Val : node.Id.ToString();
                string? parentId = (sdfObj.ParentAsSdf != null) ? (GetModel().Config.UseNamesAsIds ? sdfObj.ParentAsSdf.Name.Val : sdfObj.ParentAsSdf.Id.ToString()) : "";
                string? mixOpFuncName = sdfObj.GetMixOperatorFunctionName(isMaterialsBlendingFunction);

                // mix operator
                sb.AppendLine("    " + GetNodeComment(node));
                if (sdfObj.ParentAsSdf == null)
                {
                    sb.AppendLine(String.Format("    return obj_{0};", nodeId));
                }
                else
                {
                    if (isMaterialsBlendingFunction)
                        sb.Append(String.Format("    obj_{2} = {0}(obj_{1}, obj_{2}, g_obj_{1}_matBlendF", mixOpFuncName, nodeId, parentId));
                    else
                        sb.Append(String.Format("    obj_{2} = {0}(obj_{1}, obj_{2}, g_obj_{1}_blendF", mixOpFuncName, nodeId, parentId));

                    AppendFunctionAdditionalParameters(sb, nodeId, sdfObj.FunctionMixOp);
                    sb.Append(");");
                    sb.AppendLine("");
                }

                sb.AppendLine("");
            });

            sb.AppendLine("}");

            generatedCode = sb.ToString();

            shaderSource = shaderSource.Replace(tag, generatedCode);

            return shaderSource;
        }

        private void AppendFunctionsCallCodeForOperatorsCollection(StringBuilder sb, string? nodeId, string paramName, List<OperatorEntity>? operators)
        {
            if (operators == null)
                return;

            for(int i=0; i<operators.Count; i++)
            {
                OperatorEntity? opent = operators[i];
                if (opent == null)
                    continue;
                if (!opent.Enabled.Val)
                    continue;
                if (opent.Definition == null)
                    continue;

                if (opent.Definition.ExportsCellIndex)
                {
                    sb.Append(String.Format("    cellIndex_{1} = {2}({0}{1}", paramName, nodeId, opent.Definition?.FunctionName));
                }
                else
                {
                    sb.Append(String.Format("    {2}({0}{1}", paramName, nodeId, opent.Definition?.FunctionName));
                }
                AppendFunctionAdditionalParameters(sb, nodeId, opent, i);
                sb.Append(");");
                sb.AppendLine("");
            }
        }

        private static void AppendUniformCodeForFunctionEntity(StringBuilder sb, string? nodeId, bool isFixed, FunctionEntity? functionEntity, int entityIndex = -1)
        {
            if (functionEntity == null || functionEntity.Definition == null)
                return;

            bool simpleParamsFormat = String.IsNullOrEmpty(nodeId);

            foreach(FunctionDefParameter p in functionEntity.Definition.Parameters)
            {
                if (p == null || p.ParameterName == null)
                    continue;

                if (!p.IsEditable())
                    continue;

                string? fullNameId = null;
                if (entityIndex == -1)
                {
                    if (simpleParamsFormat)
                        fullNameId = String.Format("{0}_{1}"      , functionEntity.ParamPrefix, p.ParameterName);
                    else
                        fullNameId = String.Format("g_obj_{0}_{1}_{2}"      , nodeId, functionEntity.ParamPrefix, p.ParameterName);
                }
                else
                {
                    if (simpleParamsFormat)
                        fullNameId = String.Format("{0}{1}_{2}"      , functionEntity.ParamPrefix, entityIndex + 1, p.ParameterName);
                    else
                        fullNameId = String.Format("g_obj_{0}_{1}{2}_{3}"      , nodeId, functionEntity.ParamPrefix, entityIndex + 1, p.ParameterName);
                }

                ISimpleType paramVal = functionEntity.ParametersValues[p.ParameterName];
                AppendUniformCode(sb, isFixed, p.Type, fullNameId, paramVal);
            }
        }

        private static void AppendFunctionAdditionalParameters(StringBuilder sb, string? nodeId, FunctionEntity? functionEntity, int entityIndex = -1)
        {
            if (functionEntity == null || functionEntity.Definition == null)
                return;

            bool simpleParamsFormat = String.IsNullOrEmpty(nodeId);

            foreach(FunctionDefParameter p in functionEntity.Definition.Parameters)
            {
                string? fullNameId = null;

                // this is for 'extrusion' operator
                if (p.ParameterName == FunctionDefParameter.SpecialParamCurrObjPos)
                {
                    fullNameId = String.Format("calcPos_{0}", nodeId);
                    sb.Append(", " + fullNameId);
                    continue;
                }
                else if (p.ParameterName == FunctionDefParameter.SpecialParamCurrObjCellIndex)
                {
                    fullNameId = String.Format("cellIndex_{0}", nodeId);
                    sb.Append(", " + fullNameId);
                    continue;
                }

                if (entityIndex == -1)
                {
                    if (simpleParamsFormat)
                    {
                        fullNameId = String.Format("{0}_{1}"      , functionEntity.ParamPrefix, p.ParameterName);
                    }
                    else
                    {
                        fullNameId = String.Format("g_obj_{0}_{1}_{2}"      , nodeId, functionEntity.ParamPrefix, p.ParameterName);
                    }
                }
                else
                {
                    if (simpleParamsFormat)
                    {
                        fullNameId = String.Format("{0}{1}_{2}"   , functionEntity.ParamPrefix, entityIndex + 1, p.ParameterName);
                    }
                    else
                    {
                        fullNameId = String.Format("g_obj_{0}_{1}{2}_{3}"   , nodeId, functionEntity.ParamPrefix, entityIndex + 1, p.ParameterName);
                    }
                }

                sb.Append(", " + fullNameId);
            }
        }

        private static string GetNodeComment(TreeNode node)
        {
            return String.Format("//[Id:{0}] {1}", node.Id, node.Name.Val);
        }

        private static string FormatInt(int val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public string? GenerateUnityCode(StringBuilder sbErrors, RenderPassData renderPassData)
        {
            string unitySrc = File.ReadAllText("Shaders/UnityTemplate.shader");

            if (String.IsNullOrEmpty(unitySrc))
                return null;

            CodeGenResult result = new CodeGenResult();
            unitySrc = ModifyFragShaderSource(sbErrors, unitySrc, result, true, renderPassData);

            unitySrc = unitySrc.Replace("vec2", "float2");
            unitySrc = unitySrc.Replace("vec3", "float3");
            unitySrc = unitySrc.Replace("vec4", "float4");
            unitySrc = unitySrc.Replace("mat2", "float2x2");
            unitySrc = unitySrc.Replace("mat3", "float3x3");
            unitySrc = unitySrc.Replace("mat4", "float4x4");
            unitySrc = unitySrc.Replace("float2(0.0)", "float2(0.0, 0.0)");
            unitySrc = unitySrc.Replace("float2(1.0)", "float2(1.0, 1.0)");
            unitySrc = unitySrc.Replace("float3(0.0)", "float3(0.0, 0.0, 0.0)");
            unitySrc = unitySrc.Replace("float3(1.0)", "float3(1.0, 1.0, 1.0)");
            unitySrc = unitySrc.Replace("float4(0.0)", "float4(0.0, 0.0, 0.0, 0.0)");
            unitySrc = unitySrc.Replace("float4(1.0)", "float4(1.0, 1.0, 1.0, 1.0)");
            //unitySrc = unitySrc.Replace("mix(", "lerp(");
            unitySrc = unitySrc.Replace("mix", "lerp");     // bardziej ryzykownie

            return unitySrc;
        }
    }
}
