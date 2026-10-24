//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppFramework.Platform;
using System.Xml;
using TreeNode = SingleDocAppCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor.Application
{
    public class ProjectHierarchyController
    {
        private readonly SdfGlueAppContextBase  ctx_;

        // Deferred operations - executed outside of the UI building loop (HandleDeleteNode() etc.)
        private TreeNode?                       nodeToDelete_           = null;
        private TreeNode?                       nodeToMoveUp_           = null;
        private TreeNode?                       nodeToMoveDown_         = null;

        public ProjectHierarchyController(SdfGlueAppContextBase ctx)
        {
            ctx_ = ctx;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }


        public void OnAddChildObject(SdfObject? node, FunctionDefinition definition)
        {
            if (node == null)
                return;

            SdfObject newNode = new SdfObject(GetModel().NextAvailableId, String.Format("Object{0}", GetModel().NextAvailableId), definition);
            GetModel().NextAvailableId++;
            node.AddChild(newNode);

            newNode.FixMaterialReference(GetModel());

            newNode.FunctionSdf         .RefreshDefinitionReference(GetModel().Definitions.Sdf);
            newNode.FunctionMixOp       .RefreshDefinitionReference(GetModel().Definitions.MixOps);
            newNode.PositionOperators   .RefreshDefinitionReference(GetModel().Definitions.PositionOps);
            newNode.DistanceOperators   .RefreshDefinitionReference(GetModel().Definitions.DistanceOps);

            ctx_.ReinitializeShader();

            ctx_.Selection.ImportantNodeToSelect = newNode;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(newNode);
                },
                delegate
                {
                    // redo
                    node.AddChild(newNode);
                    ctx_.ReinitializeShader();
                }
                ));
        }

        public void OnDeleteNode(TreeNode? node)
        {
            if (node == null)
                return;

            if (!CanDeleteNode(node))
                return;

            nodeToDelete_ = node;

            ctx_.Selection.SelectedNode = null;
            ctx_.Selection.ImportantNodeToSelect = node.Parent;
        }

        public void OnCopyObject(SdfObject? node)
        {
            OnCopyNode(node);
        }

        private void OnCopyNode(SerializableNode? node)
        {
            if (node == null)
                return;

            XmlDocument doc = new XmlDocument();

            node.Serialize(doc, doc);

            string txt = doc.InnerXml.ToString();

            ClipboardUtils.SetText(txt);
        }

        public void OnCutObject(SdfObject? node)
        {
            OnCutNode(node);
        }
        private void OnCutNode(SerializableNode? node)
        {
            if (node == null)
                return;

            OnCopyNode(node);

            OnDeleteNode(node);
        }

        public delegate void AssignIdentifiersDelegate(TreeNode node);

        // xmlNodeName - name of the root XML node written by T.Serialize(); clipboard content of any other type is ignored
        // insertIndex  - position in pasteTarget children, -1 appends at the end
        private void OnPasteNode<T>(TreeNode? pasteTarget, int insertIndex, string xmlNodeName, AssignIdentifiersDelegate assignIdentifiersDelegate) where T : SerializableNode, new()
        {
            if (pasteTarget == null)
                return;

            //string txt = System.Windows.Forms.Clipboard.GetText();
            string? txt = ClipboardUtils.GetText();
            if (string.IsNullOrEmpty(txt))
                return;

            XmlDocument doc = new XmlDocument();
            XmlNode? xmlNode = null;

            try
            {
                doc.LoadXml(txt);
                xmlNode = doc.FirstChild;
            }
            catch(Exception)
            {
                return;
            }

            if (xmlNode == null)
                return;

            // e.g. SdfObject in the clipboard cannot be pasted as RenderPassData
            if (xmlNode.Name != xmlNodeName)
                return;

            //SdfObject obj = new SdfObject(0, "", null);
            T obj = new T();
            bool result = obj.Deserialize(xmlNode, GetModel());
            if (!result)
                return;

            // Assign new identifiers
            TreeNode.CallRecursive(obj, delegate(TreeNode node)
            {
                //node.Id = GetModel().NextAvailableId;
                //GetModel().NextAvailableId++;
                assignIdentifiersDelegate(node);
            });

            AddPastedNode(pasteTarget, obj, insertIndex);

            if (typeof(T) == typeof(RenderPassData) ||
                typeof(T) == typeof(RenderingData))
            {
                if (obj is RenderPassData passData)
                {
                    passData.RefreshDefinitionReference(GetModel().Definitions.Renderers, GetModel().Definitions.Backdrops, GetModel().Definitions.CameraControllers);
                }

                ctx_.ReinitializeRenderPasses();
            }

            if (typeof(T) == typeof(SdfObject))
            {
                // the object may come from another project or refer to a deleted material
                GetModel().FixMaterialsReferences(obj);
                ctx_.ReinitializeShader();
            }

            if (typeof(T) == typeof(MaterialInstance))
            {
                (obj as MaterialInstance)?.RefreshDefinitionReference(GetModel().GetRenderPassForMaterials()?.RendererFunc?.Definition);
                //GetModel().FixMaterialsReferences();
                ctx_.ReinitializeShader();
            }

            // PrevVal of the deserialized values = current values, otherwise the first edit of the pasted node
            // would save an undo action back to the constructor defaults
            TreeNode.CallRecursive(obj, delegate(TreeNode node)
            {
                node.ResetPrevVal();
            });

            ctx_.Selection.ImportantNodeToSelect = obj;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(obj);
                },
                delegate
                {
                    // redo
                    AddPastedNode(pasteTarget, obj, insertIndex);

                    if (typeof(T) == typeof(RenderPassData) ||
                        typeof(T) == typeof(RenderingData))
                    {
                        if (obj is RenderPassData passData)
                        {
                            passData.RefreshDefinitionReference(GetModel().Definitions.Renderers, GetModel().Definitions.Backdrops, GetModel().Definitions.CameraControllers);
                        }

                        ctx_.ReinitializeRenderPasses();
                    }

                    if (typeof(T) == typeof(SdfObject))
                    {
                        GetModel().FixMaterialsReferences(obj);
                        ctx_.ReinitializeShader();
                    }

                    if (typeof(T) == typeof(MaterialInstance))
                    {
                        (obj as MaterialInstance)?.RefreshDefinitionReference(GetModel().GetRenderPassForMaterials()?.RendererFunc?.Definition);
                        //GetModel().FixMaterialsReferences();
                        ctx_.ReinitializeShader();
                    }
                }
                ));

        }

        private static void AddPastedNode(TreeNode pasteTarget, TreeNode node, int insertIndex)
        {
            if (insertIndex < 0 || insertIndex >= pasteTarget.GetChildrenCount())
                pasteTarget.AddChild(node);
            else
                pasteTarget.AddChildAtIndex(node, insertIndex);
        }

        public void OnPasteObject(SdfObject? pasteTarget)
        {
            OnPasteNode<SdfObject>(pasteTarget, -1, "SdfObject",
            delegate (TreeNode node)
            {
                node.Id = GetModel().NextAvailableId;
                GetModel().NextAvailableId++;
            });
        }



        public void OnMoveNodeUp(TreeNode? node)
        {
            if (node == null)
                return;

            nodeToMoveUp_ = node;
        }

        public void OnMoveNodeDown(TreeNode? node)
        {
            if (node == null)
                return;

            nodeToMoveDown_ = node;
        }

        public void OnAddRenderPass(RenderingData? parent, FunctionDefinition definition)
        {
            if (parent == null)
                return;

            RenderPassData newNode = new RenderPassData(GetModel().NextAvailableRenderPassId, String.Format("RenderPass{0}", GetModel().NextAvailableRenderPassId), definition, false);
            GetModel().NextAvailableRenderPassId++;

            parent.AddChild(newNode);

            //newNode.RendererFunc    ?.RefreshDefinitionReference(GetModel().Definitions.Renderers);
            ////newNode.CameraCtrlFunc  ?.RefreshDefinitionReference(GetModel().Definitions.CameraControllers);
            //newNode.CameraOperators ?.RefreshDefinitionReference(GetModel().Definitions.CameraControllers);
            newNode.RefreshDefinitionReference(GetModel().Definitions.Renderers, GetModel().Definitions.Backdrops, GetModel().Definitions.CameraControllers);

            ctx_.ReinitializeRenderPasses();

            //ctx_.ReinitializeShader();

            ctx_.Selection.ImportantNodeToSelect = newNode;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(newNode);
                    ctx_.Selection.ImportantNodeToSelect = null;
                },
                delegate
                {
                    // redo
                    parent.AddChild(newNode);
                    ctx_.ReinitializeRenderPasses();
                }
                ));
        }

        public void OnCopyRenderPass(RenderPassData? node)
        {
            OnCopyNode(node);
        }

        public void OnCutRenderPass(RenderPassData? node)
        {
            OnCutNode(node);
        }

        // Paste position in a flat collection (render passes, materials):
        // selectedNode is a child of collection - right after it, selectedNode is the collection - at the end (-1),
        // otherwise - null (nothing to paste)
        private static int? GetPasteIndexInCollection(TreeNode collection, TreeNode? selectedNode)
        {
            if (selectedNode == collection)
                return -1;

            if (selectedNode != null && selectedNode.Parent == collection)
                return collection.GetChildrenIndex(selectedNode) + 1;

            return null;
        }

        // selectedNode: RenderPassData - the pass is pasted right after it (pass order is the rendering order),
        //               RenderingData  - the pass is appended at the end
        public void OnPasteRenderPass(TreeNode? selectedNode)
        {
            RenderingData renderingData = GetModel().RenderingSysData;

            int? insertIndex = GetPasteIndexInCollection(renderingData, selectedNode);
            if (insertIndex == null)
                return;

            OnPasteNode<RenderPassData>(renderingData, insertIndex.Value, "RenderPassData",
            delegate (TreeNode node)
            {
                node.Id = GetModel().NextAvailableRenderPassId;
                GetModel().NextAvailableRenderPassId++;
            });
        }

        public void OnAddNewMaterial()
        {
            MaterialInstance newMat = GetModel().AddNewMaterial("New material " + GetModel().NextAvailableMaterialId);
            RefreshAddedMaterial(newMat);

            ctx_.Selection.ImportantNodeToSelect = newMat;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(newMat);
                },
                delegate
                {
                    // redo
                    GetModel().Materials.AddChild(newMat);
                    RefreshAddedMaterial(newMat);
                }
                ));
        }

        private void RefreshAddedMaterial(MaterialInstance mat)
        {
            mat.RefreshDefinitionReference(GetModel().GetRenderPassForMaterials()?.RendererFunc?.Definition);
            ctx_.ReinitializeShader();
        }

        public void OnCopyMaterial(MaterialInstance? node)
        {
            OnCopyNode(node);
        }

        public void OnCutMaterial(MaterialInstance? node)
        {
            OnCutNode(node);
        }

        // selectedNode: MaterialInstance    - the material is pasted right after it,
        //               MaterialsCollection - the material is appended at the end
        public void OnPasteMaterial(TreeNode? selectedNode)
        {
            MaterialsCollection materials = GetModel().Materials;

            int? insertIndex = GetPasteIndexInCollection(materials, selectedNode);
            if (insertIndex == null)
                return;

            // Objects reference materials by id, so the pasted material is a new one (new id)
            OnPasteNode<MaterialInstance>(materials, insertIndex.Value, "Material",
            delegate (TreeNode node)
            {
                node.Id = GetModel().NextAvailableMaterialId;
                GetModel().NextAvailableMaterialId++;
            });
        }

        public void OnAddNewSignal()
        {
            SignalInstance newSignal = GetModel().AddNewSignal("Signal " + GetModel().NextAvailableSignalId);
            newSignal.ResetPrevVal();

            ctx_.Selection.ImportantNodeToSelect = newSignal;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(newSignal);
                },
                delegate
                {
                    // redo
                    GetModel().Signals.AddChild(newSignal);
                }
                ));
        }

        public void OnCopySignal(SignalInstance? node)
        {
            OnCopyNode(node);
        }

        public void OnCutSignal(SignalInstance? node)
        {
            OnCutNode(node);
        }

        // selectedNode: SignalInstance    - the signal is pasted right after it,
        //               SignalsCollection - the signal is appended at the end
        public void OnPasteSignal(TreeNode? selectedNode)
        {
            SignalsCollection signals = GetModel().Signals;

            int? insertIndex = GetPasteIndexInCollection(signals, selectedNode);
            if (insertIndex == null)
                return;

            // Parameters reference signals by id, so the pasted signal is a new one (new id)
            OnPasteNode<SignalInstance>(signals, insertIndex.Value, "Signal",
            delegate (TreeNode node)
            {
                node.Id = GetModel().NextAvailableSignalId;
                GetModel().NextAvailableSignalId++;
            });
        }

        public void HandleDeleteNode()
        {
            TreeNode? node = nodeToDelete_;
            if (node == null)
                return;

            nodeToDelete_ = null;

            if (!CanDeleteNode(node))
                return;

            TreeNode? oryginalParent = node.Parent;
            if (oryginalParent == null)
                return;
            int oryginalIndex = oryginalParent.GetChildrenIndex(node);

            // Objects using a deleted material are switched to the default one (FixMaterialsReferences),
            // so they have to be remembered to restore their material on undo
            List<SdfObject> materialUsers = (node is MaterialInstance mat) ? FindMaterialUsers(mat.Id) : new List<SdfObject>();
            int deletedMaterialId = node.Id;

            DeleteNodeInternal(node);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    oryginalParent.AddChildAtIndex(node, oryginalIndex);

                    foreach(SdfObject sdfObj in materialUsers)
                        sdfObj.MaterialId.Val = deletedMaterialId;

                    ctx_.ReinitializeShader();

                    if (node is RenderPassData)
                        ctx_.ReinitializeRenderPasses();

                },
                delegate
                {
                    // redo
                    DeleteNodeInternal(node);
                }
                ) );

        }

        private List<SdfObject> FindMaterialUsers(int materialId)
        {
            List<SdfObject> users = new List<SdfObject>();

            TreeNode.CallRecursive(GetModel().SdfRoot, delegate(TreeNode node)
            {
                if (node is SdfObject sdfObj && (int)sdfObj.MaterialId.Val == materialId)
                    users.Add(sdfObj);
            });

            return users;
        }

        private bool CanDeleteNode(TreeNode node)
        {
            // the root cannot be deleted
            if (node.Parent == null)
                return false;

            // Only selected node types can be deleted
            if (!((node is SdfObject) || (node is MaterialInstance) || (node is RenderPassData) || (node is SignalInstance)))
                return false;

            // the SDF root cannot be deleted
            if (node is SdfObject sdfObject)
            {
                if (sdfObject.ParentAsSdf == null)
                    return false;
            }

            if (node is MaterialInstance)
            {
                // the last material cannot be deleted
                if (GetModel().Materials.GetChildrenCount() <= 1)
                    return false;
            }

            return true;
        }

        private void DeleteNodeInternal(TreeNode? node)
        {
            if (node == null || node.Parent == null)
                return;

            //node.Parent.Children.Remove(node);
            node.Parent.RemoveChild(node);

            if (node is RenderPassData)
                ctx_.ReinitializeRenderPasses();

            if (node is SdfObject)
                ctx_.ReinitializeShader();

            if (node is MaterialInstance)
            {
                GetModel().FixMaterialsReferences();
                ctx_.ReinitializeShader();
            }
        }

        public void HandleMoveNodeUp()
        {
            TreeNode? node = nodeToMoveUp_;
            if (node == null)
                return;
            nodeToMoveUp_ = null;

            if (node.Parent == null)
                return;

            // first node - nothing to move, so nothing to undo either
            if (!node.Parent.MoveChildUp(node))
                return;

            if (node is RenderPassData)
                ctx_.ReinitializeRenderPasses();

            if (node is SdfObject || node is MaterialInstance)
                ctx_.ReinitializeShader();

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        ctx_.ReinitializeRenderPasses();

                    if (node is SdfObject || node is MaterialInstance)
                        ctx_.ReinitializeShader();
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        ctx_.ReinitializeRenderPasses();

                    if (node is SdfObject || node is MaterialInstance)
                        ctx_.ReinitializeShader();
                }
                ));
        }

        public void HandleMoveNodeDown()
        {
            TreeNode? node = nodeToMoveDown_;
            if (node == null)
                return;
            nodeToMoveDown_ = null;

            if (node.Parent == null)
                return;

            // last node - nothing to move, so nothing to undo either
            if (!node.Parent.MoveChildDown(node))
                return;

            if (node is RenderPassData)
                ctx_.ReinitializeRenderPasses();

            if (node is SdfObject || node is MaterialInstance)
                ctx_.ReinitializeShader();

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        ctx_.ReinitializeRenderPasses();

                    if (node is SdfObject || node is MaterialInstance)
                        ctx_.ReinitializeShader();
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        ctx_.ReinitializeRenderPasses();

                    if (node is SdfObject || node is MaterialInstance)
                        ctx_.ReinitializeShader();
                }
                ));

        }

    }
}
