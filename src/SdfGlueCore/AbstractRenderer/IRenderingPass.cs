//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.BaseTypes;
using System.Drawing;

namespace SdfGlueCore.AbstractRenderer
{
    public interface IRenderingPass
    {
        int GetTextureId();
        Bitmap GetFrameAsBitmap(IntCoords textureSize);
        //void ResetFrameCounter();
    }
}
