//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SingleDocAppFramework.Ui.CodeView
{
    // Tokenizer of C-like languages driven by CodeLanguage: line/block comments, preprocessor directives,
    // strings, numbers (decimal, hex, exponent, suffixes), identifiers, punctuation.
    // Identifiers are classified in this order: CodeLanguage words, names declared in the text (DeclarationRules),
    // prefixes, IdentifierClassifier, function calls (followed by '('), otherwise Default.
    public class CStyleTokenizer : ICodeTokenizer
    {
        public static readonly CStyleTokenizer Instance = new CStyleTokenizer();

        private struct RawToken
        {
            public  int             Start;
            public  int             Length;
            public  CodeTokenKind   Kind;
            public  bool            IsIdentifier;
            public  bool            IsWhitespace;
            public  bool            FollowedByParen;
            public  int             Line;
        }

        // Declaration rules look for the name at most this many tokens after the keyword
        private const int MaxDeclarationTokens = 16;

        public void Tokenize(string text, IReadOnlyList<CodeLineRange> lines, CodeLanguage language, List<CodeToken> tokens, List<int> lineTokenCounts)
        {
            List<RawToken> raw = new List<RawToken>();

            bool inBlockComment = false;
            for(int i=0; i<lines.Count; i++)
                TokenizeLine(text, lines[i], i, language, ref inBlockComment, raw);

            Dictionary<string, CodeTokenKind> declared = FindDeclarations(text, raw, language);

            ClassifyIdentifiers(text, raw, language, declared);

            Emit(raw, lines.Count, tokens, lineTokenCounts);
        }

        //-------------------------------------------------------------------
        // Lexing
        //-------------------------------------------------------------------

        private static void TokenizeLine(string text, CodeLineRange line, int lineIndex, CodeLanguage lang, ref bool inBlockComment, List<RawToken> raw)
        {
            int pos = line.Start;
            int end = line.Start + line.Length;
            bool firstToken = true;

            while (pos < end)
            {
                int start = pos;

                if (inBlockComment)
                {
                    int close = IndexOf(text, lang.BlockCommentEnd!, pos, end);
                    if (close < 0)
                    {
                        pos = end;
                    }
                    else
                    {
                        pos = close + lang.BlockCommentEnd!.Length;
                        inBlockComment = false;
                    }
                    Add(raw, start, pos, CodeTokenKinds.Comment, lineIndex);
                    continue;
                }

                char c = text[pos];

                if (Char.IsWhiteSpace(c))
                {
                    while (pos < end && Char.IsWhiteSpace(text[pos]))
                        pos++;
                    RawToken ws = MakeToken(start, pos, CodeTokenKinds.Default, lineIndex);
                    ws.IsWhitespace = true;
                    raw.Add(ws);
                    continue;
                }

                if (StartsWith(text, pos, end, lang.LineComment))
                {
                    Add(raw, pos, end, CodeTokenKinds.Comment, lineIndex);
                    pos = end;
                    continue;
                }

                if (StartsWith(text, pos, end, lang.BlockCommentStart) && !String.IsNullOrEmpty(lang.BlockCommentEnd))
                {
                    int close = IndexOf(text, lang.BlockCommentEnd, pos + lang.BlockCommentStart!.Length, end);
                    if (close < 0)
                    {
                        pos = end;
                        inBlockComment = true;
                    }
                    else
                    {
                        pos = close + lang.BlockCommentEnd.Length;
                    }
                    Add(raw, start, pos, CodeTokenKinds.Comment, lineIndex);
                    firstToken = false;
                    continue;
                }

                if (firstToken && lang.PreprocessorChar != '\0' && c == lang.PreprocessorChar)
                {
                    // "#", optional whitespace, directive name
                    pos++;
                    while (pos < end && (text[pos] == ' ' || text[pos] == '\t'))
                        pos++;
                    while (pos < end && IsIdentifierChar(text[pos]))
                        pos++;
                    Add(raw, start, pos, CodeTokenKinds.Preprocessor, lineIndex);
                    firstToken = false;
                    continue;
                }

                firstToken = false;

                if (lang.StringQuotes.IndexOf(c) >= 0)
                {
                    pos++;
                    while (pos < end)
                    {
                        char s = text[pos];
                        if (s == lang.EscapeChar && lang.EscapeChar != '\0' && pos + 1 < end)
                        {
                            pos += 2;
                            continue;
                        }
                        pos++;
                        if (s == c)
                            break;
                    }
                    Add(raw, start, pos, CodeTokenKinds.String, lineIndex);
                    continue;
                }

                if (Char.IsDigit(c) || (c == '.' && pos + 1 < end && Char.IsDigit(text[pos + 1])))
                {
                    pos = ReadNumber(text, pos, end, lang.NumberSuffixes);
                    Add(raw, start, pos, CodeTokenKinds.Number, lineIndex);
                    continue;
                }

                if (IsIdentifierStart(c))
                {
                    while (pos < end && IsIdentifierChar(text[pos]))
                        pos++;

                    RawToken ident = MakeToken(start, pos, CodeTokenKinds.Default, lineIndex);
                    ident.IsIdentifier = true;

                    int next = pos;
                    while (next < end && Char.IsWhiteSpace(text[next]))
                        next++;
                    ident.FollowedByParen = (next < end && text[next] == '(');

                    raw.Add(ident);
                    continue;
                }

                // punctuation / operators: one char per token (merged later with neighbours of the same kind)
                pos++;
                Add(raw, start, pos, lang.HighlightOperators ? CodeTokenKinds.Operator : CodeTokenKinds.Default, lineIndex);
            }
        }

        private static int ReadNumber(string text, int pos, int end, string suffixes)
        {
            if (text[pos] == '0' && pos + 1 < end && (text[pos + 1] == 'x' || text[pos + 1] == 'X'))
            {
                pos += 2;
                while (pos < end && Uri.IsHexDigit(text[pos]))
                    pos++;
            }
            else
            {
                while (pos < end && Char.IsDigit(text[pos]))
                    pos++;
                if (pos < end && text[pos] == '.')
                {
                    pos++;
                    while (pos < end && Char.IsDigit(text[pos]))
                        pos++;
                }
                if (pos < end && (text[pos] == 'e' || text[pos] == 'E'))
                {
                    int exp = pos + 1;
                    if (exp < end && (text[exp] == '+' || text[exp] == '-'))
                        exp++;
                    if (exp < end && Char.IsDigit(text[exp]))
                    {
                        pos = exp;
                        while (pos < end && Char.IsDigit(text[pos]))
                            pos++;
                    }
                }
            }

            while (pos < end && suffixes.IndexOf(text[pos]) >= 0)
                pos++;

            return pos;
        }

        //-------------------------------------------------------------------
        // Classification
        //-------------------------------------------------------------------

        // Names declared in the text: keyword of a rule -> name -> category
        private static Dictionary<string, CodeTokenKind> FindDeclarations(string text, List<RawToken> raw, CodeLanguage lang)
        {
            Dictionary<string, CodeTokenKind> declared = new Dictionary<string, CodeTokenKind>(lang.CaseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            if (lang.DeclarationRules.Count == 0)
                return declared;

            for(int i=0; i<raw.Count; i++)
            {
                RawToken token = raw[i];
                if (!token.IsIdentifier && token.Kind != CodeTokenKinds.Preprocessor)
                    continue;

                CodeDeclarationRule? rule = FindRule(text, token, lang);
                if (rule == null)
                    continue;

                int nameIndex = -1;
                int seen = 0;
                for(int j=i+1; j<raw.Count && seen < MaxDeclarationTokens; j++)
                {
                    RawToken t = raw[j];
                    if (t.IsWhitespace || t.Kind == CodeTokenKinds.Comment)
                        continue;
                    seen++;

                    if (rule.NamePosition == CodeDeclarationName.NextIdentifier)
                    {
                        if (t.IsIdentifier)
                            nameIndex = j;
                        break;
                    }

                    if (t.IsIdentifier)
                    {
                        nameIndex = j;
                        continue;
                    }

                    char c = text[t.Start];
                    if (c != ';' && c != '=' && c != '[' && c != ',' && c != ')')
                        nameIndex = -1;     // e.g. "uniform Block { ... }" - not a simple declaration
                    break;
                }

                if (nameIndex >= 0)
                {
                    RawToken name = raw[nameIndex];
                    declared[text.Substring(name.Start, name.Length)] = rule.Kind;
                }
            }

            return declared;
        }

        private static CodeDeclarationRule? FindRule(string text, RawToken token, CodeLanguage lang)
        {
            ReadOnlySpan<char> span = text.AsSpan(token.Start, token.Length);
            StringComparison comparison = lang.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach(CodeDeclarationRule rule in lang.DeclarationRules)
            {
                if (token.IsIdentifier)
                {
                    if (span.Equals(rule.Keyword.AsSpan(), comparison))
                        return rule;
                }
                else if (rule.Keyword.Length > 0 && rule.Keyword[0] == lang.PreprocessorChar)
                {
                    // directive: compare without the whitespace between '#' and the name ("#  define")
                    ReadOnlySpan<char> directive = span.Slice(1).TrimStart();
                    if (directive.Equals(rule.Keyword.AsSpan(1), comparison))
                        return rule;
                }
            }
            return null;
        }

        private static void ClassifyIdentifiers(string text, List<RawToken> raw, CodeLanguage lang, Dictionary<string, CodeTokenKind> declared)
        {
            Dictionary<string, CodeTokenKind>.AlternateLookup<ReadOnlySpan<char>> declaredLookup = declared.GetAlternateLookup<ReadOnlySpan<char>>();
            StringComparison comparison = lang.CaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            for(int i=0; i<raw.Count; i++)
            {
                RawToken token = raw[i];
                if (!token.IsIdentifier)
                    continue;

                ReadOnlySpan<char> word = text.AsSpan(token.Start, token.Length);
                CodeTokenKind kind;

                if (lang.TryGetWord(word, out kind) || declaredLookup.TryGetValue(word, out kind))
                {
                    token.Kind = kind;
                }
                else if (TryGetPrefix(word, lang, comparison, out kind))
                {
                    token.Kind = kind;
                }
                else
                {
                    CodeTokenKind? classified = lang.IdentifierClassifier?.Invoke(word.ToString());
                    if (classified.HasValue)
                        token.Kind = classified.Value;
                    else if (lang.HighlightFunctionCalls && token.FollowedByParen)
                        token.Kind = CodeTokenKinds.Function;
                }

                raw[i] = token;
            }
        }

        private static bool TryGetPrefix(ReadOnlySpan<char> word, CodeLanguage lang, StringComparison comparison, out CodeTokenKind kind)
        {
            foreach(CodeWordPrefix prefix in lang.Prefixes)
            {
                if (word.Length > prefix.Prefix.Length && word.StartsWith(prefix.Prefix.AsSpan(), comparison))
                {
                    kind = prefix.Kind;
                    return true;
                }
            }
            kind = CodeTokenKinds.Default;
            return false;
        }

        //-------------------------------------------------------------------
        // Output
        //-------------------------------------------------------------------

        // Neighbours of the same kind and whitespace are merged with the previous token of the line (fewer draw calls)
        private static void Emit(List<RawToken> raw, int lineCount, List<CodeToken> tokens, List<int> lineTokenCounts)
        {
            int r = 0;
            for(int line=0; line<lineCount; line++)
            {
                int first = tokens.Count;
                while (r < raw.Count && raw[r].Line == line)
                {
                    RawToken t = raw[r++];
                    if (tokens.Count > first)
                    {
                        CodeToken last = tokens[tokens.Count - 1];
                        if (t.IsWhitespace || t.Kind == last.Kind)
                        {
                            last.Length = t.Start + t.Length - last.Start;
                            tokens[tokens.Count - 1] = last;
                            continue;
                        }
                    }
                    tokens.Add(new CodeToken(t.Start, t.Length, t.Kind));
                }
                lineTokenCounts.Add(tokens.Count - first);
            }
        }

        //-------------------------------------------------------------------
        // Helpers
        //-------------------------------------------------------------------

        private static RawToken MakeToken(int start, int end, CodeTokenKind kind, int line)
        {
            RawToken token = new RawToken();
            token.Start     = start;
            token.Length    = end - start;
            token.Kind      = kind;
            token.Line      = line;
            return token;
        }

        private static void Add(List<RawToken> raw, int start, int end, CodeTokenKind kind, int line)
        {
            if (end > start)
                raw.Add(MakeToken(start, end, kind, line));
        }

        private static bool StartsWith(string text, int pos, int end, string? value)
        {
            if (String.IsNullOrEmpty(value) || pos + value.Length > end)
                return false;

            return String.CompareOrdinal(text, pos, value, 0, value.Length) == 0;
        }

        private static int IndexOf(string text, string value, int start, int end)
        {
            if (start >= end)
                return -1;

            return text.IndexOf(value, start, end - start, StringComparison.Ordinal);
        }

        private static bool IsIdentifierStart(char c)
        {
            return Char.IsLetter(c) || c == '_';
        }

        private static bool IsIdentifierChar(char c)
        {
            return Char.IsLetterOrDigit(c) || c == '_';
        }
    }
}
