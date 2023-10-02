using Dalamud.Interface.Utility;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private void Timer()
    {
        if (ImGui.BeginTabItem("Timer"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            changed |= ImGui.Checkbox("Show timer option", ref Configuration.UseTimer);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Defaults:");
            ImGui.Indent(10.0f);
            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("Hour", ref Configuration.DefaultHour, 0))
            {
                changed = true;
                Configuration.DefaultHour = Math.Clamp(Configuration.DefaultHour, 0, 23);
            }

            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("Minute", ref Configuration.DefaultMin, 0))
            {
                changed = true;
                Configuration.DefaultMin = Math.Clamp(Configuration.DefaultMin, 0, 59);
            }

            ImGui.SetNextItemWidth(25.0f);
            if (ImGui.InputInt("Second", ref Configuration.DefaultSec, 0))
            {
                changed = true;
                Configuration.DefaultSec = Math.Clamp(Configuration.DefaultSec, 0, 59);
            }
            ImGui.Unindent(10.0f);

            if (changed)
            {
                Configuration.Save();
                Plugin.MainWindow.RestoreTimerDefaults();
            }

            ImGui.EndTabItem();
        }
    }
}