//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalsCollection : TreeNode
    {
        public SignalsCollection() : base(DataModel.NodeIdSignalsCollection, "Signals")
        {
        }

        public override void ResetPrevVal()
        {
        }

        public void Update(double deltaTime)
        {
            foreach(TreeNode node in Children)
            {
                SignalInstance? signal = node as SignalInstance;
                if (signal == null)
                    continue;

                signal.Update(deltaTime);
            }
        }
    }
}
