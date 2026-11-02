//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppFramework.Ui.Fonts;
using System.Numerics;

namespace SingleDocAppFramework.Ui.CodeView
{
    public class CodeViewOptions
    {
        public  bool            ShowLineNumbers     = true;
        public  UiFontRole      FontRole            = UiFontRole.Code;
    }

    // Read-only view of code with syntax highlighting and line numbers, drawn with the ImGui draw list.
    // Only the visible lines are drawn, the text is not copied or encoded per frame (CodeViewDocument).
    //
    // Usage (the document tokenizes the text only when it changes):
    //   doc_.SetText(code, language);
    //   UiCodeView.Build("##code", doc_, options_);
    public static unsafe class UiCodeView
    {
        private const float     GutterPaddingLeft   = 6.0f;
        private const float     GutterPaddingRight  = 8.0f;
        private const float     TextPaddingLeft     = 6.0f;

        // Width of the longest line: computed once per document version and font
        private class WidthCache
        {
            public  int         DocVersion  = -1;
            public  IntPtr      Font        = IntPtr.Zero;
            public  float       FontSize    = 0.0f;
            public  float       MaxWidth    = 0.0f;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<CodeViewDocument, WidthCache> widthCaches_ = new System.Runtime.CompilerServices.ConditionalWeakTable<CodeViewDocument, WidthCache>();

        // size: <= 0 - the available space in that direction
        public static void Build(string id, CodeViewDocument doc, CodeViewOptions options, Vector2 size = default)
        {
            Vector2 avail = ImGui.GetContentRegionAvail();
            if (size.X <= 0.0f)
                size.X = Math.Max(avail.X, 50.0f);
            if (size.Y <= 0.0f)
                size.Y = Math.Max(avail.Y, 50.0f);

            using (UiFonts.Scope(options.FontRole))
            {
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
                bool visible = ImGui.BeginChild(id, size, ImGuiChildFlags.None, ImGuiWindowFlags.HorizontalScrollbar);
                ImGui.PopStyleVar();

                if (visible)
                    BuildContent(doc, options);

                ImGui.EndChild();
            }
        }

