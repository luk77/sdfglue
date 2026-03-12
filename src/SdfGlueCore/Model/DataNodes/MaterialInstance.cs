//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Xml;
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Model.Entities;
using SdfGlueCore.Utils;

namespace SdfGlueCore.Model.DataNodes
{
    public class MaterialInstance : SerializableNode
    {
        public ExBool               IsFixed             = new ExBool(false);

        public MaterialEntity       MaterialProps       = new MaterialEntity();

        public MaterialInstance() : base(0, "Material")
        {

        }

        public void RefreshDefinitionReference(FunctionDefinition? definition)
        {
            MaterialProps.Definition  = definition;
        }

        internal string GetUniqueDisplayName()
        {
            return String.Format("[{0}] {1}", Id, Name.Val);
        }

        public override void ResetPrevVal()
        {
            IsFixed         .ResetPrevVal();

            MaterialProps.ResetPrevVal();
        }

        public override bool Deserialize(XmlNode nodeThis, DataModel model)
        {
            XmlUtils.DeserializeInt     (nodeThis, "Id"             , ref Id                    );
            XmlUtils.DeserializeString  (nodeThis, "Name"           , ref Name.Val              );
            XmlUtils.DeserializeBool    (nodeThis, "IsFixed"        , ref IsFixed.Val           );

            //if (DataModel.ImportOldMaterials)
            //{
            //    MaterialProps.SetParameterVec3  ("colorAmbient"     , ColorAmbient .Val, true);
            //    MaterialProps.SetParameterVec3  ("colorDiffuse"     , ColorDiffuse .Val, true);
            //    MaterialProps.SetParameterVec3  ("colorSpecular"    , ColorSpecular.Val, true);
            //    MaterialProps.SetParameterVec3  ("colorRim"         , ColorRim     .Val, true);
            //    MaterialProps.SetParameterFloat ("shininess"        , Shininess    .Val, true);
            //    MaterialProps.SetParameterFloat ("rimStrength"      , RimStrength  .Val, true);
            //    MaterialProps.SetParameterFloat ("rimWidth"         , RimWidth     .Val, true);
            //    MaterialProps.SetParameterFloat ("reflectance"      , Reflectance  .Val, true);
            //}

            MaterialProps.Deserialize(nodeThis, this, null);

            return true;
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "Material");

            XmlUtils.AddNodeInt     (xmlDoc, nodeThis, "Id"             , Id                    );
            XmlUtils.AddNodeString  (xmlDoc, nodeThis, "Name"           , Name.Val              );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "IsFixed"        , IsFixed.Val           );

            MaterialProps.Serialize(xmlDoc, nodeThis);
        }
    }
}
