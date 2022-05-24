using Dalamud.Game;
using Dalamud.Logging;
using System.Diagnostics;
using System;
using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public class Timers : IDisposable
{
    private PluginUI pluginUi;
    private Configuration configuration;
    
    private Vector4 _greenColor = new Vector4(0.0f, 1.0f, 0.0f,1.0f);
    
    private Stopwatch stopwatch = new Stopwatch();
    private TimeSpan wantedTime = new TimeSpan();
    private TimeSpan timeElapsed = new TimeSpan();

    private int _h = 0;
    private int _m = 0;
    private int _s = 0;
    
    public Timers(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.configuration;
        RestoreDefaults();
    }

    public void RenderTimer()
    {
        if (!stopwatch.IsRunning)
        {
            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("##hourinput", ref _h, 0))
            {
                _h = Math.Clamp(_h, 0, 24);
            }
            
            ImGui.SameLine(35.0f);
            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("##mininput", ref _m, 0))
            {
                _m = Math.Clamp(_m, 0, 60);
            }
        
            ImGui.SameLine(62.0f);
            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("##secinput", ref _s, 0))
            {
                _s = Math.Clamp(_s, 0, 60);
            }
            
            ImGui.SameLine(88.0f);
            ImGui.Text("h|m|s");
            ImGui.SameLine(125.0f);
        
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
            
            if (ImGui.Button("Stop Timer"))
            {
                StopTimer();
            }
        }
    }
    
    public void Dispose()
        => StopTimer();
    
    private void StartTimer()
    {
        stopwatch.Start();
        Plugin.Framework.Update += OnFrameworkUpdate;
        
        RestoreDefaults();
        
        configuration.ActiveRound = true;
        configuration.Save();
        
        PluginLog.Debug("Timer started.");
    }

    private void StopTimer()
    {
        stopwatch.Reset();
        Plugin.Framework.Update -= OnFrameworkUpdate;

        configuration.ActiveRound = false;
        configuration.Save();
        
        PluginLog.Debug("Cleaned up timer.");
    }

    private void OnFrameworkUpdate(Framework _)
    {
        timeElapsed = stopwatch.Elapsed;
        if (wantedTime < timeElapsed)
        {
            StopTimer();
        }
    }

    public void RestoreDefaults()
    {
        _h = configuration.DefaultHour;
        _m = configuration.DefaultMin;
        _s = configuration.DefaultSec;
    }
}