        private static void BuildContent(CodeViewDocument doc, CodeViewOptions options)
        {
            ImDrawListPtr   dl          = ImGui.GetWindowDrawList();
            Vector2         winPos      = ImGui.GetWindowPos();
            Vector2         winSize     = ImGui.GetWindowSize();
            Vector2         origin      = ImGui.GetCursorScreenPos();     // moves with the scroll position
            float           lineHeight  = ImGui.GetTextLineHeight();
            int             lineCount   = doc.LineCount;

            float           gutterWidth = options.ShowLineNumbers ? GetGutterWidth(lineCount) : 0.0f;
            float           maxWidth    = GetMaxLineWidth(doc);

            // visible area (the scrollbars are excluded by the clip rect of the window - intersected below)
            float           visibleRight    = winPos.X + winSize.X;
            float           visibleBottom   = winPos.Y + winSize.Y;

            int firstLine   = Math.Max(0, (int)Math.Floor((winPos.Y - origin.Y) / lineHeight));
            int lastLine    = Math.Min(lineCount, (int)Math.Ceiling((visibleBottom - origin.Y) / lineHeight));

            byte[] utf8 = doc.Utf8;
            CodeViewDocument.LineInfo[]  lines  = doc.Lines;
            CodeViewDocument.TokenInfo[] tokens = doc.Tokens;

            fixed (byte* text = utf8)
            {
                // code (clipped, so text scrolled horizontally does not go under the line numbers)
                float codeLeft = winPos.X + gutterWidth;
                dl.PushClipRect(new Vector2(codeLeft, winPos.Y), new Vector2(visibleRight, visibleBottom), true);

                float textX0 = origin.X + gutterWidth + TextPaddingLeft;
                for(int i=firstLine; i<lastLine; i++)
                {
                    CodeViewDocument.LineInfo line = lines[i];
                    float x = textX0;
                    float y = origin.Y + i * lineHeight;

                    for(int t=line.FirstToken; t<line.FirstToken + line.TokenCount; t++)
                    {
                        CodeViewDocument.TokenInfo token = tokens[t];
                        byte* begin = text + token.ByteStart;
                        byte* end   = begin + token.ByteLength;

                        float width = CalcTextWidth(begin, end);
                        if (x + width >= codeLeft)
                            ImGuiNative.ImDrawList_AddText_Vec2(dl.NativePtr, new Vector2(x, y), CodeViewPalette.GetColorU32(token.Kind), begin, end);

                        x += width;
                        if (x > visibleRight)
                            break;
                    }
                }

                dl.PopClipRect();

                // line numbers - fixed position, not scrolled horizontally
                if (options.ShowLineNumbers)
                {
                    Vector2 gutterMin = winPos;
                    Vector2 gutterMax = new Vector2(winPos.X + gutterWidth, visibleBottom);
                    dl.AddRectFilled(gutterMin, gutterMax, ImGui.GetColorU32(ImGuiCol.FrameBg, 0.5f));
                    dl.AddLine(new Vector2(gutterMax.X - 0.5f, gutterMin.Y), new Vector2(gutterMax.X - 0.5f, gutterMax.Y), ImGui.GetColorU32(ImGuiCol.Separator));

                    uint numberColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);
                    byte* digits = stackalloc byte[16];
                    for(int i=firstLine; i<lastLine; i++)
                    {
                        int count = FormatNumber(i + 1, digits, 16);
                        byte* begin = digits + 16 - count;
                        byte* end   = digits + 16;
                        float width = CalcTextWidth(begin, end);
                        float y     = origin.Y + i * lineHeight;
                        ImGuiNative.ImDrawList_AddText_Vec2(dl.NativePtr, new Vector2(gutterMax.X - GutterPaddingRight - width, y), numberColor, begin, end);
                    }
                }
            }

            // content size for the scrollbars
            ImGui.Dummy(new Vector2(gutterWidth + TextPaddingLeft + maxWidth + TextPaddingLeft, lineCount * lineHeight));
        }

        private static float GetGutterWidth(int lineCount)
        {
            int digitCount = Math.Max(lineCount, 1).ToString().Length;
            return ImGui.CalcTextSize("0").X * Math.Max(digitCount, 2) + GutterPaddingLeft + GutterPaddingRight;
        }

        private static float GetMaxLineWidth(CodeViewDocument doc)
        {
            WidthCache cache = widthCaches_.GetOrCreateValue(doc);

            IntPtr  font        = (IntPtr)ImGui.GetFont().NativePtr;
            float   fontSize    = ImGui.GetFontSize();
            if (cache.DocVersion == doc.Version && cache.Font == font && cache.FontSize == fontSize)
                return cache.MaxWidth;

            float maxWidth = 0.0f;
            fixed (byte* text = doc.Utf8)
            {
                foreach(CodeViewDocument.LineInfo line in doc.Lines)
                {
                    if (line.ByteLength > 0)
                        maxWidth = Math.Max(maxWidth, CalcTextWidth(text + line.ByteStart, text + line.ByteStart + line.ByteLength));
                }
            }

            cache.DocVersion    = doc.Version;
            cache.Font          = font;
            cache.FontSize      = fontSize;
            cache.MaxWidth      = maxWidth;
            return maxWidth;
        }

        private static float CalcTextWidth(byte* begin, byte* end)
        {
            Vector2 size;
            ImGuiNative.igCalcTextSize(&size, begin, end, 0, -1.0f);
            return size.X;
        }

        // Writes the decimal digits at the end of the buffer, returns their count
        private static int FormatNumber(int value, byte* buffer, int bufferSize)
        {
            int count = 0;
            do
            {
                buffer[bufferSize - 1 - count] = (byte)('0' + value % 10);
                value /= 10;
                count++;
            }
            while (value > 0 && count < bufferSize);
            return count;
        }
    }
}
