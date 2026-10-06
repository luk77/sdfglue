//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using System.Numerics;

namespace SdfGlueCore.Model.BaseTypes
{
    // Vector parameters which can be driven by signals - every channel (X, Y, Z, W) has its own binding
    // (see ISignalBindable, SignalBinding). An unbound channel uses the static value.

    public  class ExVector2WithSignal : ExVector2, ISignalBindable
    {
        public int[] SignalIds = new int[2];

        public int  ChannelCount                                => 2;
        public int  GetSignalId(int channel)                    { return SignalIds[channel]; }
        public void SetSignalId(int channel, int signalId)      { SignalIds[channel] = signalId; }

        public override ISimpleType Copy()
        {
            ExVector2WithSignal copy = new ExVector2WithSignal(Val);
            SignalIds.CopyTo(copy.SignalIds, 0);
            return copy;
        }

        public ExVector2WithSignal() {}
        public ExVector2WithSignal(Vector2 val) : base(val) {}
    }

    public  class ExVector3WithSignal : ExVector3, ISignalBindable
    {
        public int[] SignalIds = new int[3];

        public int  ChannelCount                                => 3;
        public int  GetSignalId(int channel)                    { return SignalIds[channel]; }
        public void SetSignalId(int channel, int signalId)      { SignalIds[channel] = signalId; }

        public override ISimpleType Copy()
        {
            ExVector3WithSignal copy = new ExVector3WithSignal(Val);
            SignalIds.CopyTo(copy.SignalIds, 0);
            return copy;
        }

        public ExVector3WithSignal() {}
        public ExVector3WithSignal(Vector3 val) : base(val) {}
    }

    public  class ExVector4WithSignal : ExVector4, ISignalBindable
    {
        public int[] SignalIds = new int[4];

        public int  ChannelCount                                => 4;
        public int  GetSignalId(int channel)                    { return SignalIds[channel]; }
        public void SetSignalId(int channel, int signalId)      { SignalIds[channel] = signalId; }

        public override ISimpleType Copy()
        {
            ExVector4WithSignal copy = new ExVector4WithSignal(Val);
            SignalIds.CopyTo(copy.SignalIds, 0);
            return copy;
        }

        public ExVector4WithSignal() {}
        public ExVector4WithSignal(Vector4 val) : base(val) {}
    }
}
