//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalsCollection : TreeNode
    {
        private SignalContext   ctx_            = new SignalContext();
        private double          realTime_       = 0.0;
        private double          lastTime_       = 0.0;

        public SignalsCollection() : base(DataModel.NodeIdSignalsCollection, "Signals")
        {
        }

        public override void ResetPrevVal()
        {
            base.ResetPrevVal();
        }

        public SignalInstance? FindById(int id)
        {
            if (id == 0)
                return null;

            foreach(TreeNode node in Children)
            {
                if (node is SignalInstance signal && signal.Id == id)
                    return signal;
            }
            return null;
        }

        // time - project time (DataModel.CurrentTime), deltaTime - real time since the last update
        public void Update(double time, double deltaTime)
        {
            realTime_ += deltaTime;

            // the project time jumped back (frame counter reset, step back) - restart stateful operators
            bool resetState = time < lastTime_;
            lastTime_ = time;

            ctx_.Time           = time;
            ctx_.RealTime       = realTime_;
            ctx_.UpdateIndex++;
            ctx_.Inputs         = SignalInputs.Provider;
            ctx_.Signals        = this;

            foreach(TreeNode node in Children)
            {
                if (node is not SignalInstance signal)
                    continue;

                if (resetState && !signal.Source.UsesRealTime)
                    signal.ResetState();

                if (!signal.Enabled.Val)
                    continue;

                float val = signal.Evaluate(ctx_);
                signal.History.Add((float)realTime_, val);
            }
        }

        // Signals with the "Input channel" source reading the given channel
        public List<SignalInstance> FindInputChannelUsers(int channelId)
        {
            List<SignalInstance> users = new List<SignalInstance>();
            foreach(TreeNode node in Children)
            {
                if (node is SignalInstance signal && signal.Source is SignalSrcInputChannel && signal.Source.GetInt("channel") == channelId)
                    users.Add(signal);
            }
            return users;
        }

        public float GetRealTime()
        {
            return (float)realTime_;
        }

        // Value of a signal referenced by another signal (SignalRef source, Combine operator)
        internal static float EvaluateReferenced(SignalContext ctx, int signalId)
        {
            SignalInstance? signal = ctx.Signals?.FindById(signalId);
            if (signal == null || !signal.Enabled.Val)
                return 0.0f;

            return signal.Evaluate(ctx);
        }
    }
}
