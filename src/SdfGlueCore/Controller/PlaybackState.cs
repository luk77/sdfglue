//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueCore.Controller
{
    // Time of the session (iTime, iFrame) and the FPS statistics - not saved in the project.
    // Advanced by SdfGlueAppContextBase.Update() / AdvanceTime() (which also update the signals),
    // reset when the document is replaced.
    public class PlaybackState
    {
        public static readonly float    ConstTimeStep           = 1.0f / 10.0f;

        public double                   CurrentTime             = 0.0;
        public int                      CurrentFrame            = 0;
        public double                   AvgRenderTime           = 0.0;
        public double                   Fps                     = 0.0;
        public double                   FpsAccumTime            = 0.0;
        public int                      FpsAccumFrames          = 0;
        public bool                     IsPlaying               = true;
        public bool                     UseConstTimeStep        = false;    // TODO: needs polishing

        // New document: time from the beginning, playing
        public void Reset()
        {
            RestoreTimeState(new TimeState());
        }

        public void ResetFrameCounter()
        {
            CurrentFrame = 0;
        }

        public void ResetTime()
        {
            CurrentTime = 0.0;
        }

        public void TimeStepBack()
        {
            CurrentTime -= ConstTimeStep;
            if (CurrentTime < 0.0)
                CurrentTime = 0.0;
        }

        public void TimeStepForward()
        {
            CurrentTime += ConstTimeStep;
        }

        public void Update(double deltaTime)
        {
            if (IsPlaying)
            {
                CurrentTime += deltaTime;
                CurrentFrame++;
            }

            FpsAccumTime += deltaTime;
            FpsAccumFrames++;

            double fpsCheckPeriod = 0.5;

            if (FpsAccumTime > fpsCheckPeriod)
            {
                AvgRenderTime = 0.0;
                Fps = 0.0;
                if (FpsAccumFrames > 0)
                {
                    AvgRenderTime = FpsAccumTime / FpsAccumFrames;
                    Fps = 1.0 / AvgRenderTime;
                }

                FpsAccumTime -= fpsCheckPeriod;
                FpsAccumFrames = 0;
            }
        }

        // Advances the time also when paused, without the FPS statistics (image sequence export)
        public void AdvanceTime(double deltaTime)
        {
            CurrentTime += deltaTime;
            CurrentFrame++;
        }

        public TimeState CaptureTimeState()
        {
            TimeState state = new TimeState();
            state.CurrentTime       = CurrentTime;
            state.CurrentFrame      = CurrentFrame;
            state.AvgRenderTime     = AvgRenderTime;
            state.Fps               = Fps;
            state.FpsAccumTime      = FpsAccumTime;
            state.FpsAccumFrames    = FpsAccumFrames;
            state.IsPlaying         = IsPlaying;
            return state;
        }

        // Note: the internal state of signals (e.g. smoothing) is not restored
        public void RestoreTimeState(TimeState state)
        {
            CurrentTime     = state.CurrentTime;
            CurrentFrame    = state.CurrentFrame;
            AvgRenderTime   = state.AvgRenderTime;
            Fps             = state.Fps;
            FpsAccumTime    = state.FpsAccumTime;
            FpsAccumFrames  = state.FpsAccumFrames;
            IsPlaying       = state.IsPlaying;
        }
    }
}
