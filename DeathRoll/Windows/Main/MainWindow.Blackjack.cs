using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private const string HelpText = "- Hit: Player draws a card," +
                                    "\n- Stay: Player holds hand as it is" +
                                    "\n- Surrender: Player drops out of round and loses half the bet" +
                                    "\n- Double Down: Player bets double, but receives exactly one more card" +
                                    "\n- Split: Only possible at round start, and if the player has same Rank cards (e.g K and K)" +
                                    "\n    > Player opens a new hand with one card in each hand, puts another bet of same amount, and draws with both hands a card" +
                                    "\n    > Round continues as before, with the split hands turn happening later";

    private void BlackjackMode()
    {
        BlackjackControlPanel();
        ImGuiHelpers.ScaledDummy(5.0f);

        switch (Plugin.State)
        {
            case GameState.Registration:
                BlackjackRegistrationPanel();
                break;
            case GameState.DealerFirstCards or GameState.DealerSecondCards:
                DealerStartingDraw();
                break;
            case GameState.PrepareRound:
                MatchBeginningPanel();
                break;
            case GameState.DrawFirstCards or GameState.DrawSecondCards:
                MatchBeginDraw();
                break;
            case GameState.PlayerRound:
                PlayerRoundPanel();
                break;
            case GameState.Hit or GameState.DoubleDown or GameState.DrawSplit or GameState.FillDraw:
                WaitForRollPanel();
                break;
            case GameState.DealerRound:
                DealerRoundPanel();
                break;
            case GameState.DrawDealerCard:
                DealerDrawRender();
                break;
            case GameState.DealerDone:
                DealerDonePanel();
                break;
            case GameState.Done:
                MatchDonePanel();
                break;
        }

        switch (Plugin.State)
        {
            case GameState.PlayerRound:
            case GameState.DealerRound:
            case GameState.DealerDone:
            case GameState.Done:
                CardDeckRender();
                break;
        }
    }

    private void BlackjackControlPanel()
    {
        if (ImGui.Button("Show Settings"))
            Plugin.OpenConfig();

        ImGui.SameLine();

        if (Plugin.State is not GameState.NotRunning and not GameState.Crash and not GameState.Registration)
            if (ImGui.Button($"Game Field"))
                Plugin.ToggleCardField();

        var spacing = ImGui.GetScrollMaxY() == 0 ? 85.0f : 120.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        switch (Plugin.State)
        {
            case GameState.NotRunning:
                if (!ImGui.Button("Start Round"))
                    return;
                Plugin.Blackjack.Reset();
                Plugin.ClosePlayWindows();
                Plugin.SwitchState(GameState.Registration);
                return;
            case GameState.Crash:
                if (!ImGui.Button("Force Stop Round"))
                    return;
                Plugin.Blackjack.Reset();
                Plugin.ClosePlayWindows();
                Plugin.SwitchState(GameState.NotRunning);
                return;
            default:
                if (!ImGui.Button("Stop Round"))
                    return;
                Plugin.Blackjack.Reset();
                Plugin.ClosePlayWindows();
                Plugin.SwitchState(GameState.NotRunning);
                return;
        }
    }

    private void MatchDonePanel()
    {
        ImGui.TextColored(Helper.Green, $"Round finished.");
        ImGui.TextColored(Helper.Yellow, $"All bets are adjusted to the correct amount for payouts.");

        if (ImGui.Button("Play Again"))
        {
            Plugin.ClosePlayWindows();
            Plugin.Blackjack.TakePeopleIntoNextRound();
        }

        ImGui.SameLine();

        // Copy out all winnings to a single string to show all players the final standings (and leave a record if a player wants to play their push/winnings into the next round)
        if (ImGui.Button("Copy Payout"))
        {
            var finalPayout = "Payouts: | ";
            foreach (var player in Plugin.Blackjack.Players)
                finalPayout += $"{player.Name.Split()[0]} -> {(player.Bet < 1 ? "Lost!" : $"{player.Bet:N0}")} | ";
            ImGui.SetClipboardText(finalPayout);
        }

        // Let's allow 'Copy Dealer' everywhere so newbies can be shown the dealer's hand (especially if it resolves on the first two cards)
        ImGui.SameLine();
        DealerCardButton();
    }

    private void DealerDonePanel()
    {
        ImGui.TextColored(Helper.Yellow, $"The dealer is not allowed to hit anymore!");
        ImGui.TextColored(Helper.Yellow, $"Please proceed by pressing 'Calculate Winnings' below.");
        if (ImGui.Button("Calculate Winnings"))
            Plugin.Blackjack.EndMatch();

        DealerCardButton();
    }

    private void DealerCardButton()
    {
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = Plugin.Blackjack.Dealer.CalculateCardValues();
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", Plugin.Blackjack.Dealer.Cards.Select(Cards.ShowCardSimple))} -- Total: {cards}");
        }
    }

    private void DealerDrawRender()
    {
        if (!Plugin.Blackjack.IsDealerDone())
        {
            Plugin.Blackjack.DealerRound();
            if (Plugin.State == GameState.Done)
                return;

            Plugin.Blackjack.Dealer.LastAction = BlackjackActions.None;
            Plugin.SwitchState(GameState.DealerDone);
            return;
        }

        ImGui.TextColored(Helper.Green, "Waiting for dealer roll ...");
        ImGui.TextColored(Helper.Green, "Dealer must draw a card with /random 13 or /dice 13");
        ImGui.TextColored(Helper.Green, $"The cards have currently a value of {Plugin.Blackjack.Dealer.CalculateCardValues()}");

        ImGuiHelpers.ScaledDummy(5.0f);
        DealerCardButton();
    }

    private void DealerRoundPanel()
    {
        ImGui.TextColored(Helper.Yellow, "All players done!");
        if (ImGui.Button("Begin Dealer Round"))
            Plugin.Blackjack.DealerAction();

        ImGuiHelpers.ScaledDummy(5.0f);
        DealerCardButton();
    }

    private void WaitForRollPanel()
    {
        ImGui.TextColored(Helper.Green, $"Awaiting {Plugin.Blackjack.CurrentPlayer.DisplayName}'s roll ...");
        ImGui.TextColored(Helper.Green, $"{(Plugin.Configuration.DealerDrawsAll ? "Dealer" : "Player")} must draw a card with /random 13 or /dice 13");
    }

    private void PlayerRoundPanel()
    {
        ImGui.TextColored(Helper.Yellow, $"Current Player: {Plugin.Blackjack.CurrentPlayer.DisplayName}");
        ImGui.Text("Player Options:");
        ImGuiComponents.HelpMarker(HelpText);

        if (ImGui.Button("Hit"))
        {
            Plugin.SwitchState(GameState.Hit);
            Plugin.Blackjack.PlayerAction();
        }

        if (ImGui.Button("Stay"))
            Plugin.Blackjack.Stay();

        if (ImGui.Button("Surrender"))
            Plugin.Blackjack.Surrender();

        if (ImGui.Button("Double Down"))
        {
            Plugin.SwitchState(GameState.DoubleDown);
            Plugin.Blackjack.PlayerAction();
        }

        if (Plugin.Blackjack.CurrentPlayer.CanSplit)
        {
            if (ImGui.Button("Split"))
                Plugin.Blackjack.Split();
        }
    }

    private void CardDeckRender()
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);

        if (ImGui.BeginTable("##BlackjackDeckTable", 5))
        {
            ImGui.TableSetupColumn("Name - (Click to Copy)", 0, 0.6f);
            ImGui.TableSetupColumn("Cards");
            ImGui.TableSetupColumn("Total", 0, 0.3f);
            ImGui.TableSetupColumn("Bet", 0, 0.4f);
            ImGui.TableSetupColumn("Last Action", 0, 0.3f);

            ImGui.TableHeadersRow();
            foreach (var player in Plugin.Blackjack.Players)
            {
                var cards = player.CalculateCardValues();

                // Feature by request so a player's hand can always be copied out
                ImGui.TableNextColumn();
                ImGui.PushStyleColor(ImGuiCol.Button, Helper.SoftBlue);
                var pFlavor = $"{player.DisplayName.Split("\uE05D ").First()}";
                if (ImGui.Button($"{pFlavor}##{player.DisplayName}"))
                {
                    var participantsString = $"{pFlavor}'s hand: {string.Join(" ", player.Cards.Select(Cards.ShowCardSimple))} -- Total: {cards}";
                    ImGui.SetClipboardText(participantsString);
                }
                ImGui.PopStyleColor();

                ImGui.TableNextColumn();
                ImGui.PushFont(Plugin.FontManager.SourceCode20);
                ImGui.Text(string.Join(" ", player.Cards.Select(Cards.ShowCardSimple)));
                ImGui.PopFont();

                ImGui.TableNextColumn();
                ImGui.Text($"{cards}");

                ImGui.TableNextColumn();
                ImGui.Text($"{player.Bet:N0}");

                ImGui.TableNextColumn();
                ImGui.Text(player.LastAction.Name());
            }

            ImGui.TableNextRow();
            ImGui.TableNextRow();

            var dcards = Plugin.Blackjack.Dealer.CalculateCardValues();

            ImGui.TableNextColumn();
            if (ImGui.Button($"Dealer"))
                ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", Plugin.Blackjack.Dealer.Cards.Select(Cards.ShowCardSimple))}");

            ImGui.TableNextColumn();
            ImGui.PushFont(Plugin.FontManager.SourceCode20);
            ImGui.Text(string.Join(" ", Plugin.Blackjack.Dealer.Cards.Select(Cards.ShowCardSimple)));
            ImGui.PopFont();

            ImGui.TableNextColumn();
            ImGui.Text($"{dcards}");

            ImGui.TableNextColumn();

            ImGui.TableNextColumn();
            ImGui.Text(Plugin.Blackjack.Dealer.LastAction.Name());

            ImGui.EndTable();
        }
    }

    private void MatchBeginDraw()
    {
        if (Plugin.Blackjack.CurrentPlayerIndex >= Plugin.Blackjack.Players.Count)
        {
            switch (Plugin.State)
            {
                case GameState.DrawFirstCards:
                    if (Plugin.Blackjack.Players.Last().Cards.Count >= 2)
                    {
                        Plugin.Blackjack.FinishDrawingRound(GameState.PrepareRound);
                        Plugin.Blackjack.PreparePlayers();
                        if (Plugin.Configuration.AutoOpenField)
                            Plugin.OpenCardField();
                        return;
                    }

                    Plugin.Blackjack.FinishDrawingRound(GameState.DrawSecondCards);
                    return;
                case GameState.DrawSecondCards:
                    Plugin.Blackjack.FinishDrawingRound(GameState.PrepareRound);
                    Plugin.Blackjack.PreparePlayers();
                    if (Plugin.Configuration.AutoOpenField)
                        Plugin.OpenCardField();
                    return;
            }
        }


        ImGui.TextWrapped($"To begin the round, players must draw their cards with either /random 13 or /dice 13 respectively.");
        if (Plugin.Configuration.DealerDrawsAll)
            ImGui.TextUnformatted($"Dealer draws all");

        ImGui.TextUnformatted("The draw order is:");
        foreach (var (player, idx) in Plugin.Blackjack.Players.Select((var, i) => (var, i)))
        {
            if (Plugin.Blackjack.CurrentPlayerIndex == idx)
                ImGui.TextColored(Helper.Green, $"{player.DisplayName}");
            else
                ImGui.TextUnformatted($"{player.DisplayName}");

            ImGui.PushFont(Plugin.FontManager.SourceCode20);
            ImGui.SameLine();
            ImGui.TextUnformatted($"{(player.Cards.Count > 0 ? Cards.ShowCardSimple(player.Cards[0]) : "?")} {(player.Cards.Count > 1 ? Cards.ShowCardSimple(player.Cards[1]) : "?")}");
            ImGui.PopFont();
        }
    }

    private void MatchBeginningPanel()
    {
        Plugin.Blackjack.StartRound();
        Plugin.Blackjack.PreparePlayers();
        if (Plugin.Configuration.AutoOpenField)
            Plugin.OpenCardField();
    }

    private void DealerStartingDraw()
    {
        switch (Plugin.State)
        {
            case GameState.DealerFirstCards when Plugin.Blackjack.Dealer.Cards.Any():
                Plugin.Blackjack.SetDrawingRound();
                break;
            case GameState.DealerSecondCards when Plugin.Blackjack.Dealer.Cards.Count == 2:
                Plugin.SwitchState(GameState.DealerRound);
                Plugin.Blackjack.CheckForRemainingPlayers();
                break;
        }

        ImGui.TextColored(Helper.Green, $"Waiting for dealer roll ...");
        ImGui.TextColored(Helper.Green, $"Dealer must draw a starting card with /random 13 or /dice 13");
    }

    private void BlackjackRegistrationPanel()
    {
        if (Plugin.Blackjack.Players.Any())
        {
            if (ImGui.Button("Close Registration"))
            {
                if (Plugin.Configuration.VenueDealer)
                    Plugin.SwitchState(GameState.DealerFirstCards);
                else
                    Plugin.Blackjack.SetDrawingRound();

                return;
            }
        }

        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TextColored(Helper.Green,$"Awaiting more players ...");
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.TextWrapped("Players can automatically enter by typing /random or /dice.");
        ImGui.TextWrapped("Alternatively players can be manually entered by targeting the character and pressing 'Add Target' below.");
        BlackjackAddTargetButton();
        TableBetRender();
    }

    private void BlackjackAddTargetButton()
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        if (ImGui.Button("Add Target"))
        {
            var result = BlackjackTargetRegistration();
            if (result != string.Empty)
                Plugin.PluginInterface.UiBuilder.AddNotification(result, "DeathRoll Helper", NotificationType.Error);
        }
        ImGuiHelpers.ScaledDummy(10.0f);
    }

    private string BlackjackTargetRegistration()
    {
        var name = Plugin.GetTargetName();
        if (name == string.Empty)
            return "Target not found";

        if (Plugin.Blackjack.Players.Exists(p => p.Name == name))
            return "Target already registered";

        Plugin.Blackjack.Players.Add(new BlackjackPlayer(name, Plugin.Configuration.DefaultBet));
        return string.Empty;
    }

    private void TableBetRender()
    {
        if (!Plugin.Blackjack.Players.Any())
            return;

        ImGui.TextColored(Helper.Yellow, "Player Bets:");
        if (ImGui.BeginTable("##BlackjackBetTable", 3))
        {
            ImGui.TableSetupColumn("##Name", ImGuiTableColumnFlags.None, 0.65f);
            ImGui.TableSetupColumn("##Number", ImGuiTableColumnFlags.None, 0.3f);
            ImGui.TableSetupColumn("##Input", ImGuiTableColumnFlags.None, 0.5f);

            foreach (var (player, idx) in Plugin.Blackjack.Players.Select((var, i) => (var, i)))
            {
                var currentBet = player.Bet;
                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Selectable($"{player.DisplayName}##Selectable{idx}");
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyShift)
                {
                    Plugin.Blackjack.Players.RemoveAt(idx);
                    break;
                }

                if (ImGui.IsItemHovered())
                    ImGui.SetTooltip("Hold Shift and right-click to delete.");

                ImGui.TableNextColumn();
                ImGui.AlignTextToFramePadding();
                ImGui.Text($"{currentBet:N0}");

                ImGui.TableNextColumn();
                ImGui.PushItemWidth(100);
                ImGui.InputInt($"##playerBet{idx}", ref currentBet, 0);

                if (currentBet == player.Bet)
                    continue;

                player.Bet = currentBet;
            }

            ImGui.EndTable();
        }
    }
}