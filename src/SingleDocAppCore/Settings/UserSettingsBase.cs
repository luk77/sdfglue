//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Xml;

namespace SingleDocAppCore.Settings
{
    // User settings common to all SingleDocApp applications, persisted in an XML file.
    // Applications derive from this class, add their own fields and override
    // Serialize(xmlDoc, node) / Deserialize(node) (calling the base implementation).
    // Derived classes must have a parameterless constructor (used by RestoreDefaults()).
    public class UserSettingsBase
    {
        public static readonly string   RootNodeName            = "UserSettings";

        public  float                   UiTextScaleFactor       = 1.0f;
        //public  bool                    UseRenderFrequencyLimit = true;
        //public  int                     RenderFrequencyLimit    = 60;
        public  bool                    UseUpdateFrequencyLimit = true;
        public  int                     UpdateFrequencyLimit    = 60;
        public  string                  LayoutFile              = "";       // relative to the layouts directory, empty - application default

        // XML of the last loaded/saved state, used by IsModified()
        private string?                 lastSavedXml_           = null;

        // Initialization of defaults that can't be expressed by field initializers (e.g. lists).
        // Called before loading the settings file.
        public virtual void InitDefault()
        {
        }

        public XmlDocument Serialize()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            XmlNode nodeData = xmlDoc.CreateElement(RootNodeName);
            xmlDoc.AppendChild(nodeData);

            Serialize(xmlDoc, nodeData);

            return xmlDoc;
        }

        protected virtual void Serialize(XmlDocument xmlDoc, XmlNode node)
        {
            XmlUtils.AddNodeFloat   (xmlDoc, node, "UiTextScaleFactor"          , UiTextScaleFactor         );
            XmlUtils.AddNodeBool    (xmlDoc, node, "UseUpdateFrequencyLimit"    , UseUpdateFrequencyLimit   );
            XmlUtils.AddNodeInt     (xmlDoc, node, "UpdateFrequencyLimit"       , UpdateFrequencyLimit      );
            XmlUtils.AddNodeString  (xmlDoc, node, "LayoutFile"                 , LayoutFile                );
        }

        // Missing nodes keep the current values (files saved by older versions stay valid)
        public virtual bool Deserialize(XmlNode parentNode)
        {
            if (parentNode == null)
                return false;

            XmlUtils.DeserializeFloat   (parentNode, "UiTextScaleFactor"          , ref UiTextScaleFactor         );
            XmlUtils.DeserializeBool    (parentNode, "UseUpdateFrequencyLimit"    , ref UseUpdateFrequencyLimit   );
            XmlUtils.DeserializeInt     (parentNode, "UpdateFrequencyLimit"       , ref UpdateFrequencyLimit      );

            // DeserializeString works on string?; keep the current value when the node is missing
            string? layoutFile = LayoutFile;
            XmlUtils.DeserializeString  (parentNode, "LayoutFile"                 , ref layoutFile                );
            LayoutFile = layoutFile ?? "";

            return true;
        }

        public bool LoadFromFile(string filePath)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                XmlNode? rootNode = xmlDoc.SelectSingleNode(RootNodeName);
                if (rootNode == null)
                {
                    Console.WriteLine("WARNING: Invalid user settings file (missing {0} node): {1}", RootNodeName, filePath);
                    return false;
                }

                Deserialize(rootNode);
                lastSavedXml_ = GetXmlText();
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Loading user settings failed: {0}, {1}", filePath, ex.Message);
                return false;
            }
        }

        public bool SaveToFile(string filePath)
        {
            try
            {
                XmlDocument xmlDoc = Serialize();
                xmlDoc.Save(filePath);
                lastSavedXml_ = xmlDoc.OuterXml;
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Saving user settings failed: {0}, {1}", filePath, ex.Message);
                return false;
            }
        }

        // True if the settings differ from the last loaded/saved state
        public bool IsModified()
        {
            return GetXmlText() != lastSavedXml_;
        }

        // Sets default values (in memory only - saving is explicit)
        public void RestoreDefaults()
        {
            UserSettingsBase? defaults = Activator.CreateInstance(GetType()) as UserSettingsBase;
            if (defaults == null)
                return;

            defaults.InitDefault();

            XmlElement? rootNode = defaults.Serialize().DocumentElement;
            if (rootNode == null)
                return;

            Deserialize(rootNode);
        }

        private string GetXmlText()
        {
            return Serialize().OuterXml;
        }
    }
}
