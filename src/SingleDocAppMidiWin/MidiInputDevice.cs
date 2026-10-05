//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using NAudio.Midi;
using SingleDocAppCore.Input;
using System.Diagnostics;

namespace SingleDocAppMidiWin
{
    // MIDI input devices (Windows, winmm through NAudio.Midi). All inputs are opened; controller (CC) values
    // and note velocities are collected from the MIDI callback thread and copied to the frame state in Update().
    // Note: winmm (MME) opens a device exclusively - a device used by another application is reported as busy.
    public class MidiInputDevice : IInputDevice
    {
        private const double    RescanInterval  = 2.0;      // [s] new / disconnected devices

        private class MidiInputPort
        {
            public  string      Name;
            public  MidiIn?     MidiIn          = null;
            public  string?     Error           = null;
            public  object      Lock            = new object();
            public  float[]     Controllers     = new float[MidiDeviceSnapshot.NumChannels * MidiDeviceSnapshot.NumValues];
            public  float[]     Notes           = new float[MidiDeviceSnapshot.NumChannels * MidiDeviceSnapshot.NumValues];

            public MidiInputPort(string name)
            {
                Name = name;
                Array.Fill(Controllers, float.NaN);
            }
        }

        private List<MidiInputPort>     ports_              = new List<MidiInputPort>();
        private List<string>            portNames_          = new List<string>();
        private Stopwatch               rescanTimer_        = Stopwatch.StartNew();

        public MidiInputDevice()
        {
            Rescan();
        }

        public string Name => "MIDI";

        public string GetStatus()
        {
            if (ports_.Count == 0)
                return "no MIDI input device";

            List<string> items = new List<string>();
            foreach(MidiInputPort port in ports_)
                items.Add(port.Error == null ? port.Name : String.Format("{0} ({1})", port.Name, port.Error));
            return String.Join(", ", items);
        }

        public void Update(InputDeviceState state)
        {
            if (rescanTimer_.Elapsed.TotalSeconds >= RescanInterval)
            {
                rescanTimer_.Restart();
                if (!GetDeviceNames().SequenceEqual(portNames_))
                    Rescan();
            }

            foreach(MidiInputPort port in ports_)
            {
                if (port.MidiIn == null)
                    continue;

                MidiDeviceSnapshot snapshot = new MidiDeviceSnapshot(port.Name);
                lock(port.Lock)
                {
                    Array.Copy(port.Controllers, snapshot.ControllerValues, snapshot.ControllerValues.Length);
                    Array.Copy(port.Notes, snapshot.NoteVelocities, snapshot.NoteVelocities.Length);
                }
                state.MidiDevices.Add(snapshot);
            }
        }

        // Closes all inputs and opens the currently available ones
        public void Rescan()
        {
            ClosePorts();

            portNames_ = GetDeviceNames();
            for (int i = 0; i < portNames_.Count; i++)
            {
                MidiInputPort port = new MidiInputPort(GetUniqueName(portNames_[i]));
                try
                {
                    MidiIn midiIn = new MidiIn(i);
                    midiIn.MessageReceived += (sender, e) => OnMessageReceived(port, e.RawMessage);
                    midiIn.Start();
                    port.MidiIn = midiIn;
                }
                catch(Exception ex)
                {
                    port.Error = "busy or unavailable";
                    Console.WriteLine("WARNING: Opening MIDI input failed: {0}, {1}", port.Name, ex.Message);
                }
                ports_.Add(port);
            }
        }

        public void Dispose()
        {
            ClosePorts();
        }

        private static List<string> GetDeviceNames()
        {
            List<string> names = new List<string>();
            try
            {
                for (int i = 0; i < MidiIn.NumberOfDevices; i++)
                    names.Add(MidiIn.DeviceInfo(i).ProductName);
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Enumerating MIDI inputs failed: {0}", ex.Message);
            }
            return names;
        }

        // Devices with the same product name get a suffix (" #2"...)
        private string GetUniqueName(string name)
        {
            string uniqueName = name;
            for (int n = 2; ports_.Exists(p => p.Name == uniqueName); n++)
                uniqueName = String.Format("{0} #{1}", name, n);
            return uniqueName;
        }

        private void ClosePorts()
        {
            foreach(MidiInputPort port in ports_)
            {
                if (port.MidiIn == null)
                    continue;

                try
                {
                    port.MidiIn.Stop();
                    port.MidiIn.Dispose();
                }
                catch(Exception ex)
                {
                    Console.WriteLine("WARNING: Closing MIDI input failed: {0}, {1}", port.Name, ex.Message);
                }
                port.MidiIn = null;
            }
            ports_.Clear();
        }

        // MIDI callback thread
        private static void OnMessageReceived(MidiInputPort port, int rawMessage)
        {
            int status  = rawMessage & 0xF0;
            int channel = (rawMessage & 0x0F) + 1;
            int data1   = (rawMessage >> 8) & 0x7F;
            int data2   = (rawMessage >> 16) & 0x7F;
            int index   = MidiDeviceSnapshot.GetIndex(channel, data1);

            lock(port.Lock)
            {
                switch(status)
                {
                    case 0xB0:  port.Controllers[index] = data2 / 127.0f;   break;  // control change
                    case 0x90:  port.Notes[index]       = data2 / 127.0f;   break;  // note on (velocity 0 = note off)
                    case 0x80:  port.Notes[index]       = 0.0f;             break;  // note off
                }
            }
        }
    }
}
