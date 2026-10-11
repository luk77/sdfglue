//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.Entities;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Parameters of the renderer
    public class RendererUniformsPart : CodeGenPart
    {
        public override string Name { get { return "Renderer uniforms"; } }

        public override void Generate(CodeGenContext ctx)
        {
            UniformCodeWriter.AppendUniformCodeForFunctionEntity(ctx, null, ctx.IsFixed(ctx.Pass.IsFixed), ctx.Pass.RendererFunc);
        }
    }

    // Backdrop function, its parameters and the wrapper sdfg_getBackdropColor()
    public class BackdropPart : CodeGenPart
    {
        public override string Name { get { return "Backdrop"; } }

        public override bool IsEnabled(CodeGenContext ctx)
        {
            return ctx.UsesBackdrop;
        }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;

            BackdropEntity? backdropEntity = ctx.Pass.BackdropFunc;

            sb.AppendLine();
            AppendSectionHeader(ctx, "Backdrop");

            sb.AppendLine("// Backdrop function");
            sb.Append(backdropEntity?.Definition?.FunctionCode);

            sb.AppendLine();
            sb.AppendLine("// Backdrop uniforms");
            UniformCodeWriter.AppendUniformCodeForFunctionEntity(ctx, null, ctx.IsFixed(ctx.Pass.IsFixed), backdropEntity);

            sb.AppendLine();
            sb.AppendLine("// Backdrop wrapper function");
            sb.AppendLine("vec3 sdfg_getBackdropColor(in vec3 dir)");
            sb.AppendLine("{");
            sb.AppendFormat("    return {0}(dir", backdropEntity?.DefinitionName);
            UniformCodeWriter.AppendFunctionAdditionalParameters(sb, null, backdropEntity);
            sb.AppendFormat(");");
            sb.AppendLine();

            sb.AppendLine("}");

            sb.AppendLine();
            AppendSectionHeader(ctx, "Backdrop (end)");
            sb.AppendLine();
        }
    }

    // Code of the renderer (defines mainImage()).
    // With materials the renderer's mainImage() is renamed and wrapped by the entry point
    // that initializes the materials first - templates only call mainImage().
    public class RendererCodePart : CodeGenPart
    {
        private const string RendererMainImageName = "sdfg_rendererMainImage";

        public override string Name { get { return "Renderer"; } }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;

            bool useEntryPoint = ctx.UsesMaterials;

            if (useEntryPoint)
                sb.AppendLine("#define mainImage " + RendererMainImageName);

            sb.Append(ctx.Pass.RendererFunc?.Definition?.FunctionCode);

            if (useEntryPoint)
            {
                sb.AppendLine();
                sb.AppendLine("#undef mainImage");
                sb.AppendLine();
                AppendSectionHeader(ctx, "Entry point");
                sb.AppendLine("void mainImage(out vec4 fragColor, in vec2 fragCoord)");
                sb.AppendLine("{");
                sb.AppendLine("    initMaterials();");
                sb.AppendLine("    " + RendererMainImageName + "(fragColor, fragCoord);");
                sb.AppendLine("}");
            }
        }
    }
}
