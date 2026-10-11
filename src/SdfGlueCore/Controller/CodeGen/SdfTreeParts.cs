//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.DataNodes;
using System.Globalization;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    public enum FunctionsCollectionType
    {
        Sdf,
        MixOp,
        PosOp,
        DistOp,
    }

    // Code of the functions (of one kind) used by the objects of the SDF tree. Every function is generated once.
    public class FunctionsPart : CodeGenPart
    {
        private readonly FunctionsCollectionType    collectionType_;
        private readonly string                     name_;

        public FunctionsPart(FunctionsCollectionType collectionType, string name)
        {
            collectionType_ = collectionType;
            name_           = name;
        }

        public override string Name { get { return name_; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return ctx.UsesSdfTree;
        }

        public override void Generate(CodeGenContext ctx)
        {
            // mix operators have variants for blending of materials
            string? featureName = (collectionType_ == FunctionsCollectionType.MixOp && ctx.UsesMaterials) ? "MaterialsBlending" : null;

            Dictionary<string, string> functions = new Dictionary<string, string>();

            TreeNode.CallRecursive(ctx.Model.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (collectionType_ == FunctionsCollectionType.Sdf)
                    sdfObj.FunctionSdf.AddCodeToCollection(functions);
                else if (collectionType_ == FunctionsCollectionType.MixOp)
                    sdfObj.FunctionMixOp.AddCodeToCollection(functions, featureName);
                else if (collectionType_ == FunctionsCollectionType.PosOp)
                    sdfObj.PositionOperators.AddCodeToCollection(functions);
                else if (collectionType_ == FunctionsCollectionType.DistOp)
                    sdfObj.DistanceOperators.AddCodeToCollection(functions);
            });

            foreach(var keyVal in functions)
            {
                ctx.Code.Append(keyVal.Value);
                ctx.Code.AppendLine("");
            }
        }
    }

    // Parameters of the objects (uniforms or #defines for fixed objects)
    public class MapUniformsPart : CodeGenPart
    {
        public override string Name { get { return "Map uniforms"; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return ctx.UsesSdfTree;
        }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;

            TreeNode.CallRecursive(ctx.Model.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                // Most uniforms are shared between the distance and material functions.
                // That is why generating them can be skipped only when both flags are disabled.
                if (!sdfObj.CanBeUsedInDistanceFunction() && !sdfObj.CanBeUsedInMaterialsFunction())
                    return;

                string nodeId = ctx.GetNodeId(node);

                string varNameBase = "g_obj_" + nodeId +"_";

                sb.AppendLine(GetNodeComment(node));

                bool isFixed = ctx.IsFixed(sdfObj.IsFixed);
                UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float, varNameBase+"blendF"      , sdfObj.BlendFactor);
                UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float, varNameBase+"matId"       , sdfObj.MaterialId);
                UniformCodeWriter.AppendUniformCode(ctx, isFixed, SdfParamType.Float, varNameBase+"matBlendF"   , sdfObj.MaterialBlendFactor);

                UniformCodeWriter.AppendUniformCodeForFunctionEntity(ctx, nodeId, isFixed, sdfObj.FunctionMixOp);

                if (sdfObj.UseShape.Val)
                {
                    UniformCodeWriter.AppendUniformCodeForFunctionEntity(ctx, nodeId, isFixed, sdfObj.FunctionSdf);
                }

                UniformCodeWriter.AppendUniformCodeForOperatorsCollection(ctx, nodeId, isFixed, sdfObj.PositionOperators.Operators);
                UniformCodeWriter.AppendUniformCodeForOperatorsCollection(ctx, nodeId, isFixed, sdfObj.DistanceOperators.Operators);

                sb.AppendLine("");
            });
        }
    }

    // Distance function (gen_getDist) or materials function (gen_getMaterial) of the SDF tree
    public class MapFunctionPart : CodeGenPart
    {
        private readonly bool isMaterialsBlendingFunction_;

        public MapFunctionPart(bool isMaterialsBlendingFunction)
        {
            isMaterialsBlendingFunction_ = isMaterialsBlendingFunction;
        }

        public override string Name { get { return isMaterialsBlendingFunction_ ? "Materials function" : "Map function"; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return isMaterialsBlendingFunction_ ? ctx.UsesMaterials : ctx.UsesSdfTree;
        }

        private bool CanBeUsed(SdfObject sdfObj)
        {
            return isMaterialsBlendingFunction_ ? sdfObj.CanBeUsedInMaterialsFunction() : sdfObj.CanBeUsedInDistanceFunction();
        }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;
            string mainVectorType = ctx.MainVectorType;

            sb.AppendLine("");
            if (isMaterialsBlendingFunction_)
                sb.AppendFormat("MaterialDesc gen_getMaterial({0} p)", mainVectorType);
            else
                sb.AppendFormat("float gen_getDist({0} p)", mainVectorType);
            sb.AppendLine();
            sb.AppendLine("{");

            TreeNode.CallRecursive(ctx.Model.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (!CanBeUsed(sdfObj))
                    return;

                string nodeId   = ctx.GetNodeId(node);
                string parentId = (sdfObj.ParentAsSdf != null) ? ctx.GetNodeId(sdfObj.ParentAsSdf) : "";

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
                UniformCodeWriter.AppendFunctionsCallCodeForOperatorsCollection(sb, nodeId, "calcPos_", sdfObj.PositionOperators.Operators);

                // sdf function
                if (sdfObj.UseShape.Val)
                {
                    // visible
                    sb.Append(String.Format("    float      dist_{0} = {1}(calcPos_{0}", nodeId, sdfObj.FunctionSdf?.Definition?.FunctionName));
                    UniformCodeWriter.AppendFunctionAdditionalParameters(sb, nodeId, sdfObj.FunctionSdf);
                    sb.Append(");");
                    sb.AppendLine("");
                }
                else
                {
                    // not visible
                    sb.AppendLine(String.Format("    float      dist_{0} = MAX_DIST;", nodeId));
                }

                // distance operators
                UniformCodeWriter.AppendFunctionsCallCodeForOperatorsCollection(sb, nodeId, "dist_", sdfObj.DistanceOperators.Operators);

                // wrap in vec2, material
                if (isMaterialsBlendingFunction_)
                {
                    string materialObjectName = String.Format("material_{0}", (((int)sdfObj.MaterialId.Val)).ToString(CultureInfo.InvariantCulture));

                    // Without struct constructor (does not work in Unity):
                    sb.AppendLine(String.Format("    MaterialDesc   obj_{0};"                   , nodeId));
                    sb.AppendLine(String.Format("    obj_{0}.distance    = dist_{0};"           , nodeId));
                    sb.AppendLine(String.Format("    obj_{0}.materialId  = g_obj_{0}_matId;"    , nodeId));
                    sb.AppendLine(String.Format("    obj_{0}.cellIndex   = cellIndex_{0};"      , nodeId));
                    sb.AppendLine(String.Format("    obj_{0}.material    = {1};"                , nodeId, materialObjectName));
                }
                else
                    sb.AppendLine(String.Format("    float      obj_{0}         = dist_{0};", nodeId));
                sb.AppendLine("");
            });

            sb.AppendLine("    // Mixing objects");
            sb.AppendLine("");

            // The mix operator is always applied relative to the parent.
            // Therefore objects must be combined from the lowest level upwards.
            TreeNode.CallRecursiveChildrenFirst(ctx.Model.SdfRoot, delegate(TreeNode node)
            {
                SdfObject? sdfObj = node as SdfObject;
                if (sdfObj == null)
                    return;

                if (!CanBeUsed(sdfObj))
                {
                    if (sdfObj.ParentAsSdf == null)
                    {
                        if (isMaterialsBlendingFunction_)
                        {
                            sb.AppendLine(String.Format("    MaterialDesc emptyMaterial;"));
                            sb.AppendLine(String.Format("    return emptyMaterial;"));
                        }
                        else
                            sb.AppendLine(String.Format("    return MAX_DIST;"));
                    }
                    return;
                }

                string nodeId   = ctx.GetNodeId(node);
                string parentId = (sdfObj.ParentAsSdf != null) ? ctx.GetNodeId(sdfObj.ParentAsSdf) : "";
                string? mixOpFuncName = sdfObj.GetMixOperatorFunctionName(isMaterialsBlendingFunction_);

                // mix operator
                sb.AppendLine("    " + GetNodeComment(node));
                if (sdfObj.ParentAsSdf == null)
                {
                    sb.AppendLine(String.Format("    return obj_{0};", nodeId));
                }
                else
                {
                    if (isMaterialsBlendingFunction_)
                        sb.Append(String.Format("    obj_{2} = {0}(obj_{1}, obj_{2}, g_obj_{1}_matBlendF", mixOpFuncName, nodeId, parentId));
                    else
                        sb.Append(String.Format("    obj_{2} = {0}(obj_{1}, obj_{2}, g_obj_{1}_blendF", mixOpFuncName, nodeId, parentId));

                    UniformCodeWriter.AppendFunctionAdditionalParameters(sb, nodeId, sdfObj.FunctionMixOp);
                    sb.Append(");");
                    sb.AppendLine("");
                }

                sb.AppendLine("");
            });

            sb.AppendLine("}");
        }
    }
}
