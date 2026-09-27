//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppFramework.Ui;

namespace SdfGlueUi.Ui
{
    // SdfGlue-specific UI actions (generic ones are inherited from IUiExecutorFramework)
    public interface IUiExecutorSdfGlue : IUiExecutorFramework
    {
        DataModel GetModel();

        void OnReloadSdfDefinitions();
        void OnRebuildShader();
        void OnRebuildPreviewTexture();
        void OnResetFrameCounter();
        void OnPreviewResolutionChanged();
        void OnBatchProcessAllProjects();
        void OnSaveImage();
        void OnExportAnimation();

        void OnDeleteNode           (TreeNode node);
        void OnMoveNodeUp           (TreeNode node);
        void OnMoveNodeDown         (TreeNode node);
        void OnAddChildObject       (SdfObject node, FunctionDefinition definition);
        void OnCopyObject           (SdfObject node);
        void OnCutObject            (SdfObject node);
        void OnPasteObject          (SdfObject node);
        void OnFocusObject          (SdfObject node);

        void OnAddRenderPass        (RenderingData parent, FunctionDefinition definition);
        void OnCutRenderPass        (RenderPassData node);
        void OnCopyRenderPass       (RenderPassData node);
        void OnPasteRenderPass      ();
    }
}
