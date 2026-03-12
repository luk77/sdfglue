//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;

namespace SdfGlueUi.Ui.Components
{
    public class MenuFunctionDefinition
    {
        public static FunctionDefinition Build(FunctionDefinitionsSet definitions)
        {
            FunctionDefinition selectedDefinition = null;

            //List<FunctionDefinition> definitionsList    = definitions.GetDefinitions();
            SortedDictionary<string, FunctionDefGroup> groups = definitions.GetGroups();
            FunctionDefGroup groupUngrouped = null;
            foreach (var keyVal in groups)
            {
                FunctionDefGroup group = keyVal.Value;
                if (group.GroupName == FunctionDefGroup.NameUngrouped)
                {
                    groupUngrouped = group;
                    continue;
                }

                if (ImGui.BeginMenu(group.GroupName))
                {
                    foreach (FunctionDefinition fd in group.Definitions)
                    {
                        if (ImGui.MenuItem(fd.DisplayName))
                        {
                            selectedDefinition = fd;
                        }
                    }
                    ImGui.EndMenu();
                }
            }

            if (groupUngrouped != null)
            {
                ImGui.Separator();
                foreach (FunctionDefinition fd in groupUngrouped.Definitions)
                {
                    if (ImGui.MenuItem(fd.DisplayName))
                    {
                        selectedDefinition = fd;
                    }
                }
            }
            return selectedDefinition;
        }

        public static FunctionDefinition BuildPopup(string menuName, FunctionEntity functionEntity, FunctionDefinitionsSet definitions)
        {
            if (!ImGui.BeginPopup(menuName))
                return null;

            FunctionDefinition selectedDefinition = MenuFunctionDefinition.Build(definitions);

            ImGui.EndPopup();

            return selectedDefinition;
        }


    }
}
