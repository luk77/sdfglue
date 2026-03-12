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
    public class OperatorsCollection : IResetable
    {
        public          List<OperatorEntity>            Operators = new List<OperatorEntity>();

        private         string                          paramPrefix_ = "";

        public OperatorsCollection(string paramPrefix)
        {
            paramPrefix_ = paramPrefix;
        }

        public OperatorEntity CreateNewEntity(string defName = "opEmpty")
        {
            OperatorEntity opEntity = new OperatorEntity(paramPrefix_);
            opEntity.DefinitionName.Val = defName;
            opEntity.DefinitionName.ResetPrevVal();
            return opEntity;
        }

        public void RefreshDefinitionReference(FunctionDefinitionsSet definitionsSet)
        {
            foreach(OperatorEntity opent in Operators)
            {
                opent.RefreshDefinitionReference(definitionsSet);
            }
        }

        public void AddCodeToCollection(Dictionary<string, string> dir)
        {
            foreach(OperatorEntity opent in Operators)
            {
                if (!opent.Enabled.Val)
                    continue;

                opent.AddCodeToCollection(dir);
            }
        }

        public void Deserialize(XmlNode parentNode, SdfObject? parentObject, FunctionDefinitionsSet? definitions)
        {
            Operators.Clear();

            XmlNodeList? nodesList = parentNode.SelectNodes("OpEntity");
            if (nodesList == null)
                return;

            foreach(XmlNode nodeOpEntity in nodesList)
            {
                OperatorEntity opent = CreateNewEntity();
                opent.Deserialize(nodeOpEntity, parentObject, definitions);
                Operators.Add(opent);
            }
        }

        public void Serialize(XmlDocument xmlDoc, XmlNode parentNode, string collectionNodeName)
        {
            XmlNode nodeCollection = XmlUtils.AddNode(xmlDoc, parentNode, collectionNodeName);
            foreach(OperatorEntity opent in Operators)
            {
                XmlNode nodeOpEntity = XmlUtils.AddNode(xmlDoc, nodeCollection, "OpEntity");
                opent.Serialize(xmlDoc, nodeOpEntity);
            }
        }

        public OperatorEntity? FindOperatorById(string opId)
        {
            if (Operators == null)
                return null;

            foreach(OperatorEntity opent in Operators)
            {
                if (opent.DefinitionName.Val == opId)
                    return opent;
            }

            return null;
        }

        public void ResetPrevVal()
        {
            foreach(OperatorEntity opent in Operators)
            {
                opent.ResetPrevVal();
            }
        }

    }
}
