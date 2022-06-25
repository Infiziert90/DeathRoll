using System;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game;
using Dalamud.Logging;
using ImGuiNET;

namespace DeathRoll.Gui;

public class Timers : IDisposable
{
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);

    private int _h;
    private int _m;
    private int _s;
    private readonly Configuration configuration;

    private readonly Stopwatch stopwatch = new();
    private TimeSpan timeElapsed;
    private TimeSpan wantedTime;

    public Timers(Configuration configuration)
    {
        this.configuration = configuration;
        RestoreDefaults();
    }

    public void Dispose()
    {
        StopTimer();
    }

    public void RenderTimer()
    {
        if (!stopwatch.IsRunning)
        {
            ImGui.SetNextItemWidth(20.0f);
            if (ImGui.InputInt("##hourinput", ref _h, 0)) _h = Math.Clamp(_h, 0, 23);

            ImGui.SameLine(30.0f);
            ImGui.Text("h");
            ImGui.SameLine(40.0f);
            ImGui.SetNextItemWidth(20.0f);
            if (ImGui.InputInt("##mininput", ref _m, 0)) _m = Math.Clamp(_m, 0, 59);

            ImGui.SameLine(62.0f);
            ImGui.Text("m");
            ImGui.SameLine(76.0f);
            ImGui.SetNextItemWidth(20.0f);
            if (ImGui.InputInt("##secinput", ref _s, 0)) _s = Math.Clamp(_s, 0, 59);

            ImGui.SameLine(98.0f);
            ImGui.Text("s");
            ImGui.SameLine(110.0f);

            if (ImGui.Button("Start Timer"))
            {
                wantedTime = new TimeSpan(_h, _m, _s);
                StartTimer();
            }
        }

        if (stopwatch.IsRunning)
        {
            var time = $"{(wantedTime - timeElapsed).Duration():hh\\:mm\\:ss}";

            //remove hours if not present
            if (time.StartsWith("00:")) time = time.Remove(0, 3);

            ImGui.TextColored(_greenColor, time);
            var textLength = ImGui.CalcTextSize(time);

            ImGui.SameLine(textLength.X + 15.0f);

            if (ImGui.Button("Stop Timer")) StopTimer();
        }
    }

    private void StartTimer()
    {
        stopwatch.Start();
        Plugin.Framework.Update += OnFrameworkUpdate;

        RestoreDefaults();

        configuration.ActiveRound = true;
        configuration.Save();

        if (configuration.DebugChat) PluginLog.Debug("Timer started.");
    }

    private void StopTimer()
    {
        stopwatch.Reset();
        Plugin.Framework.Update -= OnFrameworkUpdate;

        configuration.ActiveRound = false;
        configuration.Save();

        if (configuration.DebugChat) PluginLog.Debug("Cleaned up timer.");
    }

    private void OnFrameworkUpdate(Framework _)
    {
        timeElapsed = stopwatch.Elapsed;
        if (wantedTime < timeElapsed) StopTimer();
    }

    public void RestoreDefaults()
    {
        _h = configuration.DefaultHour;
        _m = configuration.DefaultMin;
        _s = configuration.DefaultSec;
    }
}