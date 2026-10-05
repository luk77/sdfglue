//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Input;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Values of input channels (keyboard, gamepad, MIDI - see SingleDocAppCore.Input).
    // Set by the application (the input system is optional and device access is platform specific).
    public interface ISignalInputProvider
    {
        // Returns false if the channel does not exist
        bool TryGetInputChannelValue(int channelId, out float value);
    }

    // ISignalInputProvider for the SingleDocApp input system
    public class InputSystemSignalProvider : ISignalInputProvider
    {
        private InputSystem     inputs_;

        public InputSystemSignalProvider(InputSystem inputs)
        {
            inputs_ = inputs;
        }

        public bool TryGetInputChannelValue(int channelId, out float value)
        {
            return inputs_.TryGetChannelValue(channelId, out value);
        }
    }

    // Input provider used by all signals (devices are global for the application, not per project)
    public static class SignalInputs
    {
        public static ISignalInputProvider? Provider = null;
    }

    // Data passed to signal evaluation in a single update
    public class SignalContext
    {
        public  double                  Time            = 0.0;      // project time (DataModel.CurrentTime, the same as iTime)
        public  double                  RealTime        = 0.0;      // time since the application start (used by the history/plots)
        public  int                     UpdateIndex     = 0;        // incremented on every update (evaluation cache)
        public  ISignalInputProvider?   Inputs          = null;
        public  SignalsCollection?      Signals         = null;     // for signals referring to other signals

        // Set during evaluation of a signal with a real time source (e.g. an input channel):
        // stateful operators use RealTime, so e.g. smoothing works also when the playback is paused
        public  bool                    UseRealTime     = false;

        public double GetOperatorsTime()
        {
            return UseRealTime ? RealTime : Time;
        }
    }
}
