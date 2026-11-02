//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Numerics;

namespace SingleDocAppFramework.Ui.CodeView
{
    // Category of a code token (keyword, number, comment ...) - an index into the CodeTokenKinds registry
    public readonly struct CodeTokenKind : IEquatable<CodeTokenKind>
    {
        public readonly ushort  Index;

        internal CodeTokenKind(ushort index)
        {
            Index = index;
        }

        public CodeTokenKindInfo Info { get { return CodeTokenKinds.GetInfo(this); } }

        public bool Equals(CodeTokenKind other)                         { return Index == other.Index; }
        public override bool Equals(object? obj)                        { return obj is CodeTokenKind other && Equals(other); }
        public override int GetHashCode()                               { return Index; }
        public static bool operator ==(CodeTokenKind a, CodeTokenKind b) { return a.Index == b.Index; }
        public static bool operator !=(CodeTokenKind a, CodeTokenKind b) { return a.Index != b.Index; }
        public override string ToString()                               { return Info.Id; }
    }

    public class CodeTokenKindInfo
    {
        public  readonly CodeTokenKind  Kind;
        public  readonly string         Id;                 // key in the user settings (UserSettingsBase.SyntaxColorsDark/Light)
        public  readonly string         DisplayName;        // name in the settings dialog
        public  readonly Vector3        DefaultDarkColor;   // palette of dark UI styles
        public  readonly Vector3        DefaultLightColor;  // palette of light UI styles
        public  readonly bool           UsesStyleTextColor; // default color = ImGuiCol.Text of the current style (CodeTokenKinds.Default)

        internal CodeTokenKindInfo(CodeTokenKind kind, string id, string displayName, Vector3 darkColor, Vector3 lightColor, bool usesStyleTextColor)
        {
            Kind                = kind;
            Id                  = id;
            DisplayName         = displayName;
            DefaultDarkColor    = darkColor;
            DefaultLightColor   = lightColor;
            UsesStyleTextColor  = usesStyleTextColor;
        }
    }

    // Registry of token categories used by the code view (UiCodeView). The framework registers the common ones,
    // applications add their own with Register() (e.g. names of functions from their library) - preferably at startup,
    // so they are listed in the settings dialog ("Syntax colors") from the beginning.
    public static class CodeTokenKinds
    {
        private static readonly List<CodeTokenKindInfo> kinds_ = new List<CodeTokenKindInfo>();

        // whitespace, identifiers without a category, other text
        public static readonly CodeTokenKind Default        = RegisterInternal("default"      , "Default text"              , Vector3.One       , Vector3.Zero      , true);

        public static readonly CodeTokenKind Keyword        = Register("keyword"        , "Keyword"                     , Rgb(0x569CD6)     , Rgb(0x0000FF));
        public static readonly CodeTokenKind Type           = Register("type"           , "Type"                        , Rgb(0x4EC9B0)     , Rgb(0x267F99));
        public static readonly CodeTokenKind Builtin        = Register("builtin"        , "Built-in function/variable"  , Rgb(0x4FC1FF)     , Rgb(0x0070C1));
        public static readonly CodeTokenKind Function       = Register("function"       , "Function"                    , Rgb(0xDCDCAA)     , Rgb(0x795E26));
        public static readonly CodeTokenKind Number         = Register("number"         , "Number"                      , Rgb(0xB5CEA8)     , Rgb(0x098658));
        public static readonly CodeTokenKind String         = Register("string"         , "String"                      , Rgb(0xCE9178)     , Rgb(0xA31515));
        public static readonly CodeTokenKind Comment        = Register("comment"        , "Comment"                     , Rgb(0x6A9955)     , Rgb(0x008000));
        public static readonly CodeTokenKind Preprocessor   = Register("preprocessor"   , "Preprocessor directive"      , Rgb(0xC586C0)     , Rgb(0xAF00DB));
        public static readonly CodeTokenKind Macro          = Register("macro"          , "Macro"                       , Rgb(0xBD8CE0)     , Rgb(0x6F008A));
        public static readonly CodeTokenKind Uniform        = Register("uniform"        , "Uniform / parameter"         , Rgb(0x9CDCFE)     , Rgb(0x001080));
        public static readonly CodeTokenKind Operator       = Register("operator"       , "Operator / punctuation"      , Rgb(0xB4B4B4)     , Rgb(0x505050));

        // Adds a category, or returns the registered one with the same id (ids are case-sensitive).
        // Colors: components 0..1 (see Rgb()).
        public static CodeTokenKind Register(string id, string displayName, Vector3 darkColor, Vector3 lightColor)
        {
            return RegisterInternal(id, displayName, darkColor, lightColor, false);
        }

        private static CodeTokenKind RegisterInternal(string id, string displayName, Vector3 darkColor, Vector3 lightColor, bool usesStyleTextColor)
        {
            CodeTokenKindInfo? existing = Find(id);
            if (existing != null)
                return existing.Kind;

            if (kinds_.Count >= ushort.MaxValue)
                throw new InvalidOperationException("Too many code token kinds");

            CodeTokenKind kind = new CodeTokenKind((ushort)kinds_.Count);
            kinds_.Add(new CodeTokenKindInfo(kind, id, displayName, darkColor, lightColor, usesStyleTextColor));
            return kind;
        }

        public static int Count { get { return kinds_.Count; } }

        // In registration order
        public static IReadOnlyList<CodeTokenKindInfo> GetAll()
        {
            return kinds_;
        }

        public static CodeTokenKindInfo? Find(string id)
        {
            foreach(CodeTokenKindInfo info in kinds_)
            {
                if (info.Id == id)
                    return info;
            }
            return null;
        }

        public static CodeTokenKindInfo GetInfo(CodeTokenKind kind)
        {
            return kinds_[kind.Index];
        }

        // 0xRRGGBB -> color with components 0..1
        public static Vector3 Rgb(int rgb)
        {
            return new Vector3(((rgb >> 16) & 0xFF) / 255.0f, ((rgb >> 8) & 0xFF) / 255.0f, (rgb & 0xFF) / 255.0f);
        }
    }
}
