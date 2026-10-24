//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Properties;
using System.Numerics;

namespace SdfGlueUi.Ui.Dialogs
{
    // Modal dialog of the image sequence export (File > Export image sequence...):
    //   settings - edits a copy of UserSettingsSdfGlue.ExportAnimSettings; Export saves the user settings and starts
    //              the export, Cancel / Esc / X discard the changes
    //   progress - progress of the running export; Cancel / Esc stop it (no X), closed when the export is finished
    // Owned by UiManagerSdfGlue (not a registered window - no layout entry).
    public class DlgExportAnimation
    {
        private const string                PopupName           = "Export image sequence##ExportAnimationDialog";
        private const uint                  MaxPathLength       = 1024;

        private IUiExecutorSdfGlue          executor_;
        private ExportAnimationSettings     edited_             = new ExportAnimationSettings();

        private bool                        openRequested_      = false;
        private bool                        isOpen_             = false;
        private bool                        isExporting_        = false;

        public DlgExportAnimation(IUiExecutorSdfGlue executor)
        {
            executor_ = executor;
        }

        public bool IsOpen { get { return isOpen_ || openRequested_; } }

        public void Open()
        {
            if (!isOpen_)
                openRequested_ = true;
        }

        public void Build()
        {
            UserSettingsSdfGlue config = executor_.GetModel().Config;

            if (openRequested_)
            {
                openRequested_  = false;
                isOpen_         = true;
                isExporting_    = false;
                edited_.CopyFrom(config.ExportAnimSettings);
                ImGui.OpenPopup(PopupName);
            }

            if (!isOpen_)
                return;

            float scaling = executor_.GetWindowsScaling();
            float width   = 640.0f * scaling;

            ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            ImGui.SetNextWindowSizeConstraints(new Vector2(width, 0.0f), new Vector2(width, float.MaxValue));

            ImGuiWindowFlags flags = ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.AlwaysAutoResize;

            // no X button while exporting (Cancel stops the export)
            bool open = true;
            bool visible = isExporting_ ? ImGui.BeginPopupModal(PopupName, flags) : ImGui.BeginPopupModal(PopupName, ref open, flags);
            if (!visible)
            {
                // closed by the X button (or by ImGui) - the popup is already closed
                if (isExporting_)
                    executor_.CancelAnimationExport();
                ResetState();
                return;
            }

            if (isExporting_)
                BuildProgress();
            else
                BuildSettings(config, scaling);

            ImGui.EndPopup();
        }

        // Properties of the export, also used by the settings dialog ("Export animation" category).
        // executor: null - no "Browse..." button.
        public static void BuildProperties(ref int id, ExportAnimationSettings settings, UserSettingsSdfGlue config, IUiExecutorSdfGlue? executor)
        {
            BuildOutputFolder(ref id, settings, executor);

            string filePrefix = settings.FilePrefix;
            UiString.Build(ref id, "File prefix", ref filePrefix);
            settings.FilePrefix = filePrefix;

            // combo index 0: preview resolution
            string[] resolutionNames = GetResolutionNames(config);
            int resolutionIndex = Math.Clamp(settings.ResolutionIndex + 1, 0, resolutionNames.Length - 1);
            UiComboBox.Build(ref id, "Resolution", resolutionNames, ref resolutionIndex);
            settings.ResolutionIndex = resolutionIndex - 1;

            UiInt.Build     (ref id, "FPS"                  , ref settings.Fps                      , 0.1f, 1, 240);
            UiInt.Build     (ref id, "Frames to prerender"  , ref settings.NumOfFramesToPrerender   , 0.1f, 0, 1000);
            UiInt.Build     (ref id, "Frames to export"     , ref settings.NumOfFramesToExport      , 0.1f, 1, 10000000);

            // duration is derived from the number of frames
            float duration = (float)settings.GetDuration();
            if (UiFloat.Build(ref id, "Duration [s]", ref duration, 0.05f, 0.0f, 1000000.0f) && settings.Fps > 0)
                settings.NumOfFramesToExport = Math.Max(1, (int)Math.Round(duration * settings.Fps));

            UiBool.Build    (ref id, "Start at time"        , ref settings.UseStartTime);

            ImGui.BeginDisabled(!settings.UseStartTime);
            float startTime = (float)settings.StartTime;
            if (UiFloat.Build(ref id, "Start time [s]", ref startTime, 0.01f))
                settings.StartTime = startTime;
            ImGui.EndDisabled();
        }

        private static void BuildOutputFolder(ref int id, ExportAnimationSettings settings, IUiExecutorSdfGlue? executor)
        {
            ImGui.PushID(id++);
            ImGui.Text("Output folder");
            ImGui.PopID();
            ImGui.NextColumn();

            ImGuiStylePtr style = ImGui.GetStyle();
            float browseWidth = (executor != null) ? ImGui.CalcTextSize("Browse...").X + style.FramePadding.X * 2.0f + style.ItemSpacing.X : 0.0f;

            ImGui.PushID(id++);
            string folder = settings.OutputFolder ?? "";
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - browseWidth);
            if (ImGui.InputText("##edit", ref folder, MaxPathLength))
                settings.OutputFolder = folder;
            if (executor != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Browse..."))
                {
                    string? selected = executor.SelectFolder();
                    if (!String.IsNullOrEmpty(selected))
                        settings.OutputFolder = selected;
                }
            }
            ImGui.PopID();
            ImGui.NextColumn();
        }

