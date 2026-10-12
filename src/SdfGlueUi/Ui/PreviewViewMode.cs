//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueUi.Ui
{
    // View modes switched cyclically by F12 (Esc returns to Editor)
    public enum PreviewViewMode
    {
        Editor,         // standard editor UI (menu + windows)
        Maximized,      // only the main preview, filling the application window
        Fullscreen,     // only the main preview, borderless window covering the whole monitor
    }
}
