//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Runtime.InteropServices;

namespace SingleDocAppFramework.Ui.Fonts
{
    // Purpose of a font: each role has its own font family and size (user settings UiFont/UiFontSize, CodeFont/CodeFontSize)
    public enum UiFontRole
    {
        Ui      = 0,    // user interface text (the default ImGui font)
        Code    = 1,    // code, logs etc. (a monospace font)
    }

    // Variant of a font family
    public enum UiFontStyle
    {
        Regular     = 0,
        Bold        = 1,
        Italic      = 2,
        BoldItalic  = 3,
    }

    // Fonts of the user interface (TTF/OTF files from SdAppSettings.FontsDirectory or the built-in ImGui font).
    //
    // Font families are recognized by file names "<Family>-<Style>.ttf|otf" (any subdirectory), styles: Regular, Bold,
    // Italic, BoldItalic (also Oblique, BoldOblique). Other weights (Light, Black ...) are skipped; a file without
    // a style suffix is a family with the Regular variant only. A family is identified by the path of its Regular file
    // relative to the fonts directory (UserSettingsBase.UiFont / CodeFont, "" - the built-in font).
    //
    // The atlas is built by SdAppWindow after a change of the font settings or of the UI scaling (not every frame).
    // A missing variant falls back to the Regular font of the same role (HasStyle() tells if the variant is real).
    //
    // Usage in UI code (fonts are taken every frame - ImFontPtr values are invalid after a rebuild of the atlas):
    //   using (UiFonts.Scope(UiFontRole.Code)) { ImGui.TextUnformatted(code); }
    //   UiFonts.Text("Note:", UiFontStyle.Bold); ImGui.SameLine(0.0f, 0.0f); ImGui.TextUnformatted(" plain text");
    public static unsafe class UiFonts
    {
        public const float          BuiltInFontSize     = 13.0f;
        public const string         BuiltInFontName     = "ImGui default (ProggyClean, ASCII only)";

        private static readonly string[] FontExtensions = { ".ttf", ".otf" };

        // style suffixes of file names (lower case, spaces and '_' removed)
        private static readonly Dictionary<string, UiFontStyle> StyleSuffixes = new Dictionary<string, UiFontStyle>()
        {
            { "regular"     , UiFontStyle.Regular       },
            { "bold"        , UiFontStyle.Bold          },
            { "italic"      , UiFontStyle.Italic        },
            { "oblique"     , UiFontStyle.Italic        },
            { "bolditalic"  , UiFontStyle.BoldItalic    },
            { "boldoblique" , UiFontStyle.BoldItalic    },
        };

        // other weights - such files are skipped (not separate families)
        private static readonly string[] OtherWeights =
        {
            "thin", "hairline", "extralight", "ultralight", "light", "book", "medium", "semibold", "demibold",
            "extrabold", "ultrabold", "black", "heavy", "extrablack", "ultrablack",
        };

        private static string                   fontsDirectory_     = "Fonts";
        private static ushort*                  glyphRanges_        = null;     // native memory, used by the atlas until it is rebuilt
        private static List<UiFontFamily>?      families_           = null;     // null - not scanned yet

        private static readonly int             RoleCount           = 2;
        private static readonly int             StyleCount          = 4;
        private static ImFontPtr[]              fonts_              = new ImFontPtr[RoleCount * StyleCount];
        private static bool[]                   realStyles_         = new bool[RoleCount * StyleCount];
        private static bool                     scaledInAtlas_      = false;

        // Called by SdAppWindow (constructor), before ImGui is initialized
        public static void Configure(SdAppSettings appSettings)
        {
            fontsDirectory_ = appSettings.FontsDirectory;
            families_       = null;

            ushort[] ranges = appSettings.FontGlyphRanges;
            if (glyphRanges_ != null)
                NativeMemory.Free(glyphRanges_);

            // pairs + terminating 0
            int count = ranges.Length - (ranges.Length % 2);
            glyphRanges_ = (ushort*)NativeMemory.AllocZeroed((nuint)((count + 1) * sizeof(ushort)));
            for(int i=0; i<count; i++)
                glyphRanges_[i] = ranges[i];
        }

        public static string FontsDirectory { get { return fontsDirectory_; } }

