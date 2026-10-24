//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller.CodeGen;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using System.Text;

namespace SdfGlueCore.Controller
{
    // Generates the GLSL code of a render pass. The code is built by the parts (CodeGen/*Part)
    // into one StringBuilder and inserted into the template in place of GeneratedCodeTag.
    // The generator does not keep the model - it is passed to every call (current or temporary model).
    // Options of the generated code come from the user settings (UseNamesAsIds).
    public class ShaderCodeGenerator
    {
        public const string                 GeneratedCodeTag    = "//__generated_code__";

        private UserSettingsSdfGlue         settings_;

        // Order of the generated code
        private readonly List<CodeGenPart>  parts_              = new List<CodeGenPart>
        {
            new PreludePart             (),
            new DefinitionsPart         (),
            new IncludesPart            (),
            new CameraPart              (),
            new MaterialsPart           (),
            new FunctionsPart           (FunctionsCollectionType.Sdf    , "Distance functions"  ),
            new FunctionsPart           (FunctionsCollectionType.MixOp  , "Mix operators"       ),
            new FunctionsPart           (FunctionsCollectionType.PosOp  , "Position operators"  ),
            new FunctionsPart           (FunctionsCollectionType.DistOp , "Distance operators"  ),
            new MapUniformsPart         (),
            new MapFunctionPart         (false),
            new MapFunctionPart         (true),
            new RendererUniformsPart    (),
            new BackdropPart            (),
            new RendererCodePart        (),
        };


        public ShaderCodeGenerator(UserSettingsSdfGlue settings)
        {
            settings_ = settings;
        }

        // Identifiers of the objects in the generated code: names or ids (see ShaderNames.GetNodeId())
        public bool UseNamesAsIds
        {
            get { return settings_.UseNamesAsIds; }
        }

        // Generates the code of the pass (without the template). Sections are stored in the result.
        private string GenerateCode(CodeGenContext ctx, CodeGenResult result)
        {
            ctx.Target.AppendHeader(ctx);

            foreach (CodeGenPart part in parts_)
            {
                if (!part.IsEnabled(ctx))
                    continue;

                int start = ctx.Code.Length;
                part.Generate(ctx);
                result.Sections.Add(new CodeGenSection(part.Name, ctx.Code.ToString(start, ctx.Code.Length - start)));
                ctx.Code.AppendLine();
            }

            return ctx.Code.ToString();
        }

        // Inserts the generated code into the template (shaderSource). Template null - only the generated code.
        public string ModifyFragShaderSource(DataModel model, StringBuilder sbErrors, string? shaderSource, CodeGenResult result, ShaderExportTarget target, RenderPassData renderPassData, string? exportName = null)
        {
            result.Clean();

            CodeGenContext ctx = new CodeGenContext(model, renderPassData, sbErrors, target, UseNamesAsIds, exportName);
            string code = target.PostProcessGeneratedCode(ctx, GenerateCode(ctx, result));

            string shader = code;
            if (shaderSource != null)
            {
                if (shaderSource.Contains(GeneratedCodeTag))
                    shader = shaderSource.Replace(GeneratedCodeTag, code);
                else
                    sbErrors.AppendLine(String.Format("Tag: '{0}' not found in the shader template. Code modification not possible.", GeneratedCodeTag));
            }

            shader = target.PostProcess(ctx, shader);

            result.GeneratedCode    = code;
            result.FullShader       = shader;

            return shader;
        }

        // Full shader for the target (template read from target.TemplatePath). Null on error (see sbErrors).
        // exportName: name of the exported shader (e.g. the file name), used by some targets (Unity).
        public string? GenerateShader(DataModel model, StringBuilder sbErrors, ShaderExportTarget target, RenderPassData renderPassData, CodeGenResult result, string? exportName = null)
        {
            string? template = null;
            if (target.TemplatePath != null)
            {
                try
                {
                    template = File.ReadAllText(target.TemplatePath);
                }
                catch (Exception ex)
                {
                    sbErrors.AppendLine(String.Format("Cannot read the shader template '{0}': {1}", target.TemplatePath, ex.Message));
                    return null;
                }
            }

            return ModifyFragShaderSource(model, sbErrors, template, result, target, renderPassData, exportName);
        }
    }
}
