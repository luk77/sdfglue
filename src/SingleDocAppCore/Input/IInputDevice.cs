//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Input
{
    // Hardware/platform specific input backend (keyboard, gamepads, MIDI, ...).
    // Implemented outside SingleDocAppCore; the input system works only through this interface.
    public interface IInputDevice : IDisposable
    {
        // e.g. "Keyboard", "Gamepads", "MIDI"
        string  Name            { get; }

        // Short status for the UI (e.g. names of connected devices, errors)
        string  GetStatus       ();

        // Called once per frame on the main thread: polls the device and writes its part of the state
        void    Update          (InputDeviceState state);

        // Re-enumerates devices (e.g. after connecting a new MIDI controller)
        void    Rescan          ();
    }
}
