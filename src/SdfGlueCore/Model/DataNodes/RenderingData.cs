//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SdfGlueCore.Model.CodeFragments;
using SdfGlueCore.Utils;
using System.Numerics;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes
{
    public class RenderingData : SerializableNode
    {
        // xy = current pixel coords (if LMB is down). zw = click pixel
        public      Vector4                     MouseData                         = new Vector4();

        //private static int debugCounter_ = 0;

        //public RenderingData(DataModel model) : base(DataModel.NodeIdRenderingData, String.Format("Rendering [{0}]", debugCounter_))
        public RenderingData(DataModel model) : base(DataModel.NodeIdRenderingData, "Rendering")
        {
            //debugCounter_++;

            AddChild(new RenderPassData(DataModel.NodeIdRenderingData - 1, "Primary pass", null, true));

            //FunctionDefinition rpColorCorrection = model.Renderers.FindDefinitionByName("SecPassTonemapACES");
            //if (rpColorCorrection != null)
            //    AddChild(new RenderPassData(DataModel.NodeIdRenderingData - 2, "Color correction", rpColorCorrection, false));

            FunctionDefinition? rpAvFrames = model.Renderers.FindDefinitionByName("SecPassAverageFramesHistory");
            if (rpAvFrames != null)
            {
                RenderPassData rp = new RenderPassData(DataModel.NodeIdRenderingData - 3, "Average frames", rpAvFrames, false);
                AddChild(rp);
                rp.ClearOnEveryFrame.Val = false;
            }
        }

        public void RefreshDefinitionReference(FunctionDefinitionsSet renderers, FunctionDefinitionsSet backdrops, FunctionDefinitionsSet cameraControllers)
        {
            foreach(TreeNode node in Children)
            {
                RenderPassData? rpData = node as RenderPassData;
                rpData?.RefreshDefinitionReference(renderers, backdrops, cameraControllers);
            }
        }

        public override void ResetPrevVal()
        {
        }

        public override void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlNode nodeThis = XmlUtils.AddNode(xmlDoc, parent, "RenderingData");

            XmlNode nodePasses = xmlDoc.CreateElement("RenderPasses");
            nodeThis.AppendChild(nodePasses);
            foreach(RenderPassData pass in Children)
            {
                pass.Serialize(xmlDoc, nodePasses);
            }
        }

        public override bool Deserialize(XmlNode nodeThis, DataModel model)
        {
            XmlNode? nodePasses = nodeThis.SelectSingleNode("RenderPasses");
            if (nodePasses == null)
                return false;

            XmlNodeList? passes = nodePasses.SelectNodes("RenderPassData");
            if (passes == null)
                return false;

            EmptyChildrenList();
            foreach (XmlNode node in passes)
            {
                RenderPassData rp = new RenderPassData(0, "", null, false);
                rp.Deserialize(node, model);
                AddChild(rp);
            }

            return true;
        }

    }
}
