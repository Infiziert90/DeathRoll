using System.Diagnostics;
using Dalamud.Interface.Components;
using Dalamud.Plugin.Services;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private int H;
    private int M;
    private int S;

    private readonly Stopwatch Stopwatch = new();
    private TimeSpan WantedTime;

    private void Timer()
    {
        switch (Stopwatch.IsRunning)
        {
            case false when Plugin.State is not GameState.Match:
                RenderNotRunning();
                break;
            case true when Plugin.State is GameState.Match:
                RenderRunning();
                break;
        }
    }

    private void RenderNotRunning()
    {
        ImGui.SetNextItemWidth(35.0f);
        if (ImGui.DragInt("##DragHour", ref H, 1, 0, 23))
            H = Math.Clamp(H, 0, 23);

        ImGui.SameLine(0.0f, 3.0f);
        ImGui.Text(":");
        ImGui.SameLine(0.0f, 3.0f);

        ImGui.SetNextItemWidth(35.0f);
        if (ImGui.DragInt("##DragMin", ref M, 1, 0, 59))
            M = Math.Clamp(M, 0, 59);

        ImGui.SameLine(0.0f, 3.0f);
        ImGui.Text(":");
        ImGui.SameLine(0.0f, 3.0f);

        ImGui.SetNextItemWidth(35.0f);
        if (ImGui.DragInt("##DragSec", ref S, 1, 0, 59))
            S = Math.Clamp(S, 0, 59);
        ImGui.SameLine(0.0f, 3.0f);
        ImGuiComponents.HelpMarker("Hours : Minutes : Seconds" +
                                   "\nHold ALT for slower edit." +
                                   "\nDouble-click to input value.");

        ImGui.SameLine();
        if (ImGui.Button("Start Timer"))
            StartTimer();
    }

    private void RenderRunning()
    {
        var time = $@"{(WantedTime - Stopwatch.Elapsed).Duration():hh\:mm\:ss}";

        //remove hours if not present
        if (time.StartsWith("00:"))
            time = time.Remove(0, 3);
        time += time.Length < 6 ? " min" : " hr";

        ImGui.TextColored(Helper.Green, $"Time Left: {time}");
    }

    public void BeginTimer()
    {
        if (Configuration.TimerResets)
            Plugin.Participants.Reset();

        StartTimer();
    }

    public void StopTimer()
    {
        Stopwatch.Reset();
        Plugin.Framework.Update -= OnFrameworkUpdate;

        Plugin.SwitchState(GameState.Done);

        if (Configuration.Debug)
            Plugin.Log.Information("Timer stopped.");
    }

    private void StartTimer()
    {
        WantedTime = new TimeSpan(H, M, S);
        Stopwatch.Start();
        Plugin.Framework.Update += OnFrameworkUpdate;

        RestoreTimerDefaults();
        Configuration.Save();
        Plugin.SwitchState(GameState.Match);

        if (Configuration.Debug)
            Plugin.Log.Information("Timer started.");
    }

    private void OnFrameworkUpdate(IFramework _)
    {
        if (WantedTime < Stopwatch.Elapsed)
            StopTimer();
    }

    public void RestoreTimerDefaults()
    {
        H = Configuration.DefaultHour;
        M = Configuration.DefaultMin;
        S = Configuration.DefaultSec;
    }

    public bool IsTimerActive() => Stopwatch.IsRunning;
}