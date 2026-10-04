//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using System.Globalization;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Binding of float parameters (ExFloatWithSignal) to signals.
    // Bindings are always loaded and saved; with DataModel.UseSignals = false they are ignored
    // (the static value is used and the generated code does not change).
    public static class SignalBinding
    {
        private const string    AttributeName   = "signal";

        public static int GetSignalId(ISimpleType? val)
        {
            return (val is ExFloatWithSignal exObj) ? exObj.SignalId : 0;
        }

        // True if the parameter is driven by a signal - such a parameter is always a uniform,
        // also in fixed objects (the binding is enough: a disabled or deleted signal gives the static value)
        public static bool IsBound(ISimpleType? val)
        {
            return DataModel.UseSignals && GetSignalId(val) != 0;
        }

        public static bool HasBoundParameters(Dictionary<string, ISimpleType> values)
        {
            if (!DataModel.UseSignals)
                return false;

            foreach(ISimpleType val in values.Values)
            {
                if (IsBound(val))
                    return true;
            }
            return false;
        }

        // Value passed to the shader: the signal value, or the static value if the signal is missing or disabled
        public static float GetValue(ExFloat val, SignalsCollection? signals)
        {
            if (!IsBound(val) || signals == null)
                return val.Val;

            SignalInstance? signal = signals.FindById(GetSignalId(val));
            if (signal == null || !signal.Enabled.Val)
                return val.Val;

            return signal.GetCurrentValue();
        }

        // Adds the binding attribute to the node of the value (only for bound values - files without signals do not change)
        public static void SerializeBinding(XmlElement nodeValue, ISimpleType val)
        {
            int signalId = GetSignalId(val);
            if (signalId != 0)
                XmlUtils.AddAtributeString(nodeValue, AttributeName, signalId.ToString(CultureInfo.InvariantCulture));
        }

        public static void DeserializeBinding(XmlNode? nodeValue, ISimpleType val)
        {
            if (nodeValue == null || val is not ExFloatWithSignal exObj)
                return;

            string? text = XmlUtils.LoadAttributeAsString(nodeValue, AttributeName, null);
            int signalId = 0;
            if (text != null && XmlUtils.TryParseInt(text, ref signalId))
                exObj.SignalId = signalId;
        }
    }
}
