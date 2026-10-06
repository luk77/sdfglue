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
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueCore.Model.Entities
{
    // Roughly the same as FunctionEntity, with an additional 'enabled' field
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
