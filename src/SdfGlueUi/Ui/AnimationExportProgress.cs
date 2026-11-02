//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueUi.Ui
{
    // State of the running animation export (IUiExecutorSdfGlue.GetAnimationExportProgress).
    // The frames are rendered on the main thread and written (encoded) in the background - a few frames behind.
    public class AnimationExportProgress
    {
        public  bool                    IsActive                = false;
        public  bool                    IsPrerendering          = false;
        public  bool                    IsFinishing             = false;    // all frames rendered, the file is being finished
        public  int                     NumOfFramesToPrerender  = 0;
        public  int                     NumOfFramesToExport     = 0;
        public  int                     NumOfPrerenderedFrames  = 0;
        public  int                     NumOfRenderedFrames     = 0;
        public  int                     NumOfWrittenFrames      = 0;
        public  double                  CurrentTime             = 0.0;      // time of the next rendered frame
        public  TimeSpan                Elapsed                 = TimeSpan.Zero;
        public  string                  Output                  = "";       // the folder (PNG) or the file (MP4, GIF)

        public int GetNumOfDoneSteps()
        {
            return NumOfPrerenderedFrames + NumOfWrittenFrames;
        }

        public int GetNumOfSteps()
        {
            return NumOfFramesToPrerender + NumOfFramesToExport;
        }

        // 0..1
        public float GetFraction()
        {
            int numOfSteps = GetNumOfSteps();
            return (numOfSteps > 0) ? (float)GetNumOfDoneSteps() / numOfSteps : 1.0f;
        }

        // null until the first step is done
        public TimeSpan? GetRemainingTime()
        {
            int doneSteps = GetNumOfDoneSteps();
            if (doneSteps <= 0)
                return null;

            return TimeSpan.FromTicks(Elapsed.Ticks / doneSteps * (GetNumOfSteps() - doneSteps));
        }
    }
}
