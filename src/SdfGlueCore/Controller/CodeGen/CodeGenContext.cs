//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Model.DataNodes;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // State of a single code generation (one render pass). All parts write into the common Code builder.
    public class CodeGenContext
    {
        public  readonly DataModel          Model;
        public  readonly RenderPassData     Pass;
        public  readonly StringBuilder      Errors;
        public  readonly StringBuilder      Code                = new StringBuilder(100000);
        public  readonly ShaderExportTarget Target;
        // Name of the exported shader (e.g. the file name), may be null
        public  readonly string?            ExportName;

        // Features of the pass (secondary passes have no backdrop, camera, materials and SDF tree)
        public  readonly bool               UsesBackdrop;
        public  readonly bool               UsesCamera;
        public  readonly bool               UsesMaterials;
        public  readonly bool               UsesSdfTree;

        public CodeGenContext(DataModel model, RenderPassData pass, StringBuilder errors, ShaderExportTarget target, string? exportName = null)
        {
            Model       = model;
            Pass        = pass;
            Errors      = errors;
            Target      = target;
            ExportName  = exportName;

            UsesBackdrop    = pass.IsPrimaryPass;
            UsesCamera      = pass.IsPrimaryPass;
            UsesMaterials   = pass.IsPrimaryPass && GetMaterialDefinition() != null;
            UsesSdfTree     = pass.IsPrimaryPass;
        }

        public string MainVectorType
        {
            get { return Model.ProjSettings.Use4d.Val ? "vec4" : "vec3"; }
        }

        // Fixed parameters are generated as constants (#define), the others as uniforms
        public bool IsFixed(ExBool isFixed)
        {
            return Target.ForceFixed || Model.ProjSettings.FixAllObjects.Val || isFixed.Val;
        }

        public bool IsCameraFixed()
        {
            return Target.ForceFixed || Model.ProjSettings.FixAllObjects.Val;
        }

        // Fixed material - generated with constant values
        // (a material with a parameter driven by a signal is fixed only when the target uses static signal values)
        public bool IsMaterialFixed(MaterialInstance mat)
        {
            return IsFixed(mat.IsFixed) && (Target.UseStaticSignalValues || !mat.HasSignalBindings());
        }

        public string GetNodeId(TreeNode node)
        {
            return ShaderNames.GetNodeId(Model, node);
        }

        // Definition of the material parameters (comes from the renderer of the first primary pass).
        // Null when the renderer does not use the materials system.
        public FunctionDefinition? GetMaterialDefinition()
        {
            if (Model.Materials?.GetChildrenCount() == 0)
                return null;

            // This is required only to access current material definition.
            // It could also be done by GetModel().GetFirstPrimaryRPass() ...
            MaterialInstance? firstMaterial = Model.Materials?.GetChildrenAt(0) as MaterialInstance;
            FunctionDefinition? materialDefinition = firstMaterial?.MaterialProps?.Definition;

            // If materialDefinition is empty it means render pass do not use materials system at all.
            if (materialDefinition == null)
                return null;
            if (materialDefinition.MaterialParameters.Count == 0)
                return null;

            return materialDefinition;
        }
    }
}
