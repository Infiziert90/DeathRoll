using System;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui.Settings;

public class BlackjackSettings
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
    
    public BlackjackSettings(Configuration configuration)
    {
        this.configuration = configuration;
    }
    
    public void RenderSettings()
    {
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
        
        var autoDrawDealer = configuration.AutoDrawDealer;
        if (ImGui.Checkbox("(Dealer) Automatically draw", ref autoDrawDealer))
        {
            configuration.AutoDrawDealer = autoDrawDealer;
            configuration.Save();
        }                    
        ImGui.SameLine();
        Helper.ShowHelpMarker("Automatically draw all dealer cards (first two cards are always automatic).");
        
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
    }
}