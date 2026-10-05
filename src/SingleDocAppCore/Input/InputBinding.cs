//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;
using System.Globalization;
using System.Xml;

namespace SingleDocAppCore.Input
{
    // Output range of analog controls (gamepad axes, MIDI controllers)
    public enum InputValueRange
    {
        Unipolar,       // 0..1
        Bipolar,        // -1..1
    }

    // Binding of an input channel to a single control of a device: the address of the control
    // and the transformation of its value. Reads the device state only through InputDeviceState
    // (no hardware access here).
    public abstract class InputBinding
    {
        // Type name stored in the input settings file - see InputBindingFactory
        public abstract string  TypeName            { get; }

        // Value of the control in the current frame (buttons: 0/1)
        public abstract float   Evaluate            (InputDeviceState state);

        // e.g. "Key Z", "Gamepad A", "MIDI CC 16 (ch 1)"
        public abstract string  GetDisplayName      ();

        protected abstract void SerializeAttributes     (XmlElement node);
        protected abstract void DeserializeAttributes   (XmlNode node);

        public InputBinding Clone()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlElement node = xmlDoc.CreateElement("Binding");
            SerializeAttributes(node);

            InputBinding copy = InputBindingFactory.Create(TypeName)!;
            copy.DeserializeAttributes(node);
            return copy;
        }

        public void Serialize(XmlDocument xmlDoc, XmlNode parent)
        {
            XmlElement node = XmlUtils.AddNode(xmlDoc, parent, "Binding");
            XmlUtils.AddAtributeString(node, "type", TypeName);
            SerializeAttributes(node);
        }

        // Returns null for an unknown binding type
        public static InputBinding? Deserialize(XmlNode node)
        {
            string? typeName = XmlUtils.LoadAttributeAsString(node, "type", null);
            InputBinding? binding = InputBindingFactory.Create(typeName);
            if (binding == null)
            {
                Console.WriteLine("WARNING: Unknown input binding type: {0}", typeName);
                return null;
            }

            binding.DeserializeAttributes(node);
            return binding;
        }

        // helpers

        protected static string FloatToString(float val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        protected static string BoolToString(bool val)
        {
            return val ? "true" : "false";
        }

        protected static T LoadEnum<T>(XmlNode node, string attributeName, T defaultValue) where T : struct
        {
            string? text = XmlUtils.LoadAttributeAsString(node, attributeName, null);
            if (text != null && Enum.TryParse(text, out T val))
                return val;
            return defaultValue;
        }

        // Unipolar (0..1) value converted to the requested range
        protected static float ToRange(float value01, InputValueRange range)
        {
            return (range == InputValueRange.Bipolar) ? value01 * 2.0f - 1.0f : value01;
        }
    }

    // Keyboard key (names: SingleDocAppFramework.Input.UiKey / OpenTK Keys), 0/1
    public class InputBindingKey : InputBinding
    {
        public  string      Key         = "Z";

        public override string TypeName => "Key";

        public override float Evaluate(InputDeviceState state)
        {
            return state.IsKeyDown(Key) ? 1.0f : 0.0f;
        }

        public override string GetDisplayName()
        {
            return String.Format("Key {0}", Key);
        }

        protected override void SerializeAttributes(XmlElement node)
        {
            XmlUtils.AddAtributeString(node, "key", Key);
        }

        protected override void DeserializeAttributes(XmlNode node)
        {
            Key = XmlUtils.LoadAttributeAsString(node, "key", Key) ?? Key;
        }
    }

    // Gamepad button, 0/1
    public class InputBindingGamepadButton : InputBinding
    {
        public  int                 Pad         = -1;       // gamepad slot, -1 - any
        public  GamepadButtonId     Button      = GamepadButtonId.A;

        public override string TypeName => "GamepadButton";

        public override float Evaluate(InputDeviceState state)
        {
            state.TryGetGamepadButton(Pad, Button, out bool down);
            return down ? 1.0f : 0.0f;
        }

        public override string GetDisplayName()
        {
            return String.Format("{0} {1}", InputBindingFactory.GetPadName(Pad), Button);
        }

