//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.CodeFragments;

namespace SdfGlueCore.Model.DataNodes
{
    public class MaterialsCollection : TreeNode
    {
        public MaterialsCollection() : base(DataModel.NodeIdMaterialsCollection, "Materials")
        {
        }

        public override void ResetPrevVal()
        {
        }

        public void RefreshDefinitionReference(FunctionDefinition? definition)
        {
            foreach(MaterialInstance mat in this.Children)
            {
                mat.RefreshDefinitionReference(definition);
            }
        }

    }
}
