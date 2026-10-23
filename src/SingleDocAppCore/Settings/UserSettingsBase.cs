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
    // Derived classes must have a parameterless constructor (used by CreateDefaults()).
    public class UserSettingsBase
    {
        public static readonly string   RootNodeName            = "UserSettings";

        public  float                   UiTextScaleFactor       = 1.0f;
        //public  bool                    UseRenderFrequencyLimit = true;
        //public  int                     RenderFrequencyLimit    = 60;
        public  bool                    UseUpdateFrequencyLimit = true;
        public  int                     UpdateFrequencyLimit    = 60;
        public  string                  LayoutFile              = "";       // relative to the layouts directory, empty - application default
        public  string                  UiStyle                 = "Dark";   // name of a style registered in the framework (UiStyles)
        public  bool                    FullscreenOnPrimaryMonitor = false; // borderless fullscreen: false - monitor with the window, true - primary monitor
        public  float                   WindowOpacity           = 0.75f;    // opacity of window backgrounds, used by apps that draw under the UI (SdAppWindow.GetWindowOpacity)

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
            XmlUtils.AddNodeString  (xmlDoc, node, "UiStyle"                    , UiStyle                   );
            XmlUtils.AddNodeBool    (xmlDoc, node, "FullscreenOnPrimaryMonitor" , FullscreenOnPrimaryMonitor);
            XmlUtils.AddNodeFloat   (xmlDoc, node, "WindowOpacity"              , WindowOpacity             );
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

            string? uiStyle = UiStyle;
            XmlUtils.DeserializeString  (parentNode, "UiStyle"                    , ref uiStyle                   );
            UiStyle = String.IsNullOrEmpty(uiStyle) ? UiStyle : uiStyle;

            XmlUtils.DeserializeBool    (parentNode, "FullscreenOnPrimaryMonitor" , ref FullscreenOnPrimaryMonitor);
            XmlUtils.DeserializeFloat   (parentNode, "WindowOpacity"              , ref WindowOpacity             );
            WindowOpacity = Math.Clamp(WindowOpacity, 0.0f, 1.0f);

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
            RestoreSnapshot(CreateDefaults().TakeSnapshot());
        }

        // New instance of the same (derived) type with default values
        public UserSettingsBase CreateDefaults()
        {
            UserSettingsBase? defaults = Activator.CreateInstance(GetType()) as UserSettingsBase;
            if (defaults == null)
                throw new InvalidOperationException(String.Format("Can't create default user settings: {0}", GetType().Name));

            defaults.InitDefault();
            return defaults;
        }

        // Copy of the current state, e.g. to revert changes made in the settings dialog
        public XmlDocument TakeSnapshot()
        {
            return Serialize();
        }

        public void RestoreSnapshot(XmlDocument snapshot)
        {
            XmlElement? rootNode = snapshot.DocumentElement;
            if (rootNode != null)
                Deserialize(rootNode);
        }

        // Independent copy of the current state (same type)
        public UserSettingsBase Clone()
        {
            UserSettingsBase copy = CreateDefaults();
            copy.RestoreSnapshot(TakeSnapshot());
            return copy;
        }

        private string GetXmlText()
        {
            return Serialize().OuterXml;
        }
    }
}
