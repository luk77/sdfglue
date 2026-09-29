//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueEditor.Application
{
    // Demo mode: periodically opens a random example project
    public class DemoModeController
    {
        private static readonly string  ExamplesDirectory       = "Examples";
        private static readonly string  ProjectSearchPattern    = "*.xml";

        private DocumentController      documents_;

        private double                  projectDemoDuration_    = 2.0;
        private double                  demoModeTimer_          = 0.0;
        private Random                  rndDemoMode_            = new Random();

        public DemoModeController(DocumentController documents)
        {
            documents_ = documents;
        }

        // enabled is turned off when the user cancels discarding unsaved changes
        public void Update(double deltaTime, ref bool enabled)
        {
            if (!enabled)
                return;

            demoModeTimer_ += deltaTime;

            if (demoModeTimer_ > projectDemoDuration_)
            {
                // reset (not decrement) - the question about unsaved changes may block for a long time
                demoModeTimer_ = 0.0;

                if (!documents_.ConfirmDiscardChanges())
                {
                    Console.WriteLine("Demo mode disabled (unsaved changes)");
                    enabled = false;
                    return;
                }

                LoadRandomProject(ExamplesDirectory, ProjectSearchPattern);
            }
        }

        private void LoadRandomProject(string rootPath, string searchPattern)
        {
            if (!Directory.Exists(rootPath))
                return;

            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);
            if (files.Length == 0)
                return;

            int rndIndex = rndDemoMode_.Next(files.Length);
            string path = files[rndIndex];

            Console.WriteLine("Loading project: {0}", path);
            documents_.LoadProject(path, false);
        }
    }
}
