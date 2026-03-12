//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueUi.Ui.Components;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using System.Numerics;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;

namespace SdfGlueUi.Ui.Properties
{
    class UiFunctionDefinition
    {
        public static void Build(IUiActionsExecutor actionsExec, ref int id, string name, FunctionEntity functionEntity, FunctionDefinitionsSet definitions)
        {
            ImGui.PushID(id++);
            ImGui.Text(name);
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.SetNextItemWidth(-1);
            ImGui.PushID(id++);
            if (ImGui.Button(functionEntity.Definition.DisplayName, new Vector2(-1, 0)))
            {
                ImGui.OpenPopup("menu_" + name);
            }
            FunctionDefinition fd = MenuFunctionDefinition.BuildPopup("menu_" + name, functionEntity, definitions);
            if (fd != null)
            {
                functionEntity.Definition = fd;
                functionEntity.DefinitionName.Val = functionEntity.Definition.FunctionName;
                actionsExec.OnRebuildShader();
                AddFunctionEntityUndoAction(name, functionEntity, definitions, actionsExec);
            }
            ImGui.PopID();
            ImGui.NextColumn();
        }

        public static void AddFunctionEntityUndoAction(string name, FunctionEntity functionEntity, FunctionDefinitionsSet definitions, IUiActionsExecutor actionsExec)
        {
            //Console.WriteLine("Undo save: {0}: {1}", name, functionEntity.DefinitionName.Val);
            UndoManager.Instance.SaveAction(new ActionString(functionEntity.DefinitionName, delegate 
            { 
                if (!String.IsNullOrEmpty(functionEntity.DefinitionName.Val))
                {
                    functionEntity.RefreshDefinitionReference(definitions); 
                }
                else
                {
                    // To nigdy nie powinno wystąpić przy poprawnym zarządzaniu: ResetPrevVal()
                    Console.WriteLine("WARNING: Undo unconsistency. functionEntity.DefinitionName = null for object: {0}", name);
                }
                actionsExec.OnRebuildShader(); 
            }));
        }

    }
}
