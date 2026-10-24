//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.DataNodes;
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.Entities
{
    public class OperatorsCollection : IResetable
    {
        public          List<OperatorEntity>            Operators = new List<OperatorEntity>();

        private         string                          paramPrefix_ = "";
        private         int                             nextId_      = 1;

        public OperatorsCollection(string paramPrefix)
        {
            paramPrefix_ = paramPrefix;
        }

        public OperatorEntity CreateNewEntity(string defName = "opEmpty")
        {
            OperatorEntity opEntity = CreateEntity(defName);
            opEntity.Id = NextId();
            return opEntity;
        }

        private OperatorEntity CreateEntity(string defName)
        {
            OperatorEntity opEntity = new OperatorEntity(paramPrefix_);
            opEntity.DefinitionName.Val = defName;
            opEntity.DefinitionName.ResetPrevVal();
            return opEntity;
        }

        // a new id: never one of the operators in the collection, nor one used before in this session (a deleted
        // operator can come back by undo)
        private int NextId()
        {
            foreach(OperatorEntity opent in Operators)
                nextId_ = Math.Max(nextId_, opent.Id + 1);
            return nextId_++;
        }

        // operators without an id (projects saved before the ids) or with a duplicated one get a new id
        private void EnsureIds()
        {
            HashSet<int> used = new HashSet<int>();
            foreach(OperatorEntity opent in Operators)
            {
                if (opent.Id > 0)
                    used.Add(opent.Id);
            }
            HashSet<int> seen = new HashSet<int>();
            foreach(OperatorEntity opent in Operators)
            {
                if (opent.Id > 0 && seen.Add(opent.Id))
                    continue;
                while (used.Contains(nextId_))
                    nextId_++;
                opent.Id = nextId_++;
                used.Add(opent.Id);
                seen.Add(opent.Id);
            }
        }

        public OperatorEntity? FindOperator(int id)
        {
            foreach(OperatorEntity opent in Operators)
            {
                if (opent.Id == id)
                    return opent;
            }
            return null;
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
            nextId_ = 1;

            XmlNodeList? nodesList = parentNode.SelectNodes("OpEntity");
            if (nodesList == null)
                return;

            foreach(XmlNode nodeOpEntity in nodesList)
            {
                OperatorEntity opent = CreateEntity("opEmpty");
                opent.Deserialize(nodeOpEntity, parentObject, definitions);
                Operators.Add(opent);
            }
            EnsureIds();
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
