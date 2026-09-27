//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;

namespace SingleDocAppFramework.Ui.Components
{
    public class MenuFileTree
    {
        public static string Build(string rootPath, string searchPattern)
        {
            if (!Directory.Exists(rootPath))
                return string.Empty;

            string[] files = Directory.GetFiles(rootPath, searchPattern, SearchOption.AllDirectories);

            foreach (string path in files)
            {
                if (ImGui.MenuItem(String.Format("{0}", path)))
                {
                    return path;
                }
            }

            return string.Empty;
        }
    }
}
