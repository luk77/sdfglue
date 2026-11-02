//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Text;

namespace SingleDocAppFramework.Ui.CodeView
{
    // Text prepared for UiCodeView: lines and colored tokens as ranges of one UTF-8 buffer (drawn without
    // allocations). SetText() can be called every frame - the text is tokenized again only when the text,
    // the language (or its Version) or the tab size changes.
    public class CodeViewDocument
    {
        internal struct LineInfo
        {
            public  int             ByteStart;
            public  int             ByteLength;
            public  int             FirstToken;
            public  int             TokenCount;
        }

        internal struct TokenInfo
        {
            public  int             ByteStart;
            public  int             ByteLength;
            public  CodeTokenKind   Kind;
        }

        public  int                 TabSize                 = 4;

        private string?             sourceText_             = null;
        private CodeLanguage?       language_               = null;
        private int                 languageVersion_        = 0;
        private int                 tabSize_                = 0;

        private byte[]              utf8_                   = Array.Empty<byte>();
        private LineInfo[]          lines_                  = Array.Empty<LineInfo>();
        private TokenInfo[]         tokens_                 = Array.Empty<TokenInfo>();

        // Incremented when the content changes (caches of the view, e.g. the width of the longest line)
        public int                  Version                 { get; private set; } = 0;

        public int                  LineCount               { get { return lines_.Length; } }
        public string?              Text                    { get { return sourceText_; } }
        public CodeLanguage?        Language                { get { return language_; } }

        internal byte[]             Utf8                    { get { return utf8_; } }
        internal LineInfo[]         Lines                   { get { return lines_; } }
        internal TokenInfo[]        Tokens                  { get { return tokens_; } }

        // Returns true if the text was tokenized again. text: null - empty document.
        public bool SetText(string? text, CodeLanguage language)
        {
            if (language == language_ && language.Version == languageVersion_ && TabSize == tabSize_)
            {
                // the same instance (cheap) or an equal text built again (e.g. by an input widget)
                if (ReferenceEquals(text, sourceText_) || String.Equals(text, sourceText_, StringComparison.Ordinal))
                {
                    sourceText_ = text;
                    return false;
                }
            }

            sourceText_         = text;
            language_           = language;
            languageVersion_    = language.Version;
            tabSize_            = TabSize;

            Rebuild(text ?? "", language);
            Version++;
            return true;
        }

        public void Clear()
        {
            sourceText_ = null;
            language_   = null;
            utf8_       = Array.Empty<byte>();
            lines_      = Array.Empty<LineInfo>();
            tokens_     = Array.Empty<TokenInfo>();
            Version++;
        }

        private void Rebuild(string source, CodeLanguage language)
        {
            List<CodeLineRange> lineRanges = new List<CodeLineRange>();
            string text = SplitLines(source, Math.Max(TabSize, 1), lineRanges);

            List<CodeToken> tokens          = new List<CodeToken>();
            List<int>       lineTokenCounts = new List<int>();
            language.Tokenizer.Tokenize(text, lineRanges, language, tokens, lineTokenCounts);

            if (lineTokenCounts.Count != lineRanges.Count)
                throw new InvalidOperationException(String.Format("Tokenizer of {0} returned token counts of {1} lines, expected {2}", language.Name, lineTokenCounts.Count, lineRanges.Count));

            // char positions -> byte positions (tokens and lines are ordered, so the conversion is incremental)
            utf8_   = Encoding.UTF8.GetBytes(text);
            lines_  = new LineInfo[lineRanges.Count];
            tokens_ = new TokenInfo[tokens.Count];

            int charPos = 0;
            int bytePos = 0;
            int toByte(int pos)
            {
                bytePos += Encoding.UTF8.GetByteCount(text.AsSpan(charPos, pos - charPos));
                charPos = pos;
                return bytePos;
            }

            int t = 0;
            for(int i=0; i<lineRanges.Count; i++)
            {
                CodeLineRange range = lineRanges[i];
                LineInfo line = new LineInfo();
                line.ByteStart  = toByte(range.Start);
                line.FirstToken = t;
                line.TokenCount = lineTokenCounts[i];

                for(int k=0; k<line.TokenCount; k++, t++)
                {
                    CodeToken token = tokens[t];
                    int start = toByte(token.Start);
                    int end   = toByte(token.Start + token.Length);
                    tokens_[t].ByteStart    = start;
                    tokens_[t].ByteLength   = end - start;
                    tokens_[t].Kind         = token.Kind;
                }

                line.ByteLength = toByte(range.Start + range.Length) - line.ByteStart;
                lines_[i] = line;
            }
        }

        // Expands tabs and splits the text into lines (\n, \r\n, \r). The returned text contains '\n' between lines.
        private static string SplitLines(string source, int tabSize, List<CodeLineRange> lines)
        {
            StringBuilder sb = new StringBuilder(source.Length + 64);
            int lineStart = 0;
            int column = 0;

            for(int i=0; i<source.Length; i++)
            {
                char c = source[i];
                if (c == '\r' || c == '\n')
                {
                    lines.Add(new CodeLineRange(lineStart, sb.Length - lineStart));
                    if (c == '\r' && i + 1 < source.Length && source[i + 1] == '\n')
                        i++;
                    sb.Append('\n');
                    lineStart = sb.Length;
                    column = 0;
                }
                else if (c == '\t')
                {
                    int spaces = tabSize - (column % tabSize);
                    sb.Append(' ', spaces);
                    column += spaces;
                }
                else
                {
                    sb.Append(c);
                    column++;
                }
            }

            // the last line (also an empty one after the final line break - like in text editors)
            if (source.Length > 0)
                lines.Add(new CodeLineRange(lineStart, sb.Length - lineStart));

            return sb.ToString();
        }
    }
}
