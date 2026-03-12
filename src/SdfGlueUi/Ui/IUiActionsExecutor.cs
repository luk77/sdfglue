//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueUi.Input;
using SdfGlueUi.Layouts;

namespace SdfGlueUi.Ui
{
    public interface IUiActionsExecutor
    {
        void OnReloadSdfDefinitions();
        void OnRebuildShader();
        void OnRebuildPreviewTexture();
        void OnResetFrameCounter();
        //void OnRendererChanged();
        void OnPreviewResolutionChanged();
        void OnBatchProcessAllProjects();
        DataModel GetModel();
        void OnNewProject();
        void OnOpenProject();
        void OnOpenProject(string path);
        void OnSaveProject();
        void OnSaveProjectAs();
        void OnSaveImage();
        void OnExportAnimation();
        void OnExitApp();
        void OnDeleteNode           (TreeNode node);
        void OnMoveNodeUp           (TreeNode node);
        void OnMoveNodeDown         (TreeNode node);
        void OnAddChildObject       (SdfObject node, FunctionDefinition definition);
        void OnCopyObject           (SdfObject node);
        void OnCutObject            (SdfObject node);
        void OnPasteObject          (SdfObject node);
        void OnFocusObject          (SdfObject node);
        void CopyTextToClipboard    (string text);
        float GetWindowsScaling     ();

        void OnAddRenderPass        (RenderingData parent, FunctionDefinition definition);
        void OnCutRenderPass        (RenderPassData node);
        void OnCopyRenderPass       (RenderPassData node);
        void OnPasteRenderPass      ();

        void OnLoadLayout           (string layoutFilePath);
        void OnSaveCurrentLayout    (WindowsVisibilityCollection windowsVisibility);

        // input (shortcuts, navigation, etc.)
        bool    IsKeyDown           (UiKey key);
        bool    IsKeyPressed        (UiKey key);
        bool    IsDownAnyCtrl       ();
        bool    IsDownAnyShift      ();
        bool    IsDownAnyAlt        ();
        float   GetWheelPrecise     ();
        bool    IsRmbDown           ();
        bool    IsLmbDown           ();
        int     GetMouseStateX      ();
        int     GetMouseStateY      ();

    }
}
