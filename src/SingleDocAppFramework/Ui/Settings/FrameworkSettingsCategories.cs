//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Settings;
using SingleDocAppFramework.Ui.Fonts;
using SingleDocAppFramework.Ui.Properties;
using SingleDocAppFramework.Ui.Styles;

namespace SingleDocAppFramework.Ui.Settings
{
    // Settings categories of UserSettingsBase. The Build*/Section helpers can be used by applications
    // which extend these categories or place the properties in their own ones.
    public static class FrameworkSettingsCategories
    {
        // category ids (SettingsRegistry.ExtendCategory)
        public const string UserInterface   = "UserInterface";
        public const string Performance     = "Performance";

        // Order of the framework categories; application categories usually go in between
        public const int    UserInterfaceOrder  = 100;
        public const int    PerformanceOrder    = 900;

        public static void Register(SettingsRegistry registry)
        {
            registry.AddCategory(UserInterface, "User interface", UserInterfaceOrder)
                .AddSection(null, delegate(SettingsBuildContext ctx)
                {
                    BuildUiStyle(ctx);
                    BuildUiTextScaleFactor(ctx);
                    BuildFullscreenMonitor(ctx);
                },
                delegate(UserSettingsBase settings, UserSettingsBase defaults)
                {
                    settings.UiStyle                    = defaults.UiStyle;
                    settings.UiTextScaleFactor          = defaults.UiTextScaleFactor;
                    settings.FullscreenOnPrimaryMonitor = defaults.FullscreenOnPrimaryMonitor;
                })
                .AddSection(CreateFontsSection("Fonts"));

            registry.AddCategory(Performance, "Performance", PerformanceOrder)
                .AddSection("FPS limits", BuildFpsLimit, delegate(UserSettingsBase settings, UserSettingsBase defaults)
                {
                    settings.UseUpdateFrequencyLimit    = defaults.UseUpdateFrequencyLimit;
                    settings.UpdateFrequencyLimit       = defaults.UpdateFrequencyLimit;
                });
        }

        // UI style (colors and sizes)
        public static void BuildUiStyle(SettingsBuildContext ctx)
        {
            string[] styleNames = UiStyles.GetStyleNames();
            int styleIndex = Array.FindIndex(styleNames, n => String.Equals(n, ctx.Settings.UiStyle, StringComparison.OrdinalIgnoreCase));

            UiComboBox.Build(ref ctx.Index, "User interface style", styleNames, ref styleIndex);

            if (styleIndex >= 0 && styleIndex < styleNames.Length)
                ctx.Settings.UiStyle = styleNames[styleIndex];
        }

        public static void BuildUiTextScaleFactor(SettingsBuildContext ctx)
        {
            UiFloat.Build   (ref ctx.Index, "User interface text scale factor"  , ref ctx.Settings.UiTextScaleFactor, 0.01f, 0.05f, 2.0f);
        }

        // Fonts of the user interface and of code (UiFonts). Hidden when there are no font files to choose from
        // (unless a setting refers to a missing file - then it can be switched back to the built-in font).
        public static SettingsSection CreateFontsSection(string? title)
        {
            SettingsSection section = new SettingsSection(title, delegate(SettingsBuildContext ctx)
            {
                BuildFont       (ref ctx.Index, "User interface font"       , ref ctx.Settings.UiFont);
                BuildFontSize   (ref ctx.Index, "User interface font size"  , ref ctx.Settings.UiFontSize, ctx.Settings.UiFont);
                BuildFont       (ref ctx.Index, "Code font"                 , ref ctx.Settings.CodeFont);
                BuildFontSize   (ref ctx.Index, "Code font size"            , ref ctx.Settings.CodeFontSize, ctx.Settings.CodeFont);
            },
            delegate(UserSettingsBase settings, UserSettingsBase defaults)
            {
                settings.UiFont         = defaults.UiFont;
                settings.UiFontSize     = defaults.UiFontSize;
                settings.CodeFont       = defaults.CodeFont;
                settings.CodeFontSize   = defaults.CodeFontSize;
            });

            section.IsVisible = delegate(UserSettingsBase settings)
            {
                return UiFonts.HasFamilies() || !String.IsNullOrEmpty(settings.UiFont) || !String.IsNullOrEmpty(settings.CodeFont);
            };

            return section;
        }

        // Combo: the built-in font + font families from the fonts directory (+ the current value if its file is missing)
        public static void BuildFont(ref int id, string label, ref string fontId)
        {
            IReadOnlyList<UiFontFamily> families = UiFonts.GetFamilies();

            List<string> names  = new List<string>() { UiFonts.BuiltInFontName };
            List<string> ids    = new List<string>() { "" };
            foreach(UiFontFamily family in families)
            {
                names.Add(family.GetDisplayName());
                ids.Add(family.Id);
            }

            string current = fontId;
            int index = ids.FindIndex(i => String.Equals(i, current, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
            {
                names.Insert(1, String.Format("{0} (missing)", fontId));
                ids.Insert(1, fontId);
                index = 1;
            }

            UiComboBox.Build(ref id, label, names.ToArray(), ref index);

            if (index >= 0 && index < ids.Count)
                fontId = ids[index];
        }

        // Size in pixels at scale 1.0; disabled for the built-in font (fixed size)
        public static void BuildFontSize(ref int id, string label, ref float size, string fontId)
        {
            bool builtIn = String.IsNullOrEmpty(fontId);

            ImGui.BeginDisabled(builtIn);
            UiFloat.Build(ref id, label, ref size, 0.05f, UserSettingsBase.MinFontSize, UserSettingsBase.MaxFontSize);
            ImGui.EndDisabled();
        }

        // Monitor used by the borderless fullscreen mode (SdAppWindow.SetBorderlessFullscreen)
        public static void BuildFullscreenMonitor(SettingsBuildContext ctx)
        {
            string[] monitorNames = { "Monitor with the window", "Primary monitor" };
            int monitorIndex = ctx.Settings.FullscreenOnPrimaryMonitor ? 1 : 0;

            UiComboBox.Build(ref ctx.Index, "Fullscreen monitor", monitorNames, ref monitorIndex);

            ctx.Settings.FullscreenOnPrimaryMonitor = (monitorIndex == 1);
        }

        // Opacity of window backgrounds - used only if the application overrides SdAppWindow.GetWindowOpacity
        // (e.g. while something is drawn under the UI), so it is not in the framework category by default
        public static SettingsSection CreateWindowOpacitySection(string label)
        {
            return new SettingsSection(null, delegate(SettingsBuildContext ctx)
            {
                UiFloat.Build   (ref ctx.Index, label, ref ctx.Settings.WindowOpacity, 0.01f, 0.0f, 1.0f);
            },
            delegate(UserSettingsBase settings, UserSettingsBase defaults)
            {
                settings.WindowOpacity = defaults.WindowOpacity;
            });
        }

        public static void BuildFpsLimit(SettingsBuildContext ctx)
        {
            //UiBool.Build    (ref ctx.Index, "Use render frequency limit"      , ref ctx.Settings.UseRenderFrequencyLimit);
            //UiInt.Build     (ref ctx.Index, "Render frequency limit"          , ref ctx.Settings.RenderFrequencyLimit, 0.01f, 1, 500);
            UiBool.Build    (ref ctx.Index, "Use render/update frequency limit" , ref ctx.Settings.UseUpdateFrequencyLimit);
            UiInt.Build     (ref ctx.Index, "Render/Update frequency limit"     , ref ctx.Settings.UpdateFrequencyLimit, 0.01f, 1, 500);
        }
    }
}
