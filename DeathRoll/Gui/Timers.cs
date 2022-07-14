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
        ImGui.Dummy(new Vector2(0.0f, 1.0f));
        
        if (!stopwatch.IsRunning)
        {
            ImGui.SetNextItemWidth(35.0f);
            if (ImGui.DragInt("##drag_hour", ref _h, 1, 0, 23)) _h = Math.Clamp(_h, 0, 23);
            ImGui.SameLine(0.0f, 3.0f);
            ImGui.Text(":");
            ImGui.SameLine(0.0f, 3.0f);
            ImGui.SetNextItemWidth(35.0f);
            if (ImGui.DragInt("##drag_min", ref _m, 1, 0, 59)) _m = Math.Clamp(_m, 0, 59);
            ImGui.SameLine(0.0f, 3.0f);
            ImGui.Text(":");
            ImGui.SameLine(0.0f, 3.0f);
            ImGui.SetNextItemWidth(35.0f);
            if (ImGui.DragInt("##drag_sec", ref _s, 1, 0, 59)) _s = Math.Clamp(_s, 0, 59);
            ImGui.SameLine(0.0f, 3.0f);
            Helper.ShowHelpMarker("Hours : Minutes : Seconds\nHold ALT for slower edit.\nDouble-click to input value.");
            
            ImGui.SameLine(155.0f);
            if (ImGui.Button("Start Timer"))
            {
                wantedTime = new TimeSpan(_h, _m, _s);
                StartTimer();
            }
            return;
        }
        
        var time = $"{(wantedTime - timeElapsed).Duration():hh\\:mm\\:ss}";

        //remove hours if not present
        if (time.StartsWith("00:")) time = time.Remove(0, 3);
        time += time.Length < 6 ? " min" : " hr";
        
        ImGui.TextColored(_greenColor, time);
        
        ImGui.SameLine(ImGui.CalcTextSize(time).X + 15.0f);
        if (ImGui.Button("Stop Timer")) StopTimer();
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

    public void StopTimer()
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