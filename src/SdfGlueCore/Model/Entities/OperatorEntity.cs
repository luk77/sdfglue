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
    // Z grubsza to samo co FunctionEntity, tylko z dodatkowym polem 'enabled'
    public class OperatorEntity : FunctionEntity
    {
        public  ExBool                  Enabled = new ExBool(true);

        public OperatorEntity(string paramPrefix) : base(paramPrefix)
        {
        }

        public override void Deserialize(XmlNode parentNode, TreeNode? parentObject, FunctionDefinitionsSet? definitions)
        {
            XmlUtils.DeserializeBool(parentNode, "Enabled", ref Enabled.Val);

            base.Deserialize(parentNode, parentObject, definitions);
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parentNode)
        {
            XmlUtils.AddNodeBool(xmlDoc, parentNode, "Enabled", Enabled.Val);

            base.Serialize(xmlDoc, parentNode);
        }

        public override void ResetPrevVal()
        {
            base.ResetPrevVal();

            Enabled.ResetPrevVal();
        }
    }
}
