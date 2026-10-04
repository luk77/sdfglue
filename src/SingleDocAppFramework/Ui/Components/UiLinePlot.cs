//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using ImGuiNET;
using System.Globalization;
using System.Numerics;

namespace SingleDocAppFramework.Ui.Components
{
    // A series of points (x ascending) for UiLinePlot
    public class UiPlotSeries
    {
        public  string              Label;
        public  Vector4             Color;
        public  int                 Count;
        public  Func<int, float>    GetX;
        public  Func<int, float>    GetY;

        public UiPlotSeries(string label, Vector4 color, int count, Func<int, float> getX, Func<int, float> getY)
        {
            Label   = label;
            Color   = color;
            Count   = count;
            GetX    = getX;
            GetY    = getY;
        }
    }

    public class UiPlotOptions
    {
        public  bool        ShowGrid        = true;
        public  bool        ShowLegend      = true;
        public  bool        ShowTooltip     = true;
        public  bool        AutoFitY        = true;
        public  float       YMin            = 0.0f;     // used when AutoFitY is false
        public  float       YMax            = 1.0f;
        public  string      XUnit           = "s";
        public  bool        XRelativeToMax  = true;     // x labels relative to xMax (e.g. -5s .. 0s)
    }

    // Simple line plot drawn with the ImGui draw list (no external plotting library).
    // The API is based on series, so the implementation can be replaced later (e.g. by ImPlot).
    public static class UiLinePlot
    {
        private static readonly Vector4[] DefaultColors =
        {
            new Vector4(0.30f, 0.58f, 0.95f, 1.0f),
            new Vector4(0.95f, 0.55f, 0.25f, 1.0f),
            new Vector4(0.40f, 0.80f, 0.40f, 1.0f),
            new Vector4(0.90f, 0.35f, 0.40f, 1.0f),
            new Vector4(0.65f, 0.50f, 0.90f, 1.0f),
            new Vector4(0.90f, 0.80f, 0.30f, 1.0f),
            new Vector4(0.35f, 0.85f, 0.85f, 1.0f),
            new Vector4(0.90f, 0.50f, 0.80f, 1.0f),
        };

        public static Vector4 GetDefaultColor(int index)
        {
            return DefaultColors[((index % DefaultColors.Length) + DefaultColors.Length) % DefaultColors.Length];
        }

        // Full plot: background, grid with labels, series, legend and tooltip
        public static void Build(string id, Vector2 size, IReadOnlyList<UiPlotSeries> series, float xMin, float xMax, UiPlotOptions options)
        {
            Vector2 p0 = ImGui.GetCursorScreenPos();
            if (size.X <= 0.0f)
                size.X = Math.Max(ImGui.GetContentRegionAvail().X, 50.0f);
            if (size.Y <= 0.0f)
                size.Y = Math.Max(ImGui.GetContentRegionAvail().Y, 50.0f);

            ImGui.InvisibleButton(id, size);
            bool hovered = ImGui.IsItemHovered();
            Vector2 p1 = p0 + size;

            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(p0, p1, ImGui.GetColorU32(ImGuiCol.FrameBg));

            float yMin = options.YMin;
            float yMax = options.YMax;
            if (options.AutoFitY)
                ComputeYRange(series, xMin, xMax, out yMin, out yMax);

            uint gridColor = ImGui.GetColorU32(ImGuiCol.Border);
            uint textColor = ImGui.GetColorU32(ImGuiCol.TextDisabled);

            dl.PushClipRect(p0, p1, true);

            if (options.ShowGrid)
            {
                // horizontal lines with value labels
                float stepY = NiceStep((yMax - yMin) / 4.0f);
                for (float y = MathF.Ceiling(yMin / stepY) * stepY; y <= yMax; y += stepY)
                {
                    float py = MapY(y, yMin, yMax, p0.Y, p1.Y);
                    dl.AddLine(new Vector2(p0.X, py), new Vector2(p1.X, py), gridColor, (Math.Abs(y) < stepY * 0.001f) ? 2.0f : 1.0f);
                    dl.AddText(new Vector2(p0.X + 3.0f, py - ImGui.GetTextLineHeight()), textColor, FormatValue(y, stepY));
                }

                // vertical lines with time labels
                if (xMax > xMin)
                {
                    float stepX = NiceStep((xMax - xMin) / 5.0f);
                    float xRef = options.XRelativeToMax ? xMax : 0.0f;
                    for (float x = MathF.Ceiling((xMin - xRef) / stepX) * stepX; x <= xMax - xRef; x += stepX)
                    {
                        float px = MapX(x + xRef, xMin, xMax, p0.X, p1.X);
                        dl.AddLine(new Vector2(px, p0.Y), new Vector2(px, p1.Y), gridColor);
                        dl.AddText(new Vector2(px + 2.0f, p1.Y - ImGui.GetTextLineHeight()), textColor, FormatValue(x, stepX) + options.XUnit);
                    }
                }
            }

            foreach (UiPlotSeries s in series)
                DrawSeries(dl, s, xMin, xMax, yMin, yMax, p0, p1, 1.5f);

            if (options.ShowLegend && series.Count > 0)
                DrawLegend(dl, series, p0, p1);

            dl.PopClipRect();

            if (hovered && options.ShowTooltip && xMax > xMin)
            {
                float mouseX = ImGui.GetIO().MousePos.X;
                dl.AddLine(new Vector2(mouseX, p0.Y), new Vector2(mouseX, p1.Y), ImGui.GetColorU32(ImGuiCol.Text, 0.5f));

                float x = xMin + (mouseX - p0.X) / (p1.X - p0.X) * (xMax - xMin);
                ImGui.BeginTooltip();
                float xLabel = options.XRelativeToMax ? x - xMax : x;
                ImGui.TextDisabled(String.Format(CultureInfo.InvariantCulture, "{0:0.00}{1}", xLabel, options.XUnit));
                foreach (UiPlotSeries s in series)
                {
                    int i = FindNearestIndex(s, x);
                    if (i < 0)
                        continue;
                    ImGui.TextColored(s.Color, String.Format(CultureInfo.InvariantCulture, "{0}: {1:0.####}", s.Label, s.GetY(i)));
                }
                ImGui.EndTooltip();
            }
        }