        protected override void SerializeAttributes(XmlElement node)
        {
            XmlUtils.AddAtributeString(node, "pad"      , Pad.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "button"   , Button.ToString());
        }

        protected override void DeserializeAttributes(XmlNode node)
        {
            Pad     = XmlUtils.LoadAttributeAsInt(node, "pad", Pad);
            Button  = LoadEnum(node, "button", Button);
        }
    }

    // Gamepad axis: deadzone, sensitivity, invert, output range.
    // Sticks are -1..1 (Unipolar: 0.5 in the center), triggers 0..1 (Bipolar: -1 when released).
    public class InputBindingGamepadAxis : InputBinding
    {
        public  int                 Pad         = -1;       // gamepad slot, -1 - any
        public  GamepadAxisId       Axis        = GamepadAxisId.LeftX;
        public  float               Deadzone    = 0.1f;
        public  float               Sensitivity = 1.0f;
        public  bool                Invert      = false;
        public  InputValueRange     Range       = InputValueRange.Bipolar;

        public override string TypeName => "GamepadAxis";

        public static bool IsTrigger(GamepadAxisId axis)
        {
            return axis == GamepadAxisId.LeftTrigger || axis == GamepadAxisId.RightTrigger;
        }

        public override float Evaluate(InputDeviceState state)
        {
            state.TryGetGamepadAxis(Pad, Axis, out float v);

            // deadzone (rescaled, so the output starts at 0 at the edge of the deadzone)
            float dz = Math.Clamp(Deadzone, 0.0f, 0.99f);
            float mag = Math.Abs(v);
            mag = (mag <= dz) ? 0.0f : (mag - dz) / (1.0f - dz);
            v = Math.Sign(v) * Math.Min(1.0f, mag * Sensitivity);

            if (IsTrigger(Axis))
            {
                // 0..1
                if (Invert)
                    v = 1.0f - v;
                return ToRange(v, Range);
            }

            // -1..1
            if (Invert)
                v = -v;
            return (Range == InputValueRange.Unipolar) ? (v + 1.0f) * 0.5f : v;
        }

        public override string GetDisplayName()
        {
            return String.Format("{0} {1}", InputBindingFactory.GetPadName(Pad), Axis);
        }

        protected override void SerializeAttributes(XmlElement node)
        {
            XmlUtils.AddAtributeString(node, "pad"          , Pad.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "axis"         , Axis.ToString());
            XmlUtils.AddAtributeString(node, "deadzone"     , FloatToString(Deadzone));
            XmlUtils.AddAtributeString(node, "sensitivity"  , FloatToString(Sensitivity));
            XmlUtils.AddAtributeString(node, "invert"       , BoolToString(Invert));
            XmlUtils.AddAtributeString(node, "range"        , Range.ToString());
        }

        protected override void DeserializeAttributes(XmlNode node)
        {
            Pad         = XmlUtils.LoadAttributeAsInt   (node, "pad"        , Pad);
            Axis        = LoadEnum                      (node, "axis"       , Axis);
            Deadzone    = XmlUtils.LoadAttributeAsFloat (node, "deadzone"   , Deadzone);
            Sensitivity = XmlUtils.LoadAttributeAsFloat (node, "sensitivity", Sensitivity);
            Invert      = XmlUtils.LoadAttributeAsBool  (node, "invert"     , Invert);
            Range       = LoadEnum                      (node, "range"      , Range);
        }
    }

    // MIDI controller (CC): invert, output range. Before the first message the value is 0 (Bipolar: -1).
    public class InputBindingMidiController : InputBinding
    {
        public  string              Device      = "";       // device name, empty - any
        public  int                 Channel     = 1;        // 1..16
        public  int                 Controller  = 1;        // 0..127
        public  bool                Invert      = false;
        public  InputValueRange     Range       = InputValueRange.Unipolar;

        public override string TypeName => "MidiCC";

        public override float Evaluate(InputDeviceState state)
        {
            state.TryGetMidiController(Device, Channel, Controller, out float v);
            if (Invert)
                v = 1.0f - v;
            return ToRange(v, Range);
        }

        public override string GetDisplayName()
        {
            return String.Format("MIDI CC {0} (ch {1}){2}", Controller, Channel, InputBindingFactory.GetMidiDeviceSuffix(Device));
        }

