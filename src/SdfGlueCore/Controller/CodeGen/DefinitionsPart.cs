//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;
using SingleDocAppCore.Model.BaseTypes;
using System.Globalization;
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Helper functions available in all passes and targets
    public class PreludePart : CodeGenPart
    {
        public override string Name { get { return "Prelude"; } }

        public override void Generate(CodeGenContext ctx)
        {
            StringBuilder sb = ctx.Code;

            sb.AppendLine("//-------------------------------------------------------------------------------");
            sb.AppendLine("// UV / FragCoords conversion functions");
            sb.AppendLine("// (for better compatibility with Shadertoy)");
            sb.AppendLine("// uv - range: 0.0 - 1.0");
            sb.AppendLine("// frag coords - range: 0.0 - iResolution.xy");
            sb.AppendLine("//-------------------------------------------------------------------------------");
            sb.AppendLine("vec2 convertUvToFragCoords(vec2 uv)");
            sb.AppendLine("{");
            sb.AppendLine("    uv *= iResolution.xy;");
            sb.AppendLine("    return uv;");
            sb.AppendLine("}");
            sb.AppendLine("vec2 convertFragCoordsToUv(vec2 fragCoords)");
            sb.AppendLine("{");
            sb.AppendLine("    fragCoords /= iResolution.xy;");
            sb.AppendLine("    return fragCoords;");
            sb.AppendLine("}");
        }
    }

    // SDFG_VEC and the compilation parameters of the renderer (#define) - in all passes
    public class DefinitionsPart : CodeGenPart
    {
        public override string Name { get { return "Definitions"; } }

        public override void Generate(CodeGenContext ctx)
        {
            // Ray marching settings (MAX_STEPS, MIN_DIST, MAX_DIST) are compilation parameters of the renderers that use them.
            ctx.Code.AppendLine(String.Format("#define SDFG_VEC                 {0}", ctx.MainVectorType));

            // Compilation parameters
            FunctionDefParametersCollection? compParameters = ctx.Pass.RendererFunc?.Definition?.CompilationParameters;
            if (compParameters == null)
                return;

            foreach(FunctionDefParameter p in compParameters)
            {
                if (p == null)
                    continue;
                if (p.ParameterName == null)
                    continue;
                ISimpleType? paramVal = ctx.Pass.RendererFunc?.CompilationParametersValues[p.ParameterKey];
                if (paramVal == null)
                    continue;
                ctx.Code.AppendLine(String.Format("#define {0}                  ({1})", p.ParameterName, FormatCompilationParameter(p, paramVal)));
            }
        }

        // Value of a compilation parameter (#define). Toggles are always 0/1, combo indices are clamped to the options.
        private static string FormatCompilationParameter(FunctionDefParameter param, ISimpleType paramVal)
        {
            if (paramVal is ExInt valInt)
            {
                if (param.EditorType == ParamEditorType.Toggle)
                    return valInt.Val != 0 ? "1" : "0";

                if ((param.EditorType == ParamEditorType.Combo) && (param.Options != null))
                    return Math.Clamp(valInt.Val, 0, param.Options.Length - 1).ToString(CultureInfo.InvariantCulture);
            }

            return paramVal.FormatAsStringForUniform();
        }
    }

    // Include files of all functions used by the pass
    public class IncludesPart : CodeGenPart
    {
        public override string Name { get { return "Includes"; } }

        public override void Generate(CodeGenContext ctx)
        {
            ctx.Code.Append(ctx.Pass.CollectIncludes(ctx.Model, ctx.Errors));
        }
    }
}
