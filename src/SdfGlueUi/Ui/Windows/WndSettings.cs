//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueUi.Ui.Properties;

namespace SdfGlueUi.Ui.Windows
{
    public class WndSettings : UiWindowBase
    {
        public override string Title => "Settings";

        public override void Build()
        {
            int materialsPosY = uiMgr_.BasePosY + uiMgr_.MaterialsHeight + uiMgr_.DistanceY;
            BuildWindow(uiMgr_.RightColPosX, materialsPosY, uiMgr_.RightColWidth, uiMgr_.BaseHeight - materialsPosY + 3 * uiMgr_.DistanceY, delegate ()
            {
                int firstColumnWidth = (int)GetDefaultFirstColumnWidth();

                //ImGui.Text("Shader generation");

                int index = 1;

                if (ImGui.CollapsingHeader("Code generation"))
                {
                    BeginPropertyGrid(firstColumnWidth);

                    UiBool.Build(ref index, "Use names as identifiers", ref GetModel().Config.UseNamesAsIds);
                    //HelpMarker("To ustawienie powoduje, że w shaderze zostaną użyte nazwy obiektów zamiast identyfikatorów liczbowych.\nNazwy muszą być unikalne.");

                    EndPropertyGrid();
                }

                //ImGui.Text("Editor");
                if (ImGui.CollapsingHeader("Editor"))
                {
                    BeginPropertyGrid(firstColumnWidth);

                    UiBool.Build    (ref index, "Invert mouse X"                    , ref GetModel().Config.MouseCameraRotationInvPitch);
                    UiBool.Build    (ref index, "Invert mouse Y"                    , ref GetModel().Config.MouseCameraRotationInvYaw);
                    UiFloat.Build   (ref index, "Camera pan speed"                  , ref GetModel().Config.MouseCameraPanSpeed            , 0.01f, 0.05f, 2.0f);
                    UiFloat.Build   (ref index, "Camera rotation speed X (yaw)"     , ref GetModel().Config.MouseCameraRotationSpeedYaw    , 0.01f, 0.05f, 2.0f);
                    UiFloat.Build   (ref index, "Camera rotation speed Y (pitch)"   , ref GetModel().Config.MouseCameraRotationSpeedPitch  , 0.01f, 0.05f, 2.0f);
                    UiBool.Build    (ref index, "Invert mouse wheel "               , ref GetModel().Config.MouseWheelInvert);
                    UiFloat.Build   (ref index, "Mouse wheel speed"                 , ref GetModel().Config.MouseWheelSpeed, 0.01f, 0.05f, 2.0f);
                    UiBool.Build    (ref index, "Use shift key to zoom"             , ref GetModel().Config.UseShiftKeyToZoom);
                    UiFloat.Build   (ref index, "User interface text scale factor"  , ref GetModel().Config.UiTextScaleFactor, 0.01f, 0.05f, 2.0f);

                    EndPropertyGrid();
                }

                if (ImGui.CollapsingHeader("File system"))
                {
                    BeginPropertyGrid(firstColumnWidth);
                    UiBool.Build    (ref index, "Monitor file system changes" , ref GetModel().Config.MonitorFileSystemChanges);
                    EndPropertyGrid();
                }

                if (ImGui.CollapsingHeader("FPS Limits"))
                {
                    BeginPropertyGrid(firstColumnWidth);

                    //UiBool.Build    (ref index, "Use render frequency limit"      , ref GetModel().Config.UseRenderFrequencyLimit);
                    //UiInt.Build     (ref index, "Render frequency limit"          , ref GetModel().Config.RenderFrequencyLimit, 0.01f, 1, 500);
                    //UiBool.Build    (ref index, "Use update frequency limit"      , ref GetModel().Config.UseUpdateFrequencyLimit);
                    //UiInt.Build     (ref index, "Update frequency limit"          , ref GetModel().Config.UpdateFrequencyLimit, 0.01f, 1, 500);
                    UiBool.Build    (ref index, "Use render/update frequency limit"      , ref GetModel().Config.UseUpdateFrequencyLimit);
                    UiInt.Build     (ref index, "Render/Update frequency limit"          , ref GetModel().Config.UpdateFrequencyLimit, 0.01f, 1, 500);

                    EndPropertyGrid();
                }

                //if (ImGui.CollapsingHeader("Export image settings"))
                //{
                //    BeginPropertyGrid(firstColumnWidth);
                //
                //    UiComboBox.Build(index++, "Image resolution", GetModel().Config.ResolutionsAsStrings, ref GetModel().ExportImageResolutionIndex);
                //
                //    EndPropertyGrid();
                //}

                if (ImGui.CollapsingHeader("Export animation settings"))
                {
                    BeginPropertyGrid(firstColumnWidth);


                    UiInt.Build    (ref index, "FPS"                     , ref GetModel().Config.ExportAnimSettings.Fps                       , 0.1f, 1, 240);
                    UiInt.Build    (ref index, "Frames to prerender"     , ref GetModel().Config.ExportAnimSettings.NumOfFramesToPrerender    , 0.1f, 0, 1000);
                    UiInt.Build    (ref index, "Frames to export"        , ref GetModel().Config.ExportAnimSettings.NumOfFramesToExport       , 0.1f, 0, 10000000);

                    EndPropertyGrid();
                }
            });
        }
    }
}
