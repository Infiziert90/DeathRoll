using Dalamud.Interface.Components;
using DeathRoll.Data;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private void General()
    {
        if (ImGui.BeginTabItem("General"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            changed |= ImGui.Checkbox("On", ref Configuration.On);

            var spacing = ImGui.GetScrollMaxY() == 0 ? 65.0f : 80.0f;
            ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

            if (ImGui.Button("Show UI"))
                Plugin.OpenMain();

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Game Mode:");
            ImGuiComponents.HelpMarker("Venue: Useful for games like Truth&Dare" +
                                       "\nTournament: 1 vs 1 DeathRoll with a bracket system");

            var gameMode = (int) Configuration.GameMode;
            var gameModes = GameModeUtils.ListOfNames();
            if (ImGui.Combo("##GameModeCombo", ref gameMode, gameModes, gameModes.Length))
            {
                changed = true;
                Configuration.GameMode = (GameModes) gameMode;
                Plugin.SwitchState(GameState.NotRunning);
            }

            if (Configuration.GameMode is GameModes.Venue or GameModes.Blackjack)
            {
                ImGuiHelpers.ScaledDummy(5.0f);
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Game Mode Options:");
                ImGui.Indent(10.0f);
                switch (Configuration.GameMode)
                {
                    case GameModes.Venue:
                    {
                        changed |= ImGui.Checkbox("Reroll Allowed", ref Configuration.RerollAllowed);
                        ImGuiComponents.HelpMarker("Player can roll as often as they want," +
                                                   "\noverwriting there previous roll in the process.");

                        changed |= ImGui.Checkbox("Reset On Timer Start", ref Configuration.TimerResets);
                        ImGuiComponents.HelpMarker("On timer start the current list of rolls gets reset.");
                        break;
                    }
                    case GameModes.Blackjack:
                        Blackjack(ref changed);
                        break;
                }
                ImGui.Unindent(10.0f);
            }

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Options:");
            ImGui.Indent(10.0f);
            if (ImGui.Checkbox("Accept Only /random", ref Configuration.OnlyRandom))
            {
                changed = true;
                Configuration.OnlyDice = false;
            }

            if (ImGui.Checkbox("Accept Only /dice", ref Configuration.OnlyDice))
            {
                changed = true;
                Configuration.OnlyRandom = false;
            }
            ImGui.Unindent(10.0f);

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}