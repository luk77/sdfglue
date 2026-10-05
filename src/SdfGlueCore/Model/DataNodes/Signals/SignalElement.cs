//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Common base of signal sources and operators: a type name (stored in the project file)
    // and a list of parameters described by SignalParamDef.
    // Parameter values: ExFloatSimple (Float) or ExInt (Int, Bool, Enum, SignalRef).
    public abstract class SignalElement : IResetable
    {
        public  List<SignalParamDef>                ParamDefs   = new List<SignalParamDef>();
        public  Dictionary<string, ISimpleType>     Values      = new Dictionary<string, ISimpleType>();

        // Type name used in the project file - see SignalTypesRegistry
        public string TypeName { get; internal set; } = "";

        protected SignalParamDef AddFloat(string name, string displayName, float defaultVal, float speed, LimitsType limitsType = LimitsType.None, float minVal = 0.0f, float maxVal = 0.0f)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.Float);
            def.Speed       = speed;
            def.LimitsType  = limitsType;
            def.MinVal      = minVal;
            def.MaxVal      = maxVal;
            ParamDefs.Add(def);
            Values[name] = new ExFloatSimple(defaultVal);
            return def;
        }

        protected SignalParamDef AddInt(string name, string displayName, int defaultVal, LimitsType limitsType = LimitsType.None, int minVal = 0, int maxVal = 0)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.Int);
            def.Speed       = 0.1f;
            def.LimitsType  = limitsType;
            def.MinVal      = minVal;
            def.MaxVal      = maxVal;
            ParamDefs.Add(def);
            Values[name] = new ExInt(defaultVal);
            return def;
        }

        protected SignalParamDef AddBool(string name, string displayName, bool defaultVal)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.Bool);
            ParamDefs.Add(def);
            Values[name] = new ExInt(defaultVal ? 1 : 0);
            return def;
        }

        protected SignalParamDef AddEnum(string name, string displayName, string[] enumNames, int defaultVal)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.Enum);
            def.EnumNames   = enumNames;
            ParamDefs.Add(def);
            Values[name] = new ExInt(defaultVal);
            return def;
        }

        protected SignalParamDef AddSignalRef(string name, string displayName)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.SignalRef);
            ParamDefs.Add(def);
            Values[name] = new ExInt(0);
            return def;
        }

        protected SignalParamDef AddInputChannelRef(string name, string displayName, int defaultVal)
        {
            SignalParamDef def = new SignalParamDef(name, displayName, SignalParamType.InputChannelRef);
            ParamDefs.Add(def);
            Values[name] = new ExInt(defaultVal);
            return def;
        }

        public float GetFloat(string name)
        {
            if (Values.TryGetValue(name, out ISimpleType? val) && val is ExFloat exFloat)
                return exFloat.Val;
            return 0.0f;
        }

        public int GetInt(string name)
        {
            if (Values.TryGetValue(name, out ISimpleType? val) && val is ExInt exInt)
                return exInt.Val;
            return 0;
        }

        public bool GetBool(string name)
        {
            return GetInt(name) != 0;
        }

        public void SetFloat(string name, float val)
        {
            if (Values.TryGetValue(name, out ISimpleType? exVal) && exVal is ExFloat exFloat)
                exFloat.Initialize(val);
        }

        public void SetInt(string name, int val)
        {
            if (Values.TryGetValue(name, out ISimpleType? exVal) && exVal is ExInt exInt)
                exInt.Initialize(val);
        }

        // Clears the internal state of stateful elements (e.g. smoothing)
        public virtual void ResetState()
        {
        }

        public virtual void ResetPrevVal()
        {
            foreach(ISimpleType val in Values.Values)
                val.ResetPrevVal();
        }

        // Copies parameter values of the same name and type (used when the source type is changed)
        public void CopyCompatibleValuesFrom(SignalElement other)
        {
            foreach(SignalParamDef def in ParamDefs)
            {
                SignalParamDef? otherDef = other.ParamDefs.Find(d => d.Name == def.Name && d.Type == def.Type);
                if (otherDef == null)
                    continue;

                if (def.Type == SignalParamType.Float)
                    SetFloat(def.Name, other.GetFloat(def.Name));
                else
                    SetInt(def.Name, other.GetInt(def.Name));
            }
        }

        internal void SerializeParameters(XmlDocument xmlDoc, XmlNode nodeThis)
        {
            foreach(SignalParamDef def in ParamDefs)
            {
                XmlElement nodeParam;
                if (def.Type == SignalParamType.Float)
                    nodeParam = XmlUtils.AddNodeFloat(xmlDoc, nodeThis, "PValue", GetFloat(def.Name));
                else
                    nodeParam = XmlUtils.AddNodeInt(xmlDoc, nodeThis, "PValue", GetInt(def.Name));

                XmlUtils.AddAtributeString(nodeParam, "name", def.Name);
                XmlUtils.AddAtributeString(nodeParam, "type", SignalParamDef.TypeToString(def.Type));
            }
        }

        // Unknown parameters and parameters of another type are ignored (defaults stay)
        internal void DeserializeParameters(XmlNode nodeThis)
        {
            XmlNodeList? paramsList = nodeThis.SelectNodes("PValue");
            if (paramsList == null)
                return;

            foreach(XmlNode nodeParam in paramsList)
            {
                string? paramName = XmlUtils.LoadAttributeAsString(nodeParam, "name", null);
                string? paramType = XmlUtils.LoadAttributeAsString(nodeParam, "type", null);
                if ((paramName == null) || (paramType == null))
                    continue;

                SignalParamDef? def = ParamDefs.Find(d => d.Name == paramName);
                if (def == null || SignalParamDef.TypeToString(def.Type) != paramType)
                    continue;

                if (def.Type == SignalParamType.Float)
                {
                    float readVal = 0.0f;
                    if (XmlUtils.TryParseFloat(nodeParam.InnerText, ref readVal))
                        SetFloat(def.Name, readVal);
                }
                else
                {
                    int readVal = 0;
                    if (XmlUtils.TryParseInt(nodeParam.InnerText, ref readVal))
                        SetInt(def.Name, readVal);
                }
            }
        }
    }

    public abstract class SignalSource : SignalElement
    {
        public abstract float Evaluate(SignalContext ctx);

        // True for sources driven by real time inputs (not by the project time) - see SignalContext.UseRealTime
        public virtual bool UsesRealTime => false;
    }

    public abstract class SignalOperator : SignalElement
    {
        public  ExBool      Enabled     = new ExBool(true);

        public abstract float Process(float val, SignalContext ctx);

        public override void ResetPrevVal()
        {
            base.ResetPrevVal();
            Enabled.ResetPrevVal();
        }
    }
}