        // Small plot without grid and labels (e.g. next to a parameter value)
        public static void BuildSparkline(string id, Vector2 size, UiPlotSeries series, float xMin, float xMax)
        {
            Vector2 p0 = ImGui.GetCursorScreenPos();
            ImGui.InvisibleButton(id, size);
            Vector2 p1 = p0 + size;

            ImDrawListPtr dl = ImGui.GetWindowDrawList();
            dl.AddRectFilled(p0, p1, ImGui.GetColorU32(ImGuiCol.FrameBg));

            List<UiPlotSeries> list = new List<UiPlotSeries>() { series };
            ComputeYRange(list, xMin, xMax, out float yMin, out float yMax);

            dl.PushClipRect(p0, p1, true);
            DrawSeries(dl, series, xMin, xMax, yMin, yMax, p0 + new Vector2(0.0f, 1.0f), p1 - new Vector2(0.0f, 1.0f), 1.0f);
            dl.PopClipRect();
        }

        private static void DrawSeries(ImDrawListPtr dl, UiPlotSeries s, float xMin, float xMax, float yMin, float yMax, Vector2 p0, Vector2 p1, float thickness)
        {
            if (s.Count < 2 || xMax <= xMin)
                return;

            uint color = ImGui.ColorConvertFloat4ToU32(s.Color);

            int first = FindFirstIndex(s, xMin);
            if (first > 0)
                first--;    // the line starts outside the plot (clipped)

            // limit the number of segments to ~2 per pixel
            int visible = s.Count - first;
            int step = Math.Max(1, visible / Math.Max(1, (int)((p1.X - p0.X) * 2.0f)));

            Vector2 prev = new Vector2(MapX(s.GetX(first), xMin, xMax, p0.X, p1.X), MapY(s.GetY(first), yMin, yMax, p0.Y, p1.Y));
            for (int i = first + step; i < s.Count; i += step)
            {
                Vector2 curr = new Vector2(MapX(s.GetX(i), xMin, xMax, p0.X, p1.X), MapY(s.GetY(i), yMin, yMax, p0.Y, p1.Y));
                dl.AddLine(prev, curr, color, thickness);
                prev = curr;
            }

            // always include the last point
            int last = s.Count - 1;
            Vector2 end = new Vector2(MapX(s.GetX(last), xMin, xMax, p0.X, p1.X), MapY(s.GetY(last), yMin, yMax, p0.Y, p1.Y));
            if (end != prev)
                dl.AddLine(prev, end, color, thickness);
        }

