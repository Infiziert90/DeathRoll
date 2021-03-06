using System;
using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public class TimerSetting
{
    private readonly Configuration configuration;
    private readonly RollTable rollTable;

    public TimerSetting(Configuration configuration, RollTable rollTable)
    {
        this.configuration = configuration;
        this.rollTable = rollTable;
    }

    public void RenderTimerSettings()
    {
        if (!ImGui.BeginTabItem("Timer###timer-tab")) return;

        var useTimer = configuration.UseTimer;
        if (ImGui.Checkbox("Show timer option", ref useTimer))
        {
            configuration.UseTimer = useTimer;
            configuration.Save();
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Defaults:");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));

        ImGui.SetNextItemWidth(25.0f);
        var _h = configuration.DefaultHour;
        if (ImGui.InputInt("Hour##defaulthourinput", ref _h, 0))
        {
            _h = Math.Clamp(_h, 0, 23);
            if (_h != configuration.DefaultHour)
            {
                configuration.DefaultHour = _h;
                configuration.Save();

                rollTable.Timers.RestoreDefaults();
            }
        }

        ImGui.SetNextItemWidth(25.0f);
        var _m = configuration.DefaultMin;
        if (ImGui.InputInt("Minute##defaultmininput", ref _m, 0))
        {
            _m = Math.Clamp(_m, 0, 59);
            if (_m != configuration.DefaultMin)
            {
                configuration.DefaultMin = _m;
                configuration.Save();

                rollTable.Timers.RestoreDefaults();
            }
        }

        ImGui.SetNextItemWidth(25.0f);
        var _s = configuration.DefaultSec;
        if (ImGui.InputInt("Second##defaultsecinput", ref _s, 0))
        {
            _s = Math.Clamp(_s, 0, 59);
            if (_s != configuration.DefaultSec)
            {
                configuration.DefaultSec = _s;
                configuration.Save();

                rollTable.Timers.RestoreDefaults();
            }
        }

        ImGui.EndTabItem();
    }
}