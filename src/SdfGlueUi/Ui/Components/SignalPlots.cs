//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Model.DataNodes.Signals;
using SingleDocAppFramework.Ui.Components;
using System.Numerics;

namespace SdfGlueUi.Ui.Components
{
    // Plots of signal values (history of recent values, x axis: real time in seconds)
    public static class SignalPlots
    {
        public static readonly float[]      TimeWindows         = { 2.0f, 5.0f, 10.0f, 30.0f };
        public static readonly string[]     TimeWindowNames     = { "2 s", "5 s", "10 s", "30 s" };

        public static Vector4 GetColor(SignalInstance signal)
        {
            return UiLinePlot.GetDefaultColor(signal.Id - 1);
        }

        public static UiPlotSeries MakeSeries(SignalInstance signal, SignalsCollection signals)
        {
            SignalHistory history = signal.History;
            return new UiPlotSeries(MenuSignals.GetSignalLabel(signals, signal.Id), GetColor(signal), history.Count, history.GetTime, history.GetValue);
        }

        public static void BuildSparkline(string id, SignalInstance signal, SignalsCollection signals, Vector2 size, float seconds = 5.0f)
        {
            float xMax = signals.GetRealTime();
            UiLinePlot.BuildSparkline(id, size, MakeSeries(signal, signals), xMax - seconds, xMax);
        }

        // xMax - end of the time range (real time of signals, or a frozen time when the plot is paused)
        public static void BuildPlot(string id, IReadOnlyList<SignalInstance> visibleSignals, SignalsCollection signals, Vector2 size, float seconds, float xMax)
        {
            List<UiPlotSeries> series = new List<UiPlotSeries>();
            foreach (SignalInstance signal in visibleSignals)
                series.Add(MakeSeries(signal, signals));

            UiLinePlot.Build(id, size, series, xMax - seconds, xMax, new UiPlotOptions());
        }
    }
}
