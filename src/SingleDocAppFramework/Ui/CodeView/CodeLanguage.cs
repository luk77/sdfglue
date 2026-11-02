//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SingleDocAppFramework.Ui.CodeView
{
    // Which identifier after the keyword of a declaration is the declared name
    public enum CodeDeclarationName
    {
        NextIdentifier,                     // "#define NAME ..."
        LastIdentifierBeforePunctuation,    // "uniform highp vec3 NAME;" (the identifier before ; = [ , ) )
    }

    // A declaration found in the text gives its name a category in the whole text,
    // e.g. all uses of a uniform are colored like the uniform.
    public class CodeDeclarationRule
    {
        public  string                  Keyword;        // identifier ("uniform") or preprocessor directive ("#define")
        public  CodeTokenKind           Kind;
        public  CodeDeclarationName     NamePosition;

        public CodeDeclarationRule(string keyword, CodeTokenKind kind, CodeDeclarationName namePosition)
        {
            Keyword         = keyword;
            Kind            = kind;
            NamePosition    = namePosition;
        }
    }

    // Identifiers starting with a prefix (e.g. "gl_") get a category - checked when the identifier is not in Words
    public class CodeWordPrefix
    {
        public  string                  Prefix;
        public  CodeTokenKind           Kind;

        public CodeWordPrefix(string prefix, CodeTokenKind kind)
        {
            Prefix  = prefix;
            Kind    = kind;
        }
    }

    // Data-driven description of a language for the code view (UiCodeView / CodeViewDocument).
    // The default tokenizer (CStyleTokenizer) handles C-like languages; a language with different rules
    // can provide its own ICodeTokenizer.
    //
    // Extending a language (e.g. names of an application's own functions):
    //   CodeLanguage lang = CodeLanguages.Glsl();          // every call returns a new instance
    //   lang.AddWords(myKind, myFunctionNames);
    // or Clone() a language that is already in use. After changing the public fields directly call MarkChanged(),
    // so documents using the language are tokenized again.
    public class CodeLanguage
    {
        private static int                          nextVersion_            = 1;

        public  string                              Name;
        public  string?                             LineComment             = "//";
        public  string?                             BlockCommentStart       = "/*";
        public  string?                             BlockCommentEnd         = "*/";
        public  char                                PreprocessorChar        = '#';      // '\0' - no preprocessor; a directive must be the first token of a line
        public  string                              StringQuotes            = "\"";
        public  char                                EscapeChar              = '\\';
        public  string                              NumberSuffixes          = "";       // letters allowed at the end of numbers (e.g. "uUfF")
        public  bool                                HighlightFunctionCalls  = true;     // identifier followed by '(' -> CodeTokenKinds.Function
        public  bool                                HighlightOperators      = true;     // punctuation -> CodeTokenKinds.Operator (otherwise Default)
        public  List<CodeDeclarationRule>           DeclarationRules        = new List<CodeDeclarationRule>();
        public  List<CodeWordPrefix>                Prefixes                = new List<CodeWordPrefix>();
        // Last resort for identifiers that are not words, declared names or prefixes; null result - no category
        public  Func<string, CodeTokenKind?>?       IdentifierClassifier    = null;
        public  ICodeTokenizer                      Tokenizer               = CStyleTokenizer.Instance;

        private Dictionary<string, CodeTokenKind>   words_;

        public CodeLanguage(string name, bool caseSensitive = true)
        {
            Name            = name;
            CaseSensitive   = caseSensitive;
            words_          = new Dictionary<string, CodeTokenKind>(caseSensitive ? StringComparer.Ordinal : StringComparer.OrdinalIgnoreCase);
            Version         = nextVersion_++;
        }

        public bool CaseSensitive { get; }

        // Changed by every modification (MarkChanged) - documents compare it to know when to tokenize again
        public int Version { get; private set; }

        public void MarkChanged()
        {
            Version = nextVersion_++;
        }

        public IReadOnlyDictionary<string, CodeTokenKind> Words { get { return words_; } }

        // A word already added gets the new category
        public void AddWords(CodeTokenKind kind, IEnumerable<string> words)
        {
            foreach(string word in words)
            {
                if (!String.IsNullOrEmpty(word))
                    words_[word] = kind;
            }
            MarkChanged();
        }

        // Words separated by whitespace
        public void AddWords(CodeTokenKind kind, string words)
        {
            AddWords(kind, words.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }

        public void RemoveWord(string word)
        {
            if (words_.Remove(word))
                MarkChanged();
        }

        public bool TryGetWord(ReadOnlySpan<char> word, out CodeTokenKind kind)
        {
            Dictionary<string, CodeTokenKind>.AlternateLookup<ReadOnlySpan<char>> lookup = words_.GetAlternateLookup<ReadOnlySpan<char>>();
            return lookup.TryGetValue(word, out kind);
        }

        public void AddPrefix(string prefix, CodeTokenKind kind)
        {
            Prefixes.Add(new CodeWordPrefix(prefix, kind));
            MarkChanged();
        }

        public void AddDeclarationRule(string keyword, CodeTokenKind kind, CodeDeclarationName namePosition)
        {
            DeclarationRules.Add(new CodeDeclarationRule(keyword, kind, namePosition));
            MarkChanged();
        }

        // Independent copy (words, rules and prefixes are copied; the tokenizer and the classifier are shared)
        public CodeLanguage Clone(string name)
        {
            CodeLanguage copy = new CodeLanguage(name, CaseSensitive);
            copy.LineComment            = LineComment;
            copy.BlockCommentStart      = BlockCommentStart;
            copy.BlockCommentEnd        = BlockCommentEnd;
            copy.PreprocessorChar       = PreprocessorChar;
            copy.StringQuotes           = StringQuotes;
            copy.EscapeChar             = EscapeChar;
            copy.NumberSuffixes         = NumberSuffixes;
            copy.HighlightFunctionCalls = HighlightFunctionCalls;
            copy.HighlightOperators     = HighlightOperators;
            copy.IdentifierClassifier   = IdentifierClassifier;
            copy.Tokenizer              = Tokenizer;

            foreach(CodeDeclarationRule rule in DeclarationRules)
                copy.DeclarationRules.Add(new CodeDeclarationRule(rule.Keyword, rule.Kind, rule.NamePosition));
            foreach(CodeWordPrefix prefix in Prefixes)
                copy.Prefixes.Add(new CodeWordPrefix(prefix.Prefix, prefix.Kind));
            foreach(KeyValuePair<string, CodeTokenKind> word in words_)
                copy.words_[word.Key] = word.Value;

            return copy;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
