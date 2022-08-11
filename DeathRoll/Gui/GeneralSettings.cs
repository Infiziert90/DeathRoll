using System;
using System.Numerics;
using DeathRoll.Data;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui;

public class GeneralSettings
{
    private readonly Configuration configuration;
    private const string DealerHitMsg = @"Hard = Dealer stays at number
Hard17 Example: 
- Hand: Ace (11) + 6 (6) Value: 17 
- Dealer has 17 and stops

Soft = Dealer hits with ace
Soft17 Example: 
- Hand: Ace (11) + 6 (6) Value: 17 
- Draws Queen 
- Hand: Ace (1) + 6 (6) + Queen (10) Value: 17 
- Dealer has hard 17 and stops";
    
    public GeneralSettings(Configuration configuration)
    {
        this.configuration = configuration;
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
                    var autoDrawCard = configuration.AutoDrawCard;
                    if (ImGui.Checkbox("(After) Automatically draw", ref autoDrawCard))
                    {
                        configuration.AutoDrawCard = autoDrawCard;
                        configuration.Save();
                    }
                    ImGui.SameLine();
                    Helper.ShowHelpMarker("Automatically draw all cards for players after the first two starting cards.");

                    var autoDrawOpening = configuration.AutoDrawOpening;
                    if (ImGui.Checkbox("(Start) Automatically draw", ref autoDrawOpening))
                    {
                        configuration.AutoDrawOpening = autoDrawOpening;
                        configuration.Save();
                    }                    
                    ImGui.SameLine();
                    Helper.ShowHelpMarker("Automatically draw two cards for all players at game start.");
                    
                    var autoOpenField = configuration.AutoOpenField;
                    if (ImGui.Checkbox("Open card field on game start", ref autoOpenField))
                    {
                        configuration.AutoOpenField = autoOpenField;
                        configuration.Save();
                    }
                    
                    var defaultBet = configuration.DefaultBet;
                    ImGui.PushItemWidth(120f);
                    if (ImGui.InputInt("##default_bet_input:", ref defaultBet, 0))
                    {
                        defaultBet = Math.Clamp(defaultBet, 10, int.MaxValue);
                        configuration.DefaultBet = defaultBet;
                        configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Default Bet");
                    
                    
                    var dealerRule = (int) configuration.DealerRule;
                    var list1 = Enum.GetNames(typeof(DealerRules));
                    ImGui.PushItemWidth(120f);
                    ImGui.Combo("##dealerrules_combo", ref dealerRule, list1, list1.Length);
                    if (dealerRule != (int) configuration.DealerRule)
                    {
                        configuration.DealerRule = (DealerRules) dealerRule;
                        configuration.Save();
                    }
                    ImGui.SameLine();
                    ImGui.Text("Dealer Rule");
                    ImGui.SameLine();
                    Helper.ShowHelpMarker(DealerHitMsg);
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