        //-------------------------------------------------------------------
        // Font families (files in the fonts directory)
        //-------------------------------------------------------------------

        // Families sorted by name; the directory is scanned on the first call and by Refresh()
        public static IReadOnlyList<UiFontFamily> GetFamilies()
        {
            if (families_ == null)
                families_ = ScanFamilies(fontsDirectory_);

            return families_;
        }

        // Scans the fonts directory again (e.g. when the settings dialog is opened)
        public static void Refresh()
        {
            families_ = null;
        }

        public static bool HasFamilies()
        {
            return GetFamilies().Count > 0;
        }

        // id: path of the Regular file relative to the fonts directory; null - not found (or the built-in font)
        public static UiFontFamily? FindFamily(string id)
        {
            if (String.IsNullOrEmpty(id))
                return null;

            string normalized = NormalizeId(id);
            foreach(UiFontFamily family in GetFamilies())
            {
                if (String.Equals(family.Id, normalized, StringComparison.OrdinalIgnoreCase))
                    return family;
            }
            return null;
        }

        private static string NormalizeId(string path)
        {
            return path.Replace('\\', '/');
        }

        private static List<UiFontFamily> ScanFamilies(string directory)
        {
            List<UiFontFamily> families = new List<UiFontFamily>();

            if (!Directory.Exists(directory))
                return families;

            // key: relative directory + family name
            Dictionary<string, UiFontFamily> byKey = new Dictionary<string, UiFontFamily>(StringComparer.OrdinalIgnoreCase);

            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(directory, "*.*", SearchOption.AllDirectories).ToList();
            }
            catch(Exception ex)
            {
                Console.WriteLine("WARNING: Can't read the fonts directory: {0}, {1}", directory, ex.Message);
                return families;
            }

