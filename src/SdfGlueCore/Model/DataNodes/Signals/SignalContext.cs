//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Input values from external devices (MIDI, later: keyboard, gamepad, UDP).
    // Implemented by the application (device access is platform specific).
    public interface ISignalInputProvider
    {
        // Normalized value (0..1) of a MIDI controller (CC); channel: 1..16, controller: 0..127.
        // Returns false if the value is not available (no device / no message received yet).
        bool TryGetMidiControllerValue(int channel, int controller, out float value);
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
    }
}
