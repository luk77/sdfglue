//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Utils;

namespace SdfGlueCore.Model.DataNodes.Signals
{
    // Ring buffer with recent values of a signal (for plots). Runtime only - not serialized.
    public class SignalHistory : ValueHistory
    {
        public SignalHistory(int capacity = DefaultCapacity) : base(capacity)
        {
        }
    }
}
