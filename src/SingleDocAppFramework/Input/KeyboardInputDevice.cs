//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using OpenTK.Windowing.Desktop;
using SingleDocAppCore.Input;
using Keys = OpenTK.Windowing.GraphicsLibraryFramework.Keys;

namespace SingleDocAppFramework.Input
{
    // Keyboard input device (OpenTK keyboard state of the main window). Key names are UiKey names.
    // isActive - when false no key is reported (e.g. text input in ImGui, Ctrl/Alt shortcuts, no focus).
    public class KeyboardInputDevice : IInputDevice
    {
        private NativeWindow        window_;
        private Func<bool>          isActive_;
        private bool                active_         = false;

        // all keys (without aliases), names as in UiKey
        private static readonly (Keys Key, string Name)[] AllKeys = GetAllKeys();

        public KeyboardInputDevice(NativeWindow window, Func<bool> isActive)
        {
            window_     = window;
            isActive_   = isActive;
        }

        public string Name => "Keyboard";

        public static IEnumerable<string> GetKeyNames()
        {
            return AllKeys.Select(k => k.Name);
        }

        private static (Keys, string)[] GetAllKeys()
        {
            List<(Keys, string)> keys = new List<(Keys, string)>();
            HashSet<int> values = new HashSet<int>();
            foreach(string name in Enum.GetNames<UiKey>())
            {
                int value = (int)Enum.Parse<UiKey>(name);
                if (value < 0 || name == nameof(UiKey.LastKey) || !values.Add(value))
                    continue;
                keys.Add(((Keys)value, name));
            }
            return keys.ToArray();
        }

        public string GetStatus()
        {
            return active_ ? "active" : "inactive (text input, Ctrl/Alt or no focus)";
        }

        public void Update(InputDeviceState state)
        {
            active_ = isActive_();
            if (!active_)
                return;

            OpenTK.Windowing.GraphicsLibraryFramework.KeyboardState keyboard = window_.KeyboardState;
            foreach((Keys key, string name) in AllKeys)
            {
                if (keyboard.IsKeyDown(key))
                    state.KeysDown.Add(name);
            }
        }

        public void Rescan()
        {
        }

        public void Dispose()
        {
        }
    }
}
