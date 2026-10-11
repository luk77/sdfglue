//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Text;

namespace SdfGlueCore.Controller.CodeGen
{
    // Token-based translation of the generated GLSL code to HLSL (Unity). Not a full compiler:
    // - identifiers are replaced only as whole tokens (types, built-in functions),
    // - atan(y, x) -> atan2, texture -> tex2D, textureLod -> tex2Dlod, one-argument vector constructors -> casts,
    //   mat*(scalar) -> scaled identity,
    // - global variables get "static" (in HLSL a global is a uniform), local struct variables are zero-initialized,
    // - arrays: "float[N] a" -> "float a[N]", array constructors "T[](a, b)" -> "{a, b}",
    // - multiplications with a matrix operand: GLSL "a * b" -> HLSL "mul(b, a)" (matrices built with the same constructor
    //   arguments are transposed in HLSL - rows instead of columns - and the reversed order of mul() compensates it).
    //   Matrix operands are recognized by the declarations (variables, parameters, functions, macros) - expressions with
    //   matrices that cannot be recognized this way should use SDFG_MUL (Includes/Compatibility.glsl).
    public class GlslToHlslTranslator
    {
        private enum TokenKind
        {
            Whitespace,
            Comment,
            Identifier,
            Number,
            Punct,
        }

        private class Token
        {
            public  TokenKind   Kind;
            public  string      Text;
            public  int         Scope;      // function body index (0 - global), used for local matrix declarations

            public Token(TokenKind kind, string text, int scope = 0)
            {
                Kind    = kind;
                Text    = text;
                Scope   = scope;
            }

            public bool IsSignificant { get { return Kind != TokenKind.Whitespace && Kind != TokenKind.Comment; } }

            public override string ToString() { return Text; }
        }

        private static readonly Dictionary<string, string> TypeNames = new Dictionary<string, string>
        {
            { "vec2",   "float2"    }, { "vec3",   "float3"    }, { "vec4",   "float4"    },
            { "ivec2",  "int2"      }, { "ivec3",  "int3"      }, { "ivec4",  "int4"      },
            { "uvec2",  "uint2"     }, { "uvec3",  "uint3"     }, { "uvec4",  "uint4"     },
            { "bvec2",  "bool2"     }, { "bvec3",  "bool3"     }, { "bvec4",  "bool4"     },
            { "mat2",   "float2x2"  }, { "mat3",   "float3x3"  }, { "mat4",   "float4x4"  },
            { "mat2x2", "float2x2"  }, { "mat2x3", "float2x3"  }, { "mat2x4", "float2x4"  },
            { "mat3x2", "float3x2"  }, { "mat3x3", "float3x3"  }, { "mat3x4", "float3x4"  },
            { "mat4x2", "float4x2"  }, { "mat4x3", "float4x3"  }, { "mat4x4", "float4x4"  },
        };

        private static readonly Dictionary<string, string> FunctionNames = new Dictionary<string, string>
        {
            { "mix",            "lerp"  },
            { "fract",          "frac"  },
            { "inversesqrt",    "rsqrt" },
            { "dFdx",           "ddx"   },
            { "dFdy",           "ddy"   },
            { "inverse",        "sdfg_inverse" },   // defined in the Unity template
        };

        private static readonly HashSet<string> ScalarTypes = new HashSet<string> { "float", "int", "uint", "bool" };

        // keywords that can stand before '(' (not function calls)
        private static readonly HashSet<string> Keywords    = new HashSet<string> { "return", "if", "while", "for", "switch", "else", "case", "do" };

        private static readonly string[] MultiCharOperators = { "*=", "+=", "-=", "/=", "%=", "==", "!=", "<=", ">=", "&&", "||", "++", "--", "<<", ">>", "##" };

        // names of the structs (types of global variables) and of the matrix symbols
        private HashSet<string>                 structNames_        = new HashSet<string>();
        // macros that stand for a type, e.g. "#define SDFG_VEC vec3" (name -> GLSL type)
        private Dictionary<string, string>      typeAliases_        = new Dictionary<string, string>();
        private HashSet<string>                 globalMatrices_     = new HashSet<string>();
        private Dictionary<int, HashSet<string>> localMatrices_     = new Dictionary<int, HashSet<string>>();

