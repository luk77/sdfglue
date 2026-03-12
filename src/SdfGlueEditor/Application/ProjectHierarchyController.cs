//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.AbstractRenderer;
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.UndoSystem;
using SdfGlueCore.UndoSystem.Actions;
using SdfGlueEditor.Platform;
using System.Xml;
using TreeNode = SdfGlueCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor.Application
{
    public class ProjectHierarchyController
    {
        private DataModel?              model_                  = null;
        private IRenderingSystem?       renderingSystem_        = null;
        private ShaderCodeGenerator?    codeGenerator_          = null;

        public ProjectHierarchyController(DataModel  model, IRenderingSystem renderingSystem, ShaderCodeGenerator codeGenerator)
        {
            model_              = model;
            renderingSystem_    = renderingSystem;
            codeGenerator_      = codeGenerator;
        }

        private DataModel? GetModel()
        {
            return model_;
        }


        public void OnAddChildObject(SdfObject node, FunctionDefinition definition)
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

        public void OnDeleteNode(TreeNode node)
        {
            if (node == null)
                return;

            if (!CanDeleteNode(node))
                return;

            GetModel().NodeToDelete = node;

            GetModel().SelectedNode = null;
            GetModel().ImportantNodeToSelect = node.Parent;
        }

        public void OnCopyObject(SdfObject node)
        {
            OnCopyNode(node);
        }

        private void OnCopyNode(SerializableNode node)
        {
            if (node == null)
                return;

            XmlDocument doc = new XmlDocument();

            node.Serialize(doc, doc);

            string txt = doc.InnerXml.ToString();

            //new SetClipboardHelper(System.Windows.Forms.DataFormats.Text, txt).Go();
            ClipboardHelper.SetTextToClipboard(txt);
        }

        public void OnCutObject(SdfObject node)
        {
            OnCutNode(node);
        }
        private void OnCutNode(SerializableNode node)
        {
            if (node == null)
                return;

            OnCopyNode(node);

            OnDeleteNode(node);
        }

        public delegate void AssignIdentifiersDelegate(TreeNode node);

        private void OnPasteNode<T>(TreeNode pasteTarget, AssignIdentifiersDelegate assignIdentifiersDelegate) where T : SerializableNode, new()
        {
            if (pasteTarget == null)
                return;

            //string txt = System.Windows.Forms.Clipboard.GetText();
            string txt = ClipboardHelper.GetTextFromClipboard();
            XmlDocument doc = new XmlDocument();
            XmlNode xmlNode = null;

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

            pasteTarget.AddChild(obj);

            if (typeof(T) == typeof(RenderPassData) ||
                typeof(T) == typeof(RenderingData))
            {
                RenderPassData passData = obj as RenderPassData;

                passData.RendererFunc    ?.RefreshDefinitionReference(GetModel().Renderers);
                //passData.CameraCtrlFunc  ?.RefreshDefinitionReference(GetModel().CameraControllers);
                passData.CameraOperators ?.RefreshDefinitionReference(GetModel().CameraControllers);

                renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
            }

            if (typeof(T) == typeof(SdfObject))
            {
                renderingSystem_.ReinitializeShader(codeGenerator_);
            }

            if (typeof(T) == typeof(MaterialInstance))
            {
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
                    pasteTarget.AddChild(obj);
                    
                    if (typeof(T) == typeof(RenderPassData) ||
                        typeof(T) == typeof(RenderingData))
                    {
                        RenderPassData passData = obj as RenderPassData;

                        passData.RendererFunc    ?.RefreshDefinitionReference(GetModel().Renderers);
                        //passData.CameraCtrlFunc  ?.RefreshDefinitionReference(GetModel().CameraControllers);
                        passData.CameraOperators ?.RefreshDefinitionReference(GetModel().CameraControllers);

                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());
                    }

                    if (typeof(T) == typeof(SdfObject))
                    {
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                    }

                    if (typeof(T) == typeof(MaterialInstance))
                    {
                        //GetModel().FixMaterialsReferences();
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                    }
                }
                ));

        }

        public void OnPasteObject(SdfObject pasteTarget)
        {
            OnPasteNode<SdfObject>(pasteTarget,
            delegate (TreeNode node)
            {
                node.Id = GetModel().NextAvailableId;
                GetModel().NextAvailableId++;
            });
        }



        public void OnMoveNodeUp(TreeNode node)
        {
            if (node == null)
                return;

            GetModel().NodeToMoveUp = node;
        }

        public void OnMoveNodeDown(TreeNode node)
        {
            if (node == null)
                return;

            GetModel().NodeToMoveDown = node;
        }

        public void OnAddRenderPass(RenderingData parent, FunctionDefinition definition)
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

        public void OnCopyRenderPass(RenderPassData node)
        {
            // TODO: to wymaga dokończenia:
            //OnCopyNode(node);
        }

        public void OnCutRenderPass(RenderPassData node)
        {
            // TODO: to wymaga dokończenia:
            //OnCutNode(node);
        }

        public void OnPasteRenderPass()
        {
            // TODO: to wymaga dokończenia:
            // jest crash gdy skopiujemy niekompatybilny obiekt (SdfObject) jako RenderPassData

            //OnPasteNode<RenderPassData>(GetModel().RenderingSysData,
            //delegate (TreeNode node)
            //{
            //    node.Id = GetModel().NextAvailableRenderPassId;
            //    GetModel().NextAvailableRenderPassId++;
            //});
        }

        public void HandleDeleteNode()
        {
            TreeNode? node = GetModel().NodeToDelete;
            if (node == null)
                return;

            GetModel().NodeToDelete = null;

            if (!CanDeleteNode(node))
                return;

            TreeNode oryginalParent = node.Parent;
            int oryginalIndex = node.Parent.GetChildrenIndex(node);

            DeleteNodeInternal(node);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    oryginalParent.AddChildAtIndex(node, oryginalIndex); 

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

        private bool CanDeleteNode(TreeNode node)
        {
            // nie pozwalamy usunąć roota
            if (node.Parent == null)
                return false;

            // Tylko wybrane typy węzłów można usuwać
            if (!((node is SdfObject) || (node is MaterialInstance) || (node is RenderPassData)))
                return false;

            // nie pozwalamy usunąć roota SDF
            if (node is SdfObject)
            {
                if ((node as SdfObject).ParentAsSdf == null)
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

            if (node is SdfObject)
                renderingSystem_.ReinitializeShader(codeGenerator_);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject)
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

            if (node is SdfObject)
                renderingSystem_.ReinitializeShader(codeGenerator_);

            // undo/redo support
            UndoManager.Instance.SaveAction(new ActionDelegates(
                delegate
                {
                    // undo
                    node.Parent.MoveChildUp(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                },
                delegate
                {
                    // redo
                    node.Parent.MoveChildDown(node);

                    if (node is RenderPassData)
                        renderingSystem_.Reinitialize(GetModel().RenderingSysData, codeGenerator_, GetModel().Config.GetPreviewResolution());

                    if (node is SdfObject)
                        renderingSystem_.ReinitializeShader(codeGenerator_);
                }
                ));

        }

    }
}
