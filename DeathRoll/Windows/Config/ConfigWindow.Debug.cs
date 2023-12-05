using Dalamud.Interface.Utility;
using DeathRoll.Data;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private void Debug()
    {
        if (ImGui.BeginTabItem("Debug"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            ImGui.TextColored(Helper.Red,"Please do not run debug all the time!");
            ImGui.TextColored(Helper.Red,"This will bloat your log.");

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Separator();
            ImGuiHelpers.ScaledDummy(5.0f);

            if (ImGui.Checkbox("Debug", ref DebugConfig.Debug))
            {
                DebugConfig.AllowDiceCheat = false;
                DebugConfig.RandomizeNames = false;

                Configuration.Save();
            }

            if (DebugConfig.Debug)
            {
                ImGuiHelpers.ScaledIndent(10.0f);
                ImGui.Checkbox("Randomize Names", ref DebugConfig.RandomizeNames);
                ImGui.Checkbox("Allow Dice Cheat", ref DebugConfig.AllowDiceCheat);
                ImGuiHelpers.ScaledIndent(-10.0f);
            }

            ImGui.EndTabItem();
        }
    }
}