        public string Translate(string glsl)
        {
            List<Token> tokens = Tokenize(glsl);

            CollectSymbols(tokens);

            tokens = RewriteMatrixMultiplications(tokens);
            tokens = RewriteCalls(tokens);
            RewriteArrayDeclarations(tokens);
            AddStaticToGlobalVariables(tokens);
            InitializeLocalStructs(tokens);
            RenameIdentifiers(tokens);

            StringBuilder sb = new StringBuilder(glsl.Length + glsl.Length / 10);
            foreach (Token t in tokens)
                sb.Append(t.Text);
            return sb.ToString();
        }

        //---------------------------------------------------------------------------
        // Tokenizer
        //---------------------------------------------------------------------------
        private static List<Token> Tokenize(string src, int scope = 0)
        {
            List<Token> tokens = new List<Token>();
            int i = 0;
            while (i < src.Length)
            {
                char c = src[i];
                int start = i;

                if (Char.IsWhiteSpace(c))
                {
                    while (i < src.Length && Char.IsWhiteSpace(src[i]))
                        i++;
                    tokens.Add(new Token(TokenKind.Whitespace, src.Substring(start, i - start), scope));
                }
                else if (c == '/' && i + 1 < src.Length && src[i + 1] == '/')
                {
                    while (i < src.Length && src[i] != '\n' && src[i] != '\r')
                        i++;
                    tokens.Add(new Token(TokenKind.Comment, src.Substring(start, i - start), scope));
                }
                else if (c == '/' && i + 1 < src.Length && src[i + 1] == '*')
                {
                    int end = src.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = (end < 0) ? src.Length : end + 2;
                    tokens.Add(new Token(TokenKind.Comment, src.Substring(start, i - start), scope));
                }
                else if (Char.IsLetter(c) || c == '_')
                {
                    while (i < src.Length && (Char.IsLetterOrDigit(src[i]) || src[i] == '_'))
                        i++;
                    tokens.Add(new Token(TokenKind.Identifier, src.Substring(start, i - start), scope));
                }
                else if (Char.IsDigit(c) || (c == '.' && i + 1 < src.Length && Char.IsDigit(src[i + 1])))
                {
                    while (i < src.Length && (Char.IsLetterOrDigit(src[i]) || src[i] == '.' ||
                           ((src[i] == '+' || src[i] == '-') && (src[i - 1] == 'e' || src[i - 1] == 'E'))))
                        i++;
                    tokens.Add(new Token(TokenKind.Number, src.Substring(start, i - start), scope));
                }
                else
                {
                    string op = c.ToString();
                    foreach (string mop in MultiCharOperators)
                    {
                        if (String.CompareOrdinal(src, i, mop, 0, mop.Length) == 0)
                        {
                            op = mop;
                            break;
                        }
                    }
                    i += op.Length;
                    tokens.Add(new Token(TokenKind.Punct, op, scope));
                }
            }
            return tokens;
        }

