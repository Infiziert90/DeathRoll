using System;
using System.Numerics;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui.Settings;

public class BlackjackSettings
{
    private readonly Configuration configuration;
    private const string DealerHitMsg = @"Hard = Dealer stays at number
Hard17 Example: 
- Hand: Ace (11) + 6 Value: 17 
- Dealer has 17 and must stay

Soft = Dealer hits with ace
Soft17 Example: 
- Hand: Ace (11) + 6 Value: 17 
- Draws 3 
- Hand: Ace (11) + 6 + 3 Value: 20 
- Dealer has 20 and must stay

Alternative Soft16 Example:
- Hand: Ace (11) + 5 Value: 16 
- Draws Queen 
- Hand: Ace (1) + 5 + Queen Value: 16 
- Dealer has hard 16 and must stay";

    private const string DealerVenueMsg = "Venue:\n" +
                                          "Modifies the game into a format suitable for a public venue, " +
                                          "this will force all players and dealer to roll for cards - but in a different " +
                                          "order to preserve the 'Hidden Card' status.";
    
    public BlackjackSettings(Configuration configuration)
    {
        this.configuration = configuration;
    }
    
    public void RenderSettings()
    {
        var current = configuration.BlackjackMode;
        ImGui.RadioButton("Normal", ref current, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Venue", ref current, 1);
        ImGui.SameLine();
        Helper.ShowHelpMarker(DealerVenueMsg);

        if (current != configuration.BlackjackMode)
        {
            configuration.BlackjackMode = current;
            switch (current)
            {
                case 0:
                    configuration.AutoDrawCard = configuration.AutoDrawOpening = configuration.AutoDrawDealer = true;
                    configuration.VenueDealer = false;
                    break;
                case 1:
                    configuration.AutoDrawCard = configuration.AutoDrawOpening = configuration.AutoDrawDealer = false;
                    configuration.VenueDealer = true;
                    break;
            }
            configuration.Save();
        }
        
        
        if (configuration.BlackjackMode == 0)
        {
            var autoDrawCard = configuration.AutoDrawCard;
            if (ImGui.Checkbox("Automate player draws", ref autoDrawCard))
            {
                configuration.VenueDealer = false;
                configuration.AutoDrawCard = autoDrawCard;
                configuration.Save();
            }
            ImGui.SameLine();
            Helper.ShowHelpMarker("Automatically draw cards for players on hit, double down and split.");

            var autoDrawOpening = configuration.AutoDrawOpening;
            if (ImGui.Checkbox("Automate opening draws", ref autoDrawOpening))
            {
                configuration.VenueDealer = false;
                configuration.AutoDrawOpening = autoDrawOpening;
                configuration.Save();
            }                    
            ImGui.SameLine();
            Helper.ShowHelpMarker("Automatically draw the first two cards for all players.");
        
            var autoDrawDealer = configuration.AutoDrawDealer;
            if (ImGui.Checkbox("Automate dealer draws", ref autoDrawDealer))
            {
                configuration.VenueDealer = false;
                configuration.AutoDrawDealer = autoDrawDealer;
                configuration.Save();
            }                    
            ImGui.SameLine();
            Helper.ShowHelpMarker("Automatically draw all dealer cards (first two cards are excluded).");
        }
        
        if (configuration.BlackjackMode == 1)
        {
            var dealerDrawsAll = configuration.DealerDrawsAll;
            if (ImGui.Checkbox("Dealer draws all cards", ref dealerDrawsAll))
            {
                configuration.VenueDealer = true;
                configuration.DealerDrawsAll = dealerDrawsAll;
                configuration.Save();
            }
            ImGui.SameLine();
            Helper.ShowHelpMarker("Dealer draws all cards instead of players. Faster gameplay!");
        }
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        var autoOpenField = configuration.AutoOpenField;
        if (ImGui.Checkbox("Open extended UI", ref autoOpenField))
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
