//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model;
using SingleDocAppCore.Settings;
using SingleDocAppFramework.Ui.Properties;
using SingleDocAppFramework.Ui.Settings;

namespace SdfGlueUi.Ui.Settings
{
    // SdfGlue categories of the settings dialog (UserSettingsSdfGlue)
    public static class SettingsCategoriesSdfGlue
    {
        public const string Editor          = "Editor";
        public const string CodeGeneration  = "CodeGeneration";
        public const string FileSystem      = "FileSystem";
        public const string ExportAnimation = "ExportAnimation";

        public static void Register(SettingsRegistry registry)
        {
            registry.ExtendCategory(FrameworkSettingsCategories.UserInterface,
                FrameworkSettingsCategories.CreateWindowOpacitySection("Window opacity (background preview)"));

            registry.AddCategory(Editor, "Editor", 200)
                .AddSection("Camera and mouse", BuildCameraAndMouse, ResetCameraAndMouse);

            registry.AddCategory(CodeGeneration, "Code generation", 300)
                .AddSection(null, delegate(SettingsBuildContext ctx)
                {
                    UserSettingsSdfGlue settings = ctx.GetSettings<UserSettingsSdfGlue>();
                    UiBool.Build(ref ctx.Index, "Use names as identifiers", ref settings.UseNamesAsIds);
                },
                delegate(UserSettingsBase settings, UserSettingsBase defaults)
                {
                    ((UserSettingsSdfGlue)settings).UseNamesAsIds = ((UserSettingsSdfGlue)defaults).UseNamesAsIds;
                });

            registry.AddCategory(FileSystem, "File system", 400)
                .AddSection(null, delegate(SettingsBuildContext ctx)
                {
                    UserSettingsSdfGlue settings = ctx.GetSettings<UserSettingsSdfGlue>();
                    UiBool.Build(ref ctx.Index, "Monitor file system changes", ref settings.MonitorFileSystemChanges);
                },
                delegate(UserSettingsBase settings, UserSettingsBase defaults)
                {
                    ((UserSettingsSdfGlue)settings).MonitorFileSystemChanges = ((UserSettingsSdfGlue)defaults).MonitorFileSystemChanges;
                });

            registry.AddCategory(ExportAnimation, "Export animation", 500)
                .AddSection(null, BuildExportAnimation, ResetExportAnimation);
        }

        private static void BuildCameraAndMouse(SettingsBuildContext ctx)
        {
            UserSettingsSdfGlue settings = ctx.GetSettings<UserSettingsSdfGlue>();

            UiBool.Build    (ref ctx.Index, "Invert mouse X"                    , ref settings.MouseCameraRotationInvPitch);
            UiBool.Build    (ref ctx.Index, "Invert mouse Y"                    , ref settings.MouseCameraRotationInvYaw);
            UiFloat.Build   (ref ctx.Index, "Camera pan speed"                  , ref settings.MouseCameraPanSpeed            , 0.01f, 0.05f, 2.0f);
            UiFloat.Build   (ref ctx.Index, "Camera rotation speed X (yaw)"     , ref settings.MouseCameraRotationSpeedYaw    , 0.01f, 0.05f, 2.0f);
            UiFloat.Build   (ref ctx.Index, "Camera rotation speed Y (pitch)"   , ref settings.MouseCameraRotationSpeedPitch  , 0.01f, 0.05f, 2.0f);
            UiBool.Build    (ref ctx.Index, "Invert mouse wheel "               , ref settings.MouseWheelInvert);
            UiFloat.Build   (ref ctx.Index, "Mouse wheel speed"                 , ref settings.MouseWheelSpeed, 0.01f, 0.05f, 2.0f);
            UiBool.Build    (ref ctx.Index, "Use shift key to zoom"             , ref settings.UseShiftKeyToZoom);
            UiBool.Build    (ref ctx.Index, "Use Alt+RMB for camera rotation"   , ref settings.UseAltRmbForCameraRotation);
        }

        private static void ResetCameraAndMouse(UserSettingsBase settingsBase, UserSettingsBase defaultsBase)
        {
            UserSettingsSdfGlue settings = (UserSettingsSdfGlue)settingsBase;
            UserSettingsSdfGlue defaults = (UserSettingsSdfGlue)defaultsBase;

            settings.MouseCameraRotationInvPitch    = defaults.MouseCameraRotationInvPitch;
            settings.MouseCameraRotationInvYaw      = defaults.MouseCameraRotationInvYaw;
            settings.MouseCameraPanSpeed            = defaults.MouseCameraPanSpeed;
            settings.MouseCameraRotationSpeedYaw    = defaults.MouseCameraRotationSpeedYaw;
            settings.MouseCameraRotationSpeedPitch  = defaults.MouseCameraRotationSpeedPitch;
            settings.MouseWheelInvert               = defaults.MouseWheelInvert;
            settings.MouseWheelSpeed                = defaults.MouseWheelSpeed;
            settings.UseShiftKeyToZoom              = defaults.UseShiftKeyToZoom;
            settings.UseAltRmbForCameraRotation     = defaults.UseAltRmbForCameraRotation;
        }

        private static void BuildExportAnimation(SettingsBuildContext ctx)
        {
            ExportAnimationSettings exportAnim = ctx.GetSettings<UserSettingsSdfGlue>().ExportAnimSettings;

            UiInt.Build     (ref ctx.Index, "FPS"                               , ref exportAnim.Fps                       , 0.1f, 1, 240);
            UiInt.Build     (ref ctx.Index, "Frames to prerender"               , ref exportAnim.NumOfFramesToPrerender    , 0.1f, 0, 1000);
            UiInt.Build     (ref ctx.Index, "Frames to export"                  , ref exportAnim.NumOfFramesToExport       , 0.1f, 0, 10000000);
        }

        // fields are copied - the ExportAnimationSettings instance stays the same
        private static void ResetExportAnimation(UserSettingsBase settingsBase, UserSettingsBase defaultsBase)
        {
            ExportAnimationSettings exportAnim  = ((UserSettingsSdfGlue)settingsBase).ExportAnimSettings;
            ExportAnimationSettings defaults    = ((UserSettingsSdfGlue)defaultsBase).ExportAnimSettings;

            exportAnim.Fps                      = defaults.Fps;
            exportAnim.NumOfFramesToPrerender   = defaults.NumOfFramesToPrerender;
            exportAnim.NumOfFramesToExport      = defaults.NumOfFramesToExport;
        }
    }
}