        private static string Text(List<Token> tokens, int from, int to)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = from; i <= to; i++)
                sb.Append(tokens[i].Text);
            return sb.ToString();
        }

        private static int NextSignificant(List<Token> tokens, int i)
        {
            for (i++; i < tokens.Count; i++)
            {
                if (tokens[i].IsSignificant)
                    return i;
            }
            return -1;
        }

        private static int PrevSignificant(List<Token> tokens, int i)
        {
            for (i--; i >= 0; i--)
            {
                if (tokens[i].IsSignificant)
                    return i;
            }
            return -1;
        }

        private static bool IsPunct(List<Token> tokens, int i, string text)
        {
            return i >= 0 && i < tokens.Count && tokens[i].Kind == TokenKind.Punct && tokens[i].Text == text;
        }

        // index of the matching closing bracket (open at index i), -1 if not found
        private static int FindClosing(List<Token> tokens, int i)
        {
            string open = tokens[i].Text;
            string close = open == "(" ? ")" : (open == "[" ? "]" : "}");
            int depth = 0;
            for (; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.Punct)
                    continue;
                if (tokens[i].Text == open)
                    depth++;
                else if (tokens[i].Text == close && --depth == 0)
                    return i;
            }
            return -1;
        }

        private static int FindOpening(List<Token> tokens, int i)
        {
            string close = tokens[i].Text;
            string open = close == ")" ? "(" : (close == "]" ? "[" : "{");
            int depth = 0;
            for (; i >= 0; i--)
            {
                if (tokens[i].Kind != TokenKind.Punct)
                    continue;
                if (tokens[i].Text == close)
                    depth++;
                else if (tokens[i].Text == open && --depth == 0)
                    return i;
            }
            return -1;
        }

        // arguments of a call: ranges of tokens between the parentheses (open, close), split by top level commas
        private static List<(int from, int to)> SplitArguments(List<Token> tokens, int open, int close)
        {
            List<(int, int)> args = new List<(int, int)>();
            int depth = 0;
            int argStart = open + 1;
            for (int i = open + 1; i < close; i++)
            {
                if (tokens[i].Kind != TokenKind.Punct)
                    continue;
                string t = tokens[i].Text;
                if (t == "(" || t == "[" || t == "{")
                    depth++;
                else if (t == ")" || t == "]" || t == "}")
                    depth--;
                else if (t == "," && depth == 0)
                {
                    args.Add((argStart, i - 1));
                    argStart = i + 1;
                }
            }
            if (NextSignificant(tokens, argStart - 1) is int n && n >= 0 && n < close)
                args.Add((argStart, close - 1));
            return args;
        }

        // Tokens of a preprocessor directive line ('#' at the beginning of a line, up to the end of the line)
        private static bool[] FindPreprocessorTokens(List<Token> tokens)
        {
            bool[] result = new bool[tokens.Count];
            bool lineStart = true;
            bool inDirective = false;
            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                if (t.Kind == TokenKind.Whitespace && t.Text.Contains('\n'))
                {
                    bool continued = inDirective && i > 0 && tokens[i - 1].Text == "\\";
                    if (!continued)
                        inDirective = false;
                    lineStart = true;
                    result[i] = inDirective;
                    continue;
                }
                if (t.IsSignificant)
                {
                    if (lineStart && t.Kind == TokenKind.Punct && t.Text == "#")
                        inDirective = true;
                    lineStart = false;
                }
                result[i] = inDirective;
            }
            return result;
        }

        //---------------------------------------------------------------------------
        // Symbols: struct names, matrix variables / parameters / functions / macros
        //---------------------------------------------------------------------------
        private static bool IsMatrixType(string name)
        {
            return name.StartsWith("mat", StringComparison.Ordinal) && TypeNames.ContainsKey(name);
        }

        private void CollectSymbols(List<Token> tokens)
        {
            bool[] preproc = FindPreprocessorTokens(tokens);

            int braceDepth = 0;
            int parenDepth = 0;
            int functionCount = 0;
            bool inFunction = false;

            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];

                if (t.Kind == TokenKind.Punct && !preproc[i])
                {
                    if (t.Text == "{")
                    {
                        // a function body starts after ')' at the global level (not a struct)
                        if (braceDepth == 0 && IsPunct(tokens, PrevSignificant(tokens, i), ")"))
                        {
                            functionCount++;
                            inFunction = true;
                        }
                        braceDepth++;
                    }
                    else if (t.Text == "}")
                    {
                        braceDepth--;
                        if (braceDepth == 0)
                            inFunction = false;
                    }
                    else if (t.Text == "(")
                        parenDepth++;
                    else if (t.Text == ")")
                        parenDepth--;
                }

                // parameters of a function belong to its body (declared before the '{')
                int scope = inFunction ? functionCount : ((braceDepth == 0 && parenDepth > 0) ? functionCount + 1 : 0);
                t.Scope = inFunction ? functionCount : 0;

                if (t.Kind != TokenKind.Identifier)
                    continue;

                if (t.Text == "struct")
                {
                    int n = NextSignificant(tokens, i);
                    if (n >= 0 && tokens[n].Kind == TokenKind.Identifier)
                        structNames_.Add(tokens[n].Text);
                    continue;
                }

                if (t.Text == "define" && preproc[i])
                {
                    // #define NAME matN(...)  /  #define NAME (matN(...))
                    int name = NextSignificant(tokens, i);
                    int value = NextSignificant(tokens, name);
                    while (value >= 0 && IsPunct(tokens, value, "("))
                        value = NextSignificant(tokens, value);
                    if (name >= 0 && value >= 0 && preproc[value] && tokens[name].Kind == TokenKind.Identifier && IsMatrixType(tokens[value].Text))
                        globalMatrices_.Add(tokens[name].Text);

                    // #define NAME type
                    int afterValue = (value >= 0) ? NextSignificant(tokens, value) : -1;
                    bool singleToken = afterValue < 0 || Text(tokens, value, afterValue).Contains('\n');
                    if (name >= 0 && value == NextSignificant(tokens, name) && preproc[value] && singleToken && TypeNames.ContainsKey(tokens[value].Text))
                        typeAliases_[tokens[name].Text] = tokens[value].Text;
                    continue;
                }

                if (!IsMatrixType(t.Text))
                    continue;

                int next = NextSignificant(tokens, i);
                if (next < 0 || tokens[next].Kind != TokenKind.Identifier)
                    continue;

                string symbol = tokens[next].Text;
                int afterSymbol = NextSignificant(tokens, next);
                bool isFunction = IsPunct(tokens, afterSymbol, "(");

                if (isFunction || scope == 0)
                    globalMatrices_.Add(symbol);
                else
                {
                    if (!localMatrices_.TryGetValue(scope, out HashSet<string>? locals))
                    {
                        locals = new HashSet<string>();
                        localMatrices_[scope] = locals;
                    }
                    locals.Add(symbol);
                }
            }
        }

        private bool IsMatrixSymbol(string name, int scope)
        {
            if (globalMatrices_.Contains(name))
                return true;
            return localMatrices_.TryGetValue(scope, out HashSet<string>? locals) && locals.Contains(name);
        }

        // Is the expression (tokens from..to) a matrix: a matrix symbol, a call of a function returning a matrix,
        // a matrix constructor, mul() of two matrices, transpose() - also in parentheses
        private bool IsMatrixOperand(List<Token> tokens, int from, int to)
        {
            int first = tokens[from].IsSignificant ? from : NextSignificant(tokens, from);
            int last = tokens[to].IsSignificant ? to : PrevSignificant(tokens, to);
            if (first < 0 || last < 0 || first > last)
                return false;

            // (expression)
            if (IsPunct(tokens, first, "(") && FindClosing(tokens, first) == last)
                return IsMatrixOperand(tokens, first + 1, last - 1);

            if (tokens[first].Kind != TokenKind.Identifier)
                return false;

            string name = tokens[first].Text;
            int scope = tokens[first].Scope;

            if (first == last)
                return IsMatrixSymbol(name, scope);

            // call: name(...)
            int open = NextSignificant(tokens, first);
            if (!IsPunct(tokens, open, "(") || FindClosing(tokens, open) != last)
                return false;

            if (IsMatrixType(name) || name == "transpose" || IsMatrixSymbol(name, scope))
                return true;

            if (name == "mul")
            {
                List<(int from, int to)> args = SplitArguments(tokens, open, last);
                return args.Count == 2 && IsMatrixOperand(tokens, args[0].from, args[0].to) && IsMatrixOperand(tokens, args[1].from, args[1].to);
            }

            return false;
        }

        //---------------------------------------------------------------------------
        // Matrix multiplications: "a * b" -> "mul(b, a)", "a *= b" -> "a = mul(b, a)"
        //---------------------------------------------------------------------------

        // Start of the operand that ends at index end: identifier/number/call/(...) with member access and indexing
        private static int FindOperandStart(List<Token> tokens, int end)
        {
            int i = end;
            while (i >= 0)
            {
                Token t = tokens[i];
                int start;
                if (t.Kind == TokenKind.Punct && (t.Text == ")" || t.Text == "]"))
                {
                    start = FindOpening(tokens, i);
                    if (start < 0)
                        return -1;
                    // function call or indexing - include the name / the indexed expression
                    int prev = PrevSignificant(tokens, start);
                    bool isCall = prev >= 0 && tokens[prev].Kind == TokenKind.Identifier && !Keywords.Contains(tokens[prev].Text);
                    if (isCall || IsPunct(tokens, prev, "]") || IsPunct(tokens, prev, ")"))
                    {
                        i = prev;
                        continue;
                    }
                }
                else if (t.Kind == TokenKind.Identifier || t.Kind == TokenKind.Number)
                    start = i;
                else
                    return -1;

                // member access: a.b
                int dot = PrevSignificant(tokens, start);
                if (IsPunct(tokens, dot, "."))
                {
                    i = PrevSignificant(tokens, dot);
                    continue;
                }
                return start;
            }
            return -1;
        }

        // End of the operand that starts at index start
        private static int FindOperandEnd(List<Token> tokens, int start)
        {
            int i = start;
            // unary sign
            if (IsPunct(tokens, i, "-") || IsPunct(tokens, i, "+"))
                i = NextSignificant(tokens, i);
            if (i < 0)
                return -1;

            int end;
            Token t = tokens[i];
            if (IsPunct(tokens, i, "("))
                end = FindClosing(tokens, i);
            else if (t.Kind == TokenKind.Identifier || t.Kind == TokenKind.Number)
                end = i;
            else
                return -1;

            while (end >= 0)
            {
                int next = NextSignificant(tokens, end);
                if (IsPunct(tokens, next, "(") || IsPunct(tokens, next, "["))
                    end = FindClosing(tokens, next);
                else if (IsPunct(tokens, next, "."))
                {
                    int member = NextSignificant(tokens, next);
                    if (member < 0 || tokens[member].Kind != TokenKind.Identifier)
                        return end;
                    end = member;
                }
                else
                    return end;
            }
            return -1;
        }

        // End of the right side of "a *= b": up to ';' or ',' / ')' at the same level
        private static int FindAssignmentEnd(List<Token> tokens, int start)
        {
            int depth = 0;
            for (int i = start; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.Punct)
                    continue;
                string t = tokens[i].Text;
                if (t == "(" || t == "[")
                    depth++;
                else if (t == ")" || t == "]")
                {
                    if (depth == 0)
                        return PrevSignificant(tokens, i);
                    depth--;
                }
                else if ((t == ";" || t == ",") && depth == 0)
                    return PrevSignificant(tokens, i);
            }
            return -1;
        }

        private List<Token> RewriteMatrixMultiplications(List<Token> tokens)
        {
            bool[] preproc = FindPreprocessorTokens(tokens);

            // from the end - the replaced ranges do not change the indices of the earlier tokens
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                if (tokens[i].Kind != TokenKind.Punct || (tokens[i].Text != "*" && tokens[i].Text != "*="))
                    continue;
                if (i < preproc.Length && preproc[i])
                    continue;

                bool compound = tokens[i].Text == "*=";

                int leftEnd = PrevSignificant(tokens, i);
                int rightStart = NextSignificant(tokens, i);
                if (leftEnd < 0 || rightStart < 0)
                    continue;

                int leftStart = FindOperandStart(tokens, leftEnd);
                int rightEnd = compound ? FindAssignmentEnd(tokens, rightStart) : FindOperandEnd(tokens, rightStart);
                if (leftStart < 0 || rightEnd < 0)
                    continue;

                if (!IsMatrixOperand(tokens, leftStart, leftEnd) && !IsMatrixOperand(tokens, rightStart, rightEnd))
                    continue;

                string left  = Text(tokens, leftStart, leftEnd);
                string right = Text(tokens, rightStart, rightEnd);
                string replacement = compound ? String.Format("{0} = mul({1}, {0})", left, right) : String.Format("mul({0}, {1})", right, left);

                List<Token> newTokens = Tokenize(replacement, tokens[leftStart].Scope);
                tokens.RemoveRange(leftStart, rightEnd - leftStart + 1);
                tokens.InsertRange(leftStart, newTokens);
                i = leftStart;
            }

            return tokens;
        }

        //---------------------------------------------------------------------------
        // Calls: atan(y, x), texture, textureLod, constructors with one argument
        //---------------------------------------------------------------------------
        private List<Token> RewriteCalls(List<Token> tokens)
        {
            for (int i = tokens.Count - 1; i >= 0; i--)
            {
                Token t = tokens[i];
                if (t.Kind != TokenKind.Identifier)
                    continue;

                int open = NextSignificant(tokens, i);
                if (!IsPunct(tokens, open, "("))
                    continue;
                // member function / field (e.g. a.texture) - not a built-in
                if (IsPunct(tokens, PrevSignificant(tokens, i), "."))
                    continue;

                int close = FindClosing(tokens, open);
                if (close < 0)
                    continue;

                List<(int from, int to)> args = SplitArguments(tokens, open, close);
                string? replacement = null;

                if (t.Text == "atan" && args.Count == 2)
                {
                    t.Text = "atan2";
                }
                else if (t.Text == "texture" && args.Count == 2)
                {
                    t.Text = "tex2D";
                }
                else if (t.Text == "textureLod" && args.Count == 3)
                {
                    replacement = String.Format("tex2Dlod({0}, float4({1}, 0.0, {2}))",
                        Text(tokens, args[0].from, args[0].to).Trim(), Text(tokens, args[1].from, args[1].to).Trim(), Text(tokens, args[2].from, args[2].to).Trim());
                }
                else if (args.Count == 1 && TypeNames.ContainsKey(ResolveType(t.Text)))
                {
                    string arg = Text(tokens, args[0].from, args[0].to).Trim();
                    // a type alias (macro) is kept - it may be redefined (e.g. SDFG_VEC: 3d / 4d)
                    string hlslType = typeAliases_.ContainsKey(t.Text) ? t.Text : TypeNames[t.Text];
                    if (IsMatrixType(ResolveType(t.Text)))
                    {
                        // GLSL: matN(scalar) - diagonal matrix, matN(matM) - conversion
                        if (IsMatrixOperand(tokens, args[0].from, args[0].to))
                            replacement = String.Format("(({0})({1}))", hlslType, arg);
                        else
                            replacement = String.Format("(({1}) * {0}({2}))", hlslType, arg, IdentityValues(ResolveType(t.Text)));
                    }
                    else
                    {
                        // GLSL: vecN(scalar) - all components, vecN(vecM) - first components
                        replacement = String.Format("(({0})({1}))", hlslType, arg);
                    }
                }

                if (replacement != null)
                {
                    List<Token> newTokens = Tokenize(replacement, t.Scope);
                    tokens.RemoveRange(i, close - i + 1);
                    tokens.InsertRange(i, newTokens);
                }
            }
            return tokens;
        }

        private string ResolveType(string name)
        {
            return typeAliases_.TryGetValue(name, out string? type) ? type : name;
        }

        private bool IsTypeName(string name)
        {
            return ScalarTypes.Contains(name) || TypeNames.ContainsKey(name) || structNames_.Contains(name) || typeAliases_.ContainsKey(name);
        }

        //---------------------------------------------------------------------------
        // Arrays: GLSL "float[N] name" -> HLSL "float name[N]", "float[](a, b)" -> "{a, b}"
        //---------------------------------------------------------------------------
        private void RewriteArrayDeclarations(List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                if (tokens[i].Kind != TokenKind.Identifier || !IsTypeName(tokens[i].Text))
                    continue;

                int open = NextSignificant(tokens, i);
                if (!IsPunct(tokens, open, "["))
                    continue;
                int close = FindClosing(tokens, open);
                int name = (close >= 0) ? NextSignificant(tokens, close) : -1;

                // array constructor (initializer)
                if (IsPunct(tokens, name, "("))
                {
                    int argsClose = FindClosing(tokens, name);
                    if (argsClose < 0)
                        continue;
                    string values = Text(tokens, name + 1, argsClose - 1);
                    List<Token> list = Tokenize("{" + values + "}", tokens[i].Scope);
                    tokens.RemoveRange(i, argsClose - i + 1);
                    tokens.InsertRange(i, list);
                    continue;
                }

                if (name < 0 || tokens[name].Kind != TokenKind.Identifier)
                    continue;
                int after = NextSignificant(tokens, name);
                if (!IsPunct(tokens, after, ";") && !IsPunct(tokens, after, "=") && !IsPunct(tokens, after, ",") && !IsPunct(tokens, after, ")"))
                    continue;

                List<Token> size = tokens.GetRange(open, close - open + 1);
                tokens.RemoveRange(open, close - open + 1);
                name -= size.Count;
                tokens.InsertRange(name + 1, size);
            }
        }

        private static string IdentityValues(string matrixType)
        {
            int n = matrixType[3] - '0';
            List<string> values = new List<string>();
            for (int r = 0; r < n; r++)
            {
                for (int c = 0; c < n; c++)
                    values.Add(r == c ? "1.0" : "0.0");
            }
            return String.Join(", ", values);
        }

        //---------------------------------------------------------------------------
        // Global variables: "static" (a global variable without it is a uniform in HLSL)
        //---------------------------------------------------------------------------
        private void AddStaticToGlobalVariables(List<Token> tokens)
        {
            bool[] preproc = FindPreprocessorTokens(tokens);

            int depth = 0;
            bool statementStart = true;
            List<int> insertAt = new List<int>();

            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                if (!t.IsSignificant || preproc[i])
                    continue;

                if (t.Kind == TokenKind.Punct)
                {
                    if (t.Text == "{" || t.Text == "(" || t.Text == "[")
                        depth++;
                    else if (t.Text == "}" || t.Text == ")" || t.Text == "]")
                    {
                        depth--;
                        if (t.Text == "}" && depth == 0)
                            statementStart = true;
                        continue;
                    }
                    else if (t.Text == ";" && depth == 0)
                    {
                        statementStart = true;
                        continue;
                    }
                }

                if (depth == 0 && statementStart)
                {
                    statementStart = false;
                    if (IsGlobalVariableDeclaration(tokens, i))
                        insertAt.Add(i);
                }
            }

            for (int k = insertAt.Count - 1; k >= 0; k--)
                tokens.InsertRange(insertAt[k], Tokenize("static "));
        }

        //---------------------------------------------------------------------------
        // Local struct variables without initialization: "= (T)0" (HLSL reports partially initialized structs
        // as errors, in GLSL the values are undefined)
        //---------------------------------------------------------------------------
        private void InitializeLocalStructs(List<Token> tokens)
        {
            bool[] preproc = FindPreprocessorTokens(tokens);
            List<(int index, string type)> initializations = new List<(int, string)>();

            // braces: true - body of a struct (members, not variables)
            Stack<bool> braces = new Stack<bool>();
            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                if (preproc[i])
                    continue;
                if (t.Kind == TokenKind.Punct)
                {
                    if (t.Text == "{")
                    {
                        int structName = PrevSignificant(tokens, i);
                        int structKeyword = (structName >= 0) ? PrevSignificant(tokens, structName) : -1;
                        braces.Push(structKeyword >= 0 && tokens[structKeyword].Text == "struct");
                    }
                    else if (t.Text == "}" && braces.Count > 0)
                        braces.Pop();
                    continue;
                }
                if (braces.Count == 0 || braces.Peek() || t.Kind != TokenKind.Identifier || !structNames_.Contains(t.Text))
                    continue;

                // statement start: after '{', '}' or ';' (preprocessor lines are skipped)
                int prev = PrevSignificant(tokens, i);
                while (prev >= 0 && preproc[prev])
                    prev = PrevSignificant(tokens, prev);
                if (!IsPunct(tokens, prev, "{") && !IsPunct(tokens, prev, "}") && !IsPunct(tokens, prev, ";"))
                    continue;

                int name = NextSignificant(tokens, i);
                if (name < 0 || tokens[name].Kind != TokenKind.Identifier)
                    continue;
                int after = NextSignificant(tokens, name);
                if (!IsPunct(tokens, after, ";"))
                    continue;

                initializations.Add((after, t.Text));
            }

            for (int k = initializations.Count - 1; k >= 0; k--)
                tokens.InsertRange(initializations[k].index, Tokenize(String.Format(" = ({0})0", initializations[k].type)));
        }

        // [const] type name ( ';' | '=' | '[' | ',' )
        private bool IsGlobalVariableDeclaration(List<Token> tokens, int i)
        {
            int typeIndex = (tokens[i].Text == "const") ? NextSignificant(tokens, i) : i;
            if (typeIndex < 0)
                return false;

            string type = tokens[typeIndex].Text;
            if (!IsTypeName(type))
                return false;

            int name = NextSignificant(tokens, typeIndex);
            if (name < 0 || tokens[name].Kind != TokenKind.Identifier)
                return false;

            int after = NextSignificant(tokens, name);
            return IsPunct(tokens, after, ";") || IsPunct(tokens, after, "=") || IsPunct(tokens, after, "[") || IsPunct(tokens, after, ",");
        }

        //---------------------------------------------------------------------------
        // Types and built-in functions
        //---------------------------------------------------------------------------
        private static void RenameIdentifiers(List<Token> tokens)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                Token t = tokens[i];
                if (t.Kind != TokenKind.Identifier)
                    continue;
                if (IsPunct(tokens, PrevSignificant(tokens, i), "."))
                    continue;

                if (TypeNames.TryGetValue(t.Text, out string? typeName))
                    t.Text = typeName;
                else if (FunctionNames.TryGetValue(t.Text, out string? functionName))
                    t.Text = functionName;
            }
        }
    }
}
