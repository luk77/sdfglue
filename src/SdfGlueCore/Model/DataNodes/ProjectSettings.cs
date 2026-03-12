//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes
{
    public class ProjectSettings : TreeNode
    {
        // globally fix all uniforms (usefull for exporting shaders)
        public  ExBool                  FixAllObjects           = new ExBool(false);

        // experimental: use 4d raymarching
        public  ExBool                  Use4d                   = new ExBool(false);

        public ProjectSettings(DataModel model) : base(DataModel.NodeIdProjectSettings, "Project settings")
        {

        }

        public override void ResetPrevVal()
        {
            FixAllObjects.ResetPrevVal();
            Use4d.ResetPrevVal();
        }

        internal bool Deserialize(XmlNode nodeThis)
        {
            XmlUtils.DeserializeBool    (nodeThis, "FixAllObjects"  , ref FixAllObjects.Val     );
            XmlUtils.DeserializeBool    (nodeThis, "Use4d"          , ref Use4d.Val             );
            return true;
        }

        public void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "ProjectSettings");

            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "FixAllObjects"  , FixAllObjects.Val     );
            XmlUtils.AddNodeBool    (xmlDoc, nodeThis, "Use4d"          , Use4d.Val             );
        }
    }
}
