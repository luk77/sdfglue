//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using SdfGlueCore.Model;
using SdfGlueCore.Model.Animation;
using SingleDocAppCore.Model.BaseTypes;
using SingleDocAppFramework.Ui;
using SingleDocAppFramework.Ui.Properties;
using System.Numerics;

namespace SdfGlueUi.Ui.Dialogs
{
    // Modal dialog of the animation export (File > Export animation...): PNG sequence, MP4 video, animated GIF
    //   settings - edits a copy of UserSettingsSdfGlue.ExportAnimSettings; Export saves the user settings and starts
    //              the export, Cancel / Esc / X discard the changes
    //   progress - progress of the running export; Cancel / Esc stop it (no X), closed when the export is finished
    // Owned by UiManagerSdfGlue (not a registered window - no layout entry).
    public class DlgExportAnimation
    {
        private const string                PopupName           = "Export animation##ExportAnimationDialog";
        private const uint                  MaxPathLength       = 1024;

        private static readonly Vector4     WarningColor        = new Vector4(1.0f, 0.6f, 0.2f, 1.0f);
        private static readonly string[]    FormatNames         = { "PNG image sequence", "MP4 video (H.264)", "Animated GIF" };
        private static readonly string[]    Mp4QualityNames     = { "Low", "Medium", "High", "Custom bitrate" };
        private static readonly string[]    GifDitherNames      = { "None", "Ordered (Bayer)", "Floyd-Steinberg" };

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
            UserSettingsSdfGlue config = executor_.GetAppContext().Settings;

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
        // executor: null - no "Browse..." / "From timeline" buttons.
        public static void BuildProperties(ref int id, ExportAnimationSettings settings, UserSettingsSdfGlue config, IUiExecutorSdfGlue? executor)
        {
            int format = Math.Clamp((int)settings.Format, 0, FormatNames.Length - 1);
            UiComboBox.Build(ref id, "Format", FormatNames, ref format);
            settings.Format = (ExportAnimationFormat)format;

            if (settings.IsVideoFile())
            {
                BuildOutputFile(ref id, settings, executor);
            }
            else
            {
                BuildOutputFolder(ref id, settings, executor);

                string filePrefix = settings.FilePrefix;
                UiString.Build(ref id, "File prefix", ref filePrefix);
                settings.FilePrefix = filePrefix;
            }

            // combo index 0: preview resolution
            string[] resolutionNames = GetResolutionNames(config);
            int resolutionIndex = Math.Clamp(settings.ResolutionIndex + 1, 0, resolutionNames.Length - 1);
            UiComboBox.Build(ref id, "Resolution", resolutionNames, ref resolutionIndex);
            settings.ResolutionIndex = resolutionIndex - 1;

            UiInt.Build     (ref id, "FPS"                  , ref settings.Fps                      , 0.1f, 1, 240);
            if (settings.Format == ExportAnimationFormat.Gif && settings.Fps > ExportAnimationSettings.MaxGifFps)
                BuildNote(ref id, String.Format("GIF: at most {0} FPS (browsers play faster GIFs slower)", ExportAnimationSettings.MaxGifFps), true);
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

            if (executor != null)
                BuildFromTimeline(ref id, settings, executor);

            switch (settings.Format)
            {
                case ExportAnimationFormat.Mp4:
                {
                    int quality = Math.Clamp((int)settings.Mp4Quality, 0, Mp4QualityNames.Length - 1);
                    UiComboBox.Build(ref id, "Quality", Mp4QualityNames, ref quality);
                    settings.Mp4Quality = (ExportMp4Quality)quality;

                    ImGui.BeginDisabled(settings.Mp4Quality != ExportMp4Quality.Custom);
                    float bitrate = (float)settings.Mp4BitrateMbps;
                    if (UiFloat.Build(ref id, "Bitrate [Mbit/s]", ref bitrate, 0.1f, 0.1f, 200.0f))
                        settings.Mp4BitrateMbps = bitrate;
                    ImGui.EndDisabled();
                    break;
                }

                case ExportAnimationFormat.Gif:
                {
                    int dither = Math.Clamp((int)settings.GifDither, 0, GifDitherNames.Length - 1);
                    UiComboBox.Build(ref id, "Dithering", GifDitherNames, ref dither);
                    settings.GifDither = (ExportGifDither)dither;

                    string[] scaleNames = ExportAnimationSettings.GifScales.Select(sc => String.Format("{0}%", 100 / sc)).ToArray();
                    int scaleIndex = Math.Max(0, Array.IndexOf(ExportAnimationSettings.GifScales, settings.GifScale));
                    UiComboBox.Build(ref id, "GIF size", scaleNames, ref scaleIndex);
                    settings.GifScale = ExportAnimationSettings.GifScales[scaleIndex];

                    UiBool.Build(ref id, "Loop"                     , ref settings.GifLoop);
                    UiBool.Build(ref id, "Skip unchanged pixels"    , ref settings.GifOptimizeDelta);
                    break;
                }
            }
        }

