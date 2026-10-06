//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppCore.Utils;
using System.Globalization;
using System.Numerics;
using System.Xml;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Binding of float and vector parameters (ISignalBindable: ExFloatWithSignal, ExVector2/3/4WithSignal) to signals.
    // Every channel of a vector has its own binding.
    // Bindings are always loaded and saved; with DataModel.UseSignals = false they are ignored
    // (the static value is used and the generated code does not change).
    public static class SignalBinding
    {
        // float: signal="id", vectors: signalX="id", signalY="id", ... (only bound channels)
        private const string    AttributeName   = "signal";

        public static readonly string[] ChannelNames = { "X", "Y", "Z", "W" };

        public static int GetSignalId(ISimpleType? val, int channel = 0)
        {
            return (val is ISignalBindable bindable && channel < bindable.ChannelCount) ? bindable.GetSignalId(channel) : 0;
        }

        // True if any channel has a binding (regardless of DataModel.UseSignals)
        public static bool HasBinding(ISimpleType? val)
        {
            return val is ISignalBindable bindable && HasBinding(bindable);
        }

        public static bool HasBinding(ISignalBindable bindable)
        {
            for(int ch=0; ch<bindable.ChannelCount; ch++)
            {
                if (bindable.GetSignalId(ch) != 0)
                    return true;
            }
            return false;
        }

        // True if the parameter is driven by a signal (any channel) - such a parameter is always a uniform,
        // also in fixed objects (the binding is enough: a disabled or deleted signal gives the static value)
        public static bool IsBound(ISimpleType? val)
        {
            return DataModel.UseSignals && HasBinding(val);
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

        // Value of a single channel: the signal value, or the static value if the channel is not bound
        // or the signal is missing or disabled
        public static float GetChannelValue(ISignalBindable obj, int channel, float staticVal, SignalsCollection? signals)
        {
            if (!DataModel.UseSignals || signals == null)
                return staticVal;

            int signalId = obj.GetSignalId(channel);
            if (signalId == 0)
                return staticVal;

            SignalInstance? signal = signals.FindById(signalId);
            if (signal == null || !signal.Enabled.Val)
                return staticVal;

            return signal.GetCurrentValue();
        }

        // Values passed to the shader
        public static float GetValue(ExFloat val, SignalsCollection? signals)
        {
            if (val is not ISignalBindable bindable)
                return val.Val;

            return GetChannelValue(bindable, 0, val.Val, signals);
        }

        public static Vector2 GetValue(ExVector2WithSignal val, SignalsCollection? signals)
        {
            Vector2 v = val.Val;
            v.X = GetChannelValue(val, 0, v.X, signals);
            v.Y = GetChannelValue(val, 1, v.Y, signals);
            return v;
        }

        public static Vector3 GetValue(ExVector3WithSignal val, SignalsCollection? signals)
        {
            Vector3 v = val.Val;
            v.X = GetChannelValue(val, 0, v.X, signals);
            v.Y = GetChannelValue(val, 1, v.Y, signals);
            v.Z = GetChannelValue(val, 2, v.Z, signals);
            return v;
        }

        public static Vector4 GetValue(ExVector4WithSignal val, SignalsCollection? signals)
        {
            Vector4 v = val.Val;
            v.X = GetChannelValue(val, 0, v.X, signals);
            v.Y = GetChannelValue(val, 1, v.Y, signals);
            v.Z = GetChannelValue(val, 2, v.Z, signals);
            v.W = GetChannelValue(val, 3, v.W, signals);
            return v;
        }

        private static string GetAttributeName(ISignalBindable bindable, int channel)
        {
            return bindable.ChannelCount == 1 ? AttributeName : AttributeName + ChannelNames[channel];
        }

        // Adds the binding attributes to the node of the value (only for bound channels - files without signals do not change)
        public static void SerializeBinding(XmlElement nodeValue, ISimpleType val)
        {
            if (val is not ISignalBindable bindable)
                return;

            for(int ch=0; ch<bindable.ChannelCount; ch++)
            {
                int signalId = bindable.GetSignalId(ch);
                if (signalId != 0)
                    XmlUtils.AddAtributeString(nodeValue, GetAttributeName(bindable, ch), signalId.ToString(CultureInfo.InvariantCulture));
            }
        }

        public static void DeserializeBinding(XmlNode? nodeValue, ISimpleType val)
        {
            if (nodeValue == null || val is not ISignalBindable bindable)
                return;

            for(int ch=0; ch<bindable.ChannelCount; ch++)
            {
                string? text = XmlUtils.LoadAttributeAsString(nodeValue, GetAttributeName(bindable, ch), null);
                int signalId = 0;
                if (text != null && XmlUtils.TryParseInt(text, ref signalId))
                    bindable.SetSignalId(ch, signalId);
            }
        }
    }
}