            foreach(string filePath in files)
            {
                string extension = Path.GetExtension(filePath);
                if (!FontExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    continue;

                if (!ParseFileName(Path.GetFileNameWithoutExtension(filePath), out string familyName, out UiFontStyle style))
                    continue;

                string relativePath = NormalizeId(Path.GetRelativePath(directory, filePath));
                string relativeDir  = NormalizeId(Path.GetDirectoryName(relativePath) ?? "");
                string key          = relativeDir + "/" + familyName;

                if (!byKey.TryGetValue(key, out UiFontFamily? family))
                {
                    family = new UiFontFamily(familyName);
                    byKey.Add(key, family);
                }

                family.SetFile(style, filePath, relativePath);
            }

            foreach(UiFontFamily family in byKey.Values)
            {
                if (family.HasStyle(UiFontStyle.Regular))
                    families.Add(family);
            }

            families.Sort((a, b) => String.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return families;
        }

        // "Lato-BoldItalic" -> "Lato", BoldItalic; "Lato-Black" -> false (other weight); "MyFont" -> "MyFont", Regular
        private static bool ParseFileName(string name, out string familyName, out UiFontStyle style)
        {
            familyName  = name;
            style       = UiFontStyle.Regular;

            int dash = name.LastIndexOf('-');
            if (dash <= 0 || dash == name.Length - 1)
                return true;

            string suffix = name.Substring(dash + 1).Replace(" ", "").Replace("_", "").ToLowerInvariant();

            if (StyleSuffixes.TryGetValue(suffix, out style))
            {
                familyName = name.Substring(0, dash);
                return true;
            }

            string weight = suffix.EndsWith("italic") ? suffix.Substring(0, suffix.Length - "italic".Length) : suffix;
            if (OtherWeights.Contains(weight))
                return false;

            // not a style suffix - part of the family name
            style = UiFontStyle.Regular;
            return true;
        }

        //-------------------------------------------------------------------
        // Atlas
        //-------------------------------------------------------------------

        // True if the fonts are rasterized in the scaled size (io.FontGlobalScale = 1). False if only the built-in
        // font is used - it is scaled with io.FontGlobalScale, as before the TTF fonts support.
        public static bool IsScaledInAtlas { get { return scaledInAtlas_; } }

        // Key of the atlas state - the atlas is rebuilt only when it changes
        public static string GetBuildKey(string uiFontId, float uiFontSize, string codeFontId, float codeFontSize, float scaling)
        {
            if (String.IsNullOrEmpty(uiFontId) && String.IsNullOrEmpty(codeFontId))
                return "builtin";

            return String.Format("{0}|{1}|{2}|{3}|{4}", uiFontId, GetPixelSize(uiFontSize, scaling), codeFontId, GetPixelSize(codeFontSize, scaling), GetPixelSize(BuiltInFontSize, scaling));
        }

        private static float GetPixelSize(float size, float scaling)
        {
            return Math.Max(1.0f, MathF.Round(size * scaling));
        }

        // Rebuilds the font atlas of the current ImGui context (outside ImGui.NewFrame() / ImGui.Render()).
        // The renderer texture must be recreated afterwards (ImguiImplOpenGL3.InvalidateFontsTexture).
        // Missing font files: a warning in the log, the built-in font is used.
        public static void Build(string uiFontId, float uiFontSize, string codeFontId, float codeFontSize, float scaling)
        {
            UiFontFamily? uiFamily      = ResolveFamily(uiFontId);
            UiFontFamily? codeFamily    = ResolveFamily(codeFontId);

            ImFontAtlasPtr atlas = ImGui.GetIO().Fonts;
            atlas.Clear();

            Array.Clear(realStyles_);

            if (uiFamily == null && codeFamily == null)
            {
                // only the built-in font, scaled by io.FontGlobalScale
                scaledInAtlas_ = false;
                ImFontPtr builtIn = atlas.AddFontDefault();
                for(int i=0; i<fonts_.Length; i++)
                    fonts_[i] = builtIn;
                realStyles_[Index(UiFontRole.Ui  , UiFontStyle.Regular)] = true;
                realStyles_[Index(UiFontRole.Code, UiFontStyle.Regular)] = true;
                return;
            }

            scaledInAtlas_ = true;

            // the same file/size is loaded once
            Dictionary<string, ImFontPtr> loaded = new Dictionary<string, ImFontPtr>(StringComparer.OrdinalIgnoreCase);

            // the Ui Regular font is added first - the first font of the atlas is the default ImGui font
            AddRoleFonts(atlas, loaded, UiFontRole.Ui  , uiFamily  , GetPixelSize(uiFontSize  , scaling), scaling);
            AddRoleFonts(atlas, loaded, UiFontRole.Code, codeFamily, GetPixelSize(codeFontSize, scaling), scaling);
        }

        private static UiFontFamily? ResolveFamily(string fontId)
        {
            if (String.IsNullOrEmpty(fontId))
                return null;

            UiFontFamily? family = FindFamily(fontId);
            if (family == null)
                Console.WriteLine("WARNING: Font not found: {0} (fonts directory: {1}), the built-in font is used", fontId, fontsDirectory_);

            return family;
        }

        private static void AddRoleFonts(ImFontAtlasPtr atlas, Dictionary<string, ImFontPtr> loaded, UiFontRole role, UiFontFamily? family, float pixelSize, float scaling)
        {
            ImFontPtr regular = AddFont(atlas, loaded, family?.GetFilePath(UiFontStyle.Regular), pixelSize, scaling);
            fonts_[Index(role, UiFontStyle.Regular)]        = regular;
            realStyles_[Index(role, UiFontStyle.Regular)]   = true;

            for(int s=1; s<StyleCount; s++)
            {
                UiFontStyle style = (UiFontStyle)s;
                string? filePath = family?.GetFilePath(style);

                if (filePath != null)
                {
                    fonts_[Index(role, style)]      = AddFont(atlas, loaded, filePath, pixelSize, scaling);
                    realStyles_[Index(role, style)] = true;
                }
                else
                {
                    fonts_[Index(role, style)]      = regular;
                }
            }
        }

        // filePath null - the built-in font
        private static ImFontPtr AddFont(ImFontAtlasPtr atlas, Dictionary<string, ImFontPtr> loaded, string? filePath, float pixelSize, float scaling)
        {
            if (filePath != null && !File.Exists(filePath))
            {
                Console.WriteLine("WARNING: Font file not found: {0}, the built-in font is used", filePath);
                filePath = null;
            }

            float size = (filePath != null) ? pixelSize : GetPixelSize(BuiltInFontSize, scaling);
            string key = String.Format("{0}|{1}", filePath ?? "<builtin>", size);

            if (loaded.TryGetValue(key, out ImFontPtr existing))
                return existing;

            ImFontConfig* cfg = ImGuiNative.ImFontConfig_ImFontConfig();
            ImFontPtr font;
            try
            {
                cfg->SizePixels     = size;
                cfg->OversampleH    = 2;
                cfg->OversampleV    = 1;

                if (filePath != null)
                    font = atlas.AddFontFromFileTTF(filePath, size, new ImFontConfigPtr(cfg), (IntPtr)glyphRanges_);
                else
                    font = atlas.AddFontDefault(new ImFontConfigPtr(cfg));
            }
            finally
            {
                // the atlas keeps its own copy of the config
                ImGuiNative.ImFontConfig_destroy(cfg);
            }

            if (font.NativePtr == null)
            {
                Console.WriteLine("WARNING: Loading the font failed: {0}, the built-in font is used", filePath);
                return AddFont(atlas, loaded, null, pixelSize, scaling);
            }

            loaded.Add(key, font);
            return font;
        }

        private static int Index(UiFontRole role, UiFontStyle style)
        {
            return (int)role * StyleCount + (int)style;
        }

        //-------------------------------------------------------------------
        // Usage in UI code
        //-------------------------------------------------------------------

        // The font of the role and style (the Regular font of the role if the style is not available).
        // Do not keep the result between frames - it is invalid after a rebuild of the atlas.
        public static ImFontPtr Get(UiFontRole role, UiFontStyle style = UiFontStyle.Regular)
        {
            ImFontPtr font = fonts_[Index(role, style)];
            if (font.NativePtr == null)
                return ImGui.GetFont();     // atlas not built by UiFonts (should not happen after SdAppWindow.OnLoad)

            return font;
        }

        // True if the family of the role has a real file for the style (not a fallback to Regular)
        public static bool HasStyle(UiFontRole role, UiFontStyle style)
        {
            return realStyles_[Index(role, style)];
        }

        public static void Push(UiFontRole role, UiFontStyle style = UiFontStyle.Regular)
        {
            ImGui.PushFont(Get(role, style));
        }

        public static void Pop()
        {
            ImGui.PopFont();
        }

        // using (UiFonts.Scope(UiFontRole.Code)) { ... }
        public static UiFontScope Scope(UiFontRole role, UiFontStyle style = UiFontStyle.Regular)
        {
            Push(role, style);
            return new UiFontScope();
        }

        // Text in the given style (and role)
        public static void Text(string text, UiFontStyle style, UiFontRole role = UiFontRole.Ui)
        {
            Push(role, style);
            ImGui.TextUnformatted(text);
            Pop();
        }
    }

