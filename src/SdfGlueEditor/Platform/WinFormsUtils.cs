//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueEditor.Platform
{
    public class WinFormsUtils
    {
        public static string? OpenProjectFilePath()
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                //dlg.InitialDirectory = "c:\\";
                dlg.Filter = "Project files (*.xml)|*.xml|All files (*.*)|*.*";
                dlg.FilterIndex = 1;
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return null;
        }

        public static string? SaveAsProjectFilePath()
        {
            return SaveAsXmlFilePath("Project files");
        }
        public static string? SaveAsXmlFilePath(string filterFileTypeName)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                //dlg.InitialDirectory = "c:\\";
                dlg.Filter = String.Format("{0} (*.xml)|*.xml|All files (*.*)|*.*", filterFileTypeName);
                dlg.FilterIndex = 1;
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return null;
        }

        public static string? SaveAsLayoutFilePath()
        {
            return SaveAsXmlFilePath("Layout files");

//            using (SaveFileDialog dlg = new SaveFileDialog())
//            {
//                //dlg.InitialDirectory = "c:\\";
//                dlg.Filter = "Layout files (*.ini)|*.ini|All files (*.*)|*.*";
//                dlg.FilterIndex = 1;
//                dlg.RestoreDirectory = true;
//                dlg.InitialDirectory = "Layouts";
//
//                if (dlg.ShowDialog() == DialogResult.OK)
//                {
//                    return dlg.FileName;
//                }
//            }
//
//            return null;
        }

        public static string? SaveAsImageFilePath()
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                //dlg.InitialDirectory = "c:\\";
                dlg.Filter = "PNG file (*.png)|*.png";
                dlg.FilterIndex = 1;
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return null;
        }

        public static string? GetExportAnimationDirectory()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.SelectedPath;
                }
            }

            return null;
        }

        //public static void ShowMessageBox(string message, string title)
        //{
        //    MessageBox.Show(message, title, MessageBoxButtons.OK);
        //}
    }
}
