using Dalamud.Interface.Components;
using DeathRoll.Data;
using DeathRoll.Logic;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private readonly Blackjack Backend;
    private const string HelpText = "- Hit: Player draws a card," +
                                    "\n- Stay: Player holds hand as it is" +
                                    "\n- Surrender: Player drops out of round and loses half the bet" +
                                    "\n- Double Down: Player bets double, but receives exactly one more card" +
                                    "\n- Split: Only possible at round start, and if the player has same Rank cards (e.g K and K)" +
                                    "\n    > Player opens a new hand with one card in each hand, puts another bet of same amount, and draws with both hands a card" +
                                    "\n    > Round continues as before, with the split hands turn happening later";

    private void Blackjack()
    {
        BlackjackControlPanel();
        ImGuiHelpers.ScaledDummy(new Vector2(5.0f));

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
            case GameState.Hit or GameState.DoubleDown or GameState.DrawSplit:
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
                Plugin.Participants.Reset();
                Plugin.ClosePlayWindows();
                Plugin.SwitchState(GameState.Registration);
                return;
            case GameState.Crash:
                if (!ImGui.Button("Force Stop Round"))
                    return;
                Plugin.Participants.Reset();
                Plugin.ClosePlayWindows();
                Plugin.SwitchState(GameState.NotRunning);
                return;
            default:
                if (!ImGui.Button("Stop Round"))
                    return;
                Plugin.Participants.Reset();
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
            Backend.TakePeopleIntoNextRound();
        }

        // Copy out all winnings to a single string to show all players the final standings (and leave a record if a player wants to play their push/winnings into the next round)
        if (ImGui.Button("Copy Payout"))
        {
            var finalPayout = "Payouts: | ";
            foreach (var (name, player) in Plugin.Participants.PlayerBets)
                finalPayout += $"{name.Split()[0]} -> {(player.Bet < 1 ? "Lost!" : $"{player.Bet:N0}")} | ";
            ImGui.SetClipboardText(finalPayout);
        }

        // Let's allow 'Copy Dealer' everywhere so newbies can be shown the dealer's hand (especially if it resolves on the first two cards)
        DealerCardButton();
    }

    private void DealerDonePanel()
    {
        ImGui.TextColored(Helper.Yellow, $"The dealer is not allowed to hit anymore!");
        ImGui.TextColored(Helper.Yellow, $"Please proceed by pressing 'Calculate Winnings' below.");
        if (ImGui.Button("Calculate Winnings"))
            Backend.EndMatch();

        DealerCardButton();
    }

    private void DealerCardButton()
    {
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = Logic.Blackjack.CalculateCardValues(Plugin.Participants.DealerCards);
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", Plugin.Participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}");
        }
    }

    private void DealerDrawRender()
    {
        if (!Backend.DealerCheckHand())
        {
            Backend.DealerRound();
            if (Plugin.State == GameState.Done)
                return;

            Plugin.Participants.DealerAction = "End";
            Plugin.SwitchState(GameState.DealerDone);
            return;
        }

        ImGui.TextColored(Helper.Green, $"Waiting for dealer roll ...");
        ImGui.TextColored(Helper.Green, $"Dealer must draw a card with either /random 13 or /dice 13 respectively.");

        ImGuiHelpers.ScaledDummy(5.0f);
        DealerCardButton();
    }

    private void DealerRoundPanel()
    {
        ImGui.TextColored(Helper.Yellow, $"All players done!");
        if (ImGui.Button("Begin Dealer Round"))
            Backend.DealerAction();

        ImGuiHelpers.ScaledDummy(5.0f);
        DealerCardButton();
    }

    private void WaitForRollPanel()
    {
        ImGui.TextColored(Helper.Yellow, $"Current Player: {Plugin.Participants.GetParticipant().GetDisplayName()}");
        ImGui.TextColored(Helper.Green, $"Waiting for player roll ...");
        ImGui.TextColored(Helper.Green, $"Player must draw a card with either /random 13 or /dice 13 respectively.");

        if (Plugin.State != GameState.DrawSplit)
            return;

        ImGui.TextColored(Helper.Green, $"Player has drawn: {Plugin.Participants.SplitDraw.Count} out of 2 cards");

        if (Plugin.Participants.SplitDraw.Count != 2)
            return;

        Backend.Split();
    }

    private void PlayerRoundPanel()
    {
        ImGui.TextColored(Helper.Yellow, $"Current Player: {Plugin.Participants.GetParticipant().GetDisplayName()}");
        ImGui.Text("Player Options:");
        ImGuiComponents.HelpMarker(HelpText);

        if (ImGui.Button("Hit"))
            Plugin.SwitchState(GameState.Hit); Backend.PlayerAction();

        if (ImGui.Button("Stay"))
            Backend.Stay();

        if (ImGui.Button("Surrender"))
            Backend.Surrender();

        if (ImGui.Button("Double Down"))
            Plugin.SwitchState(GameState.DoubleDown); Backend.PlayerAction();

        if (!Plugin.Participants.GetParticipant().CanSplit)
            return;

        if (ImGui.Button("Split"))
            Backend.Split();
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
            foreach (var player in Plugin.Participants.PlayerNameList.Select(value => value))
            {
                var p = Plugin.Participants.FindAll(player);
                var cards = Logic.Blackjack.CalculateCardValues(p);

                // Feature by request so a player's hand can always be copied out
                ImGui.TableNextColumn();
                ImGui.PushStyleColor(ImGuiCol.Button, Helper.SoftBlue);
                var pFlavor = $"{p[0].GetDisplayName().Split("\uE05D ").First()}";
                if (ImGui.Button($"{pFlavor}##{p[0].GetDisplayName()}"))
                {
                    var participantsString = $"{pFlavor}'s hand: {string.Join(" ", p.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}";
                    ImGui.SetClipboardText(participantsString);
                }
                ImGui.PopStyleColor();

                ImGui.TableNextColumn();
                ImGui.PushFont(Plugin.FontManager.Font2);
                ImGui.Text(string.Join(" ", p.Select(x => Cards.ShowCardSimple(x.Card))));
                ImGui.PopFont();

                ImGui.TableNextColumn();
                ImGui.Text($"{cards}");

                ImGui.TableNextColumn();
                ImGui.Text($"{Plugin.Participants.PlayerBets[player].Bet:N0}");

                ImGui.TableNextColumn();
                ImGui.Text(Plugin.Participants.PlayerBets[player].LastAction);
            }

            ImGui.TableNextRow();
            ImGui.TableNextRow();

            var dcards = Logic.Blackjack.CalculateCardValues(Plugin.Participants.DealerCards);

            ImGui.TableNextColumn();
            if (ImGui.Button($"Dealer"))
            {
                ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", Plugin.Participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))}");
            }

            ImGui.TableNextColumn();
            ImGui.PushFont(Plugin.FontManager.Font2);
            ImGui.Text(string.Join(" ", Plugin.Participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card))));
            ImGui.PopFont();

            ImGui.TableNextColumn();
            ImGui.Text($"{dcards}");

            ImGui.TableNextColumn();

            ImGui.TableNextColumn();
            ImGui.Text(Plugin.Participants.DealerAction);

            ImGui.EndTable();
        }
    }

    private void MatchBeginDraw()
    {
        var currentPlayer = Plugin.Participants.GetCurrentIndex();
        if (currentPlayer >= Plugin.Participants.PlayerNameList.Count)
        {
            switch (Plugin.State)
            {
                case GameState.DrawFirstCards:
                    Backend.FinishDrawingRound(GameState.DrawSecondCards);
                    return;
                case GameState.DrawSecondCards:
                    Backend.FinishDrawingRound(GameState.PrepareRound);
                    Backend.PreparePlayers();
                    if (Plugin.Configuration.AutoOpenField)
                        Plugin.OpenCardField();
                    return;
            }
        }

        ImGui.TextWrapped($"To begin the round, players must draw their cards with either /random 13 or /dice 13 respectively.");
        ImGui.Text("The draw order is:");
        foreach (var (name, i) in Plugin.Participants.PlayerNameList.Select((value, i) => (value, i)))
        {
            if (currentPlayer == i)
                ImGui.TextColored(Helper.Green, $"{Plugin.Participants.FindPlayer(name).GetDisplayName()}");
            else
                ImGui.Text($"{Plugin.Participants.FindPlayer(name).GetDisplayName()}");

            var playerCards = Plugin.Participants.FindAll(name);
            ImGui.PushFont(Plugin.FontManager.Font2);
            ImGui.SameLine();
            ImGui.Text($"{(playerCards.Count > 0 ? Cards.ShowCardSimple(playerCards[0].Card) : "?")} {(playerCards.Count > 1 ? Cards.ShowCardSimple(playerCards[1].Card) : "?")}");
            ImGui.PopFont();
        }
    }

    private void MatchBeginningPanel()
    {
        Backend.StartRound();
        Backend.PreparePlayers();
        if (Plugin.Configuration.AutoOpenField)
            Plugin.OpenCardField();
    }

    private void DealerStartingDraw()
    {
        switch (Plugin.State)
        {
            case GameState.DealerFirstCards when Plugin.Participants.DealerCards.Any():
                Backend.SetDrawingRound();
                break;
            case GameState.DealerSecondCards when Plugin.Participants.DealerCards.Count == 2:
                Plugin.SwitchState(GameState.DealerRound);
                Backend.CheckForRemainingPlayers();
                break;
        }

        ImGui.TextColored(Helper.Green, $"Waiting for dealer roll ...");
        ImGui.TextColored(Helper.Green, $"Dealer must draw a card with either /random 13 or /dice 13 respectively.");
    }

    private void BlackjackRegistrationPanel()
    {
        if (Plugin.Participants.PlayerNameList.Any())
        {
            if (ImGui.Button("Close Registration"))
            {
                if (Plugin.Configuration.VenueDealer)
                    Plugin.SwitchState(GameState.DealerFirstCards);
                else
                    Backend.SetDrawingRound();

                return;
            }
        }

        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TextColored(Helper.Green,$"Awaiting more players ...");
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.TextWrapped("Players can automatically enter by typing /random or /dice respectively while round registration is active.");
        ImGui.TextWrapped("Alternatively players can be manually entered by targetting the character and pressing 'Add Target' below.");
        AddTargetButton();
        TableBetRender();
    }

    private void TableBetRender()
    {
        if (!Plugin.Participants.PlayerBets.Any())
            return;

        ImGui.TextColored(Helper.Yellow, "Player Bets:");
        if (ImGui.BeginTable("##BlackjackBetTable", 3))
        {
            ImGui.TableSetupColumn("##bj_name", ImGuiTableColumnFlags.None, 0.65f);
            ImGui.TableSetupColumn("##bj_formatted", ImGuiTableColumnFlags.None, 0.3f);
            ImGui.TableSetupColumn("##bj_bet", ImGuiTableColumnFlags.None, 0.5f);

            var newBet = -1;
            var updateName = string.Empty;
            foreach (var (name, player) in Plugin.Participants.PlayerBets)
            {
                var currentBet = player.Bet;
                var participant = Plugin.Participants.FindPlayer(name);

                ImGui.TableNextColumn();
                if (Helper.SelectableDelete(participant, Plugin.Participants))
                    break; // break because we deleted an entry

                ImGui.TableNextColumn();
                ImGui.Text($"{currentBet:N0}");

                ImGui.TableNextColumn();
                var p = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new Vector2(p.X, p.Y-3));
                ImGui.PushItemWidth(100);
                ImGui.InputInt($"##playerBet{name}", ref currentBet, 0);

                if (currentBet != player.Bet)
                {
                    newBet = currentBet;
                    updateName = name;
                }
            }

            if (updateName != string.Empty)
                Plugin.Participants.PlayerBets[updateName].Bet = newBet;

            ImGui.EndTable();
        }
    }
}