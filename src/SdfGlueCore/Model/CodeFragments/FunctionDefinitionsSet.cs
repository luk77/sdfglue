//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Xml;

namespace SdfGlueCore.Model.CodeFragments
{
    public class FunctionDefGroup
    {
        public      static readonly string      NameUngrouped         = "_ungrouped";
        public      List<FunctionDefinition>    Definitions           = new List<FunctionDefinition>();
        private     string                      groupName_;

        public string GroupName
        {
            get { return groupName_; }
        }

        public FunctionDefGroup(string groupName)
        {
            groupName_ = groupName;
        }
    }

    public class FunctionDefinitionsSet
    {
        private     List<FunctionDefinition>                definitions_        = new List<FunctionDefinition>();

        private     SortedDictionary<string, FunctionDefGroup>    groups_             = new SortedDictionary<string, FunctionDefGroup>();

        public void Reload(string functionsDirectory)
        {
            string[] files = Directory.GetFiles(functionsDirectory, "*.xml", SearchOption.AllDirectories);

            definitions_    = new List<FunctionDefinition>();
            groups_         = new SortedDictionary<string, FunctionDefGroup>();

            int comboIndex = 0;

            foreach(string filePath in files)
            {
                XmlDocument xmlDoc = new XmlDocument();
                try
                {
                    xmlDoc.Load(filePath);

                    FunctionDefinition def = new FunctionDefinition();
                    bool success = def.Deserialize(xmlDoc);
                    if (!success)
                        continue;

                    def.ComboIndex = comboIndex;
                    comboIndex++;

                    definitions_.Add(def);

                    // add item to group
                    FunctionDefGroup? group = null;
                    string? groupName = def.GroupName;
                    if (String.IsNullOrEmpty(groupName))
                        groupName = FunctionDefGroup.NameUngrouped;
                    if (groups_.ContainsKey(groupName))
                        group = groups_[groupName];
                    else
                    {
                        group = new FunctionDefGroup(groupName);
                        groups_[groupName] = group;
                    }
                    group.Definitions.Add(def);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("WARNING: " + ex.ToString());
                }
            }
        }

        public List<FunctionDefinition> GetDefinitions()
        {
            return definitions_;
        }

        public SortedDictionary<string, FunctionDefGroup> GetGroups()
        {
            return groups_;
        }

        public FunctionDefinition? GetDefinition(int index)
        {
            if (index < 0)
                return null;

            if (index >= definitions_.Count)
                return null;

            return definitions_[index];
        }

        public string[] GetDefinitionsNames()
        {
            List<string> defs = new List<string>();
            foreach(FunctionDefinition d in definitions_)
            {
                if (d == null)
                    continue;
                if (d.DisplayName == null)
                    continue;
                defs.Add(d.DisplayName);
            }
            return defs.ToArray();
        }

        public FunctionDefinition? FindDefinitionByName(string? name)
        {
            if (name == null)
                return null;

            foreach(FunctionDefinition d in definitions_)
            {
                if (d.FunctionName == name)
                    return d;
            }

            return null;
        }

        public void CollectIncludeNames(Dictionary<string, string> includeNames)
        {
            foreach(FunctionDefinition d in definitions_)
            {
                d.CollectIncludeNames(includeNames);
            }
        }
    }
}
