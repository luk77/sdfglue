//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppCore.Input
{
    // Optional input system of a SingleDocApp application: input devices (backends), the state of
    // all devices in the current frame, input channels with their bindings and "Learn" (binding a channel
    // by moving a control). Channels are user data stored in a separate file, saved automatically after changes.
    public class InputSystem : IDisposable
    {
        public const double             LearnTimeout        = 10.0;     // [s]
        public const double             SaveDelay           = 0.5;      // [s] after the last change
        public const string             CancelLearnKey      = "Escape";

        private List<IInputDevice>                  devices_;
        private InputDeviceState                    state_              = new InputDeviceState();
        private InputDeviceState                    learnBaseline_      = new InputDeviceState();   // analog controls: state at the start
        private InputDeviceState                    learnPrevious_      = new InputDeviceState();   // digital controls: previous frame
        private string                              filePath_;
        private Action<InputChannelsCollection>?    initDefaults_;

        private double                  realTime_           = 0.0;
        private bool                    dirty_              = false;
        private double                  lastChangeTime_     = 0.0;

        // learn
        private InputChannel?           learnChannel_       = null;
        private int                     learnBindingIndex_  = -1;       // -1 - add a new binding
        private double                  learnStartTime_     = 0.0;

        public  InputChannelsCollection Channels            = new InputChannelsCollection();

        // Keys that can't be bound (used by the application as shortcuts, navigation etc.), names as in UiKey
        public  HashSet<string>         ReservedKeys        = new HashSet<string>();

        // Message of the last "Learn" (e.g. the learned control is used by another channel)
        public  string?                 LastLearnMessage    = null;

        // initDefaults - fills the default channels (used when there is no file and by RestoreDefaults())
        public InputSystem(IEnumerable<IInputDevice> devices, string filePath, Action<InputChannelsCollection>? initDefaults)
        {
            devices_        = new List<IInputDevice>(devices);
            filePath_       = filePath;
            initDefaults_   = initDefaults;
        }

        public IReadOnlyList<IInputDevice>  Devices     { get { return devices_; } }
        public InputDeviceState             State       { get { return state_; } }
        public string                       FilePath    { get { return filePath_; } }

        // Time since the start of the input system (x axis of plots)
        public float GetRealTime()
        {
            return (float)realTime_;
        }

        // Loads the channels from the file. A missing file is created with the default channels,
        // an invalid one is left untouched (defaults are used until the next change).
        public void Load()
        {
            if (File.Exists(filePath_))
            {
                if (Channels.LoadFromFile(filePath_))
                {
                    Console.WriteLine("Input settings loaded: {0}", filePath_);
                    return;
                }
                Console.WriteLine("WARNING: Default input channels are used");
                InitDefaults();
                return;
            }

            InitDefaults();
            if (Channels.SaveToFile(filePath_))
                Console.WriteLine("Input settings file created with default values: {0}", filePath_);
        }

        public void RestoreDefaults()
        {
            CancelLearn();
            InitDefaults();
            NotifyChanged();
        }

        private void InitDefaults()
        {
            Channels.Clear();
            initDefaults_?.Invoke(Channels);
        }

        // Called after every change of channels/bindings - the file is saved automatically
        public void NotifyChanged()
        {
            dirty_          = true;
            lastChangeTime_ = realTime_;
        }

        public void SaveIfChanged()
        {
            if (!dirty_)
                return;

            dirty_ = false;
            Channels.SaveToFile(filePath_);
        }

        public bool TryGetChannelValue(int channelId, out float value)
        {
            InputChannel? channel = Channels.FindById(channelId);
            value = channel?.Value ?? 0.0f;
            return channel != null;
        }

        public void RemoveChannel(InputChannel channel)
        {
            if (learnChannel_ == channel)
                CancelLearn();
            Channels.Channels.Remove(channel);
            NotifyChanged();
        }

        // Called once per frame (main thread): polls devices, evaluates channels, handles "Learn" and saving
        public void Update(double deltaTime)
        {
            realTime_ += deltaTime;

            state_.Clear();
            foreach(IInputDevice device in devices_)
            {
                try
                {
                    device.Update(state_);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("ERROR: Input device {0}: {1}", device.Name, ex.Message);
                }
            }

            foreach(InputChannel channel in Channels.Channels)
            {
                channel.Value = channel.Evaluate(state_);
                channel.History.Add((float)realTime_, channel.Value);
            }

            UpdateLearn();

            if (dirty_ && realTime_ - lastChangeTime_ >= SaveDelay)
                SaveIfChanged();
        }

        public void RescanDevices()
        {
            foreach(IInputDevice device in devices_)
                device.Rescan();
        }

        public void Dispose()
        {
            SaveIfChanged();

            foreach(IInputDevice device in devices_)
                device.Dispose();
            devices_.Clear();
        }

        //-------------------------------------------------------------------
        // Learn: the first control changed after StartLearn() is bound to the channel
        //-------------------------------------------------------------------

        // bindingIndex: index of the binding to replace, -1 - add a new binding
        public void StartLearn(InputChannel channel, int bindingIndex = -1)
        {
            learnChannel_       = channel;
            learnBindingIndex_  = bindingIndex;
            learnStartTime_     = realTime_;
            learnBaseline_.CopyFrom(state_);
            learnPrevious_.CopyFrom(state_);
            LastLearnMessage    = null;
        }

        public void CancelLearn()
        {
            learnChannel_ = null;
        }

        public bool IsLearning()
        {
            return learnChannel_ != null;
        }

        public bool IsLearning(InputChannel channel, int bindingIndex = -1)
        {
            return learnChannel_ == channel && learnBindingIndex_ == bindingIndex;
        }

        public float GetLearnTimeLeft()
        {
            return (float)Math.Max(0.0, LearnTimeout - (realTime_ - learnStartTime_));
        }

        private void UpdateLearn()
        {
            if (learnChannel_ == null)
                return;

            if (state_.IsKeyDown(CancelLearnKey) || GetLearnTimeLeft() <= 0.0f || !Channels.Channels.Contains(learnChannel_))
            {
                learnChannel_ = null;
                return;
            }

            InputBinding? binding = DetectChange(learnBaseline_, learnPrevious_, state_);
            learnPrevious_.CopyFrom(state_);
            if (binding == null)
                return;

            InputChannel channel = learnChannel_;
            learnChannel_ = null;

            if (learnBindingIndex_ >= 0 && learnBindingIndex_ < channel.Bindings.Count)
                channel.Bindings[learnBindingIndex_] = binding;
            else
                channel.Bindings.Add(binding);

            // the same control in other channels is allowed, but reported
            string bindingName = binding.GetDisplayName();
            List<string> otherChannels = new List<string>();
            foreach(InputChannel other in Channels.Channels)
            {
                if (other != channel && other.Bindings.Exists(b => b.GetDisplayName() == bindingName))
                    otherChannels.Add(other.Name);
            }
            LastLearnMessage = (otherChannels.Count > 0)
                ? String.Format("{0} is also used by: {1}", bindingName, String.Join(", ", otherChannels))
                : String.Format("Learned: {0}", bindingName);

            NotifyChanged();
        }

        // Returns a binding for the first control that changed (or null).
        // Keys, buttons and notes: pressed since the previous frame; axes and controllers (CC): moved
        // since the start (baseline), so a slow movement is detected too.
        public InputBinding? DetectChange(InputDeviceState baseline, InputDeviceState previous, InputDeviceState current)
        {
            // keyboard
            foreach(string key in current.KeysDown)
            {
                if (!previous.KeysDown.Contains(key) && !ReservedKeys.Contains(key))
                    return new InputBindingKey() { Key = key };
            }

            // gamepads
            foreach(GamepadSnapshot pad in current.Gamepads)
            {
                GamepadSnapshot? basePad = baseline.Gamepads.Find(p => p.Slot == pad.Slot);
                GamepadSnapshot? prevPad = previous.Gamepads.Find(p => p.Slot == pad.Slot);
                for (int i = 0; i < pad.Buttons.Length; i++)
                {
                    bool wasDown = prevPad != null && prevPad.Buttons[i];
                    if (pad.Buttons[i] && !wasDown)
                        return new InputBindingGamepadButton() { Pad = -1, Button = (GamepadButtonId)i };
                }
                for (int i = 0; i < pad.Axes.Length; i++)
                {
                    GamepadAxisId axis = (GamepadAxisId)i;
                    float baseVal = (basePad != null) ? basePad.Axes[i] : 0.0f;
                    if (Math.Abs(pad.Axes[i] - baseVal) > 0.5f)
                    {
                        InputBindingGamepadAxis binding = new InputBindingGamepadAxis() { Pad = -1, Axis = axis };
                        binding.Range = InputBindingGamepadAxis.IsTrigger(axis) ? InputValueRange.Unipolar : InputValueRange.Bipolar;
                        return binding;
                    }
                }
            }

            // MIDI
            foreach(MidiDeviceSnapshot midi in current.MidiDevices)
            {
                MidiDeviceSnapshot? baseMidi = baseline.MidiDevices.Find(m => m.Name == midi.Name);
                MidiDeviceSnapshot? prevMidi = previous.MidiDevices.Find(m => m.Name == midi.Name);
                for (int channel = 1; channel <= MidiDeviceSnapshot.NumChannels; channel++)
                {
                    for (int number = 0; number < MidiDeviceSnapshot.NumValues; number++)
                    {
                        int index = MidiDeviceSnapshot.GetIndex(channel, number);

                        float cc = midi.ControllerValues[index];
                        float baseCC = (baseMidi != null) ? baseMidi.ControllerValues[index] : float.NaN;
                        if (!float.IsNaN(cc) && (float.IsNaN(baseCC) || Math.Abs(cc - baseCC) > 0.5f / 127.0f))
                            return new InputBindingMidiController() { Device = midi.Name, Channel = channel, Controller = number };

                        float prevNote = (prevMidi != null) ? prevMidi.NoteVelocities[index] : 0.0f;
                        if (midi.NoteVelocities[index] > 0.0f && prevNote <= 0.0f)
                            return new InputBindingMidiNote() { Device = midi.Name, Channel = channel, Note = number };
                    }
                }
            }

            return null;
        }
    }
}
