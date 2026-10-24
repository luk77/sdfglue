//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueCore
{
    // Compile-time switches of optional features
    public static class SdfGlueFeatures
    {
        public static readonly bool     UseCameraControllers        = true;
        // Signals and input channels (the signals data is always loaded and saved)
        public static readonly bool     UseSignals                  = true;
    }
}
