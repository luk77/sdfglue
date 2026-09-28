//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Settings;
using SingleDocAppFramework.Ui.Properties;

namespace SingleDocAppFramework.Ui.Windows
{
    // Editor of the user settings (UserSettings.xml) with "Save settings" / "Restore defaults".
    // Applications with their own settings derive from this window and override BuildSettings()
    // (the protected Build*Settings helpers draw the sections of UserSettingsBase).
    public class WndUserSettingsBase : UiWindowBase
    {
        // Title is a key in layout files - do not change it
        public override string Title => "Settings";

        protected UserSettingsBase UserSettings { get { return Executor.GetUserSettings(); } }

        public override void Build()
        {
            BuildWindow(delegate()
            {
                BuildToolbar();

                int firstColumnWidth = (int)GetDefaultFirstColumnWidth();
                int index = 1;

                BuildSettings(ref index, firstColumnWidth);
            });
        }

        private void BuildToolbar()
        {
            if (ImGui.Button("Save settings"))
            {
                Executor.OnSaveUserSettings();
            }
            if (UserSettings.IsModified())
            {
                ImGui.SameLine();
                ImGui.Text("* unsaved changes");
            }

            if (ImGui.Button("Restore defaults"))
            {
                Executor.OnRestoreDefaultUserSettings();
            }
        }

        // Override to add application-specific sections (and to choose their order)
        protected virtual void BuildSettings(ref int index, float firstColumnWidth)
        {
            BuildUserInterfaceSettings(ref index, firstColumnWidth);
            BuildFpsLimitSettings(ref index, firstColumnWidth);
        }

        protected void BuildUserInterfaceSettings(ref int index, float firstColumnWidth)
        {
            if (ImGui.CollapsingHeader("User interface"))
            {
                BeginPropertyGrid(firstColumnWidth);
                BuildUiTextScaleFactor(ref index);
                EndPropertyGrid();
            }
        }

        // Single property (without a section) - for applications that place it in their own section
        protected void BuildUiTextScaleFactor(ref int index)
        {
            UiFloat.Build   (ref index, "User interface text scale factor"  , ref UserSettings.UiTextScaleFactor, 0.01f, 0.05f, 2.0f);
        }

        protected void BuildFpsLimitSettings(ref int index, float firstColumnWidth)
        {
            if (ImGui.CollapsingHeader("FPS Limits"))
            {
                BeginPropertyGrid(firstColumnWidth);

                //UiBool.Build    (ref index, "Use render frequency limit"      , ref UserSettings.UseRenderFrequencyLimit);
                //UiInt.Build     (ref index, "Render frequency limit"          , ref UserSettings.RenderFrequencyLimit, 0.01f, 1, 500);
                //UiBool.Build    (ref index, "Use update frequency limit"      , ref UserSettings.UseUpdateFrequencyLimit);
                //UiInt.Build     (ref index, "Update frequency limit"          , ref UserSettings.UpdateFrequencyLimit, 0.01f, 1, 500);
                UiBool.Build    (ref index, "Use render/update frequency limit"     , ref UserSettings.UseUpdateFrequencyLimit);
                UiInt.Build     (ref index, "Render/Update frequency limit"          , ref UserSettings.UpdateFrequencyLimit, 0.01f, 1, 500);

                EndPropertyGrid();
            }
        }
    }
}
