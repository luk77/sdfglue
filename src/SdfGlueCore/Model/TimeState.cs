//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueCore.Model
{
    // Snapshot of the time of a session (DataModel.CaptureTimeState / RestoreTimeState)
    public class TimeState
    {
        public double                   LastDeltaTime           = 0.0;
        public double                   CurrentTime             = 0.0;
        public int                      CurrentFrame            = 0;
        public double                   AvgRenderTime           = 0.0;
        public double                   Fps                     = 0.0;
        public double                   FpsAccumTime            = 0.0;
        public int                      FpsAccumFrames          = 0;
        public bool                     IsPlaying               = true;
    }
}
