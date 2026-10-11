//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.Entities;
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueCore.Controller.CodeGen
{
    // Names of the generated GLSL identifiers - shared by the code generator and the code that sets the uniforms
    public static class ShaderNames
    {
        public static string GetNodeId(DataModel model, TreeNode node)
        {
            return (model.Config.UseNamesAsIds ? node.Name.Val : node.Id.ToString()) ?? "";
        }

        // nodeId: null/empty for parameters that do not belong to an object (renderer, backdrop, camera operators)
        // entityIndex: index of an operator in its collection, -1 for other functions
        public static string GetParamName(string? nodeId, FunctionEntity functionEntity, int entityIndex, string? paramName)
        {
            bool simpleParamsFormat = String.IsNullOrEmpty(nodeId);

            if (entityIndex == -1)
            {
                if (simpleParamsFormat)
                    return String.Format("{0}_{1}"                  , functionEntity.ParamPrefix, paramName);
                else
                    return String.Format("g_obj_{0}_{1}_{2}"        , nodeId, functionEntity.ParamPrefix, paramName);
            }
            else
            {
                if (simpleParamsFormat)
                    return String.Format("{0}{1}_{2}"               , functionEntity.ParamPrefix, entityIndex + 1, paramName);
                else
                    return String.Format("g_obj_{0}_{1}{2}_{3}"     , nodeId, functionEntity.ParamPrefix, entityIndex + 1, paramName);
            }
        }
    }
}
