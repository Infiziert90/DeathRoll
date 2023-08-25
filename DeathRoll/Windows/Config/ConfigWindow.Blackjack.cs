using Dalamud.Interface.Components;
using DeathRoll.Data;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private const string DealerHitMsg = "Hard = Dealer stays at number" +
                                        "\nHard17 Example:" +
                                        "\n- Hand: Ace (11) + 6 Value: 17" +
                                        "\n- Dealer has 17 and must stay" +
                                        "\n\nSoft = Dealer hits with ace" +
                                        "\nSoft17 Example:" +
                                        "\n- Hand: Ace (11) + 6 Value: 17" +
                                        "\n- Draws 3" +
                                        "\n- Hand: Ace (11) + 6 + 3 Value: 20" +
                                        "\n- Dealer has 20 and must stay" +
                                        "\n\nAlternative Soft16 Example:" +
                                        "\n- Hand: Ace (11) + 5 Value: 16" +
                                        "\n- Draws Queen" +
                                        "\n- Hand: Ace (1) + 5 + Queen Value: 16" +
                                        "\n- Dealer has hard 16 and must stay";

    private const string DealerVenueMsg = "Venue:" +
                                          "\nModifies the game into a format suitable for a public venue, " +
                                          "this will force all players and dealer to roll for cards - but in a different " +
                                          "order to preserve the 'Hidden Card' status.";

    private void Blackjack(ref bool changed)
    {
        var current = Configuration.BlackjackMode;
        ImGui.RadioButton("Normal", ref current, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Venue", ref current, 1);
        ImGuiComponents.HelpMarker(DealerVenueMsg);

        if (current != Configuration.BlackjackMode)
        {
            changed = true;
            Configuration.BlackjackMode = current;
            switch (current)
            {
                case 0:
                    Configuration.AutoDrawCard = Configuration.AutoDrawOpening = Configuration.AutoDrawDealer = true;
                    Configuration.VenueDealer = false;
                    break;
                case 1:
                    Configuration.AutoDrawCard = Configuration.AutoDrawOpening = Configuration.AutoDrawDealer = false;
                    Configuration.VenueDealer = true;
                    break;
            }
        }


        changed |= ImGui.Checkbox("Draw Both On Start", ref Configuration.StartingDraw);
        ImGuiComponents.HelpMarker("This changes the starting behaviour from single draws into both draws at once.");

        changed |= ImGui.Checkbox("21 Blackjack Behaviour", ref Configuration.StartingBlackjack);
        ImGuiComponents.HelpMarker("This changes the 21 win behaviour from always to just starting hand.");

        if (Configuration.BlackjackMode == 0)
        {
            changed |= ImGui.Checkbox("Automate Player Draws", ref Configuration.AutoDrawCard);
            ImGuiComponents.HelpMarker("Automatically draw cards for players on hit, double down and split.");

            changed |= ImGui.Checkbox("Automate Start Draws", ref Configuration.AutoDrawOpening);
            ImGuiComponents.HelpMarker("Automatically draw the first two cards for all players.");

            changed |= ImGui.Checkbox("Automate Dealer Draws", ref Configuration.AutoDrawDealer);
            ImGuiComponents.HelpMarker("Automatically draw all dealer cards (first two cards are excluded).");

            if (changed)
                Configuration.VenueDealer = false;
        }
        else
        {
            changed |= ImGui.Checkbox("Dealer Draws All Cards", ref Configuration.DealerDrawsAll);
            ImGuiComponents.HelpMarker("Dealer draws all cards instead of players. Faster gameplay!");

            if (changed)
                Configuration.VenueDealer = true;
        }

        ImGuiHelpers.ScaledDummy(5.0f);

        changed |= ImGui.Checkbox("Automatically Open Card UI", ref Configuration.AutoOpenField);

        ImGui.PushItemWidth(120f);
        if (ImGui.InputInt("##DefaultBetInput:", ref Configuration.DefaultBet, 0))
        {
            changed = true;
            Configuration.DefaultBet = Math.Clamp(Configuration.DefaultBet, 10, int.MaxValue);
        }
        ImGui.SameLine();
        ImGui.Text("Default Bet");


        var dealerRule = (int) Configuration.DealerRule;
        var rules = RuleUtils.ListOfNames;
        ImGui.PushItemWidth(120f);
        if (ImGui.Combo("##DealerRulesCombo", ref dealerRule, rules, rules.Length))
        {
            changed = true;
            Configuration.DealerRule = (DealerRules) dealerRule;
        }
        ImGui.SameLine();
        ImGui.Text("Dealer Rule");
        ImGuiComponents.HelpMarker(DealerHitMsg);
    }
}