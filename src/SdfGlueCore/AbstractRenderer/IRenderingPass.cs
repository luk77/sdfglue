//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.BaseTypes;
using System.Drawing;

namespace SdfGlueCore.AbstractRenderer
{
    public interface IRenderingPass
    {
        int GetTextureId();
        // Copy of the current frame (the caller disposes it), null if there is no frame
        Bitmap? GetFrameAsBitmap();
        // Copies the current frame (sourceSize - preview resolution) into the given rectangle of the application window
        // (window pixels, origin in the bottom-left corner)
        void BlitToWindow(IntCoords sourceSize, int dstX0, int dstY0, int dstX1, int dstY1);
        //void ResetFrameCounter();
    }
}