        private static string[] GetResolutionNames(UserSettingsSdfGlue config)
        {
            IntCoords preview = config.GetPreviewResolution();

            string[] names = new string[config.ResolutionsAsStrings.Length + 1];
            names[0] = String.Format("Preview ({0}x{1})", preview.X, preview.Y);
            Array.Copy(config.ResolutionsAsStrings, 0, names, 1, config.ResolutionsAsStrings.Length);
            return names;
        }

        private void BuildSettings(UserSettingsSdfGlue config, float scaling)
        {
            int id = 1;
            float firstColumnWidth = Math.Max(UiWindowBase.GetDefaultFirstColumnWidth(scaling), ImGui.GetContentRegionAvail().X * 0.4f);

            UiWindowBase.BeginPropertyGrid(firstColumnWidth);
            BuildProperties(ref id, edited_, config, executor_);
            UiWindowBase.EndPropertyGrid();

            // summary
            ImGui.TextDisabled(String.Format("Files: {0} ... {1}", edited_.GetFileName(0), edited_.GetFileName(Math.Max(0, edited_.NumOfFramesToExport - 1))));
            double startTime = edited_.UseStartTime ? edited_.StartTime : executor_.GetModel().CurrentTime;
            ImGui.TextDisabled(String.Format("Time: {0:0.###} s ... {1:0.###} s{2}", startTime, startTime + edited_.GetDuration(),
                                             edited_.UseStartTime ? "" : " (from the current time)"));

            ImGui.Spacing();
            BuildSettingsFooter(config);
        }

        private void BuildSettingsFooter(UserSettingsSdfGlue config)
        {
            float rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;

            ImGuiStylePtr style = ImGui.GetStyle();
            float buttonWidth = ImGui.CalcTextSize("Cancel").X + style.FramePadding.X * 2.0f;
            ImGui.SetCursorPosX(rightEdge - buttonWidth * 2.0f - style.ItemSpacing.X);

            if (ImGui.Button("Export", new Vector2(buttonWidth, 0.0f)))
            {
                config.ExportAnimSettings.CopyFrom(edited_);
                executor_.OnSaveUserSettings();

                // errors and questions (e.g. overwriting) are shown by the executor - the dialog stays open then
                if (executor_.StartAnimationExport())
                    isExporting_ = true;
                return;
            }
            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0.0f)) || IsEscapePressed())
                Close();
        }

        private void BuildProgress()
        {
            AnimationExportProgress progress = executor_.GetAnimationExportProgress();
            if (!progress.IsActive)
            {
                // finished (or failed) - the result is in the log
                Close();
                return;
            }

            if (progress.IsPrerendering)
                ImGui.Text(String.Format("Prerendering {0}/{1}", progress.NumOfPrerenderedFrames, progress.NumOfFramesToPrerender));
            else
                ImGui.Text(String.Format("Frame {0}/{1}", progress.NumOfSavedFrames, progress.NumOfFramesToExport));
            ImGui.SameLine();
            ImGui.TextDisabled(String.Format("(time {0:0.000} s)", progress.CurrentTime));

            float fraction = progress.GetFraction();
            ImGui.ProgressBar(fraction, new Vector2(-1.0f, 0.0f), String.Format("{0:0}%", fraction * 100.0f));

            TimeSpan? remaining = progress.GetRemainingTime();
            ImGui.Text(String.Format("Elapsed: {0}   Remaining: {1}", FormatTime(progress.Elapsed), remaining.HasValue ? FormatTime(remaining.Value) : "-"));
            ImGui.TextDisabled(String.Format("Output: {0}", progress.OutputFolder));

            ImGui.Spacing();

            float rightEdge = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
            ImGuiStylePtr style = ImGui.GetStyle();
            float buttonWidth = ImGui.CalcTextSize("Cancel").X + style.FramePadding.X * 2.0f;
            ImGui.SetCursorPosX(rightEdge - buttonWidth);

            // the dialog is closed in the next frame (the export is not active then)
            if (ImGui.Button("Cancel", new Vector2(buttonWidth, 0.0f)) || IsEscapePressed())
                executor_.CancelAnimationExport();
        }

        private static string FormatTime(TimeSpan time)
        {
            return time.ToString(@"hh\:mm\:ss");
        }

        // Esc only when no widget is being edited (Esc cancels the edit) and no combo is open
        private static bool IsEscapePressed()
        {
            return ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsAnyItemActive() && ImGui.IsKeyPressed(ImGuiKey.Escape, false);
        }

        // Called between BeginPopupModal() and EndPopup()
        private void Close()
        {
            ImGui.CloseCurrentPopup();
            ResetState();
        }

        private void ResetState()
        {
            isOpen_         = false;
            isExporting_    = false;
        }
    }
}