        // "From timeline": the range and the frame rate of the animation timeline of the project
        private static void BuildFromTimeline(ref int id, ExportAnimationSettings settings, IUiExecutorSdfGlue executor)
        {
            ProjectAnimation animation = executor.GetAppContext().Animation.Data;

            ImGui.PushID(id++);
            ImGui.Text("Timeline");
            ImGui.PopID();
            ImGui.NextColumn();

            ImGui.PushID(id++);
            if (ImGui.Button("From timeline"))
            {
                settings.UseStartTime           = true;
                settings.StartTime              = 0.0;
                settings.Fps                    = Math.Clamp((int)Math.Round(animation.Fps), 1, 240);
                settings.NumOfFramesToExport    = Math.Max(1, (int)Math.Round(animation.Length * settings.Fps));
            }
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Export the whole timeline: start at 0 s, its length and frame rate");
            ImGui.SameLine();
            ImGui.TextDisabled(String.Format("{0:0.###} s, {1:0.###} fps", animation.Length, animation.Fps));
            ImGui.PopID();
            ImGui.NextColumn();
        }

        // a text in the value column of the property grid
        private static void BuildNote(ref int id, string text, bool warning)
        {
            ImGui.PushID(id++);
            ImGui.Text("");
            ImGui.PopID();
            ImGui.NextColumn();
            if (warning)
                ImGui.TextColored(WarningColor, text);
            else
                ImGui.TextDisabled(text);
            ImGui.NextColumn();
        }

        private static void BuildOutputFile(ref int id, ExportAnimationSettings settings, IUiExecutorSdfGlue? executor)
        {
            ImGui.PushID(id++);
            ImGui.Text("Output file");
            ImGui.PopID();
            ImGui.NextColumn();

            ImGuiStylePtr style = ImGui.GetStyle();
            float browseWidth = (executor != null) ? ImGui.CalcTextSize("Browse...").X + style.FramePadding.X * 2.0f + style.ItemSpacing.X : 0.0f;

            ImGui.PushID(id++);
            string file = settings.OutputFile ?? "";
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - browseWidth);
            if (ImGui.InputText("##edit", ref file, MaxPathLength))
                settings.OutputFile = file;
            if (executor != null)
            {
                ImGui.SameLine();
                if (ImGui.Button("Browse..."))
                {
                    string? selected = executor.SelectExportFile(settings.Format, settings.OutputFile);
                    if (!String.IsNullOrEmpty(selected))
                        settings.OutputFile = selected;
                }
            }
            ImGui.PopID();
            ImGui.NextColumn();
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
            IntCoords resolution = config.GetExportAnimResolution(edited_);
            switch (edited_.Format)
            {
                case ExportAnimationFormat.Mp4:
                    // H.264: even size
                    ImGui.TextDisabled(String.Format("Video: {0}x{1}, H.264 (Windows Media Foundation)", resolution.X & ~1, resolution.Y & ~1));
                    break;
                case ExportAnimationFormat.Gif:
                {
                    int scale = Math.Max(1, edited_.GifScale);
                    ImGui.TextDisabled(String.Format("GIF: {0}x{1}", Math.Max(1, resolution.X / scale), Math.Max(1, resolution.Y / scale)));
                    break;
                }
                default:
                    ImGui.TextDisabled(String.Format("Files: {0} ... {1}", edited_.GetFileName(0), edited_.GetFileName(Math.Max(0, edited_.NumOfFramesToExport - 1))));
                    break;
            }
            double startTime = edited_.UseStartTime ? edited_.StartTime : executor_.GetAppContext().Playback.CurrentTime;
            ImGui.TextDisabled(String.Format("Time: {0:0.###} s ... {1:0.###} s{2}", startTime, startTime + edited_.GetDuration(),
                                             edited_.UseStartTime ? "" : " (from the current time)"));
            if (!edited_.UseStartTime)
            {
                double playhead = executor_.GetAppContext().Animation.Transport.Playhead;
                ImGui.TextDisabled(String.Format("Timeline: {0:0.###} s ... {1:0.###} s (from the playhead)", playhead, playhead + edited_.GetDuration()));
            }

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
            else if (progress.IsFinishing)
                ImGui.Text(String.Format("Finishing the file... {0}/{1}", progress.NumOfWrittenFrames, progress.NumOfFramesToExport));
            else
                ImGui.Text(String.Format("Frame {0}/{1}, written {2}", progress.NumOfRenderedFrames, progress.NumOfFramesToExport, progress.NumOfWrittenFrames));
            ImGui.SameLine();
            ImGui.TextDisabled(String.Format("(time {0:0.000} s)", progress.CurrentTime));

            float fraction = progress.GetFraction();
            ImGui.ProgressBar(fraction, new Vector2(-1.0f, 0.0f), String.Format("{0:0}%", fraction * 100.0f));

            TimeSpan? remaining = progress.GetRemainingTime();
            ImGui.Text(String.Format("Elapsed: {0}   Remaining: {1}", FormatTime(progress.Elapsed), remaining.HasValue ? FormatTime(remaining.Value) : "-"));
            ImGui.TextDisabled(String.Format("Output: {0}", progress.Output));

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
