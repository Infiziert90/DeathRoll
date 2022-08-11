using System;
using System.Numerics;
using DeathRoll.Data;
using DeathRoll.Gui.Settings;
using ImGuiNET;

namespace DeathRoll.Gui;

public class GeneralSettings
{
    private readonly Configuration configuration;
    private readonly BlackjackSettings blackjackSettings;
    
    public GeneralSettings(Configuration configuration)
    {
        this.configuration = configuration;
        blackjackSettings = new BlackjackSettings(configuration);
    }

    public void RenderGeneralSettings()
    {
        if (!ImGui.BeginTabItem("General###general-tab")) return;
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        var on = configuration.On;
        if (ImGui.Checkbox("On", ref on))
        {
            configuration.On = on;
            configuration.Save();
        }
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Game Mode:");
        ImGui.SameLine();
        Helper.ShowHelpMarker("Venue: Useful for games like Truth&Dare\nTournament: 1 vs 1 DeathRoll with a bracket system");
        
        var gameMode = (int) configuration.GameMode;
        var list = Enum.GetNames(typeof(GameModes));
        ImGui.Combo("##gamemode_combo", ref gameMode, list, list.Length);
        
        if (gameMode != (int) configuration.GameMode)
        {
            Plugin.SwitchState(GameState.NotRunning);
            
            configuration.GameMode = (GameModes) gameMode;
            configuration.Save();
        }
        
        if (configuration.GameMode is GameModes.Venue or GameModes.Blackjack)
        {
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            ImGui.Text("Game Mode Options:");
            ImGui.Dummy(new Vector2(0.0f, 5.0f));

            switch (configuration.GameMode)
            {
                case GameModes.Venue:
                {
                    var allowReroll = configuration.RerollAllowed;
                    if (ImGui.Checkbox("Reroll allowed", ref allowReroll))
                    {
                        configuration.RerollAllowed = allowReroll;
                        configuration.Save();
                    }
                    ImGui.SameLine();
                    Helper.ShowHelpMarker("Player can roll as often as they want,\noverwriting there previous roll in the process.");
                    break;
                }
                case GameModes.Blackjack:
                    blackjackSettings.RenderSettings();
                    break;
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Options:");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));

        var onlyRandom = configuration.OnlyRandom;
        if (ImGui.Checkbox("Accept only /random", ref onlyRandom))
        {
            configuration.OnlyRandom = onlyRandom;
            configuration.OnlyDice = false;
            configuration.Save();
        }

        var onlyDice = configuration.OnlyDice;
        if (ImGui.Checkbox("Accept only /dice", ref onlyDice))
        {
            configuration.OnlyDice = onlyDice;
            configuration.OnlyRandom = false;
            configuration.Save();
        }

        var verboseChatlog = configuration.Debug;
        if (ImGui.Checkbox("Debug", ref verboseChatlog))
        {
            configuration.Debug = verboseChatlog;
            DebugConfig.AllowDiceCheat = false;
            DebugConfig.RandomizeNames = false;
            configuration.Save();
        }

        if (verboseChatlog)
        {
            ImGui.Dummy(new Vector2(15.0f, 0.0f));
            ImGui.SameLine();
            var randomizePlayers = DebugConfig.RandomizeNames;
            if (ImGui.Checkbox("Randomize names", ref randomizePlayers))
                DebugConfig.RandomizeNames = randomizePlayers;

            ImGui.Dummy(new Vector2(15.0f, 0.0f));
            ImGui.SameLine();
            var allowDiceCheat = DebugConfig.AllowDiceCheat;
            if (ImGui.Checkbox("Allow dice cheat", ref allowDiceCheat))
                DebugConfig.AllowDiceCheat = allowDiceCheat;
        }

        ImGui.EndTabItem();
    }
}