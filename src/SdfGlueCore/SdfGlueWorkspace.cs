//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Text;

namespace SdfGlueCore
{
    // Folder with the data of the application: function definitions, includes, shader templates, examples, projects.
    // Selected once at startup (command line: --workspace=<path>, default: "Workspace"), not changed while running.
    // A relative path is relative to the working directory (not to the exe folder) - intentional, it makes debugging in VS easy.
    public static class SdfGlueWorkspace
    {
        public const string             DefaultDirectory        = "Workspace";
        public const string             ArgWorkspace            = "--workspace=";

        public const string             FunctionsDirectory      = "Functions";
        public const string             IncludesDirectory       = "Includes";
        public const string             ShadersDirectory        = "Shaders";
        public const string             ExamplesDirectory       = "Examples";
        public const string             ProjectsDirectory       = "Projects";

        // "." - the working directory (e.g. tools using SdfGlueCore without the editor)
        public static string            RootDirectory           = ".";

        // Folders and files needed to start the application (the minimal workspace - see recreate_bin_mini.cmd)
        private static readonly string[] RequiredDirectories    =
        {
            "Functions/Renderers",
            "Functions/Backdrops",
            "Functions/CameraOperators",
            "Functions/Sdf",
            "Functions/MixOperators",
            "Functions/PositionOperators",
            "Functions/DistanceOperators",
            "Includes",
            "Shaders",
        };

        private static readonly string[] RequiredFiles          =
        {
            "Functions/Sdf/Basic/Box.xml",
            "Functions/Sdf/Basic/Plane.xml",
            "Functions/Renderers/Default.xml",
            "Functions/MixOperators/BasicSmooth/SmoothUnion.xml",
            "Functions/Backdrops/Default.xml",
            "Functions/CameraOperators/Empty.xml",
            "Functions/CameraOperators/Default.xml",
            "Functions/PositionOperators/Empty.xml",
            "Functions/PositionOperators/Translate.xml",
            "Functions/PositionOperators/Rotations/Rotate.xml",
            "Functions/DistanceOperators/Empty.xml",
            "Functions/DistanceOperators/Rounding.xml",
            "Shaders/Template.vert",
            "Shaders/Template.frag",
            "Shaders/UnityTemplate.shader",
        };

        // Path of a file or folder in the workspace, e.g. GetPath("Functions/Sdf/")
        public static string GetPath(string relativePath)
        {
            return Path.Combine(RootDirectory, relativePath);
        }

        // Path of the workspace from the command line (DefaultDirectory if not given)
        public static string GetRootDirectoryFromCommandLine(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith(ArgWorkspace, StringComparison.OrdinalIgnoreCase))
                    return arg.Substring(ArgWorkspace.Length);
            }
            return DefaultDirectory;
        }

        // Checks that the folder exists and contains the required folders and files
        public static bool Validate(string rootDirectory, out string errorMessage)
        {
            errorMessage = "";

            if (String.IsNullOrWhiteSpace(rootDirectory) || !Directory.Exists(rootDirectory))
            {
                errorMessage = "The folder does not exist.";
                return false;
            }

            StringBuilder sbMissing = new StringBuilder();
            foreach (string dir in RequiredDirectories)
            {
                if (!Directory.Exists(Path.Combine(rootDirectory, dir)))
                    sbMissing.AppendLine(String.Format("    {0}/", dir));
            }
            foreach (string file in RequiredFiles)
            {
                if (!File.Exists(Path.Combine(rootDirectory, file)))
                    sbMissing.AppendLine(String.Format("    {0}", file));
            }

            if (sbMissing.Length > 0)
            {
                errorMessage = "Missing required folders or files:\n" + sbMissing.ToString();
                return false;
            }

            return true;
        }
    }
}
