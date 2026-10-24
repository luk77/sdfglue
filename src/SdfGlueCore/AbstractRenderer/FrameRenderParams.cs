//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;

namespace SdfGlueCore.AbstractRenderer
{
    // State of the application (not saved in the document) used to render a frame,
    // see SdfGlueAppContextBase.GetFrameRenderParams()
    public readonly struct FrameRenderParams
    {
        public readonly double          Time;               // iTime
        public readonly int             Frame;              // iFrame
        public readonly IntCoords       Resolution;         // size of the rendered frame (iResolution)
        public readonly bool            RenderEnabled;      // false - the passes are not rendered (diagnostics)

        public FrameRenderParams(double time, int frame, IntCoords resolution, bool renderEnabled)
        {
            Time            = time;
            Frame           = frame;
            Resolution      = resolution;
            RenderEnabled   = renderEnabled;
        }
    }
}
