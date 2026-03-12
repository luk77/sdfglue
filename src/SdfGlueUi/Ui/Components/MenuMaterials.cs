//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;

namespace SdfGlueUi.Ui.Components
{
    public class MenuMaterials
    {
        public static MaterialInstance Build(MaterialsCollection materials, DataModel.OnValueChanged onValueChanged = null)
        {
            MaterialInstance selected = null;
            foreach (MaterialInstance mat in materials.Children)
            {
                if (ImGui.MenuItem(mat.Name.Val))
                {
                    //materialId = mat.Id;
                    selected = mat;
                    if (onValueChanged != null)
                        onValueChanged();
                }
            }
            return selected;
        }

        public static MaterialInstance BuildPopup(string menuName, MaterialsCollection materials, DataModel.OnValueChanged onValueChanged = null)
        {
            if (!ImGui.BeginPopup(menuName))
                return null;

            MaterialInstance mat = Build(materials, onValueChanged);

            ImGui.EndPopup();

            return mat;
        }


    }
}
