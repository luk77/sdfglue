//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueEditor.Rendering.OpenTk;
using SingleDocAppFramework.Platform;
using System.Text;

namespace SdfGlueEditor.Application
{
    // Export of the generated shader code (Shadertoy, Unity)
    public class ShaderExportController
    {
        // Declarations added by shadertoy.com before the code of the "Image" tab (and its main()) -
        // used to compile the exported code locally as GLSL ES 3.00
        private const string ShadertoyPrologue =
            "#version 300 es\n" +
            "precision highp float;\n" +
            "precision highp int;\n" +
            "precision mediump sampler3D;\n" +
            "uniform vec3      iResolution;\n" +
            "uniform float     iTime;\n" +
            "uniform float     iTimeDelta;\n" +
            "uniform float     iFrameRate;\n" +
            "uniform int       iFrame;\n" +
            "uniform float     iChannelTime[4];\n" +
            "uniform vec3      iChannelResolution[4];\n" +
            "uniform vec4      iMouse;\n" +
            "uniform vec4      iDate;\n" +
            "uniform float     iSampleRate;\n" +
            "uniform sampler2D iChannel0;\n" +
            "uniform sampler2D iChannel1;\n" +
            "uniform sampler2D iChannel2;\n" +
            "uniform sampler2D iChannel3;\n" +
            "out vec4 sdfg_shadertoyOutColor;\n" +
            "#line 1\n";

        private const string ShadertoyEpilogue =
            "\nvoid main()\n" +
            "{\n" +
            "    vec4 color = vec4(0.0);\n" +
            "    mainImage(color, gl_FragCoord.xy);\n" +
            "    sdfg_shadertoyOutColor = color;\n" +
            "}\n";

        private SdfGlueAppContext       ctx_;
        private DocumentController      documents_;

        public ShaderExportController(SdfGlueAppContext ctx, DocumentController documents)
        {
            ctx_        = ctx;
            documents_  = documents;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }

        // Code of the pass for the target, generated on demand. Null on error (see errors).
        // exportName: name of the shader (Unity), by default the name of the project file.
        public string? GenerateCode(ShaderExportTarget target, RenderPassData pass, StringBuilder errors, string? exportName = null)
        {
            CodeGenResult result = new CodeGenResult();
            return ctx_.CodeGenerator.GenerateShader(GetModel(), errors, target, pass, result, exportName ?? GetProjectName());
        }

        private string? GetProjectName()
        {
            return String.IsNullOrEmpty(ctx_.DocumentFilePath) ? null : Path.GetFileNameWithoutExtension(ctx_.DocumentFilePath);
        }

        public void ExportToClipboard(ShaderExportTarget target)
        {
            string? code = GenerateExportCode(target, null);
            if (code == null)
                return;

            ClipboardUtils.SetText(code);
            Console.WriteLine(String.Format("{0} code copied to the clipboard.", target.Name));
        }

        public void ExportToFile(ShaderExportTarget target)
        {
            FileFilter filter = new FileFilter(String.Format("{0} shader", target.Name), "*." + target.FileExtension);
            string? filePath = ctx_.Platform.SaveFileDialog(filter);
            if (String.IsNullOrEmpty(filePath))
                return;

            string? code = GenerateExportCode(target, Path.GetFileNameWithoutExtension(filePath));
            if (code == null)
                return;

            try
            {
                File.WriteAllText(filePath, code);
                Console.WriteLine(String.Format("{0} code saved: {1}", target.Name, filePath));
            }
            catch (Exception ex)
            {
                ShowError(target, String.Format("Cannot save the file '{0}':\n{1}", filePath, ex.Message));
            }
        }

        // Code of the first primary pass (the pass with the SDF scene). Errors are shown to the user.
        private string? GenerateExportCode(ShaderExportTarget target, string? exportName)
        {
            RenderPassData? pass = GetModel().GetFirstPrimaryRPass();
            if (pass == null)
            {
                ShowError(target, "The project has no primary render pass.");
                return null;
            }

            StringBuilder errors = new StringBuilder();
            string? code = GenerateCode(target, pass, errors, exportName);
            if (code == null || errors.Length > 0)
            {
                ShowError(target, errors.ToString());
                return null;
            }

            return code;
        }

