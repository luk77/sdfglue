//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.Entities
{
    public abstract class FunctionEntity : IResetable
    {
        public  ExString                            DefinitionName = new ExString(null);
        private FunctionDefinition?                 definition_;
        private string                              paramPrefix_;   // to jest prefix używany przy generowaniu kodu
        public  ParametersValuesCollection          ParametersValues            = new ParametersValuesCollection();
        public  ParametersValuesCollection          CompilationParametersValues = new ParametersValuesCollection();
        //public  ParametersValuesCollection          MaterialParametersValues    = new ParametersValuesCollection();

        public FunctionDefinition? Definition
        {
            get
            {
                return definition_;
            }

            set
            {
                bool changed = definition_ != value;
                definition_ = value;
                if (changed)
                {
                    RefreshDefaultParameters();
                }
            }
        }


        public string ParamPrefix
        {
            get
            {
                return paramPrefix_;
            }
        }

        public FunctionEntity(string paramPrefix)
        {
            paramPrefix_ = paramPrefix;
        }

        public void RefreshDefinitionReference(FunctionDefinitionsSet definitions)
        {
            Definition = definitions.FindDefinitionByName(DefinitionName.Val);
        }

        private void RefreshDefaultParameters()
        {
            if (Definition == null)
                return;

            // tu wyjątek dla MaterialEntity - w tym przypadku musimy podpiąć
            // defaultowe parametry z innej listy w definicji
            if (this is MaterialEntity)
            {
                ParametersValues            = Definition.GetDefaultMaterialParameters(ParametersValues);
            }
            else
            {
                ParametersValues            = Definition.GetDefaultParameters(ParametersValues);
                CompilationParametersValues = Definition.GetDefaultCompilationParameters(CompilationParametersValues);
            }

            //MaterialParametersValues    = Definition.GetDefaultMaterialParameters(MaterialParametersValues);
        }


        public virtual void Deserialize(XmlNode parentNode, TreeNode? parentObject, FunctionDefinitionsSet? definitions)
        {
            XmlUtils.DeserializeString  (parentNode, "DefinitionName" , ref DefinitionName.Val);
            if (definitions != null)
                RefreshDefinitionReference(definitions);

            // Ustawiamy defaultowe parametry
            RefreshDefaultParameters();

            // Compilation parameters values
            XmlNode? nodeCompilationParametersValues = parentNode.SelectSingleNode("CompilationParametersValues");
            if (nodeCompilationParametersValues != null)
            {
                CompilationParametersValues.Deserialize(nodeCompilationParametersValues, parentObject);
            }

            // Parameters values
            XmlNode? nodeParametersValues = parentNode.SelectSingleNode("ParametersValues");
            if (nodeParametersValues != null)
            {
                ParametersValues.Deserialize(nodeParametersValues, parentObject);
            }
        }

        public virtual void Serialize(XmlDocument xmlDoc, XmlNode parentNode)
        {
            XmlUtils.AddNodeString(xmlDoc, parentNode, "DefinitionName" , DefinitionName.Val        );

            // Compilation parameters values
            XmlNode nodeCompilationParametersValues = XmlUtils.AddNode(xmlDoc, parentNode, "CompilationParametersValues");
            CompilationParametersValues.Serialize(xmlDoc, nodeCompilationParametersValues);

            // Parameters values
            XmlNode nodeParametersValues = XmlUtils.AddNode(xmlDoc, parentNode, "ParametersValues");
            ParametersValues.Serialize(xmlDoc, nodeParametersValues);
        }

        public void AddCodeToCollection(Dictionary<string, string> dir, string? featureName = null)
        {
            if (Definition == null)
                return;
            if (Definition?.FunctionName == null)
                return;
            if (Definition?.FunctionCode == null)
                return;

            if (dir.ContainsKey(Definition.FunctionName))
                return; // already added

            string codeToAdd = Definition.FunctionCode;

            if (featureName != null)
            {
                string? featureCode = Definition.TryToFindFeatureCode(featureName);
                if (!String.IsNullOrEmpty(featureCode))
                    codeToAdd += featureCode;
            }

            dir[Definition.FunctionName] = codeToAdd;
        }

        public bool SetParameterVec3(string paramId, System.Numerics.Vector3 paramVal, bool force = false)
        {
            return SetParameter<ExVector3, System.Numerics.Vector3>(ParametersValues, paramId, paramVal, force);
        }

        public bool SetParameterFloat(string paramId, float paramVal, bool force = false)
        {
            //return SetParameter<ExFloat, float>(ParametersValues, paramId, paramVal, force);
            return SetParameter<ExFloatWithSignal, float>(ParametersValues, paramId, paramVal, force);
        }

//        public bool SetMaterialParameterVec3(string paramId, System.Numerics.Vector3 paramVal, bool force = false)
//        {
//            return SetParameter<ExVector3, System.Numerics.Vector3>(MaterialParametersValues, paramId, paramVal, force);
//        }
//
//        public bool SetMaterialParameterFloat(string paramId, float paramVal, bool force = false)
//        {
//            return SetParameter<ExFloat, float>(MaterialParametersValues, paramId, paramVal, force);
//        }

        // Ta metoda to zło...
        private static bool SetParameter<TEx, TVal>(ParametersValuesCollection destCollection, string paramId, TVal paramVal, bool force) where TEx : ExSimpleType<TVal>, new() where TVal : struct
        {
            if (destCollection == null)
                return false;

            if (!destCollection.ContainsKey(paramId))
            {
                if (force)
                {
                    TEx newParam = new TEx();
                    newParam.Initialize(paramVal);
                    destCollection.Add(paramId, newParam);
                }
                else
                {
                    return false;
                }
            }

            TEx? param = destCollection[paramId] as TEx;
            if (param == null)
                return false;

            param.Val = paramVal;

            return true;
        }


        public ISimpleType? GetParameter(string paramId)
        {
            if (ParametersValues == null)
                return null;

            if (!ParametersValues.ContainsKey(paramId))
                return null;

            return ParametersValues[paramId];
        }

        public virtual void ResetPrevVal()
        {
            DefinitionName.ResetPrevVal();

            foreach(var keyVal in ParametersValues)
            {
                keyVal.Value.ResetPrevVal();
            }
        }

        public bool HasAnyCompilationParameters()
        {
            return Definition?.CompilationParameters.Count > 0;
        }

        public bool HasAnyParameters()
        {
            return Definition?.Parameters.Count > 0;
        }
    }
}
