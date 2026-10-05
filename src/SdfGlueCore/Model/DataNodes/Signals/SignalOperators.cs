//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalOpScaleOffset : SignalOperator
    {
        public SignalOpScaleOffset()
        {
            AddFloat("gain"     , "Gain"    , 1.0f, 0.01f);
            AddFloat("bias"     , "Bias"    , 0.0f, 0.01f);
        }

        public override float Process(float val, SignalContext ctx)
        {
            return val * GetFloat("gain") + GetFloat("bias");
        }
    }

    public class SignalOpRemap : SignalOperator
    {
        public SignalOpRemap()
        {
            AddFloat("inMin"    , "In min"  , -1.0f, 0.01f);
            AddFloat("inMax"    , "In max"  ,  1.0f, 0.01f);
            AddFloat("outMin"   , "Out min" ,  0.0f, 0.01f);
            AddFloat("outMax"   , "Out max" ,  1.0f, 0.01f);
            AddBool ("clamp"    , "Clamp"   , false);
        }

        public override float Process(float val, SignalContext ctx)
        {
            float inMin = GetFloat("inMin");
            float inMax = GetFloat("inMax");
            float range = inMax - inMin;
            float t = (range != 0.0f) ? (val - inMin) / range : 0.0f;
            if (GetBool("clamp"))
                t = Math.Clamp(t, 0.0f, 1.0f);
            return GetFloat("outMin") + (GetFloat("outMax") - GetFloat("outMin")) * t;
        }
    }

    public class SignalOpClamp : SignalOperator
    {
        public SignalOpClamp()
        {
            AddFloat("min"      , "Min"     , 0.0f, 0.01f);
            AddFloat("max"      , "Max"     , 1.0f, 0.01f);
        }

        public override float Process(float val, SignalContext ctx)
        {
            // no exception when min > max
            return Math.Min(Math.Max(val, GetFloat("min")), GetFloat("max"));
        }
    }

    public class SignalOpMath : SignalOperator
    {
        public static readonly string[] OperationNames = { "Abs", "Negate", "1 - x", "Square", "Sqrt", "Sign", "Fract", "Floor" };

        public SignalOpMath()
        {
            AddEnum("op", "Operation", OperationNames, 0);
        }

        public override float Process(float val, SignalContext ctx)
        {
            switch (GetInt("op"))
            {
                case 0: return Math.Abs(val);
                case 1: return -val;
                case 2: return 1.0f - val;
                case 3: return val * val;
                case 4: return Math.Sign(val) * MathF.Sqrt(Math.Abs(val));     // sign preserving
                case 5: return Math.Sign(val);
                case 6: return val - MathF.Floor(val);
                case 7: return MathF.Floor(val);
            }
            return val;
        }
    }

    // Sign preserving power curve
    public class SignalOpCurve : SignalOperator
    {
        public SignalOpCurve()
        {
            AddFloat("exponent" , "Exponent", 2.0f, 0.01f, LimitsType.Min, 0.0f);
        }

        public override float Process(float val, SignalContext ctx)
        {
            return Math.Sign(val) * MathF.Pow(Math.Abs(val), GetFloat("exponent"));
        }
    }

    public class SignalOpQuantize : SignalOperator
    {
        public SignalOpQuantize()
        {
            AddFloat("step"     , "Step"    , 0.25f, 0.01f, LimitsType.Min, 0.0f);
        }

        public override float Process(float val, SignalContext ctx)
        {
            float step = GetFloat("step");
            if (step <= 0.0f)
                return val;
            return MathF.Round(val / step) * step;
        }
    }

    // Base of stateful operators: the state follows the project time,
    // so it is frozen on pause and reset when the time jumps (rewind, frame counter reset)
    public abstract class SignalOpStateful : SignalOperator
    {
        protected   bool        hasState_   = false;
        protected   float       state_      = 0.0f;
        protected   double      lastTime_   = 0.0;

        public override void ResetState()
        {
            hasState_ = false;
        }

        public override float Process(float val, SignalContext ctx)
        {
            double time = ctx.GetOperatorsTime();
            double dt = time - lastTime_;
            lastTime_ = time;

            if (!hasState_ || dt < 0.0 || dt > 1.0)
            {
                hasState_   = true;
                state_      = val;
                return state_;
            }

            if (dt > 0.0)
                state_ = Step(val, (float)dt);

            return state_;
        }

        protected abstract float Step(float val, float dt);
    }

    // One-pole low pass filter (exponential smoothing)
    public class SignalOpSmooth : SignalOpStateful
    {
        public SignalOpSmooth()
        {
            AddFloat("time"     , "Time constant [s]", 0.2f, 0.01f, LimitsType.Min, 0.0f);
        }

        protected override float Step(float val, float dt)
        {
            float time = GetFloat("time");
            if (time <= 0.0f)
                return val;
            float a = 1.0f - MathF.Exp(-dt / time);
            return state_ + (val - state_) * a;
        }
    }

    // Limits the rate of change (units per second)
    public class SignalOpSlew : SignalOpStateful
    {
        public SignalOpSlew()
        {
            AddFloat("rise"     , "Max rise [1/s]", 1.0f, 0.01f, LimitsType.Min, 0.0f);
            AddFloat("fall"     , "Max fall [1/s]", 1.0f, 0.01f, LimitsType.Min, 0.0f);
        }

        protected override float Step(float val, float dt)
        {
            float delta = val - state_;
            float maxUp     = GetFloat("rise") * dt;
            float maxDown   = GetFloat("fall") * dt;
            delta = Math.Min(Math.Max(delta, -maxDown), maxUp);
            return state_ + delta;
        }
    }

    // Combines the value with another signal
    public class SignalOpCombine : SignalOperator
    {
        public static readonly string[] ModeNames = { "Add", "Multiply", "Min", "Max", "Mix" };

        public SignalOpCombine()
        {
            AddEnum     ("mode"     , "Mode"    , ModeNames, 0);
            AddSignalRef("signal"   , "Signal");
            AddFloat    ("mix"      , "Mix"     , 0.5f, 0.01f, LimitsType.MinMax, 0.0f, 1.0f);
        }

        public override float Process(float val, SignalContext ctx)
        {
            float other = SignalsCollection.EvaluateReferenced(ctx, GetInt("signal"));
            switch (GetInt("mode"))
            {
                case 0: return val + other;
                case 1: return val * other;
                case 2: return Math.Min(val, other);
                case 3: return Math.Max(val, other);
                case 4: return val + (other - val) * GetFloat("mix");
            }
            return val;
        }
    }
}
