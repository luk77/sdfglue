//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Helper functions used by signal sources and operators
    internal static class SignalMath
    {
        public static double Frac(double x)
        {
            return x - Math.Floor(x);
        }

        // Positive modulo
        public static double Mod(double x, double y)
        {
            return x - y * Math.Floor(x / y);
        }

        // Integer hash -> 0..1 (deterministic for a given seed)
        public static float Hash01(long i, int seed)
        {
            unchecked
            {
                uint h = (uint)i * 0x9E3779B1u ^ (uint)(i >> 32) * 0x85EBCA77u ^ (uint)seed * 0xC2B2AE3Du;
                h ^= h >> 15;
                h *= 0x2C1B3C6Du;
                h ^= h >> 12;
                h *= 0x297A2D39u;
                h ^= h >> 15;
                return (h & 0x00FFFFFFu) / (float)0x01000000u;
            }
        }

        // 1D value noise, result -1..1
        public static float ValueNoise(double x, int seed)
        {
            double i = Math.Floor(x);
            float f = (float)(x - i);
            float u = f * f * (3.0f - 2.0f * f);
            float a = Hash01((long)i, seed);
            float b = Hash01((long)i + 1, seed);
            return 2.0f * (a + (b - a) * u) - 1.0f;
        }
    }

    public class SignalSrcConstant : SignalSource
    {
        public SignalSrcConstant()
        {
            AddFloat("value", "Value", 0.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            return GetFloat("value");
        }
    }

    public class SignalSrcTime : SignalSource
    {
        public SignalSrcTime()
        {
            AddFloat("speed"    , "Speed"       , 1.0f, 0.01f);
            AddFloat("offset"   , "Offset"      , 0.0f, 0.01f);
            AddFloat("period"   , "Period (0 = off)", 0.0f, 0.01f, LimitsType.Min, 0.0f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            double t = ctx.Time * GetFloat("speed") + GetFloat("offset");
            float period = GetFloat("period");
            if (period > 0.0f)
                t = SignalMath.Mod(t, period);
            return (float)t;
        }
    }

    public class SignalSrcOscillator : SignalSource
    {
        public static readonly string[] WaveformNames = { "Sine", "Triangle", "Saw", "Square", "Pulse" };

        public SignalSrcOscillator()
        {
            AddEnum ("waveform"     , "Waveform"    , WaveformNames, 0);
            AddFloat("frequency"    , "Frequency [Hz]", 0.5f, 0.01f, LimitsType.Min, 0.0f);
            AddFloat("phase"        , "Phase [0..1]", 0.0f, 0.01f);
            AddFloat("duty"         , "Pulse duty"  , 0.5f, 0.01f, LimitsType.MinMax, 0.0f, 1.0f);
            AddFloat("amplitude"    , "Amplitude"   , 1.0f, 0.01f);
            AddFloat("offset"       , "Offset"      , 0.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            double p = SignalMath.Frac(ctx.Time * GetFloat("frequency") + GetFloat("phase"));

            // all waveforms: -1..1, value 0 (or rising edge) at phase 0
            double wave = 0.0;
            switch (GetInt("waveform"))
            {
                case 0: wave = Math.Sin(2.0 * Math.PI * p);                         break;
                case 1: wave = 1.0 - 4.0 * Math.Abs(SignalMath.Frac(p + 0.25) - 0.5); break;
                case 2: wave = 2.0 * SignalMath.Frac(p + 0.5) - 1.0;               break;
                case 3: wave = (p < 0.5) ? 1.0 : -1.0;                              break;
                case 4: wave = (p < GetFloat("duty")) ? 1.0 : -1.0;                 break;
            }

            return GetFloat("offset") + GetFloat("amplitude") * (float)wave;
        }
    }

    public class SignalSrcSmoothNoise : SignalSource
    {
        public SignalSrcSmoothNoise()
        {
            AddFloat("frequency"    , "Frequency [Hz]", 1.0f, 0.01f, LimitsType.Min, 0.0f);
            AddInt  ("seed"         , "Seed"        , 1);
            AddInt  ("octaves"      , "Octaves"     , 1, LimitsType.MinMax, 1, 6);
            AddFloat("amplitude"    , "Amplitude"   , 1.0f, 0.01f);
            AddFloat("offset"       , "Offset"      , 0.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            int     seed        = GetInt("seed");
            int     octaves     = Math.Clamp(GetInt("octaves"), 1, 6);
            double  x           = ctx.Time * GetFloat("frequency");

            float   sum         = 0.0f;
            float   amp         = 1.0f;
            float   ampSum      = 0.0f;
            for (int i = 0; i < octaves; i++)
            {
                sum     += amp * SignalMath.ValueNoise(x, seed + i * 1013);
                ampSum  += amp;
                amp     *= 0.5f;
                x       *= 2.0;
            }

            return GetFloat("offset") + GetFloat("amplitude") * (sum / ampSum);
        }
    }

    // Sample and hold: a new random value rate times per second
    public class SignalSrcRandomStep : SignalSource
    {
        public SignalSrcRandomStep()
        {
            AddFloat("rate"         , "Rate [Hz]"   , 2.0f, 0.01f, LimitsType.Min, 0.0f);
            AddInt  ("seed"         , "Seed"        , 1);
            AddFloat("min"          , "Min"         , 0.0f, 0.01f);
            AddFloat("max"          , "Max"         , 1.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            long step = (long)Math.Floor(ctx.Time * GetFloat("rate"));
            float r = SignalMath.Hash01(step, GetInt("seed"));
            return GetFloat("min") + (GetFloat("max") - GetFloat("min")) * r;
        }
    }

    // Value set by hand in the inspector (later also by keyboard)
    public class SignalSrcManual : SignalSource
    {
        public SignalSrcManual()
        {
            AddFloat("value", "Value", 0.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            return GetFloat("value");
        }
    }

    // MIDI controller (CC) value mapped to min..max
    public class SignalSrcMidiCC : SignalSource
    {
        public SignalSrcMidiCC()
        {
            AddInt  ("channel"      , "Channel"     , 1, LimitsType.MinMax, 1, 16);
            AddInt  ("controller"   , "Controller (CC)", 1, LimitsType.MinMax, 0, 127);
            AddFloat("min"          , "Min"         , 0.0f, 0.01f);
            AddFloat("max"          , "Max"         , 1.0f, 0.01f);
        }

        public override float Evaluate(SignalContext ctx)
        {
            float val01 = 0.0f;
            if (ctx.Inputs != null)
                ctx.Inputs.TryGetMidiControllerValue(GetInt("channel"), GetInt("controller"), out val01);

            return GetFloat("min") + (GetFloat("max") - GetFloat("min")) * val01;
        }
    }

    // Value of another signal (reuse of a signal chain)
    public class SignalSrcSignalRef : SignalSource
    {
        public SignalSrcSignalRef()
        {
            AddSignalRef("signal", "Signal");
        }

        public override float Evaluate(SignalContext ctx)
        {
            return SignalsCollection.EvaluateReferenced(ctx, GetInt("signal"));
        }
    }
}
