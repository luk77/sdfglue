//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Numerics;

namespace SingleDocAppFramework.Ui.Styles
{
    // Registry of named ImGui styles (colors + sizes) selectable in the user settings (UserSettingsBase.UiStyle).
    // Built-in: the three Dear ImGui styles (Dark, Light, Classic).
    // Applications can add their own styles with Register() before the window is loaded (e.g. in Program.Main
    // or in the window constructor). A style function gets a style already reset to the Dear ImGui defaults
    // (sizes) with the Dark colors, so it only needs to set what differs.
    public static class UiStyles
    {
        public const string DefaultStyleName = "Dark";

        private class StyleEntry
        {
            public string                   Name;
            public Action<ImGuiStylePtr>    Apply;

            public StyleEntry(string name, Action<ImGuiStylePtr> apply)
            {
                Name    = name;
                Apply   = apply;
            }
        }

        private static readonly List<StyleEntry>    styles_         = new List<StyleEntry>();
        private static string[]?                    styleNames_     = null;

        static UiStyles()
        {
            Register("Dark"     , delegate(ImGuiStylePtr style) { ImGui.StyleColorsDark(style);     });
            Register("Light"    , delegate(ImGuiStylePtr style) { ImGui.StyleColorsLight(style);    });
            Register("Classic"  , delegate(ImGuiStylePtr style) { ImGui.StyleColorsClassic(style);  });
        }

        // Adds a style, or replaces a registered style with the same name (names are case-insensitive)
        public static void Register(string name, Action<ImGuiStylePtr> apply)
        {
            int index = FindStyle(name);
            if (index >= 0)
                styles_[index] = new StyleEntry(name, apply);
            else
                styles_.Add(new StyleEntry(name, apply));

            styleNames_ = null;
        }

        // Names in registration order (for menus and combo boxes)
        public static string[] GetStyleNames()
        {
            if (styleNames_ == null)
                styleNames_ = styles_.Select(s => s.Name).ToArray();

            return styleNames_;
        }

        public static bool IsRegistered(string name)
        {
            return FindStyle(name) >= 0;
        }

        // Applies the style to the current ImGui context. Unknown name - the default style is applied
        // and false is returned.
        public static bool Apply(string name)
        {
            int index = FindStyle(name);
            bool found = index >= 0;
            if (!found)
                index = FindStyle(DefaultStyleName);

            ImGuiStylePtr style = ImGui.GetStyle();
            ResetToDefaults(style);
            styles_[index].Apply(style);

            return found;
        }

        private static int FindStyle(string name)
        {
            return styles_.FindIndex(s => String.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        // Sizes, roundings etc. back to the Dear ImGui defaults (a previous style may have changed them),
        // colors - Dark
        private static unsafe void ResetToDefaults(ImGuiStylePtr style)
        {
            ImGuiStyle* defaultStyle = ImGuiNative.ImGuiStyle_ImGuiStyle();
            *style.NativePtr = *defaultStyle;
            ImGuiNative.ImGuiStyle_destroy(defaultStyle);

            ImGui.StyleColorsDark(style);
        }

        // Helpers for style definitions

        public static void SetColor(ImGuiStylePtr style, ImGuiCol col, float r, float g, float b, float a)
        {
            style.Colors[(int)col] = new Vector4(r, g, b, a);
        }

        public static Vector4 GetColor(ImGuiStylePtr style, ImGuiCol col)
        {
            return style.Colors[(int)col];
        }

        // Older styles (written for Dear ImGui versions without tabs and docking) do not set the colors of tabs,
        // docking and tables - they are derived from the header/title colors like in ImGui.StyleColorsDark()
        public static void DeriveTabAndDockingColors(ImGuiStylePtr style)
        {
            Vector4 header          = GetColor(style, ImGuiCol.Header);
            Vector4 headerHovered   = GetColor(style, ImGuiCol.HeaderHovered);
            Vector4 headerActive    = GetColor(style, ImGuiCol.HeaderActive);
            Vector4 titleBg         = GetColor(style, ImGuiCol.TitleBg);
            Vector4 titleBgActive   = GetColor(style, ImGuiCol.TitleBgActive);

            Vector4 tab             = Vector4.Lerp(header, titleBgActive, 0.80f);
            Vector4 tabSelected     = Vector4.Lerp(headerActive, titleBgActive, 0.60f);

            style.Colors[(int)ImGuiCol.Tab]                     = tab;
            style.Colors[(int)ImGuiCol.TabHovered]              = headerHovered;
            style.Colors[(int)ImGuiCol.TabSelected]             = tabSelected;
            style.Colors[(int)ImGuiCol.TabSelectedOverline]     = headerActive;
            style.Colors[(int)ImGuiCol.TabDimmed]               = Vector4.Lerp(tab, titleBg, 0.80f);
            style.Colors[(int)ImGuiCol.TabDimmedSelected]       = Vector4.Lerp(tabSelected, titleBg, 0.40f);
            style.Colors[(int)ImGuiCol.DockingPreview]          = headerActive * new Vector4(1.0f, 1.0f, 1.0f, 0.7f);
            style.Colors[(int)ImGuiCol.TableHeaderBg]           = titleBg;
            style.Colors[(int)ImGuiCol.TextLink]                = headerActive;
        }
    }
}
