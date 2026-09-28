//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.UndoSystem;
using SingleDocAppCore.UndoSystem.Actions;
using SingleDocAppFramework.Platform;
using System.Xml;
using TreeNode = SingleDocAppCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor.Application
{
    public class ProjectHierarchyController
    {
        private readonly IRenderingSystem       renderingSystem_;
        private readonly ShaderCodeGenerator    codeGenerator_;

        public ProjectHierarchyController(IRenderingSystem renderingSystem, ShaderCodeGenerator codeGenerator)
        {
            renderingSystem_    = renderingSystem;
            codeGenerator_      = codeGenerator;
        }

        private DataModel GetModel()
        {
            return codeGenerator_.GetModel();
        }


        public void OnAddChildObject(SdfObject? node, FunctionDefinition definition)
        {
            if (node == null)
                return;

            SdfObject newNode = new SdfObject(GetModel().NextAvailableId, String.Format("Object{0}", GetModel().NextAvailableId), definition);
            GetModel().NextAvailableId++;
            node.AddChild(newNode);

            newNode.FixMaterialReference(GetModel());

            newNode.FunctionSdf         .RefreshDefinitionReference(GetModel().SdfDefinitions);
            newNode.FunctionMixOp       .RefreshDefinitionReference(GetModel().MixOpDefinitions);
            newNode.PositionOperators   .RefreshDefinitionReference(GetModel().PositionOpDefinitions);
            newNode.DistanceOperators   .RefreshDefinitionReference(GetModel().DistanceOpDefinitions);

            renderingSystem_.ReinitializeShader(codeGenerator_);

            GetModel().ImportantNodeToSelect = newNode;

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
                    renderingSystem_.ReinitializeShader(codeGenerator_);
                }
                ));
        }

        public void OnDeleteNode(TreeNode? node)
        {
            if (node == null)
                return;

            if (!CanDeleteNode(node))
                return;

            GetModel().NodeToDelete = node;

            GetModel().SelectedNode = null;
            GetModel().ImportantNodeToSelect = node.Parent;
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
                    passData.RefreshDefinitionReference(GetModel().Renderers, GetModel().BackdropsDefinitions, GetModel().CameraControllers);
                }

                renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            }

            if (typeof(T) == typeof(SdfObject))
            {
                renderingSystem_.ReinitializeShader(codeGenerator_);
            }

            if (typeof(T) == typeof(MaterialInstance))
            {
                (obj as MaterialInstance)?.RefreshDefinitionReference(GetModel().GetRenderPassForMaterials()?.RendererFunc?.Definition);
                //GetModel().FixMaterialsReferences();
                renderingSystem_.ReinitializeShader(codeGenerator_);
            }

            GetModel().ImportantNodeToSelect = obj;

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
                            passData.RefreshDefinitionReference(GetModel().Renderers, GetModel().BackdropsDefinitions, GetModel().CameraControllers);
                        }

                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
                    }

                    if (typeof(T) == typeof(SdfObject))
                    {
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                    }

                    if (typeof(T) == typeof(MaterialInstance))
                    {
                        (obj as MaterialInstance)?.RefreshDefinitionReference(GetModel().GetRenderPassForMaterials()?.RendererFunc?.Definition);
                        //GetModel().FixMaterialsReferences();
                        renderingSystem_.ReinitializeShader(codeGenerator_);
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

            GetModel().NodeToMoveUp = node;
        }

        public void OnMoveNodeDown(TreeNode? node)
        {
            if (node == null)
                return;

            GetModel().NodeToMoveDown = node;
        }

        public void OnAddRenderPass(RenderingData? parent, FunctionDefinition definition)
        {
            if (parent == null)
                return;

            RenderPassData newNode = new RenderPassData(GetModel().NextAvailableRenderPassId, String.Format("RenderPass{0}", GetModel().NextAvailableRenderPassId), definition, false);
            GetModel().NextAvailableRenderPassId++;

            parent.AddChild(newNode);

            //newNode.RendererFunc    ?.RefreshDefinitionReference(GetModel().Renderers);
            ////newNode.CameraCtrlFunc  ?.RefreshDefinitionReference(GetModel().CameraControllers);
            //newNode.CameraOperators ?.RefreshDefinitionReference(GetModel().CameraControllers);
            newNode.RefreshDefinitionReference(GetModel().Renderers, GetModel().BackdropsDefinitions, GetModel().CameraControllers);

            renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

            //renderingSystem_.ReinitializeShader(codeGenerator_);

            GetModel().ImportantNodeToSelect = newNode;

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    DeleteNodeInternal(newNode);
                    GetModel().ImportantNodeToSelect = null;
                },
                delegate
                {
                    // redo
                    parent.AddChild(newNode);
                    renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
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

        public void HandleDeleteNode()
        {
            TreeNode? node = GetModel().NodeToDelete;
            if (node == null)
                return;

            GetModel().NodeToDelete = null;

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

                    renderingSystem_.ReinitializeShader(codeGenerator_);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

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
            // nie pozwalamy usunąć roota
            if (node.Parent == null)
                return false;

            // Tylko wybrane typy węzłów można usuwać
            if (!((node is SdfObject) || (node is MaterialInstance) || (node is RenderPassData)))
                return false;

            // nie pozwalamy usunąć roota SDF
            if (node is SdfObject sdfObject)
            {
                if (sdfObject.ParentAsSdf == null)
                    return false;
            }

            if (node is MaterialInstance)
            {
                // nie pozwalamy usunąć ostatniego materiału
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
                renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

            if (node is SdfObject)
                renderingSystem_.ReinitializeShader(codeGenerator_);

            if (node is MaterialInstance)
            {
                GetModel().FixMaterialsReferences();
                renderingSystem_.ReinitializeShader(codeGenerator_);
            }
        }

        public void HandleMoveNodeUp()
        {
            TreeNode? node = GetModel().NodeToMoveUp;
            if (node == null)
                return;
            GetModel().NodeToMoveUp = null;

            if (node.Parent == null)
                return;
            node.Parent.MoveChildUp(node);

            if (node is RenderPassData)
                renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

            if (node is SdfObject || node is MaterialInstance)
                renderingSystem_.ReinitializeShader(codeGenerator_);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject || node is MaterialInstance)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject || node is MaterialInstance)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                }
                ));
        }

        public void HandleMoveNodeDown()
        {
            TreeNode? node = GetModel().NodeToMoveDown;
            if (node == null)
                return;
            GetModel().NodeToMoveDown = null;

            if (node.Parent == null)
                return;
            node.Parent.MoveChildDown(node);

            if (node is RenderPassData)
                renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

            if (node is SdfObject || node is MaterialInstance)
                renderingSystem_.ReinitializeShader(codeGenerator_);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject || node is MaterialInstance)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject || node is MaterialInstance)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                }
                ));

        }

    }
}
