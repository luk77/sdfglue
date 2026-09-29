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

            // Hot reload: a file which cannot be loaded now (e.g. caught in the middle of saving by a text editor,
            // or with a typo) keeps its previously loaded version, so objects do not lose their definition.
            Dictionary<string, FunctionDefinition> previousDefinitions = new Dictionary<string, FunctionDefinition>();
            foreach(FunctionDefinition d in definitions_)
            {
                if (d.FilePath != null)
                    previousDefinitions[d.FilePath] = d;
            }

            definitions_    = new List<FunctionDefinition>();
            groups_         = new SortedDictionary<string, FunctionDefGroup>();

            int comboIndex = 0;

            // functionName -> file path. Projects refer to definitions by functionName only (FindDefinitionByName),
            // so a duplicate could never be selected reliably - it is skipped (the first loaded one is kept).
            Dictionary<string, string> loadedNames = new Dictionary<string, string>();

            foreach(string filePath in files)
            {
                try
                {
                    FunctionDefinition? def = LoadDefinition(filePath, out string? errorMessage);
                    if (def == null)
                    {
                        if (previousDefinitions.TryGetValue(filePath, out FunctionDefinition? previousDef))
                        {
                            Console.WriteLine("WARNING: Cannot load function definition {0} ({1}). The previously loaded version is used.", filePath, errorMessage);
                            def = previousDef;
                        }
                        else
                        {
                            Console.WriteLine("WARNING: Cannot load function definition {0} ({1}).", filePath, errorMessage);
                            continue;
                        }
                    }

                    if (def.FunctionName != null)
                    {
                        if (loadedNames.TryGetValue(def.FunctionName, out string? firstFilePath))
                        {
                            Console.WriteLine("WARNING: Duplicated functionName \"{0}\" in {1} (already defined in {2}). The definition is ignored.", def.FunctionName, filePath, firstFilePath);
                            continue;
                        }
                        loadedNames[def.FunctionName] = filePath;
                    }

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

        // Returns null (with the reason) if the file cannot be read or is not a valid definition
        private static FunctionDefinition? LoadDefinition(string filePath, out string? errorMessage)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(filePath);

                FunctionDefinition def = new FunctionDefinition();
                if (!def.Deserialize(xmlDoc))
                {
                    errorMessage = "missing FuncDef or Code node";
                    return null;
                }

                def.FilePath = filePath;
                errorMessage = null;
                return def;
            }
            catch(Exception ex)
            {
                errorMessage = ex.Message;
                return null;
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

        public void CollectIncludeNames(Dictionary<string, string> includeNames, System.Text.StringBuilder sbErrors)
        {
            foreach(FunctionDefinition d in definitions_)
            {
                d.CollectIncludeNames(includeNames, sbErrors);
            }
        }
    }
}
