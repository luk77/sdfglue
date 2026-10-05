//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Input
{
    // Standard gamepad layout (the order matches GLFW gamepad buttons)
    public enum GamepadButtonId
    {
        A, B, X, Y,
        LeftBumper, RightBumper,
        Back, Start, Guide,
        LeftThumb, RightThumb,
        DPadUp, DPadRight, DPadDown, DPadLeft,
    }

    // Standard gamepad layout (the order matches GLFW gamepad axes).
    // Sticks: -1..1, triggers: 0..1 (released = 0).
    public enum GamepadAxisId
    {
        LeftX, LeftY, RightX, RightY,
        LeftTrigger, RightTrigger,
    }

    // State of a single gamepad in the current frame
    public class GamepadSnapshot
    {
        public  int         Slot;
        public  string      Name;
        public  bool[]      Buttons     = new bool[Enum.GetValues<GamepadButtonId>().Length];
        public  float[]     Axes        = new float[Enum.GetValues<GamepadAxisId>().Length];

        public GamepadSnapshot(int slot, string name)
        {
            Slot = slot;
            Name = name;
        }
    }

    // State of a single MIDI input device in the current frame
    public class MidiDeviceSnapshot
    {
        public const int    NumChannels     = 16;
        public const int    NumValues       = 128;

        public  string      Name;
        public  float[]     ControllerValues    = new float[NumChannels * NumValues];   // 0..1, NaN - no message received yet
        public  float[]     NoteVelocities      = new float[NumChannels * NumValues];   // 0..1, 0 - note off

        public MidiDeviceSnapshot(string name)
        {
            Name = name;
            Array.Fill(ControllerValues, float.NaN);
        }

        // channel: 1..16, number: 0..127
        public static int GetIndex(int channel, int number)
        {
            return (channel - 1) * NumValues + number;
        }

        public static bool IsValidAddress(int channel, int number)
        {
            return channel >= 1 && channel <= NumChannels && number >= 0 && number < NumValues;
        }
    }

    // Snapshot of all input devices for the current frame, filled by IInputDevice.Update()
    // and read by bindings. Values are consistent during the whole frame.
    public class InputDeviceState
    {
        public  HashSet<string>             KeysDown        = new HashSet<string>();
        public  List<GamepadSnapshot>       Gamepads        = new List<GamepadSnapshot>();
        public  List<MidiDeviceSnapshot>    MidiDevices     = new List<MidiDeviceSnapshot>();

        public void Clear()
        {
            KeysDown.Clear();
            Gamepads.Clear();
            MidiDevices.Clear();
        }

        // Deep copy (e.g. the reference state for "Learn")
        public void CopyFrom(InputDeviceState other)
        {
            Clear();
            KeysDown.UnionWith(other.KeysDown);
            foreach(GamepadSnapshot src in other.Gamepads)
            {
                GamepadSnapshot dst = new GamepadSnapshot(src.Slot, src.Name);
                Array.Copy(src.Buttons, dst.Buttons, dst.Buttons.Length);
                Array.Copy(src.Axes, dst.Axes, dst.Axes.Length);
                Gamepads.Add(dst);
            }
            foreach(MidiDeviceSnapshot src in other.MidiDevices)
            {
                MidiDeviceSnapshot dst = new MidiDeviceSnapshot(src.Name);
                Array.Copy(src.ControllerValues, dst.ControllerValues, dst.ControllerValues.Length);
                Array.Copy(src.NoteVelocities, dst.NoteVelocities, dst.NoteVelocities.Length);
                MidiDevices.Add(dst);
            }
        }

        public bool IsKeyDown(string keyName)
        {
            return KeysDown.Contains(keyName);
        }

        // pad: gamepad slot, -1 - any gamepad
        public bool TryGetGamepadButton(int pad, GamepadButtonId button, out bool down)
        {
            down = false;
            bool found = false;
            foreach(GamepadSnapshot gamepad in Gamepads)
            {
                if (pad >= 0 && gamepad.Slot != pad)
                    continue;

                found = true;
                if (gamepad.Buttons[(int)button])
                    down = true;
            }
            return found;
        }

        // pad: gamepad slot, -1 - any gamepad (the value with the largest magnitude)
        public bool TryGetGamepadAxis(int pad, GamepadAxisId axis, out float value)
        {
            value = 0.0f;
            bool found = false;
            foreach(GamepadSnapshot gamepad in Gamepads)
            {
                if (pad >= 0 && gamepad.Slot != pad)
                    continue;

                float v = gamepad.Axes[(int)axis];
                if (!found || Math.Abs(v) > Math.Abs(value))
                    value = v;
                found = true;
            }
            return found;
        }

        // device: device name, empty - any device; channel: 1..16
        public bool TryGetMidiController(string device, int channel, int controller, out float value01)
        {
            value01 = 0.0f;
            if (!MidiDeviceSnapshot.IsValidAddress(channel, controller))
                return false;

            int index = MidiDeviceSnapshot.GetIndex(channel, controller);
            foreach(MidiDeviceSnapshot midi in MidiDevices)
            {
                if (device.Length > 0 && midi.Name != device)
                    continue;

                float v = midi.ControllerValues[index];
                if (!float.IsNaN(v))
                {
                    value01 = v;
                    return true;
                }
            }
            return false;
        }

        // device: device name, empty - any device; channel: 1..16; velocity01 = 0 - note off
        public bool TryGetMidiNote(string device, int channel, int note, out float velocity01)
        {
            velocity01 = 0.0f;
            if (!MidiDeviceSnapshot.IsValidAddress(channel, note))
                return false;

            int index = MidiDeviceSnapshot.GetIndex(channel, note);
            bool found = false;
            foreach(MidiDeviceSnapshot midi in MidiDevices)
            {
                if (device.Length > 0 && midi.Name != device)
                    continue;

                found = true;
                velocity01 = Math.Max(velocity01, midi.NoteVelocities[index]);
            }
            return found;
        }
    }
}