        // Development tool: exports the first primary pass of all projects (Projects, Examples) to a folder
        // (Shadertoy and Unity) and compiles the Shadertoy code as GLSL ES 3.00 with the Shadertoy prologue.
        // Requires an OpenGL driver that accepts "#version 300 es" (ARB_ES3_compatibility).
        public void ValidateExportsOfAllProjects()
        {
            List<string> files = DocumentController.FindBatchProjectFiles();
            if (files.Count == 0)
            {
                Console.WriteLine("Validate shader exports: no project files found.");
                return;
            }

            string? outputDir = ctx_.Platform.SelectFolderDialog();
            if (String.IsNullOrEmpty(outputDir))
                return;

            StringBuilder report = new StringBuilder();
            int compiledCount = 0;
            List<string> failedFiles = new List<string>();

            foreach (string path in files)
            {
                string baseName = Path.Combine(outputDir, path.Replace(Path.DirectorySeparatorChar, '_').Replace(Path.AltDirectorySeparatorChar, '_'));
                string? error = ValidateProjectExport(path, baseName);
                if (error == null)
                {
                    compiledCount++;
                    continue;
                }

                failedFiles.Add(path);
                report.AppendLine(String.Format("FAILED: {0}", path));
                report.AppendLine(error);
            }

            string summary = String.Format("Validate shader exports: Shadertoy code compiled: {0}, failed: {1} (output: {2})", compiledCount, failedFiles.Count, outputDir);
            report.Insert(0, summary + "\n\n");
            File.WriteAllText(Path.Combine(outputDir, "report.txt"), report.ToString());

            Console.WriteLine(summary);
            foreach (string path in failedFiles)
                Console.WriteLine("ERROR: Shadertoy export does not compile: {0}", path);

            if (failedFiles.Count > 0)
                ctx_.Platform.ShowErrorMessage("Validate shader exports", summary + "\n\nDetails: report.txt");
        }

        // Returns null when the Shadertoy export compiles, otherwise the error description
        private string? ValidateProjectExport(string projectPath, string outputBaseName)
        {
            string loadError;
            DataModel? model = documents_.LoadProjectFromFile(projectPath, out loadError);
            if (model == null)
                return "Cannot load the project: " + loadError;

            RenderPassData? pass = model.GetFirstPrimaryRPass();
            if (pass == null)
                return "No primary render pass.";

            ShaderCodeGenerator generator = ctx_.CodeGenerator;

            StringBuilder unityErrors = new StringBuilder();
            string? unityCode = generator.GenerateShader(model, unityErrors, ShaderExportTarget.Unity, pass, new CodeGenResult(), Path.GetFileNameWithoutExtension(projectPath));
            if (unityCode != null)
                File.WriteAllText(outputBaseName + "." + ShaderExportTarget.Unity.FileExtension, unityCode);

            StringBuilder errors = new StringBuilder();
            string? code = generator.GenerateShader(model, errors, ShaderExportTarget.Shadertoy, pass, new CodeGenResult());
            if (code == null || errors.Length > 0)
                return "Code generation errors:\n" + errors.ToString();

            File.WriteAllText(outputBaseName + "." + ShaderExportTarget.Shadertoy.FileExtension, code);

            string log;
            if (OpenTkShader.TryCompileFragmentShader(ShadertoyPrologue + code + ShadertoyEpilogue, out log))
                return null;

            return log;
        }

        private void ShowError(ShaderExportTarget target, string message)
        {
            Console.WriteLine(String.Format("ERROR: {0} export: {1}", target.Name, message));
            ctx_.Platform.ShowErrorMessage(String.Format("{0} export", target.Name), message);
        }
    }
}
