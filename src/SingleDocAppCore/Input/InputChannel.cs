//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Globalization;
using System.Xml;

namespace SingleDocAppCore.Input
{
    // Logical input (e.g. "Input 1") with any number of bindings to device controls.
    // The value is the value of the binding with the largest magnitude (for buttons: logical OR).
    public class InputChannel
    {
        public  int                     Id;
        public  string                  Name;
        public  List<InputBinding>      Bindings    = new List<InputBinding>();

        // runtime only
        public  float                   Value       = 0.0f;
        public  ValueHistory            History     = new ValueHistory(1024);

        public InputChannel(int id, string name)
        {
            Id      = id;
            Name    = name;
        }

        public float Evaluate(InputDeviceState state)
        {
            float result = 0.0f;
            foreach(InputBinding binding in Bindings)
            {
                float v = binding.Evaluate(state);
                if (!float.IsFinite(v))
                    continue;
                if (Math.Abs(v) > Math.Abs(result))
                    result = v;
            }
            return result;
        }

        // e.g. "Input 1 [Key Z]"
        public string GetLabel()
        {
            if (Bindings.Count == 0)
                return Name;
            if (Bindings.Count == 1)
                return String.Format("{0} [{1}]", Name, Bindings[0].GetDisplayName());
            return String.Format("{0} [{1}, +{2}]", Name, Bindings[0].GetDisplayName(), Bindings.Count - 1);
        }
    }

    // The list of input channels, stored in the input settings file (user data, not the document)
    public class InputChannelsCollection
    {
        public static readonly string   RootNodeName        = "InputSettings";

        public  List<InputChannel>      Channels            = new List<InputChannel>();
        public  int                     NextChannelId       = 1;

        public InputChannel? FindById(int id)
        {
            return Channels.Find(c => c.Id == id);
        }

        public InputChannel AddChannel(string? name = null)
        {
            int id = NextChannelId++;
            InputChannel channel = new InputChannel(id, name ?? String.Format("Input {0}", id));
            Channels.Add(channel);
            return channel;
        }

        public void Clear()
        {
            Channels.Clear();
            NextChannelId = 1;
        }

        public XmlDocument Serialize()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlNode docNode = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(docNode);

            XmlElement nodeRoot = xmlDoc.CreateElement(RootNodeName);
            xmlDoc.AppendChild(nodeRoot);
            XmlUtils.AddAtributeString(nodeRoot, "NextChannelId", NextChannelId.ToString(CultureInfo.InvariantCulture));

            foreach(InputChannel channel in Channels)
            {
                XmlElement nodeChannel = XmlUtils.AddNode(xmlDoc, nodeRoot, "Channel");
                XmlUtils.AddAtributeString(nodeChannel, "id"    , channel.Id.ToString(CultureInfo.InvariantCulture));
                XmlUtils.AddAtributeString(nodeChannel, "name"  , channel.Name);

                foreach(InputBinding binding in channel.Bindings)
                    binding.Serialize(xmlDoc, nodeChannel);
            }

            return xmlDoc;
        }

        public bool Deserialize(XmlNode nodeRoot)
        {
            Clear();

            XmlNodeList? channelsList = nodeRoot.SelectNodes("Channel");
            if (channelsList != null)
            {
                foreach(XmlNode nodeChannel in channelsList)
                {
                    int id = XmlUtils.LoadAttributeAsInt(nodeChannel, "id", 0);
                    if (id <= 0 || FindById(id) != null)
                    {
                        Console.WriteLine("WARNING: Input settings: invalid or duplicated channel id: {0}", id);
                        continue;
                    }

                    string name = XmlUtils.LoadAttributeAsString(nodeChannel, "name", null) ?? String.Format("Input {0}", id);
                    InputChannel channel = new InputChannel(id, name);

                    XmlNodeList? bindingsList = nodeChannel.SelectNodes("Binding");
                    if (bindingsList != null)
                    {
                        foreach(XmlNode nodeBinding in bindingsList)
                        {
                            InputBinding? binding = InputBinding.Deserialize(nodeBinding);
                            if (binding != null)
                                channel.Bindings.Add(binding);
                        }
                    }

                    Channels.Add(channel);
                }
            }

            int maxId = Channels.Count > 0 ? Channels.Max(c => c.Id) : 0;
            NextChannelId = Math.Max(XmlUtils.LoadAttributeAsInt(nodeRoot, "NextChannelId", 1), maxId + 1);

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
                    Console.WriteLine("WARNING: Invalid input settings file (missing {0} node): {1}", RootNodeName, filePath);
                    return false;
                }

                return Deserialize(rootNode);
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Loading input settings failed: {0}, {1}", filePath, ex.Message);
                return false;
            }
        }

        public bool SaveToFile(string filePath)
        {
            try
            {
                Serialize().Save(filePath);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Saving input settings failed: {0}, {1}", filePath, ex.Message);
                return false;
            }
        }
    }
}
