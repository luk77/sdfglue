//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using TextCopy;

namespace SingleDocAppFramework.Platform
{
    // TextCopy is cross-platform, so it does not need IPlatformServices
    public static class ClipboardUtils
    {
        public static void SetText(string txt)
        {
            ClipboardService.SetText(txt);
        }

        public static string? GetText()
        {
            return ClipboardService.GetText();
        }
    }
}
