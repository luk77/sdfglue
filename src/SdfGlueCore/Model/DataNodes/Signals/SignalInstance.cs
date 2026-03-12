//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.DataNodes.Signals
{
    public abstract class SignalInstance : TreeNode
    {
        protected float value_ = 0.0f;

        public SignalInstance() : base(0, "Signal")
        {
            Update(0.0f);
        }

        public override void ResetPrevVal()
        {
        }

        public float GetCurrentValue()
        {
            return value_;
        }

        public abstract void Update(double deltaTime);

    }
}
