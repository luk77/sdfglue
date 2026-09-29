//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SingleDocAppCore.UndoSystem;
using System.Xml;

namespace SdfGlueEditor.Application
{
    // Project (document) operations: new, open, save, batch processing
    public class DocumentController
    {
        private SdfGlueAppContext       ctx_;

        public DocumentController(SdfGlueAppContext ctx)
        {
            ctx_ = ctx;
        }

        private DataModel GetModel()
        {
            return ctx_.GetModel();
        }

        public string GetDocumentDisplayName()
        {
            string projectPath = "<Unnamed project>";
            string? filePath = GetModel().ProjectFilePath;
            if (!String.IsNullOrEmpty(filePath))
                projectPath = filePath;

            return "Project: " + projectPath;
        }

        public bool HasProjectFilePath()
        {
            return !String.IsNullOrEmpty(GetModel().ProjectFilePath);
        }

        // Asks whether to save unsaved changes; false - the operation should be cancelled
        public bool ConfirmDiscardChanges()
        {
            return ctx_.Window.Executor.ConfirmDiscardChanges();
        }

        public void NewProject()
        {
            if (!ConfirmDiscardChanges())
                return;

            SetCurrentModel(new DataModel(ctx_.Settings));
        }

        public void OpenProject()
        {
            if (!ConfirmDiscardChanges())
                return;

            string? filePath = ctx_.Platform.OpenFileDialog(ctx_.Window.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            LoadProject(filePath, true);
        }

        public void OpenProject(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return;

            if (!ConfirmDiscardChanges())
                return;

            LoadProject(filePath, true);
        }

        // Opens a project without asking about unsaved changes (see ConfirmDiscardChanges()).
        // The project is loaded into a new model - on failure the current project stays untouched.
        public bool LoadProject(string filePath, bool showErrorMessage)
        {
            string errorMessage;
            DataModel? model = LoadProjectFromFile(filePath, out errorMessage);
            if (model == null)
            {
                Console.WriteLine("ERROR: Cannot open project: {0}. {1}", filePath, errorMessage);
                if (showErrorMessage)
                    ctx_.Platform.ShowErrorMessage("Open project", String.Format("Cannot open project:\n{0}\n\n{1}", filePath, errorMessage));
                return false;
            }

            model.ProjectFilePath = filePath;
            SetCurrentModel(model);
            return true;
        }

        private void SetCurrentModel(DataModel model)
        {
            ctx_.CodeGenerator.SetModel(model);

            // undo actions refer to the objects of the previous model
            UndoManager.Instance.ClearAll();

            ctx_.ReinitializeRenderingSystem();

            ctx_.Window.RefreshWindowTitle();
        }

        public void SaveProject()
        {
            string? filePath = GetModel().ProjectFilePath;
            if (String.IsNullOrEmpty(filePath))
                return;

            if (SaveProjectToFile(GetModel(), filePath, true))
                UndoManager.Instance.MarkSavePoint();

            ctx_.Window.RefreshWindowTitle();
        }

        public void SaveProjectAs()
        {
            string? filePath = ctx_.Platform.SaveFileDialog(ctx_.Window.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            if (SaveProjectToFile(GetModel(), filePath, true))
            {
                GetModel().ProjectFilePath = filePath;
                UndoManager.Instance.MarkSavePoint();
            }

            ctx_.Window.RefreshWindowTitle();
        }

        private static readonly string[]    BatchProjectDirectories     = { "Projects", "Examples" };
        private static readonly string      ProjectSearchPattern        = "*.xml";

        // Loads and re-saves all projects (upgrades them to the current file format).
        // Every project is loaded into its own model - the current project is not changed.
        // Files that cannot be loaded are not saved.
        public void BatchProcessAllProjects()
        {
            List<string> files = new List<string>();
            foreach (string rootPath in BatchProjectDirectories)
            {
                if (Directory.Exists(rootPath))
                    files.AddRange(Directory.GetFiles(rootPath, ProjectSearchPattern, SearchOption.AllDirectories));
            }

            if (files.Count == 0)
            {
                Console.WriteLine("Batch processing: no project files found in: {0}", String.Join(", ", BatchProjectDirectories));
                return;
            }

            string question = String.Format("{0} project files in the folders: {1} will be loaded and saved in the current file format.\n" +
                                            "Files that cannot be loaded are skipped. There is no backup copy.\n\nContinue?",
                                            files.Count, String.Join(", ", BatchProjectDirectories));
            if (!ctx_.Platform.AskOkCancel("Batch process all projects", question))
                return;

            List<string> failedFiles = new List<string>();
            int savedCount = 0;
            foreach (string path in files)
            {
                Console.WriteLine("Processing project: {0}", path);

                string errorMessage;
                DataModel? model = LoadProjectFromFile(path, out errorMessage);
                if (model == null)
                {
                    Console.WriteLine("ERROR: Cannot load project: {0}. {1} The file is skipped.", path, errorMessage);
                    failedFiles.Add(path);
                    continue;
                }

                if (!SaveProjectToFile(model, path, false))
                {
                    failedFiles.Add(path);
                    continue;
                }

                savedCount++;
            }

            Console.WriteLine("Batch processing finished. Saved: {0}, failed: {1}", savedCount, failedFiles.Count);

            if (failedFiles.Count > 0)
            {
                ctx_.Platform.ShowErrorMessage("Batch process all projects",
                    String.Format("Saved: {0}, failed: {1} (not modified, details in the log):\n\n{2}",
                                  savedCount, failedFiles.Count, String.Join("\n", failedFiles.Take(20)) + (failedFiles.Count > 20 ? "\n..." : "")));
            }
        }

        // Loads a project into a new model (function definitions are shared with the current model).
        // Returns null on error (invalid XML, missing data).
        private DataModel? LoadProjectFromFile(string filePath, out string errorMessage)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                DataModel model = new DataModel(ctx_.Settings, GetModel());
                if (!model.Deserialize(xmlDoc))
                {
                    errorMessage = "Invalid or incomplete project file.";
                    return null;
                }

                model.CreateEmptyProjectHierarchy();
                model.FixMaterialsReferences();
                model.RenderingSysData.RefreshDefinitionReference(model.Renderers, model.BackdropsDefinitions, model.CameraControllers);
                model.RefreshMaterialDefinition();
                model.ResetPrevValRecursive();

                errorMessage = "";
                return model;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return null;
            }
        }

        // The file is written to a temporary file first, so a failed save does not damage the existing file
        private bool SaveProjectToFile(DataModel model, string filePath, bool showErrorMessage)
        {
            string tempFilePath = filePath + ".tmp";
            try
            {
                XmlDocument xmlDoc = model.Serialize();
                xmlDoc.Save(tempFilePath);
                File.Move(tempFilePath, filePath, true);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: Cannot save project: {0}. {1}", filePath, ex.Message);
                if (showErrorMessage)
                    ctx_.Platform.ShowErrorMessage("Save project", String.Format("Cannot save project:\n{0}\n\n{1}", filePath, ex.Message));

                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception)
                {
                }
                return false;
            }
        }
    }
}
