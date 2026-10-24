//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Model.BaseTypes;

namespace SdfGlueCore.Controller.CodeGen
{
    // Which parameters are constants (#define) and which are uniforms. Used by the code generator (CodeGenContext,
    // UniformCodeWriter) and by everything that has to know whether a changed value reaches the shader without
    // a rebuild (the animation: a parameter that is not a uniform is disabled).
    public static class UniformRules
    {
        // the parameters of a fixed object (render pass, material) are constants
        public static bool IsFixed(DataModel model, ShaderExportTarget target, ExBool isFixed)
        {
            return target.ForceFixed || model.ProjSettings.FixAllObjects.Val || isFixed.Val;
        }

        public static bool IsCameraFixed(DataModel model, ShaderExportTarget target)
        {
            return target.ForceFixed || model.ProjSettings.FixAllObjects.Val;
        }

        // a material with a parameter driven by a signal is fixed only when the target uses static signal values
        public static bool IsMaterialFixed(DataModel model, ShaderExportTarget target, MaterialInstance mat)
        {
            return IsFixed(model, target, mat.IsFixed) && (target.UseStaticSignalValues || !mat.HasSignalBindings());
        }

        // a parameter driven by a signal stays a uniform also in a fixed object (unless the target exports the static values)
        public static bool IsParamFixed(ShaderExportTarget target, bool ownerFixed, ISimpleType paramVal)
        {
            if (ownerFixed && !target.UseStaticSignalValues && SignalBinding.IsBound(paramVal))
                return false;
            return ownerFixed;
        }
    }
}
