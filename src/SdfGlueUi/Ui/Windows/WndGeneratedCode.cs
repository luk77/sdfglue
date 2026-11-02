//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Components;
using SingleDocAppFramework.Ui.CodeView;
using System.Numerics;
using System.Text;

namespace SdfGlueUi.Ui.Windows
{
    public class WndGeneratedCode : UiWindowSdfGlue
    {
        //public override string Title => "Generated Code";

        private MenuRenderPass.PreviewMode  previewMode_            = MenuRenderPass.PreviewMode.LastSelectedPass;
        private RenderPassData?             selectedPass_           = null;

        private const string                OptionFullShader        = "Full shader";
        private const string                OptionFullShaderToy     = "Full shader (Shadertoy)";
        private const string                OptionFullShaderUnity   = "Full shader (Unity)";

        private string                      selectedOption_         = OptionFullShader;

        // export code generated on demand for the selected pass (regenerated when the pass code changes)
        private string?                     exportCode_             = null;
        private ShaderExportTarget?         exportCodeTarget_       = null;
        private RenderPassData?             exportCodePass_         = null;
        private int                         exportCodeVersion_      = -1;

        // code view with syntax highlighting (tokenized only when the shown code changes)
        private readonly CodeViewDocument   codeDocument_           = new CodeViewDocument();
        private readonly CodeViewOptions    codeViewOptions_        = new CodeViewOptions();

        private string title_ = string.Empty;
        public override string Title
        {
            get
            { 
                return title_;
            }
        }

        public WndGeneratedCode(string title)
        {
            title_ = title;
        }

        public override void Build()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            BuildWindow(delegate()
            {
                int id = 1;
                ImGui.PushID(id++);
                ImGui.Text("Pass:");
                ImGui.PopID();
                ImGui.SameLine();

                MenuRenderPass.Build(GetAppContext(), ref previewMode_, ref selectedPass_);

                // fixed options + sections of the generated code (CodeGenResult.Sections)
                List<string> options = new List<string> { OptionFullShader, OptionFullShaderToy, OptionFullShaderUnity };
                if (selectedPass_ != null)
                {
                    foreach (CodeGenSection section in selectedPass_.LastGenCode.Sections)
                        options.Add(section.Name);
                }

                // the option is remembered by name - the list of sections depends on the pass
                int selectedOptionIndex = Math.Max(options.IndexOf(selectedOption_), 0);

                ImGui.SameLine();
                ImGui.PushID(id++);
                ImGui.Text(" Code to show:");
                ImGui.PopID();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(400);
                ImGui.PushID(id++);
                if (ImGui.Combo("", ref selectedOptionIndex, options.ToArray(), options.Count, options.Count))
                    selectedOption_ = options[selectedOptionIndex];
                ImGui.PopID();

                string? selectedCode = null;
                string  option       = options[selectedOptionIndex];

                if (option == OptionFullShader)
                    selectedCode = selectedPass_?.LastGenCode.FullShader;
                else if (option == OptionFullShaderToy)
                    selectedCode = GetExportCode(uiMgr, ShaderExportTarget.Shadertoy);
                else if (option == OptionFullShaderUnity)
                    selectedCode = GetExportCode(uiMgr, ShaderExportTarget.Unity);
                else
                    selectedCode = selectedPass_?.LastGenCode.FindSectionCode(option);

                ImGui.SameLine();
                ImGui.PushID(id++);
                ImGui.Text(" ");
                ImGui.PopID();

                ImGui.SameLine();
                if (ImGui.Button("Copy code to clipboard"))
                {
                    if (!String.IsNullOrEmpty(selectedCode))
                    {
                        uiMgr.ActionsExecutor.CopyTextToClipboard(selectedCode);
                    }
                }

                ImGui.SameLine();
                ImGui.Checkbox("Line numbers", ref codeViewOptions_.ShowLineNumbers);

                //ImGui.Separator();
                ImGui.SeparatorText("Code:");
                //ImGui.Separator();

                // the Unity export is HLSL (ShaderLab), everything else GLSL
                CodeLanguage language = (option == OptionFullShaderUnity)
                    ? SdfGlueCodeLanguages.GetHlsl(GetAppContext().Definitions)
                    : SdfGlueCodeLanguages.GetGlsl(GetAppContext().Definitions);

                InsertCodeView(selectedCode, language);
            });
        }

        private string? GetExportCode(UiManagerSdfGlue uiMgr, ShaderExportTarget target)
        {
            if (selectedPass_ == null)
                return null;

            if (exportCodeTarget_ != target || exportCodePass_ != selectedPass_ || exportCodeVersion_ != selectedPass_.LastGenCode.Version)
            {
                StringBuilder errors = new StringBuilder();
                exportCode_         = uiMgr.ActionsExecutor.GenerateExportCode(target, selectedPass_, errors);
                if (errors.Length > 0)
                    exportCode_     = "// ERRORS:\n// " + errors.ToString().TrimEnd().Replace("\n", "\n// ") + "\n" + exportCode_;
                exportCodeTarget_   = target;
                exportCodePass_     = selectedPass_;
                exportCodeVersion_  = selectedPass_.LastGenCode.Version;
            }

            return exportCode_;
        }

        private void InsertCodeView(string? text, CodeLanguage language)
        {
            if (String.IsNullOrEmpty(text))
                return;

            codeDocument_.SetText(text, language);
            UiCodeView.Build("ScrollableChild", codeDocument_, codeViewOptions_);
        }

    }
}
