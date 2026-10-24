//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueCore.Controller
{
    // Selection in the editor (not saved in the project), cleared when the document is replaced
    public class EditorSelection
    {
        public  TreeNode?               SelectedNode            = null;
        // Node to be selected by the explorer in the next frame (after add / paste / delete)
        public  TreeNode?               ImportantNodeToSelect   = null;
        public  RenderPassData?         LastSelectedRPass       = null;

        public void Clear()
        {
            SelectedNode            = null;
            ImportantNodeToSelect   = null;
            LastSelectedRPass       = null;
        }

        public void SelectDefaultRenderPass(DataModel model)
        {
            if (model.RenderingSysData.GetChildrenCount() == 0)
            {
                Console.WriteLine("WARNING: SelectDefaultRenderPass() failed.");
                return;
            }

            LastSelectedRPass   = model.RenderingSysData.GetChildrenAt(0) as RenderPassData;
        }
    }
}
