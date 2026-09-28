//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueEditor.Application;
using SdfGlueUi.Ui;
using SingleDocAppFramework;
using TreeNode = SingleDocAppCore.Model.DataNodes.TreeNode;

namespace SdfGlueEditor
{
    // Facade executing UI actions for SdfGlue.
    // Generic actions come from UiExecutorFrameworkBase, SdfGlue-specific ones are delegated to controllers.
    public class UiExecutorSdfGlue : UiExecutorFrameworkBase, IUiExecutorSdfGlue
    {
        private SdfGlueAppContext               ctx_;
        private DocumentController              documents_;
        private RenderController                render_;
        private CameraController                camera_;
        private ProjectHierarchyController      hierarchy_;

        public UiExecutorSdfGlue(SdfGlueAppContext ctx, DocumentController documents, RenderController render, CameraController camera, ProjectHierarchyController hierarchy)
            : base(ctx.Window)
        {
            ctx_        = ctx;
            documents_  = documents;
            render_     = render;
            camera_     = camera;
            hierarchy_  = hierarchy;
        }

        // document
        public override void OnNewDocument      ()                  { documents_.NewProject();              }
        public override void OnOpenDocument     ()                  { documents_.OpenProject();             }
        public override void OnOpenDocument     (string filePath)   { documents_.OpenProject(filePath);     }
        public override void OnSaveDocument     ()                  { documents_.SaveProject();             }
        public override void OnSaveDocumentAs   ()                  { documents_.SaveProjectAs();           }
        public override bool CanSaveDocument    ()                  { return documents_.HasProjectFilePath(); }

        public DataModel GetModel               ()                  { return ctx_.GetModel();               }
        public void OnBatchProcessAllProjects   ()                  { documents_.BatchProcessAllProjects(); }

        // rendering
        public void OnReloadSdfDefinitions      ()                  { render_.ReloadSdfDefinitions();       }
        public void OnRebuildShader             ()                  { render_.RebuildShader();              }
        public void OnRebuildPreviewTexture     ()                  { render_.PreviewResolutionChanged();   }
        public void OnResetFrameCounter         ()                  { render_.ResetFrameCounter();          }
        public void OnPreviewResolutionChanged  ()                  { render_.PreviewResolutionChanged();   }
        public void OnSaveImage                 ()                  { render_.SaveImage();                  }
        public void OnExportAnimation           ()                  { render_.ExportAnimation();            }

        // camera
        public void OnFocusObject       (SdfObject? node)                                   { camera_.FocusObject(node);                        }

        // project hierarchy
        public void OnDeleteNode        (TreeNode? node)                                    { hierarchy_.OnDeleteNode(node);                    }
        public void OnMoveNodeUp        (TreeNode? node)                                    { hierarchy_.OnMoveNodeUp(node);                    }
        public void OnMoveNodeDown      (TreeNode? node)                                    { hierarchy_.OnMoveNodeDown(node);                  }
        public void OnAddChildObject    (SdfObject? node, FunctionDefinition definition)    { hierarchy_.OnAddChildObject(node, definition);    }
        public void OnCopyObject        (SdfObject? node)                                   { hierarchy_.OnCopyObject(node);                    }
        public void OnCutObject         (SdfObject? node)                                   { hierarchy_.OnCutObject(node);                     }
        public void OnPasteObject       (SdfObject? node)                                   { hierarchy_.OnPasteObject(node);                   }

        public void OnAddRenderPass     (RenderingData? parent, FunctionDefinition definition) { hierarchy_.OnAddRenderPass(parent, definition); }
        public void OnCutRenderPass     (RenderPassData? node)                              { hierarchy_.OnCutRenderPass(node);                 }
        public void OnCopyRenderPass    (RenderPassData? node)                              { hierarchy_.OnCopyRenderPass(node);                }
        public void OnPasteRenderPass   (TreeNode? selectedNode)                            { hierarchy_.OnPasteRenderPass(selectedNode);       }

        public void OnCutMaterial       (MaterialInstance? node)                            { hierarchy_.OnCutMaterial(node);                   }
        public void OnCopyMaterial      (MaterialInstance? node)                            { hierarchy_.OnCopyMaterial(node);                  }
        public void OnPasteMaterial     (TreeNode? selectedNode)                            { hierarchy_.OnPasteMaterial(selectedNode);         }
    }
}
