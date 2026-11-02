//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppFramework.Ui.Styles;
using System.Numerics;

namespace SingleDocAppFramework.Ui.CodeView
{
    // Colors of the code token kinds. Two palettes: for dark and for light UI styles (UiStyles.IsCurrentStyleDark),
    // each = defaults of the kinds (CodeTokenKinds.Register) + overrides from the user settings
    // (UserSettingsBase.SyntaxColorsDark/Light, set by SdAppWindow.ApplyUserSettings via SetOverrides).
    public static class CodeViewPalette
    {
        private static Dictionary<string, Vector3>  overridesDark_      = new Dictionary<string, Vector3>();
        private static Dictionary<string, Vector3>  overridesLight_     = new Dictionary<string, Vector3>();

        // packed colors (ImGui U32) per kind index; rebuilt after SetOverrides or when new kinds were registered
        private static uint[]                       dark_               = Array.Empty<uint>();
        private static uint[]                       light_              = Array.Empty<uint>();
        private static bool[]                       styleTextDark_      = Array.Empty<bool>();     // no override, color of ImGuiCol.Text
        private static bool[]                       styleTextLight_     = Array.Empty<bool>();
        private static int                          builtKindCount_     = -1;

        // Called after a change of the user settings (copies the dictionaries)
        public static void SetOverrides(IReadOnlyDictionary<string, Vector3> dark, IReadOnlyDictionary<string, Vector3> light)
        {
            overridesDark_  = new Dictionary<string, Vector3>(dark);
            overridesLight_ = new Dictionary<string, Vector3>(light);
            builtKindCount_ = -1;
        }

        public static bool IsDark { get { return UiStyles.IsCurrentStyleDark; } }

        public static Vector3 GetDefaultColor(CodeTokenKind kind, bool dark)
        {
            CodeTokenKindInfo info = kind.Info;
            return dark ? info.DefaultDarkColor : info.DefaultLightColor;
        }

        // Color of the palette (override or default)
        public static Vector3 GetColor(CodeTokenKind kind, bool dark)
        {
            Vector3 color;
            if ((dark ? overridesDark_ : overridesLight_).TryGetValue(kind.Info.Id, out color))
                return color;

            return GetDefaultColor(kind, dark);
        }

        // Color of the current palette for the draw list (inside an ImGui frame)
        public static uint GetColorU32(CodeTokenKind kind)
        {
            if (builtKindCount_ != CodeTokenKinds.Count)
                Rebuild();

            bool dark = IsDark;
            if ((dark ? styleTextDark_ : styleTextLight_)[kind.Index])
                return ImGui.GetColorU32(ImGuiCol.Text);

            return dark ? dark_[kind.Index] : light_[kind.Index];
        }

        private static void Rebuild()
        {
            IReadOnlyList<CodeTokenKindInfo> kinds = CodeTokenKinds.GetAll();
            dark_       = new uint[kinds.Count];
            light_      = new uint[kinds.Count];
            styleTextDark_  = new bool[kinds.Count];
            styleTextLight_ = new bool[kinds.Count];

            foreach(CodeTokenKindInfo info in kinds)
            {
                int i = info.Kind.Index;
                dark_[i]        = Pack(GetColor(info.Kind, true));
                light_[i]       = Pack(GetColor(info.Kind, false));
                // the default text keeps the style color unless the user overrides it
                styleTextDark_[i]   = info.UsesStyleTextColor && !overridesDark_.ContainsKey(info.Id);
                styleTextLight_[i]  = info.UsesStyleTextColor && !overridesLight_.ContainsKey(info.Id);
            }

            builtKindCount_ = kinds.Count;
        }

        // ImGui packed color (IM_COL32: A B G R from the high byte)
        private static uint Pack(Vector3 color)
        {
            uint r = (uint)Math.Clamp((int)(color.X * 255.0f + 0.5f), 0, 255);
            uint g = (uint)Math.Clamp((int)(color.Y * 255.0f + 0.5f), 0, 255);
            uint b = (uint)Math.Clamp((int)(color.Z * 255.0f + 0.5f), 0, 255);
            return 0xFF000000u | (b << 16) | (g << 8) | r;
        }
    }
}
