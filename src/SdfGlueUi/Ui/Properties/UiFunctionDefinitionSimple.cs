//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SdfGlueUi.Ui.Components;
using System.Numerics;

namespace SdfGlueUi.Ui.Properties
{
    public class UiFunctionDefinitionSimple
    {
        public static void Build(IUiActionsExecutor actionsExec, ref int id, string name, FunctionEntity functionEntity, FunctionDefinitionsSet definitions)
        {
            string[] defs = definitions.GetDefinitionsNames();

            int selectedIndex = (functionEntity.Definition != null) ? functionEntity.Definition.ComboIndex : 0;
            int oldIndex = selectedIndex;
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            //ImGui.NextColumn();
            //ImGui.SetNextItemWidth(-1);
            ImGui.SameLine();

            float controlWidth = 200.0f;

            ImGui.PushID(id++);
            if (ImGui.Button(functionEntity.Definition.DisplayName, new Vector2(controlWidth, 0)))
            {
                ImGui.OpenPopup("menu_" + name);
            }
            ImGui.PopID();
            FunctionDefinition fd = MenuFunctionDefinition.BuildPopup("menu_" + name, functionEntity, definitions);
            if (fd != null)
            {
                functionEntity.Definition = fd;
                functionEntity.DefinitionName.Val = functionEntity.Definition.FunctionName;
                actionsExec.OnRebuildShader();
                UiFunctionDefinition.AddFunctionEntityUndoAction(name, functionEntity, definitions, actionsExec);
            }
        }
    }
}