    // Pops the font pushed by UiFonts.Scope()
    public readonly struct UiFontScope : IDisposable
    {
        public void Dispose()
        {
            ImGui.PopFont();
        }
    }

    // Font family found in the fonts directory
    public class UiFontFamily
    {
        private readonly string?[]  filePaths_      = new string?[4];   // paths usable for loading (fonts directory + relative path)

        public UiFontFamily(string name)
        {
            Name = name;
        }

        public string   Name    { get; }                // e.g. "Lato"
        public string   Id      { get; private set; } = "";    // relative path of the Regular file (user settings value)

        internal void SetFile(UiFontStyle style, string filePath, string relativePath)
        {
            filePaths_[(int)style] = filePath;
            if (style == UiFontStyle.Regular)
                Id = relativePath;
        }

        public bool HasStyle(UiFontStyle style)
        {
            return filePaths_[(int)style] != null;
        }

        public string? GetFilePath(UiFontStyle style)
        {
            return filePaths_[(int)style];
        }

        // e.g. "Lato (Regular, Bold, Italic, BoldItalic)"
        public string GetDisplayName()
        {
            List<string> styles = new List<string>();
            foreach(UiFontStyle style in Enum.GetValues<UiFontStyle>())
            {
                if (HasStyle(style))
                    styles.Add(style.ToString());
            }
            return String.Format("{0} ({1})", Name, String.Join(", ", styles));
        }
    }
}
