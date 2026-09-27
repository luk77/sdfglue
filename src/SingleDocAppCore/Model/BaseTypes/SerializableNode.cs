//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.DataNodes;
using System.Xml;

namespace SingleDocAppCore.Model.BaseTypes
{
    public abstract class SerializableNode : TreeNode
    {
        public SerializableNode(int id, String name) : base(id, name) { }

        public abstract bool Deserialize(XmlNode nodeThis, IAbstractDocument model);
        public abstract void Serialize(XmlDocument xmlDoc, XmlNode parent);
    }
}
