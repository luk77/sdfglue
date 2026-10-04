//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppFramework.Input;
using SdfGlueCore.Model;
using SdfGlueUi.Ui.Components;
using SdfGlueUi.Ui.Properties;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppFramework.Ui.Properties;

namespace SdfGlueUi.Ui.Windows
{
    public class WndExplorer : UiWindowSdfGlue
    {
        public override string Title => "Project Explorer";

        private bool        pasteRequest_           = false;
        private bool        pasteAsChild_           = false;
        private TreeNode?   pasteActionSelectedNode_= null;

        public override void Build()
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            BuildWindow(delegate()
            {
                //Debug_BuildTestTree();

                int id = 1;

                TreeNode root = GetModel();

                TreeNode? clickedNode = null;

                AddTreeNodeToUi(ref id, root, ref clickedNode);

                // specjalne akcje (paste)
                if (pasteRequest_)
                {
                    pasteRequest_ = false;

                    TryToPasteObject(pasteActionSelectedNode_, pasteAsChild_);
                }


                // zaznaczanie po kliknięciu
                TreeNode? nodeToSelect = clickedNode;
                if (GetModel().ImportantNodeToSelect != null)
                {
                    nodeToSelect = GetModel().ImportantNodeToSelect;
                    GetModel().ImportantNodeToSelect = null;
                }
                
                if (nodeToSelect != null)
                {
                    TreeNode.CallRecursive(root, delegate(TreeNode node)
                    {
                        node.IsSelected = (node == nodeToSelect);
                    });
                    //GetModel().SelectedNode = (SdfObject)nodeToSelect;
                    GetModel().SelectedNode         = nodeToSelect;
                    if (nodeToSelect is RenderPassData)
                        GetModel().LastSelectedRPass    = nodeToSelect as RenderPassData;

                }
            });
        }

