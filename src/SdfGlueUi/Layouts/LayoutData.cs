//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueUi.Layouts
{
    public class WindowsVisibilityCollection : Dictionary<string, bool> {}

    public class LayoutData
    {
        public  string                      DisplayName;
        public  WindowsVisibilityCollection WindowsVisibility;
        public  string                      ImguiLayoutSettingsTxt;

        public XmlDocument Serialize()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            XmlNode nodeData = xmlDoc.CreateElement("LayoutData");
            xmlDoc.AppendChild(nodeData);

            XmlUtils.AddNodeString  (xmlDoc, nodeData, "DisplayName"                , DisplayName               );
            XmlUtils.AddNodeString  (xmlDoc, nodeData, "ImguiLayoutSettingsTxt"     , ImguiLayoutSettingsTxt    );

            XmlNode nodeWindowsVisibility = XmlUtils.AddNode(xmlDoc, nodeData, "WindowsVisibility");

            foreach (var keyVal in WindowsVisibility)
            {
                XmlElement node = xmlDoc.CreateElement("Visibility");
                nodeWindowsVisibility.AppendChild(node);
                node.SetAttribute("Window", keyVal.Key);
                node.SetAttribute("Visible", keyVal.Value.ToString());
            }

            return xmlDoc;
        }

        public bool Deserialize(XmlNode parentNode)
        {
            if (parentNode == null)
                return false;

            XmlUtils.DeserializeString (parentNode, "DisplayName"                , ref DisplayName              );
            XmlUtils.DeserializeString (parentNode, "ImguiLayoutSettingsTxt"     , ref ImguiLayoutSettingsTxt   );

            XmlNode? nodeWindowsVisibility = parentNode.SelectSingleNode("WindowsVisibility");
            if (nodeWindowsVisibility != null)
            {
                WindowsVisibility = new WindowsVisibilityCollection();
                XmlNodeList? windowVisibility = nodeWindowsVisibility.SelectNodes("Visibility");
                if (windowVisibility != null)
                {
                    foreach(XmlNode node in windowVisibility)
                    {
                        string? windowName       = XmlUtils.LoadAttributeAsString(node, "Window", null);
                        bool    windowVisible    = XmlUtils.LoadAttributeAsBool(node, "Visible", true);

                        if (String.IsNullOrEmpty(windowName))
                            continue;

                        WindowsVisibility.Add(windowName, windowVisible);
                    }
                }
            }


            return true;
        }
    }
}
