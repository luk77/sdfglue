//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SingleDocAppCore.Settings;
using System.Numerics;
using System.Xml;

namespace SingleDocAppFramework.Ui.Settings
{
    // Modal settings dialog: categories (SettingsRegistry) on the left, properties of the selected category on the right.
    // The user settings are edited in place (live preview, applied via IUiExecutorFramework.OnUserSettingsChanged):
    //   OK                 - saves the settings file
    //   Cancel / Esc / X   - restores the state from the moment the dialog was opened
    //   Reset to default   - default values of the selected category (Cancel still reverts it)
    // Owned by UiManagerBase (not a registered window - no layout entry).
    public class SettingsDialog
    {
        private const string                PopupName           = "Settings##SettingsDialog";

        private IUiExecutorFramework        executor_;
        private SettingsRegistry            registry_;
        private UserSettingsChangeDetector  changeDetector_     = new UserSettingsChangeDetector();

        private bool                        openRequested_      = false;
        private bool                        isOpen_             = false;
        private XmlDocument?                snapshot_           = null;     // state to restore on Cancel
        private string?                     selectedCategoryId_ = null;     // kept between openings

        public SettingsDialog(IUiExecutorFramework executor, SettingsRegistry registry)
        {
            executor_   = executor;
            registry_   = registry;
        }

        public bool IsOpen { get { return isOpen_ || openRequested_; } }

        public bool IsAvailable { get { return !registry_.IsEmpty; } }

        public void Open()
        {
            if (IsAvailable && !isOpen_)
                openRequested_ = true;
        }

        public void Build()
        {
            UserSettingsBase settings = executor_.GetUserSettings();

            if (openRequested_)
            {
                openRequested_  = false;
                isOpen_         = true;
                snapshot_       = settings.TakeSnapshot();
                changeDetector_.Reset();
                ImGui.OpenPopup(PopupName);
            }

            if (!isOpen_)
                return;

            float scaling = executor_.GetWindowsScaling();

            ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSize(new Vector2(760.0f, 480.0f) * scaling, ImGuiCond.Appearing);

            bool open = true;
            if (!ImGui.BeginPopupModal(PopupName, ref open, ImGuiWindowFlags.NoSavedSettings))
            {
                // closed by the X button (or by ImGui) - the popup is already closed
                RevertChanges(settings);
                ResetState();
                return;
            }

            SettingsCategory? category = GetSelectedCategory();

            float footerHeight = ImGui.GetFrameHeightWithSpacing() + ImGui.GetStyle().ItemSpacing.Y;

            BuildCategoryList(scaling, footerHeight);
            ImGui.SameLine();
            BuildCategoryContent(category, settings, scaling, footerHeight);

            // edits made by the widgets above
            if (changeDetector_.Check(settings))
                executor_.OnUserSettingsChanged();

            BuildFooter(category, settings);

            ImGui.EndPopup();
        }

        private SettingsCategory? GetSelectedCategory()
        {
            SettingsCategory? category = (selectedCategoryId_ != null) ? registry_.FindCategory(selectedCategoryId_) : null;
            if (category == null)
            {
                category = registry_.GetCategories().FirstOrDefault();
                selectedCategoryId_ = category?.Id;
            }
            return category;
        }

        private void BuildCategoryList(float scaling, float footerHeight)
        {
            ImGui.BeginChild("categories", new Vector2(200.0f * scaling, -footerHeight), ImGuiChildFlags.Borders);

            foreach(SettingsCategory category in registry_.GetCategories())
            {
                if (ImGui.Selectable(category.Title, category.Id == selectedCategoryId_))
                    selectedCategoryId_ = category.Id;
            }

            ImGui.EndChild();
        }

        private void BuildCategoryContent(SettingsCategory? category, UserSettingsBase settings, float scaling, float footerHeight)
        {
            ImGui.BeginChild("content", new Vector2(0.0f, -footerHeight), ImGuiChildFlags.Borders);

            if (category != null)
            {
                SettingsBuildContext ctx = new SettingsBuildContext(executor_, settings);
                // labels in the dialog are longer than in the property windows - part of the available width
                float firstColumnWidth = Math.Max(UiWindowBase.GetDefaultFirstColumnWidth(scaling), ImGui.GetContentRegionAvail().X * 0.5f);

                for(int i=0; i<category.Sections.Count; i++)
                {
                    SettingsSection section = category.Sections[i];

                    if (section.Title != null)
                        ImGui.SeparatorText(section.Title);

                    // each property grid (ImGui.Columns) needs its own id - column borders of several grids would conflict
                    ImGui.PushID(i);
                    UiWindowBase.BeginPropertyGrid(firstColumnWidth);
                    section.Build(ctx);
                    UiWindowBase.EndPropertyGrid();
                    ImGui.PopID();
                }
            }

            ImGui.EndChild();
        }

        private void BuildFooter(SettingsCategory? category, UserSettingsBase settings)
        {
            float rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

            if (category != null && ImGui.Button("Reset to default"))
            {
                category.ResetToDefault(settings, settings.CreateDefaults());
                executor_.OnUserSettingsChanged();
                changeDetector_.Check(settings);    // already notified
            }
            if (category != null && ImGui.IsItemHovered())
                ImGui.SetTooltip(String.Format("Restores the default values of \"{0}\"", category.Title));

            ImGuiStylePtr style = ImGui.GetStyle();
            float buttonWidth = ImGui.CalcTextSize("Cancel").X + style.FramePadding.X * 2.0f;
            ImGui.SameLine(rightEdge - buttonWidth * 2.0f - style.ItemSpacing.X);

            if (ImGui.Button("OK", new Vector2(buttonWidth, 0.0f)))
            {
                executor_.OnSaveUserSettings();
                Close();
                return;
            }
            ImGui.SameLine();

            // Esc only when no widget is being edited (Esc cancels the edit) and no combo is open
            bool escape = ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && ImGui.IsKeyPressed(ImGuiKey.Escape, false);

            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0.0f)) || escape)
            {
                RevertChanges(settings);
                Close();
            }
        }

        // Restores the state from the moment the dialog was opened
        private void RevertChanges(UserSettingsBase settings)
        {
            if (snapshot_ == null)
                return;

            settings.RestoreSnapshot(snapshot_);
            executor_.OnUserSettingsChanged();
        }

        // Called between BeginPopupModal() and EndPopup()
        private void Close()
        {
            ImGui.CloseCurrentPopup();
            ResetState();
        }

        private void ResetState()
        {
            isOpen_     = false;
            snapshot_   = null;
        }
    }
}
