//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    public class SignalParameter
    {
        public ExFloatSimple ParamValue = new ExFloatSimple(0.0f);
    }

    public abstract class SignalOperator
    {
        public Dictionary<string, SignalParameter>     Parameters = new Dictionary<string, SignalParameter>();

        public void AddParameter(string name, float initialValue)
        {
            SignalParameter param = new SignalParameter();
            param.ParamValue.Val = initialValue;
            param.ParamValue.ResetPrevVal();
            Parameters[name] = param;
        }

        public ExFloatSimple? GetParameter(string name)
        {
            if (!Parameters.ContainsKey(name))
                return null;

            return Parameters[name].ParamValue;
        }

        public float GetParameterValue(string name)
        {
            ExFloatSimple? exFloat = GetParameter(name);
            if (exFloat == null)
                return 0.0f;

            return exFloat.Val;
        }

        public abstract void InitParameters();
        public abstract void Process(ref float val);
    }

    public class SignalOpSin : SignalOperator
    {
        public override void InitParameters()
        {
            AddParameter("amplitude", 1.0F);
            AddParameter("frequency", 1.0f);
            AddParameter("offsetX"  , 0.0f);
            AddParameter("offsetY"  , 0.0f);
        }

        public override void Process(ref float val)
        {
            float amplitude    = GetParameterValue("amplitude");
            float frequency    = GetParameterValue("frequency");
            float offsetX      = GetParameterValue("offsetX"  );
            float offsetY      = GetParameterValue("offsetY"  );

            val = offsetY + amplitude * (float)Math.Sin(frequency * Math.PI * (val + offsetX));
        }
    }



    public class SignalCompound : SignalInstance
    {
        public List<SignalOperator>     Operators = new List<SignalOperator>();

        private double time_ = 0.0f;

        public override void Update(double deltaTime)
        {
            time_ += deltaTime;

            float val = (float)time_;

            // apply all operators
            foreach(SignalOperator op in Operators)
            {
                op.Process(ref val);
            }

            value_ = val;
        }
    }
}
