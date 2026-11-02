//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SingleDocAppFramework.Ui.CodeView
{
    // Fragment of the text: Start/Length are char positions in the text passed to ICodeTokenizer.Tokenize
    public struct CodeToken
    {
        public  int             Start;
        public  int             Length;
        public  CodeTokenKind   Kind;

        public CodeToken(int start, int length, CodeTokenKind kind)
        {
            Start   = start;
            Length  = length;
            Kind    = kind;
        }
    }

    // Line of the text (without the line break): char positions
    public struct CodeLineRange
    {
        public  int             Start;
        public  int             Length;

        public CodeLineRange(int start, int length)
        {
            Start   = start;
            Length  = length;
        }
    }

    // Splits the text of a document into colored tokens. Called only when the text or the language changes.
    public interface ICodeTokenizer
    {
        // text: whole text (tabs already expanded), lines: its lines in order.
        // Output: tokens of all lines in order; the tokens of a line cover the whole line without gaps
        // (whitespace included, an empty line has no tokens); lineTokenCounts[i] - number of tokens of line i.
        void Tokenize(string text, IReadOnlyList<CodeLineRange> lines, CodeLanguage language, List<CodeToken> tokens, List<int> lineTokenCounts);
    }
}
