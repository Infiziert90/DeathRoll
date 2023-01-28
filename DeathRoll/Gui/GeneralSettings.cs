using System;
using System.Numerics;
using Dalamud.Interface;
using DeathRoll.Data;
using DeathRoll.Gui.Settings;
using ImGuiNET;

namespace DeathRoll.Gui;

public class GeneralSettings
{
    private PluginUI pluginUI;
    private readonly Configuration configuration;
    private readonly BlackjackSettings blackjackSettings;
    private readonly Vector4 _redColor = new(0.980f, 0.245f, 0.245f, 1.0f);
    
    public GeneralSettings(PluginUI pluginUI, Configuration configuration)
    {
        this.pluginUI = pluginUI;
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
        
        var spacing = ImGui.GetScrollMaxY() == 0 ? 65.0f : 80.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);
        
        if (ImGui.Button("Show UI"))
        {
            pluginUI.Visible = true;
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
                    
                    var timerResets = configuration.TimerResets;
                    if (ImGui.Checkbox("Timer start resets all rolls", ref timerResets))
                    {
                        configuration.TimerResets = timerResets;
                        configuration.Save();
                    }
                    ImGui.SameLine();
                    Helper.ShowHelpMarker("On timer start the current list of rolls will get empty,\neveryone can roll again.");
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

        ImGui.EndTabItem();
    }
    
    public void RenderDebugTab()
    {
        if (!ImGui.BeginTabItem("Debug###debug-tab")) return;
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.TextColored(_redColor,"Please do not run debug all the time!");
        ImGui.TextColored(_redColor,"This will bloat your log.");
        
        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);
        
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