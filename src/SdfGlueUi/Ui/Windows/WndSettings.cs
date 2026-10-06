//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SingleDocAppFramework.Ui.Properties;
using SingleDocAppFramework.Ui.Windows;

namespace SdfGlueUi.Ui.Windows
{
    // SdfGlue user settings (save / restore defaults and common sections come from WndUserSettingsBase)
    public class WndSettings : WndUserSettingsBase
    {
        private UserSettingsSdfGlue Settings { get { return (UserSettingsSdfGlue)UserSettings; } }

        protected override void BuildSettings(ref int index, float firstColumnWidth)
        {
            //ImGui.Text("Shader generation");

            if (ImGui.CollapsingHeader("Code generation"))
            {
                BeginPropertyGrid(firstColumnWidth);

                UiBool.Build(ref index, "Use names as identifiers", ref Settings.UseNamesAsIds);
                //HelpMarker("When enabled, object names are used in the shader instead of numeric ids.\nNames must be unique.");

                EndPropertyGrid();
            }

            //ImGui.Text("Editor");
            if (ImGui.CollapsingHeader("Editor"))
            {
                BeginPropertyGrid(firstColumnWidth);

                UiBool.Build    (ref index, "Invert mouse X"                    , ref Settings.MouseCameraRotationInvPitch);
                UiBool.Build    (ref index, "Invert mouse Y"                    , ref Settings.MouseCameraRotationInvYaw);
                UiFloat.Build   (ref index, "Camera pan speed"                  , ref Settings.MouseCameraPanSpeed            , 0.01f, 0.05f, 2.0f);
                UiFloat.Build   (ref index, "Camera rotation speed X (yaw)"     , ref Settings.MouseCameraRotationSpeedYaw    , 0.01f, 0.05f, 2.0f);
                UiFloat.Build   (ref index, "Camera rotation speed Y (pitch)"   , ref Settings.MouseCameraRotationSpeedPitch  , 0.01f, 0.05f, 2.0f);
                UiBool.Build    (ref index, "Invert mouse wheel "               , ref Settings.MouseWheelInvert);
                UiFloat.Build   (ref index, "Mouse wheel speed"                 , ref Settings.MouseWheelSpeed, 0.01f, 0.05f, 2.0f);
                UiBool.Build    (ref index, "Use shift key to zoom"             , ref Settings.UseShiftKeyToZoom);
                UiBool.Build    (ref index, "Use Alt+RMB for camera rotation"   , ref Settings.UseAltRmbForCameraRotation);
                BuildUiStyle(ref index);
                BuildUiTextScaleFactor(ref index);

                EndPropertyGrid();
            }

            if (ImGui.CollapsingHeader("File system"))
            {
                BeginPropertyGrid(firstColumnWidth);
                UiBool.Build    (ref index, "Monitor file system changes" , ref Settings.MonitorFileSystemChanges);
                EndPropertyGrid();
            }

            BuildFpsLimitSettings(ref index, firstColumnWidth);

            //if (ImGui.CollapsingHeader("Export image settings"))
            //{
            //    BeginPropertyGrid(firstColumnWidth);
            //
            //    UiComboBox.Build(index++, "Image resolution", Settings.ResolutionsAsStrings, ref GetModel().ExportImageResolutionIndex);
            //
            //    EndPropertyGrid();
            //}

            if (ImGui.CollapsingHeader("Export animation settings"))
            {
                BeginPropertyGrid(firstColumnWidth);


                UiInt.Build    (ref index, "FPS"                     , ref Settings.ExportAnimSettings.Fps                       , 0.1f, 1, 240);
                UiInt.Build    (ref index, "Frames to prerender"     , ref Settings.ExportAnimSettings.NumOfFramesToPrerender    , 0.1f, 0, 1000);
                UiInt.Build    (ref index, "Frames to export"        , ref Settings.ExportAnimSettings.NumOfFramesToExport       , 0.1f, 0, 10000000);

                EndPropertyGrid();
            }
        }
    }
}