        protected override void SerializeAttributes(XmlElement node)
        {
            XmlUtils.AddAtributeString(node, "device"   , Device);
            XmlUtils.AddAtributeString(node, "channel"  , Channel.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "cc"       , Controller.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "invert"   , BoolToString(Invert));
            XmlUtils.AddAtributeString(node, "range"    , Range.ToString());
        }

        protected override void DeserializeAttributes(XmlNode node)
        {
            Device      = XmlUtils.LoadAttributeAsString(node, "device" , Device) ?? "";
            Channel     = XmlUtils.LoadAttributeAsInt   (node, "channel", Channel);
            Controller  = XmlUtils.LoadAttributeAsInt   (node, "cc"     , Controller);
            Invert      = XmlUtils.LoadAttributeAsBool  (node, "invert" , Invert);
            Range       = LoadEnum                      (node, "range"  , Range);
        }
    }

    // MIDI note: 0/1 (pressed) or the velocity (0..1)
    public class InputBindingMidiNote : InputBinding
    {
        public  string              Device      = "";       // device name, empty - any
        public  int                 Channel     = 1;        // 1..16
        public  int                 Note        = 60;       // 0..127 (60 = C4)
        public  bool                UseVelocity = false;

        public override string TypeName => "MidiNote";

        public override float Evaluate(InputDeviceState state)
        {
            state.TryGetMidiNote(Device, Channel, Note, out float velocity);
            if (velocity <= 0.0f)
                return 0.0f;
            return UseVelocity ? velocity : 1.0f;
        }

        public override string GetDisplayName()
        {
            return String.Format("MIDI note {0} (ch {1}){2}", InputBindingFactory.GetNoteName(Note), Channel, InputBindingFactory.GetMidiDeviceSuffix(Device));
        }

        protected override void SerializeAttributes(XmlElement node)
        {
            XmlUtils.AddAtributeString(node, "device"   , Device);
            XmlUtils.AddAtributeString(node, "channel"  , Channel.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "note"     , Note.ToString(CultureInfo.InvariantCulture));
            XmlUtils.AddAtributeString(node, "velocity" , BoolToString(UseVelocity));
        }

        protected override void DeserializeAttributes(XmlNode node)
        {
            Device      = XmlUtils.LoadAttributeAsString(node, "device"     , Device) ?? "";
            Channel     = XmlUtils.LoadAttributeAsInt   (node, "channel"    , Channel);
            Note        = XmlUtils.LoadAttributeAsInt   (node, "note"       , Note);
            UseVelocity = XmlUtils.LoadAttributeAsBool  (node, "velocity"   , UseVelocity);
        }
    }

    public static class InputBindingFactory
    {
        // type name (stored in the file - do not change), display name
        public static readonly (string TypeName, string DisplayName)[] Types =
        {
            ("Key"              , "Keyboard key"    ),
            ("GamepadButton"    , "Gamepad button"  ),
            ("GamepadAxis"      , "Gamepad axis"    ),
            ("MidiCC"           , "MIDI controller (CC)"),
            ("MidiNote"         , "MIDI note"       ),
        };

        private static readonly string[] NoteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };

        public static InputBinding? Create(string? typeName)
        {
            switch(typeName)
            {
                case "Key":             return new InputBindingKey();
                case "GamepadButton":   return new InputBindingGamepadButton();
                case "GamepadAxis":     return new InputBindingGamepadAxis();
                case "MidiCC":          return new InputBindingMidiController();
                case "MidiNote":        return new InputBindingMidiNote();
            }
            return null;
        }

        // 60 = C4
        public static string GetNoteName(int note)
        {
            if (note < 0 || note > 127)
                return note.ToString(CultureInfo.InvariantCulture);
            return String.Format("{0}{1}", NoteNames[note % 12], note / 12 - 1);
        }

        public static string GetPadName(int pad)
        {
            return (pad < 0) ? "Gamepad" : String.Format("Gamepad {0}", pad + 1);
        }

        public static string GetMidiDeviceSuffix(string device)
        {
            return String.IsNullOrEmpty(device) ? "" : String.Format(" [{0}]", device);
        }
    }
}
