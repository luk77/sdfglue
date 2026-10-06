//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model.BaseTypes
{
    // Parameter value which can be driven by signals (see SignalBinding).
    // Every channel (float: 1, vec2/3/4: X, Y, Z, W) has its own binding - a signal Id (0 = none),
    // so a deleted signal only leaves an unresolved binding.
    public interface ISignalBindable
    {
        int     ChannelCount    { get; }

        int     GetSignalId     (int channel);
        void    SetSignalId     (int channel, int signalId);
    }
}
