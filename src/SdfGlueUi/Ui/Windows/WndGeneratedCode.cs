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
    public class WndGeneratedCode : UiWindowBase
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
            BuildWindow(uiMgr_.CenterColPosX, uiMgr_.BasePosY, uiMgr_.CenterColWidth, uiMgr_.BaseHeight, delegate ()
            {
                int id = 1;
                ImGui.PushID(id++);
                ImGui.Text("Pass:");
                ImGui.PopID();
                ImGui.SameLine();

                MenuRenderPass.Build(GetModel(), ref previewMode_, ref selectedPass_);

                if (DataModel.GenCodeCleanup)
                {

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
                        uiMgr_.ActionsExecutor.CopyTextToClipboard(selectedCode);
                    }
                }

                //ImGui.Separator();
                ImGui.SeparatorText("Code:");
                //ImGui.Separator();

                InsertScrollableTextPanel( selectedCode );

                } //GenCodeCleanup
                else
                {
                    // Old code preview

                if (ImGui.Button("Copy shader to clipboard"))
                {
                    //new SetClipboardHelper(System.Windows.Forms.DataFormats.Text, GetModel().LastGenCode.FullShader).Go();
                    uiMgr_.ActionsExecutor.CopyTextToClipboard(selectedPass_.LastGenCode.FullShader);
                }

                if (ImGui.Button("Copy unity shader to clipboard"))
                {
                    //new SetClipboardHelper(System.Windows.Forms.DataFormats.Text, GetModel().LastGenCodeUnity).Go();
                    uiMgr_.ActionsExecutor.CopyTextToClipboard(selectedPass_.LastGenCodeUnity);
                }

                ImGui.BeginTabBar("TabsGenCode", ImGuiTabBarFlags.None); 

                if (ImGui.BeginTabItem("Defines"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.Definitions))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.Definitions);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Materials"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.Materials))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.Materials);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Includes"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.Includes))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.Includes);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Distance functions"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.DistanceFunctions))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.DistanceFunctions);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Mix operators"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.MixOpFunctions))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.MixOpFunctions);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Position operators"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.PosOpFunctions))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.PosOpFunctions);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Distance operators"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.DistOpFunctions))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.DistOpFunctions);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Map uniforms"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.MapUniforms))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.MapUniforms);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Map function"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.MapFunction))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.MapFunction);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Materials function"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.MaterialsFunction))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.MaterialsFunction);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Full shader"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCode.FullShader))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCode.FullShader);
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Full Unity shader"))
                {
                    if (!String.IsNullOrEmpty(selectedPass_.LastGenCodeUnity))
                    {
                        // long text display
                        InsertScrollableTextPanel(selectedPass_.LastGenCodeUnity);
                    }
                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
                }
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
