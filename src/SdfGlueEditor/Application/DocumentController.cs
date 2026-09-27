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

        public void NewProject()
        {
            ctx_.CodeGenerator.SetModel(new DataModel());

            ctx_.ReinitializeRenderingSystem();
        }

        public void OpenProject()
        {
            string? filePath = ctx_.Platform.OpenFileDialog(ctx_.Window.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            OpenProject(filePath);
        }

        public void OpenProject(string filePath)
        {
            if (String.IsNullOrEmpty(filePath))
                return;

            GetModel().ProjectFilePath = filePath;
            LoadProjectFromFile(filePath);

            GetModel().SelectedNode = null;

            ctx_.ReinitializeRenderingSystem();

            ctx_.Window.RefreshWindowTitle();
        }

        public void SaveProject()
        {
            string? filePath = GetModel().ProjectFilePath;
            if (String.IsNullOrEmpty(filePath))
                return;

            SaveProjectToFile(filePath);

            ctx_.Window.RefreshWindowTitle();
        }

        public void SaveProjectAs()
        {
            string? filePath = ctx_.Platform.SaveFileDialog(ctx_.Window.AppSettings.DocumentFileFilter);
            if (String.IsNullOrEmpty(filePath))
                return;

            GetModel().ProjectFilePath = filePath;
            SaveProjectToFile(filePath);

            ctx_.Window.RefreshWindowTitle();
        }

        // Loads and re-saves all projects (upgrades them to the current file format)
        public void BatchProcessAllProjects()
        {
            int countP = BatchProcessAllProjects("Projects", "*.xml");
            int countE = BatchProcessAllProjects("Examples", "*.xml");

            Console.WriteLine("Batch processing finished. Projects:{0}, examples:{1}", countP, countE);

            ctx_.ReinitializeRenderingSystem();
        }

        private int BatchProcessAllProjects(string rootPath, string searchPattern)
        {
            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);

            int count = 0;
            foreach (string path in files)
            {
                Console.WriteLine("Loading project: {0}", path);
                LoadProjectFromFile(path);

                SaveProjectToFile(path);
                count++;
            }
            return count;
        }

        private void LoadProjectFromFile(string filePath)
        {
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(filePath);
            GetModel().Deserialize(xmlDoc);
            GetModel().CreateEmptyProjectHierarchy();
            GetModel().FixMaterialsReferences();
            GetModel().RenderingSysData.RefreshDefinitionReference(GetModel().Renderers, GetModel().BackdropsDefinitions, GetModel().CameraControllers);
            GetModel().RefreshMaterialDefinition();
            GetModel().ResetPrevValRecursive();
            UndoManager.Instance.ClearAll();
        }

        private void SaveProjectToFile(string filePath)
        {
            XmlDocument xmlDoc = GetModel().Serialize();
            xmlDoc.Save(filePath);
        }
    }
}
