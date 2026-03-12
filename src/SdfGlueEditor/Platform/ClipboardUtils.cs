//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using TextCopy;

namespace SdfGlueEditor.Platform
{
    public class ClipboardHelper
    {
        public static void SetTextToClipboard(string txt)
        {
            ClipboardService.SetText(txt);
        }

        public static string? GetTextFromClipboard()
        {
            return ClipboardService.GetText();
        }
    }
}