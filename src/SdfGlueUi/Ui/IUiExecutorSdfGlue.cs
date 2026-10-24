//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppCore.Model.DataNodes;
using SingleDocAppFramework.Ui;
using System.Text;

namespace SdfGlueUi.Ui
{
    // SdfGlue-specific UI actions (generic ones are inherited from IUiExecutorFramework)
    public interface IUiExecutorSdfGlue : IUiExecutorFramework
    {
        DataModel GetModel();
        // State of the application (settings, playback, selection...); GetModel() == GetAppContext().Document
        SdfGlueAppContextBase GetAppContext();

        void OnReloadSdfDefinitions();
        void OnRebuildShader();
        void OnRebuildPreviewTexture();
        void OnResetFrameCounter();
        void OnPreviewResolutionChanged();
        void OnBatchProcessAllProjects();
        void OnSaveImage();

        // Image sequence export (File > Export image sequence..., DlgExportAnimation) with the settings from
        // UserSettingsSdfGlue.ExportAnimSettings, done in steps (one per frame).
        // Start returns false if the export was not started (errors are shown to the user).
        bool                    StartAnimationExport        ();
        void                    CancelAnimationExport       ();
        AnimationExportProgress GetAnimationExportProgress  ();
        // null when cancelled
        string?                 SelectFolder                ();

        // Code of the pass for an export target, generated on demand (null on error - see errors)
        string? GenerateExportCode  (ShaderExportTarget target, RenderPassData pass, StringBuilder errors);
        // Export of the first primary pass (File -> Export)
        void OnExportShaderToClipboard  (ShaderExportTarget target);
        void OnExportShaderToFile       (ShaderExportTarget target);
        // Development: exports and compiles (Shadertoy) all projects
        void OnValidateShaderExports    ();

        void OnDeleteNode           (TreeNode? node);
        void OnMoveNodeUp           (TreeNode? node);
        void OnMoveNodeDown         (TreeNode? node);
        void OnAddChildObject       (SdfObject? node, FunctionDefinition definition);
        void OnCopyObject           (SdfObject? node);
        void OnCutObject            (SdfObject? node);
        void OnPasteObject          (SdfObject? node);
        void OnFocusObject          (SdfObject? node);

        void OnAddRenderPass        (RenderingData? parent, FunctionDefinition definition);
        void OnCutRenderPass        (RenderPassData? node);
        void OnCopyRenderPass       (RenderPassData? node);
        void OnPasteRenderPass      (TreeNode? selectedNode);

        void OnAddNewMaterial       ();
        void OnCutMaterial          (MaterialInstance? node);
        void OnCopyMaterial         (MaterialInstance? node);
        void OnPasteMaterial        (TreeNode? selectedNode);

        void OnAddNewSignal         ();
        void OnCutSignal            (SignalInstance? node);
        void OnCopySignal           (SignalInstance? node);
        void OnPasteSignal          (TreeNode? selectedNode);
    }
}