        private void AddTreeNodeToUi(ref int id, TreeNode node, ref TreeNode? clickedNode)
        {
            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick | ImGuiTreeNodeFlags.DefaultOpen;
            if (node.IsSelected)
                flags |= ImGuiTreeNodeFlags.Selected;
            if (node.GetChildrenCount() == 0)
                flags |= ImGuiTreeNodeFlags.Leaf;


            // Render pass enable buttons
            RenderPassData? rpData = node as RenderPassData;
            if (rpData != null)
            {
                UiBool.BuildSimpleCheckBoxWithUndo(ref id, rpData.Enabled, null);

                ImGui.SameLine();

                //ImGui.PushID(id++);
                //ImGui.Text(rpData.LastRenderTime.ToString(CultureInfo.InvariantCulture));
                //ImGui.PopID();
                //ImGui.SameLine();
            }


            //string displayName = String.Format("[{0}] {1}", node.Id, node.Name);
            // an empty label would leave no clickable area of the tree node
            string displayName = String.IsNullOrEmpty(node.Name.Val) ? "<no name>" : node.Name.Val;
            ImGui.PushID(id++);
            node.IsExpanded = ImGui.TreeNodeEx(node.Id.ToString(), flags, displayName);
            if (node is SdfObject)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    //if (ImGui.BeginMenu("Edit"))
                    {
                        //if (ImGui.MenuItem("Insert new" , "CTRL+I"      ))  { uiMgr.ActionsExecutor.OnAddChildObject           (node as SdfObject, null); ImGui.CloseCurrentPopup(); }
                        if (ImGui.BeginMenu("Insert as child"))
                        {
                            //FunctionDefinition? fd = MenuFunctionDefinition.Build("insert_child_"+node.Name, ((SdfObject)node).FunctionSdf, GetModel().SdfDefinitions);
                            FunctionDefinition? fd = MenuFunctionDefinition.Build(GetModel().SdfDefinitions);
                            if (fd != null)
                            {
                                uiMgr.ActionsExecutor.OnAddChildObject(node as SdfObject, fd);
                                ImGui.CloseCurrentPopup(); 
                            }
                            ImGui.EndMenu();
                        }
                        if (ImGui.MenuItem("Delete"     , "DEL"         ))  { uiMgr.ActionsExecutor.OnDeleteNode               (node); ImGui.CloseCurrentPopup(); }
                        ImGui.Separator();
                        if (ImGui.MenuItem("Cut"        , "CTRL+X"      ))  { uiMgr.ActionsExecutor.OnCutObject                (node as SdfObject); ImGui.CloseCurrentPopup(); }
                        if (ImGui.MenuItem("Copy"       , "CTRL+C"      ))  { uiMgr.ActionsExecutor.OnCopyObject               (node as SdfObject); ImGui.CloseCurrentPopup(); }
                        //if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { uiMgr.ActionsExecutor.OnPasteObject              (node as SdfObject); ImGui.CloseCurrentPopup(); }
                        if (ImGui.MenuItem("Paste"          , "CTRL+V"          ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }
                        if (ImGui.MenuItem("Paste as child" , "CTRL+SHIFT+V"    ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = true;  ImGui.CloseCurrentPopup(); }
                        ImGui.Separator();
                        if (ImGui.MenuItem("Move up"    , "CTRL+UP"     ))  { uiMgr.ActionsExecutor.OnMoveNodeUp               (node); ImGui.CloseCurrentPopup(); }
                        if (ImGui.MenuItem("Move down"  , "CTRL+DOWN"   ))  { uiMgr.ActionsExecutor.OnMoveNodeDown             (node); ImGui.CloseCurrentPopup(); }
                        ImGui.Separator();
                        if (ImGui.MenuItem("Focus"      , "F"           ))  { uiMgr.ActionsExecutor.OnFocusObject              (node as SdfObject); ImGui.CloseCurrentPopup(); }
                        //ImGui.EndMenu();

                    }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            else if (node is RenderingData)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.BeginMenu("Add render pass"))
                    {
                        FunctionDefinition? fd = MenuFunctionDefinition.Build(GetModel().Renderers);
                        if (fd != null)
                        {
                            uiMgr.ActionsExecutor.OnAddRenderPass(node as RenderingData, fd);
                            ImGui.CloseCurrentPopup(); 
                        }

                        ImGui.EndMenu();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            else if (node is RenderPassData)
            {
                //RenderPassData rpData = node as RenderPassData;
                //ImGui.SameLine();
                //if (ImGui.Checkbox("Enabled", ref rpData.Enabled)) {}

                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"     , "DEL"         ))  { uiMgr.ActionsExecutor.OnDeleteNode           (node); ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Cut"        , "CTRL+X"      ))  { uiMgr.ActionsExecutor.OnCutRenderPass        (node as RenderPassData); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Copy"       , "CTRL+C"      ))  { uiMgr.ActionsExecutor.OnCopyRenderPass       (node as RenderPassData); ImGui.CloseCurrentPopup(); }
                    //if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { uiMgr.ActionsExecutor.OnPasteRenderPass      (node as RenderPassData); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Move up"    , "CTRL+UP"     ))  { uiMgr.ActionsExecutor.OnMoveNodeUp           (node); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Move down"  , "CTRL+DOWN"   ))  { uiMgr.ActionsExecutor.OnMoveNodeDown         (node); ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }

            }
            else if (node is MaterialsCollection)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Add new material"     , ""))
                    {
                        uiMgr.ActionsExecutor.OnAddNewMaterial();

                        ImGui.CloseCurrentPopup(); 
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            else if (node is MaterialInstance)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"     , "DEL"         ))  { uiMgr.ActionsExecutor.OnDeleteNode           (node); ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Cut"        , "CTRL+X"      ))  { uiMgr.ActionsExecutor.OnCutMaterial          (node as MaterialInstance); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Copy"       , "CTRL+C"      ))  { uiMgr.ActionsExecutor.OnCopyMaterial         (node as MaterialInstance); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Move up"    , "CTRL+UP"     ))  { uiMgr.ActionsExecutor.OnMoveNodeUp           (node); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Move down"  , "CTRL+DOWN"   ))  { uiMgr.ActionsExecutor.OnMoveNodeDown         (node); ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            else if (node is SignalsCollection)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Add new signal"     , ""))
                    {
                        uiMgr.ActionsExecutor.OnAddNewSignal();

                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            else if (node is SignalInstance)
            {
                if (ImGui.BeginPopupContextItem())
                {
                    if (ImGui.MenuItem("Delete"     , "DEL"         ))  { uiMgr.ActionsExecutor.OnDeleteNode           (node); ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Cut"        , "CTRL+X"      ))  { uiMgr.ActionsExecutor.OnCutSignal            (node as SignalInstance); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Copy"       , "CTRL+C"      ))  { uiMgr.ActionsExecutor.OnCopySignal           (node as SignalInstance); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Paste"      , "CTRL+V"      ))  { pasteRequest_ = true; pasteActionSelectedNode_ = node; pasteAsChild_ = false; ImGui.CloseCurrentPopup(); }
                    ImGui.Separator();
                    if (ImGui.MenuItem("Move up"    , "CTRL+UP"     ))  { uiMgr.ActionsExecutor.OnMoveNodeUp           (node); ImGui.CloseCurrentPopup(); }
                    if (ImGui.MenuItem("Move down"  , "CTRL+DOWN"   ))  { uiMgr.ActionsExecutor.OnMoveNodeDown         (node); ImGui.CloseCurrentPopup(); }

                    clickedNode = node;

                    ImGui.EndPopup();
                }
            }
            //ImGui.PopID();

            if (ImGui.IsItemClicked())
            {
                clickedNode = node;
            }

            if (node.IsExpanded)
            {
                foreach(TreeNode child in node.Children)
                {
                    AddTreeNodeToUi(ref id, child, ref clickedNode);
                }
                ImGui.TreePop();
            }

            // Balance the PushID call above to avoid ID-stack drift.
            ImGui.PopID();
        }



        private uint base_flags = (uint)(ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.OpenOnDoubleClick);// | ImGuiTreeNodeFlags.SpanAvailWidth;
        private bool align_label_with_current_x_position = false;
        private bool test_drag_and_drop = false;
        private int selection_mask = (1 << 2); // Dumb representation of what may be user-side selection state. You may carry selection state inside or outside your objects in whatever format you see fit.

        private void Debug_BuildTestTree()
        {
            if (ImGui.TreeNode("Advanced, with Selectable nodes"))
            {
                HelpMarker("This is a more typical looking tree with selectable nodes.\nClick to select, CTRL+Click to toggle, click on arrows or double-click to open.");
                ImGui.CheckboxFlags("ImGuiTreeNodeFlags_OpenOnArrow", ref base_flags, (uint)ImGuiTreeNodeFlags.OpenOnArrow);
                ImGui.CheckboxFlags("ImGuiTreeNodeFlags_OpenOnDoubleClick", ref base_flags, (uint)ImGuiTreeNodeFlags.OpenOnDoubleClick);
                ImGui.CheckboxFlags("ImGuiTreeNodeFlags_SpanAvailWidth", ref base_flags, 0);//ImGuiTreeNodeFlags.SpanAvailWidth); ImGui.SameLine(); HelpMarker("Extend hit area to all available width instead of allowing more items to be layed out after the node.");
                ImGui.CheckboxFlags("ImGuiTreeNodeFlags_SpanFullWidth", ref base_flags, 0);//ImGuiTreeNodeFlags.SpanFullWidth);
                ImGui.Checkbox("Align label with current X position", ref align_label_with_current_x_position);
                ImGui.Checkbox("Test tree node as drag source", ref test_drag_and_drop);
                ImGui.Text("Hello!");
                if (align_label_with_current_x_position)
                    ImGui.Unindent(ImGui.GetTreeNodeToLabelSpacing());

                int node_clicked = -1;                // Temporary storage of what node we have clicked to process selection at the end of the loop. May be a pointer to your own node type, etc.
                for (int i = 0; i < 6; i++)
                {
                    // Disable the default open on single-click behavior and pass in Selected flag according to our selection state.
                    uint node_flags = base_flags;
                    //const bool is_selected = (selection_mask & (1 << i)) != 0;
                    bool is_selected = (selection_mask & (1 << i)) != 0;
                    if (is_selected)
                        node_flags |= (uint)ImGuiTreeNodeFlags.Selected;
                    if (i < 3)
                    {
                        // Items 0..2 are Tree Node
                        //bool node_open = ImGui.TreeNodeEx((void*)(intptr_t)i, node_flags, "Selectable Node %d", i);
                        bool node_open = ImGui.TreeNodeEx(i.ToString(), (ImGuiTreeNodeFlags)node_flags, String.Format("Selectable Node {0}", i));
                        if (ImGui.IsItemClicked())
                            node_clicked = i;
                        if (test_drag_and_drop && ImGui.BeginDragDropSource())
                        {
                            ImGui.SetDragDropPayload("_TREENODE", IntPtr.Zero, 0);
                            ImGui.Text("This is a drag and drop source");
                            ImGui.EndDragDropSource();
                        }
                        if (node_open)
                        {
                            ImGui.BulletText("Blah blah\nBlah Blah");
                            ImGui.TreePop();
                        }
                    }
                    else
                    {
                        // Items 3..5 are Tree Leaves
                        // The only reason we use TreeNode at all is to allow selection of the leaf.
                        // Otherwise we can use BulletText() or advance the cursor by GetTreeNodeToLabelSpacing() and call Text().
                        node_flags |= (uint)(ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen); // ImGuiTreeNodeFlags.Bullet
                        //ImGui.TreeNodeEx((void*)(intptr_t)i, node_flags, "Selectable Leaf %d", i);
                        ImGui.TreeNodeEx(i.ToString(), (ImGuiTreeNodeFlags)node_flags, String.Format("Selectable Leaf {0}", i));
                        if (ImGui.IsItemClicked())
                            node_clicked = i;
                        if (test_drag_and_drop && ImGui.BeginDragDropSource())
                        {
                            ImGui.SetDragDropPayload("_TREENODE", IntPtr.Zero, 0);
                            ImGui.Text("This is a drag and drop source");
                            ImGui.EndDragDropSource();
                        }
                    }
                }

                if (node_clicked != -1)
                {
                    // Update selection state. Process outside of tree loop to avoid visual inconsistencies during the clicking-frame.
                    if (ImGui.GetIO().KeyCtrl)
                        selection_mask ^= (1 << node_clicked);          // CTRL+click to toggle
                    else //if (!(selection_mask & (1 << node_clicked))) // Depending on selection behavior you want, this commented bit preserve selection when clicking on item that is part of the selection
                        selection_mask = (1 << node_clicked);           // Click to single-select
                }
                if (align_label_with_current_x_position)
                    ImGui.Indent(ImGui.GetTreeNodeToLabelSpacing());
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }

        public override void HandleInput(float deltaTime)
        {
            base.HandleInput(deltaTime);

            if (!IsVisible)
                return;

            if (!IsFocused)
                return;

            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            bool isControlKeyDown   = uiMgr.ActionsExecutor.IsKeyDown(UiKey.LeftControl)  || uiMgr.ActionsExecutor.IsKeyDown(UiKey.RightControl);
            bool isShiftKeyDown     = uiMgr.ActionsExecutor.IsKeyDown(UiKey.LeftShift)    || uiMgr.ActionsExecutor.IsKeyDown(UiKey.RightShift);
            //bool isAltKeyDown     = uiMgr.ActionsExecutor.IsKeyDown(UiKey.LeftAlt)      || uiMgr.ActionsExecutor.IsKeyDown(UiKey.RightAlt);

            DataModel model = uiMgr.ActionsExecutor.GetModel();

            // Delete object
            if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.Delete))
            {
                uiMgr.ActionsExecutor.OnDeleteNode(model.SelectedNode);
            }

            if (isControlKeyDown)
            {
                // Cut, copy, paste
                if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.X))
                {
                    if (GetModel().SelectedNode is RenderPassData)
                        uiMgr.ActionsExecutor.OnCutRenderPass(GetModel().SelectedNode as RenderPassData);
                    else if (GetModel().SelectedNode is MaterialInstance)
                        uiMgr.ActionsExecutor.OnCutMaterial(GetModel().SelectedNode as MaterialInstance);
                    else if (GetModel().SelectedNode is SignalInstance)
                        uiMgr.ActionsExecutor.OnCutSignal(GetModel().SelectedNode as SignalInstance);
                    else
                        uiMgr.ActionsExecutor.OnCutObject(GetModel().SelectedNode as SdfObject);
                }
                if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.C))
                {
                    if (GetModel().SelectedNode is RenderPassData)
                        uiMgr.ActionsExecutor.OnCopyRenderPass(GetModel().SelectedNode as RenderPassData);
                    else if (GetModel().SelectedNode is MaterialInstance)
                        uiMgr.ActionsExecutor.OnCopyMaterial(GetModel().SelectedNode as MaterialInstance);
                    else if (GetModel().SelectedNode is SignalInstance)
                        uiMgr.ActionsExecutor.OnCopySignal(GetModel().SelectedNode as SignalInstance);
                    else
                        uiMgr.ActionsExecutor.OnCopyObject(GetModel().SelectedNode as SdfObject);
                }
                if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.V))
                {
                    if (isShiftKeyDown)
                    {
                        // Pasting with shift adds object as child
                        TryToPasteObject(GetModel().SelectedNode, true);
                    }
                    else
                    {
                        // We prefer to add object as sibling (to parent if it exist)
                        TryToPasteObject(GetModel().SelectedNode, false);
                    }
                }

                // Move up / down
                if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.Up))
                {
                    uiMgr.ActionsExecutor.OnMoveNodeUp(GetModel().SelectedNode);
                }
                if (uiMgr.ActionsExecutor.IsKeyPressed(UiKey.Down))
                {
                    uiMgr.ActionsExecutor.OnMoveNodeDown(GetModel().SelectedNode);
                }
            }
        }

        private void TryToPasteObject(TreeNode? selectedNode, bool pasteAsChild)
        {
            if (selectedNode == null)
                return;

            UiManagerSdfGlue uiMgr = UiMgrSdfGlue;

            SdfObject? sdfObject = selectedNode as SdfObject;
            if (sdfObject != null)
            {
                if (!pasteAsChild)
                {
                    // We prefer to add object as sibling (attach to parent if it exist)
                    if (sdfObject.Parent as SdfObject != null)
                    {
                        uiMgr.ActionsExecutor.OnPasteObject(sdfObject.Parent as SdfObject);
                    }
                    else
                    {
                        // If there is no parent, we paste as a child
                        uiMgr.ActionsExecutor.OnPasteObject(sdfObject);
                    }
                }
                else
                {
                    // Pasting with shift adds object as child
                    uiMgr.ActionsExecutor.OnPasteObject(sdfObject as SdfObject);
                }
            }

            // Render pass is pasted after the selected pass, or at the end when the rendering node is selected
            if (selectedNode is RenderPassData || selectedNode is RenderingData)
            {
                uiMgr.ActionsExecutor.OnPasteRenderPass(selectedNode);
            }

            // Material is pasted after the selected material, or at the end when the materials node is selected
            if (selectedNode is MaterialInstance || selectedNode is MaterialsCollection)
            {
                uiMgr.ActionsExecutor.OnPasteMaterial(selectedNode);
            }

            // Signal is pasted after the selected signal, or at the end when the signals node is selected
            if (selectedNode is SignalInstance || selectedNode is SignalsCollection)
            {
                uiMgr.ActionsExecutor.OnPasteSignal(selectedNode);
            }
        }
    }

}
