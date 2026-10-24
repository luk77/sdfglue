//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SingleDocAppCore.Model.BaseTypes;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Code of function parameters: declarations (uniform / #define) and arguments of the function calls
    public static class UniformCodeWriter
    {
        // Fixed parameter: "#define name (value)", otherwise: "uniform type name = value;"
        public static void AppendUniformCode(CodeGenContext ctx, bool isFixed, SdfParamType type, string paramName, ISimpleType paramVal, string? valueSuffix = null)
        {
            // a parameter driven by a signal must stay a uniform, also in fixed objects
            isFixed = UniformRules.IsParamFixed(ctx.Target, isFixed, paramVal);

            StringBuilder sb = ctx.Code;

            string paramType = FunctionDefParameter.SdfTypeToString(type);

            string paramCurrValAsString = paramVal.FormatAsStringForUniform();

            if (!String.IsNullOrEmpty(valueSuffix))
                paramCurrValAsString += valueSuffix;

            if (isFixed)
            {
                sb.Append("#define ");
                sb.Append(' ', 16);
            }
            else
            {
                sb.Append("uniform ");
                sb.Append(paramType);
                sb.Append(' ', 1 + Math.Max(15 - paramType.Length, 0));
            }

            sb.Append(paramName);
            sb.Append(' ', 1 + Math.Max(31 - paramName.Length, 0));

            if (isFixed)
            {
                sb.Append(" (");
                sb.Append(paramCurrValAsString);
                sb.Append(")");
            }
            else
            {
                sb.Append("= ");
                sb.Append(paramCurrValAsString);
                sb.Append(";");
            }

            sb.AppendLine();
        }

        public static void AppendUniformCodeForFunctionEntity(CodeGenContext ctx, string? nodeId, bool isFixed, FunctionEntity? functionEntity, int entityIndex = -1)
        {
            if (functionEntity == null || functionEntity.Definition == null)
                return;

            foreach(FunctionDefParameter p in functionEntity.Definition.Parameters)
            {
                if (p == null || p.ParameterName == null)
                    continue;

                if (!p.IsEditable())
                    continue;

                string fullNameId = ShaderNames.GetParamName(nodeId, functionEntity, entityIndex, p.ParameterName);

                ISimpleType paramVal = functionEntity.ParametersValues[p.ParameterKey];
                AppendUniformCode(ctx, isFixed, p.Type, fullNameId, paramVal);
            }
        }

        public static void AppendUniformCodeForOperatorsCollection(CodeGenContext ctx, string? nodeId, bool isFixed, List<OperatorEntity> operators)
        {
            for(int i=0; i<operators.Count; i++)
            {
                OperatorEntity opent = operators[i];
                if (!opent.Enabled.Val)
                    continue;
                AppendUniformCodeForFunctionEntity(ctx, nodeId, isFixed, opent, i);
            }
        }

        // Arguments of a function call (after the first one): ", param1, param2, ..."
        public static void AppendFunctionAdditionalParameters(StringBuilder sb, string? nodeId, FunctionEntity? functionEntity, int entityIndex = -1)
        {
            if (functionEntity == null || functionEntity.Definition == null)
                return;

            foreach(FunctionDefParameter p in functionEntity.Definition.Parameters)
            {
                string? fullNameId = null;

                // this is for 'extrusion' operator
                if (p.ParameterName == FunctionDefParameter.SpecialParamCurrObjPos)
                    fullNameId = String.Format("calcPos_{0}", nodeId);
                else if (p.ParameterName == FunctionDefParameter.SpecialParamCurrObjCellIndex)
                    fullNameId = String.Format("cellIndex_{0}", nodeId);
                else
                    fullNameId = ShaderNames.GetParamName(nodeId, functionEntity, entityIndex, p.ParameterName);

                sb.Append(", " + fullNameId);
            }
        }

        // Calls of the enabled operators, e.g. "    opTranslate(calcPos_5, g_obj_5_posOp1_offset);"
        public static void AppendFunctionsCallCodeForOperatorsCollection(StringBuilder sb, string? nodeId, string paramName, List<OperatorEntity>? operators)
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
    }
}