        private static void DrawLegend(ImDrawListPtr dl, IReadOnlyList<UiPlotSeries> series, Vector2 p0, Vector2 p1)
        {
            float lineHeight = ImGui.GetTextLineHeight();
            float maxWidth = 0.0f;
            foreach (UiPlotSeries s in series)
                maxWidth = Math.Max(maxWidth, ImGui.CalcTextSize(s.Label).X);

            Vector2 pos = new Vector2(p1.X - maxWidth - lineHeight - 12.0f, p0.Y + 4.0f);
            dl.AddRectFilled(pos - new Vector2(4.0f, 2.0f), new Vector2(p1.X - 2.0f, pos.Y + series.Count * lineHeight + 2.0f), ImGui.GetColorU32(ImGuiCol.PopupBg, 0.8f));
            foreach (UiPlotSeries s in series)
            {
                dl.AddRectFilled(pos + new Vector2(0.0f, 3.0f), pos + new Vector2(lineHeight - 6.0f, lineHeight - 3.0f), ImGui.ColorConvertFloat4ToU32(s.Color));
                dl.AddText(pos + new Vector2(lineHeight, 0.0f), ImGui.GetColorU32(ImGuiCol.Text), s.Label);
                pos.Y += lineHeight;
            }
        }

        private static void ComputeYRange(IReadOnlyList<UiPlotSeries> series, float xMin, float xMax, out float yMin, out float yMax)
        {
            yMin = float.MaxValue;
            yMax = float.MinValue;
            foreach (UiPlotSeries s in series)
            {
                for (int i = FindFirstIndex(s, xMin); i < s.Count; i++)
                {
                    float y = s.GetY(i);
                    yMin = Math.Min(yMin, y);
                    yMax = Math.Max(yMax, y);
                }
            }

            if (yMin > yMax)
            {
                yMin = -1.0f;
                yMax = 1.0f;
                return;
            }

            float range = yMax - yMin;
            if (range < 1e-4f)
            {
                float pad = Math.Max(Math.Abs(yMax) * 0.1f, 0.5f);
                yMin -= pad;
                yMax += pad;
                return;
            }

            yMin -= range * 0.08f;
            yMax += range * 0.08f;
        }

        private static int FindFirstIndex(UiPlotSeries s, float minX)
        {
            int lo = 0;
            int hi = s.Count;
            while (lo < hi)
            {
                int mid = (lo + hi) / 2;
                if (s.GetX(mid) < minX)
                    lo = mid + 1;
                else
                    hi = mid;
            }
            return lo;
        }

        private static int FindNearestIndex(UiPlotSeries s, float x)
        {
            if (s.Count == 0)
                return -1;
            int i = FindFirstIndex(s, x);
            if (i >= s.Count)
                return s.Count - 1;
            if (i > 0 && (x - s.GetX(i - 1)) < (s.GetX(i) - x))
                return i - 1;
            return i;
        }

        private static float MapX(float x, float xMin, float xMax, float px0, float px1)
        {
            return px0 + (x - xMin) / (xMax - xMin) * (px1 - px0);
        }

        private static float MapY(float y, float yMin, float yMax, float py0, float py1)
        {
            return py1 - (y - yMin) / (yMax - yMin) * (py1 - py0);
        }

        // 1, 2 or 5 times a power of 10
        private static float NiceStep(float roughStep)
        {
            if (roughStep <= 0.0f || !float.IsFinite(roughStep))
                return 1.0f;
            float exp = MathF.Pow(10.0f, MathF.Floor(MathF.Log10(roughStep)));
            float f = roughStep / exp;
            if (f < 1.5f)
                return exp;
            if (f < 3.5f)
                return 2.0f * exp;
            if (f < 7.5f)
                return 5.0f * exp;
            return 10.0f * exp;
        }

        private static string FormatValue(float val, float step)
        {
            if (Math.Abs(val) < step * 0.001f)
                val = 0.0f;
            int decimals = Math.Clamp(-(int)MathF.Floor(MathF.Log10(step)), 0, 6);
            return val.ToString("F" + decimals, CultureInfo.InvariantCulture);
        }
    }
}
