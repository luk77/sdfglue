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
        // Size of the current frame (0x0 if there is no frame)
        IntCoords GetFrameSize();
        // Copies the current frame into pixels: B, G, R, A, rows top-down, width * height * 4 bytes (no allocation).
        // false if there is no frame or its size is not width x height.
        bool ReadFrameBgra(byte[] pixels, int width, int height);
        // Copies the current frame (sourceSize - preview resolution) into the given rectangle of the application window
        // (window pixels, origin in the bottom-left corner)
        void BlitToWindow(IntCoords sourceSize, int dstX0, int dstY0, int dstX1, int dstY1);
        //void ResetFrameCounter();
    }
}
