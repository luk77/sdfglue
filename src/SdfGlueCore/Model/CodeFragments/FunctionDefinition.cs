//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.CodeFragments
{
    public class CodeBlock
    {
        public string? Code;
        public string? Feature;
    }

    public class IncludeNamesCollection : List<string>
    {
        internal void Deserialize(XmlNode nodeParent)
        {
            if (nodeParent == null)
                return;

            foreach(XmlNode p in nodeParent.ChildNodes)
            {
                if (p.Name != "Include")
                    continue;

                string? filePath = XmlUtils.LoadAttributeAsString(p, "path"    , null);
                if (String.IsNullOrEmpty(filePath) )
                    continue;

                Add(filePath);
            }
        }

    }

    public class FunctionDefinition
    {
        public          int                                 ComboIndex;
        public          string?                             DisplayName;
        public          string?                             GroupName;
        public          string?                             FunctionName;
        public          bool                                ExportsCellIndex;           // used for repetitions operators
        public          string?                             FunctionCode;
        public          List<CodeBlock>                     CodeBlocks                  = new List<CodeBlock>();
        public          FunctionDefParametersCollection     Parameters                  = new FunctionDefParametersCollection();
        public          FunctionDefParametersCollection     CompilationParameters       = new FunctionDefParametersCollection();
        public          FunctionDefParametersCollection     MaterialParameters          = new FunctionDefParametersCollection();
        public          IncludeNamesCollection              IncludeNames                = new IncludeNamesCollection();


//        public FunctionDefParameter AddParameter(SdfParamType type, string displayName, string parameterName, float valSpeed)
//        {
//            FunctionDefParameter param = new FunctionDefParameter();
//            param.Index             = Parameters.Count;
//            param.Type              = type;
//            param.DisplayName       = displayName;
//            param.ParameterName     = parameterName;
//            //param.UniformSuffix     = uniformSuffix;
//            param.ValSpeed          = valSpeed;
//            Parameters.Add(param);
//            return param;
//        }

        internal ParametersValuesCollection GetDefaultCompilationParameters(ParametersValuesCollection parametersValues)
        {
            return CompilationParameters.GetDefaultParameters(parametersValues);
        }

        internal ParametersValuesCollection GetDefaultParameters(ParametersValuesCollection parametersValues)
        {
            return Parameters.GetDefaultParameters(parametersValues);
        }

        internal ParametersValuesCollection GetDefaultMaterialParameters(ParametersValuesCollection parametersValues)
        {
            return MaterialParameters.GetDefaultParameters(parametersValues);
        }

        internal string? TryToFindFeatureCode(string featureName)
        {
            foreach(CodeBlock codeBlock in CodeBlocks)
            {
                if (codeBlock.Feature == featureName)
                    return codeBlock.Code;
            }
            return null;
        }

        internal bool Deserialize(XmlDocument xmlDoc)
        {
            XmlNode? nodeRoot = xmlDoc.SelectSingleNode("FuncDef");
            if (nodeRoot == null)
                return false;

            XmlNode? nodeCode = nodeRoot.SelectSingleNode("Code");
            if (nodeCode == null)
                return false;

            DisplayName         = XmlUtils.LoadAttributeAsString(nodeRoot, "displayName"        , null);
            GroupName           = XmlUtils.LoadAttributeAsString(nodeRoot, "groupName"          , null);
            FunctionName        = XmlUtils.LoadAttributeAsString(nodeRoot, "functionName"       , null);
            ExportsCellIndex    = XmlUtils.LoadAttributeAsBool  (nodeRoot, "exportsCellIndex"   , false);
            FunctionCode = nodeCode.InnerText;

            CompilationParameters = new FunctionDefParametersCollection();
            XmlNode? nodeCompilationParameters = nodeRoot.SelectSingleNode("CompilationParameters");
            if (nodeCompilationParameters != null)
                CompilationParameters.Deserialize(nodeCompilationParameters);

            Parameters = new FunctionDefParametersCollection();
            XmlNode? nodeParameters = nodeRoot.SelectSingleNode("Parameters");
            if (nodeParameters != null)
                Parameters.Deserialize(nodeParameters);

            MaterialParameters = new FunctionDefParametersCollection();
            XmlNode? nodeMaterialParameters = nodeRoot.SelectSingleNode("MaterialDefinition");
            if (nodeMaterialParameters != null)
                MaterialParameters.Deserialize(nodeMaterialParameters);

            IncludeNames = new IncludeNamesCollection();
            XmlNode? nodeIncludes = nodeRoot.SelectSingleNode("Includes");
            if (nodeIncludes != null)
                IncludeNames.Deserialize(nodeIncludes);

            XmlNode? nodeCodeBlocks = nodeRoot.SelectSingleNode("CodeBlocks");
            if (nodeCodeBlocks != null)
            {
                XmlNodeList? nodesList = nodeCodeBlocks.SelectNodes("Code");
                if (nodesList != null)
                {
                    foreach(XmlNode xmlNode in nodesList)
                    {
                        string code = xmlNode.InnerText;
                        if (string.IsNullOrEmpty(code))
                            continue;

                        CodeBlock cb = new CodeBlock();
                        cb.Code = code;
                        cb.Feature = XmlUtils.LoadAttributeAsString(xmlNode, "feature", null);

                        CodeBlocks.Add(cb);
                    }
                }
            }


            return true;
        }

        public void CollectIncludeNames(Dictionary<string, string> includeNames)
        {
            foreach (string name in IncludeNames)
            {
                if (includeNames.ContainsKey(name))
                    continue;

                string code = File.ReadAllText("Includes/" + name, System.Text.Encoding.UTF8);
                includeNames.Add(name, code);
            }
        }
    }
}
