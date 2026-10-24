//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation
{
    // The time of the edited document as the host defines it
    public interface IAnimTimebase
    {
        bool    IsAvailable { get; }    // false: nothing to play (no document)
        double  Length      { get; }    // s: the end
        double  Fps         { get; }    // the frames: the steps of the playhead
        bool    WholeFrames { get; }    // the time is in whole frames: the playback shows whole frames, the ruler counts frames

        // a time as the editor places things (on the grid, not negative)
        double Snap(double t);

        // the end at t (call in an edit)
        void SetLength(double t);
    }

    // The own time of the animation (not the time of the host's other systems): the playhead, the playback, the loop.
    // The host keeps the instance and calls Update once per frame of the editor.
    public sealed class AnimTransport
    {
        public readonly IAnimTimebase   Timebase;
        public bool                     Playing;
        public bool                     Loop        = true;

        // around a change of the playhead (the host ends a running edit before it, rebuilds its views after it)
        public Action?                  Seeking;
        public Action?                  Seeked;

        private double                  playhead_;
        private double                  playTime_;          // s: the continuous time of the playback
        private bool                    wasPlaying_;

        public AnimTransport(IAnimTimebase timebase)
        {
            Timebase = timebase;
        }

        // s: the time the editor shows and edits
        public double Playhead
        {
            get { return playhead_; }
            set
            {
                double t = Math.Max(0, value);
                if (t == playhead_)
                    return;
                Seeking?.Invoke();
                playhead_ = t;
                Seeked?.Invoke();
            }
        }

        // s: where the keys are made - the playhead on the grid
        public double KeyTime { get { return Timebase.Snap(playhead_); } }

        // a new document: at 0, stopped (no notifications)
        public void Reset()
        {
            playhead_ = 0;
            Playing = false;
        }

        public void TogglePlay()
        {
            Playing = !Playing;
        }

        public void PlayFromStart()
        {
            Playhead = 0;
            Playing = true;
            wasPlaying_ = false;
        }

        // the playhead moves by the time of the editor frames; called once per frame
        public void Update(double dt)
        {
            if (!Playing || !Timebase.IsAvailable)
            {
                Playing = false;
                wasPlaying_ = false;
                return;
            }
            double length = Timebase.Length;
            if (!wasPlaying_)
            {
                playTime_ = playhead_ >= length - 1e-9 ? 0 : playhead_;
                wasPlaying_ = true;
            }
            playTime_ += dt;
            if (playTime_ >= length)
            {
                if (Loop && length > 0)
                {
                    playTime_ %= length;
                }
                else
                {
                    playTime_ = length;
                    Playing = false;
                }
            }
            double fps = Timebase.Fps;
            Playhead = Timebase.WholeFrames ? AnimTime.FrameToTime(AnimTime.TimeToFrame(playTime_, fps), fps) : playTime_;
        }

        // the playhead at t (snapped, within the document); stops the playback
        public void GoTo(double t)
        {
            Playing = false;
            Playhead = Math.Min(Timebase.Snap(t), Timebase.IsAvailable ? Timebase.Length : 0);
        }

        // the previous / next frame
        public void StepFrame(int dir)
        {
            if (!Timebase.IsAvailable)
                return;
            double step = 1.0 / Timebase.Fps;
            double t = Timebase.WholeFrames ? playhead_ + dir * step
                     : (dir > 0 ? Math.Floor(playhead_ / step + 1e-6) + 1 : Math.Ceiling(playhead_ / step - 1e-6) - 1) * step;
            GoTo(t);
        }
    }
}
