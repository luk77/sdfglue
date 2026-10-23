//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Settings;
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
                });

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
