//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Windowing.GraphicsLibraryFramework;
using SingleDocAppCore.Input;

namespace SingleDocAppFramework.Input
{
    // Gamepads through the GLFW gamepad API (standard layout from the built-in SDL mapping database).
    // Must be updated on the main thread.
    public class GamepadInputDevice : IInputDevice
    {
        private const int       MaxJoysticks    = 16;   // GLFW_JOYSTICK_1 .. GLFW_JOYSTICK_16

        private List<string>    connected_      = new List<string>();

        public string Name => "Gamepads";

        public string GetStatus()
        {
            return connected_.Count > 0 ? String.Join(", ", connected_) : "no gamepad connected";
        }

        public void Update(InputDeviceState state)
        {
            connected_.Clear();

            for (int jid = 0; jid < MaxJoysticks; jid++)
            {
                if (!GLFW.JoystickPresent(jid) || !GLFW.JoystickIsGamepad(jid))
                    continue;

                if (!GLFW.GetGamepadState(jid, out GamepadState gamepadState))
                    continue;

                string name = GLFW.GetGamepadName(jid) ?? "Gamepad";
                connected_.Add(String.Format("{0}: {1}", jid + 1, name));

                GamepadSnapshot snapshot = new GamepadSnapshot(jid, name);
                unsafe
                {
                    for (int i = 0; i < snapshot.Buttons.Length; i++)
                        snapshot.Buttons[i] = gamepadState.Buttons[i] != 0;
                    for (int i = 0; i < snapshot.Axes.Length; i++)
                        snapshot.Axes[i] = gamepadState.Axes[i];
                }

                // GLFW triggers: -1 (released) .. 1, here: 0..1
                snapshot.Axes[(int)GamepadAxisId.LeftTrigger]   = (snapshot.Axes[(int)GamepadAxisId.LeftTrigger]  + 1.0f) * 0.5f;
                snapshot.Axes[(int)GamepadAxisId.RightTrigger]  = (snapshot.Axes[(int)GamepadAxisId.RightTrigger] + 1.0f) * 0.5f;

                state.Gamepads.Add(snapshot);
            }
        }

        // Gamepads are enumerated every frame
        public void Rescan()
        {
        }

        public void Dispose()
        {
        }
    }
}
