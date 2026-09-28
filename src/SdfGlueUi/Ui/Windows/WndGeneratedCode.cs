//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Ui.Components;
using System.Numerics;

namespace SdfGlueUi.Ui.Windows
{
    public class WndGeneratedCode : UiWindowSdfGlue
    {
        //public override string Title => "Generated Code";

        private MenuRenderPass.PreviewMode  previewMode_            = MenuRenderPass.PreviewMode.LastSelectedPass;
        private RenderPassData?             selectedPass_           = null;

        private int                         selectedOptionIndex_    = 0;

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

                MenuRenderPass.Build(GetModel(), ref previewMode_, ref selectedPass_);

                string[] arrOptions = { 
                    "Full shader"           , // 0
                    "Full shader (Unity)"   , // 1
                    "Defines"               , // 2
                    "Materials"             , // 3
                    "Includes"              , // 4
                    "Distance functions"    , // 5
                    "Mix operators"         , // 6
                    "Position operators"    , // 7
                    "Distance operators"    , // 8
                    "Map uniforms"          , // 9
                    "Map function"          , // 10
                    "Materials function"    , // 12
                    };

                ImGui.SameLine();
                ImGui.PushID(id++);
                ImGui.Text(" Code to show:");
                ImGui.PopID();
                ImGui.SameLine();
                ImGui.SetNextItemWidth(400);
                ImGui.PushID(id++);
                ImGui.Combo("", ref selectedOptionIndex_, arrOptions, arrOptions.Length, arrOptions.Length);
                ImGui.PopID();

                string? selectedCode = null;

                switch ( selectedOptionIndex_)
                {
                    case 0: selectedCode = selectedPass_?.LastGenCode.FullShader         ; break;
                    case 1: selectedCode = selectedPass_?.LastGenCodeUnity               ; break;
                    case 2: selectedCode = selectedPass_?.LastGenCode.Definitions        ; break;
                    case 3: selectedCode = selectedPass_?.LastGenCode.Materials          ; break;
                    case 4: selectedCode = selectedPass_?.LastGenCode.Includes           ; break;
                    case 5: selectedCode = selectedPass_?.LastGenCode.DistanceFunctions  ; break;
                    case 6: selectedCode = selectedPass_?.LastGenCode.MixOpFunctions     ; break;
                    case 7: selectedCode = selectedPass_?.LastGenCode.PosOpFunctions     ; break;
                    case 8: selectedCode = selectedPass_?.LastGenCode.DistOpFunctions    ; break;
                    case 9: selectedCode = selectedPass_?.LastGenCode.MapUniforms        ; break;
                    case 10:selectedCode = selectedPass_?.LastGenCode.MapFunction        ; break;
                    case 11:selectedCode = selectedPass_?.LastGenCode.MaterialsFunction  ; break;
                }

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

                //ImGui.Separator();
                ImGui.SeparatorText("Code:");
                //ImGui.Separator();

                InsertScrollableTextPanel( selectedCode );
            });
        }

        private static void InsertScrollableTextPanel(string? text)
        {
            if (String.IsNullOrEmpty(text))
                return;

            //ImGui.BeginChild("ScrollableChild", new Vector2(ImGui.GetContentRegionAvail().X , ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None, ImGuiWindowFlags.AlwaysVerticalScrollbar);
            ImGui.BeginChild("ScrollableChild", new Vector2(ImGui.GetContentRegionAvail().X , ImGui.GetContentRegionAvail().Y), ImGuiChildFlags.None);

            // long text display
            ImGui.TextUnformatted(text);

            ImGui.EndChild();
        }

    }
}
