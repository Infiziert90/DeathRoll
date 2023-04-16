using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using DeathRoll.Data;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui;

public static class ImGuiColors
{
    public static Vector4 CopyColor { get; internal set; } = new(0.031f, 0.376f, 0.768f, 1.0f); // A soft blue
}

public class BlackjackMode
{
    private readonly Vector4 _redColor = new(0.980f, 0.245f, 0.245f, 1.0f);
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private readonly Vector4 _yellowColor = new(0.959f, 1.0f, 0.0f, 1.0f);

    private string ErrorMsg = string.Empty;

    private const string HelpText = @"- Hit: Player draws a card,
- Stay: Player holds hand as it is
- Surrender: Player drops out of round and loses half the bet
- Double Down: Player bets double, but receives exactly one more card

- Split: Only possible at round start, and if the player has same Rank cards (e.g K and K)
Player opens a new hand with one card in each hand, puts another bet of same amount, and draws with both hands a card
Round continues as before, with the split hands turn happening later";

    private readonly Configuration configuration;
    private readonly PluginUI pluginUi;
    private readonly Participants participants;
    private readonly Blackjack blackjack;
    private readonly Plugin plugin;

    public BlackjackMode(Plugin plugin, PluginUI pluginUi)
    {
        this.plugin = plugin;
        this.pluginUi = pluginUi;
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
        blackjack = pluginUi.Rolls.Blackjack;
    }

    public void MainRender()
    {
        RenderControlPanel();
        ImGuiHelpers.ScaledDummy(new Vector2(5.0f));

        switch (Plugin.State)
        {
            case GameState.Registration:
                RegistrationPanel();
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
                DealerDoneRender();
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

    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) pluginUi.SettingsVisible = true;

        ImGui.SameLine();

        if (Plugin.State is not GameState.NotRunning and not GameState.Crash and not GameState.Registration)
            if (ImGui.Button($"{(!fieldVisible ? "Open" : "Close")} Game Field"))
                fieldVisible = !fieldVisible;

        var spacing = ImGui.GetScrollMaxY() == 0 ? 85.0f : 120.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        switch (Plugin.State)
        {
            case GameState.NotRunning:
                if (!ImGui.Button("Start Round")) return;
                participants.Reset();
                fieldVisible = false;
                Plugin.SwitchState(GameState.Registration);
                return;
            case GameState.Crash:
                if (!ImGui.Button("Force Stop Round")) return;
                participants.Reset();
                fieldVisible = false;
                Plugin.SwitchState(GameState.NotRunning);
                return;
            default:
                if (!ImGui.Button("Stop Round")) return;
                participants.Reset();
                fieldVisible = false;
                Plugin.SwitchState(GameState.NotRunning);
                return;
        }
    }

    public void MatchDonePanel()
    {
        ImGui.TextColored(_greenColor, $"Round finished.");
        ImGui.TextColored(_yellowColor, $"All bets are adjusted to the correct amount for payouts.");

        if (ImGui.Button("Play Again"))
        {
            blackjack.TakePeopleIntoNextRound();
            fieldVisible = false;
        }

        // Copy out all winnings to a single string to show all players the final standings (and leave a record if a player wants to play their push/winnings into the next round)
        var finalPayout = $"Payouts: [] ";
        if (ImGui.Button("Copy Payout"))
        {
            foreach (var (name, player) in participants.PlayerBets)
            {
                if (player.Bet < 1)
                { finalPayout += $"{name.Split()[0]} -> Lost! [] "; }
                else
                { finalPayout += $"{name.Split()[0]} -> {player.Bet:N0} [] "; }
            }
            ImGui.SetClipboardText(finalPayout);
        }

        // Let's allow 'Copy Dealer' everywhere so newbies can be shown the dealer's hand (especially if it resolves on the first two cards)
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = blackjack.CalculatePlayerCardValues(participants.DealerCards);
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}");
        }
    }

    public void DealerDoneRender()
    {
        ImGui.TextColored(_yellowColor, $"The dealer is not allowed to hit anymore!");
        ImGui.TextColored(_yellowColor, $"Please proceed by pressing 'Calculate Winnings' below.");
        if (ImGui.Button("Calculate Winnings")) { blackjack.EndMatch(); }
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = blackjack.CalculatePlayerCardValues(participants.DealerCards);
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}");
        }
    }

    public void DealerDrawRender()
    {
        if (!blackjack.DealerCheckHand())
        {
            blackjack.DealerRound();
            if (Plugin.State == GameState.Done) return;

            participants.DealerAction = "End";
            Plugin.SwitchState(GameState.DealerDone);
            return;
        }

        ImGui.TextColored(_greenColor, $"Waiting for dealer roll ...");
        ImGui.TextColored(_greenColor, $"Dealer must draw a card with either /random 13 or /dice 13 respectively.");

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = blackjack.CalculatePlayerCardValues(participants.DealerCards);
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}");
        }
    }

    public void DealerRoundPanel()
    {
        ImGui.TextColored(_yellowColor, $"All players done!");
        if (ImGui.Button("Begin Dealer Round")) { blackjack.DealerAction(); }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        if (ImGui.Button("Copy Dealer"))
        {
            var cards = blackjack.CalculatePlayerCardValues(participants.DealerCards);
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}");
        }
    }

    public void WaitForRollPanel()
    {
        ImGui.TextColored(_yellowColor, $"Current Player: {participants.GetParticipant().GetDisplayName()}");
        ImGui.TextColored(_greenColor, $"Waiting for player roll ...");
        ImGui.TextColored(_greenColor, $"Player must draw a card with either /random 13 or /dice 13 respectively.");

        if (Plugin.State != GameState.DrawSplit) return;
        ImGui.TextColored(_greenColor, $"Player has drawn: {participants.SplitDraw.Count} out of 2 cards");

        if (participants.SplitDraw.Count != 2) return;
        blackjack.Split();
    }

    public void PlayerRoundPanel()
    {
        ImGui.TextColored(_yellowColor, $"Current Player: {participants.GetParticipant().GetDisplayName()}");
        ImGui.Text("Player Options:");
        ImGui.SameLine();
        Helper.ShowHelpMarker(HelpText);
        if (ImGui.Button("Hit")) { Plugin.SwitchState(GameState.Hit); blackjack.PlayerAction(); }
        if (ImGui.Button("Stay")) { blackjack.Stay(); }
        if (ImGui.Button("Surrender")) { blackjack.Surrender(); }
        if (ImGui.Button("Double Down")) { Plugin.SwitchState(GameState.DoubleDown); blackjack.PlayerAction(); }

        if (participants.GetParticipant().CanSplit)
        {
            if (ImGui.Button("Split"))
            {
                blackjack.Split();
            }
        }
    }

    public void CardDeckRender()
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5.0f);

        if (!ImGui.BeginTable("##blackjackdeck", 5)) return;
        ImGui.TableSetupColumn("Name - (Click to Copy)", 0, 0.6f);
        ImGui.TableSetupColumn("Cards");
        ImGui.TableSetupColumn("Total", 0, 0.3f);
        ImGui.TableSetupColumn("Bet", 0, 0.4f);
        ImGui.TableSetupColumn("Last Action", 0, 0.3f);

        ImGui.TableHeadersRow();
        foreach (var player in participants.PlayerNameList.Select(value => value))
        {
            var p = participants.FindAll(player);
            var cards = blackjack.CalculatePlayerCardValues(p);

            // Feature by request so a player's hand can always be copied out
            ImGui.TableNextColumn();
            ImGui.PushStyleColor(ImGuiCol.Button, ImGuiColors.CopyColor);
            var pFlavor = $"{p[0].GetDisplayName().Split("\uE05D ").First()}";
            if (ImGui.Button($"{pFlavor}##{p[0].GetDisplayName()}"))
            {
                var participantsString = $"{pFlavor}'s hand: {string.Join(" ", p.Select(x => Cards.ShowCardSimple(x.Card)))} -- Total: {cards}";
                ImGui.SetClipboardText(participantsString);
            }
            ImGui.PopStyleColor();

            ImGui.TableNextColumn();
            ImGui.PushFont(plugin.FontManager.Font2);
            ImGui.Text(string.Join(" ", p.Select(x => Cards.ShowCardSimple(x.Card))));
            ImGui.PopFont();

            ImGui.TableNextColumn();
            ImGui.Text($"{cards}");

            ImGui.TableNextColumn();
            ImGui.Text($"{participants.PlayerBets[player].Bet:N0}");

            ImGui.TableNextColumn();
            ImGui.Text(participants.PlayerBets[player].LastAction);
        }

        ImGui.TableNextRow();
        ImGui.TableNextRow();

        var dcards = blackjack.CalculatePlayerCardValues(participants.DealerCards);

        ImGui.TableNextColumn();
        if (ImGui.Button($"Dealer"))
        {
            ImGui.SetClipboardText($"Dealer's Hand: {string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card)))}");
        }

        ImGui.TableNextColumn();
        ImGui.PushFont(plugin.FontManager.Font2);
        ImGui.Text(string.Join(" ", participants.DealerCards.Select(x => Cards.ShowCardSimple(x.Card))));
        ImGui.PopFont();

        ImGui.TableNextColumn();
        ImGui.Text($"{dcards}");

        ImGui.TableNextColumn();

        ImGui.TableNextColumn();
        ImGui.Text(participants.DealerAction);

        ImGui.EndTable();

    }

    public void MatchBeginDraw()
    {
        if (participants.GetCurrentIndex() >= participants.PlayerNameList.Count)
        {
            switch (Plugin.State)
            {
                case GameState.DrawFirstCards:
                    blackjack.FinishDrawingRound(GameState.DrawSecondCards);
                    return;
                case GameState.DrawSecondCards:
                    blackjack.FinishDrawingRound(GameState.PrepareRound);
                    blackjack.PreparePlayers();
                    if (configuration.AutoOpenField) fieldVisible = true;
                    return;
            }
        }

        var turn = Plugin.State == GameState.DrawFirstCards ? "first" : "second";
        ImGui.TextWrapped($"To begin the round, players must draw their {turn} card with either /random 13 or /dice 13 respectively.");
        ImGui.Text("The draw order is:");
        foreach (var (name, i) in participants.PlayerNameList.Select((value, i) => (value, i)))
        {
            if (participants.GetCurrentIndex() == i)
            {
                ImGui.TextColored(_greenColor, $"{participants.FindPlayer(name).GetDisplayName()}");
            }
            else
            {
                ImGui.Text($"{participants.FindPlayer(name).GetDisplayName()}");
            }
        }
    }

    public void MatchBeginningPanel()
    {
        blackjack.StartRound();
        blackjack.PreparePlayers();
        if (configuration.AutoOpenField) fieldVisible = true;
    }

    public void DealerStartingDraw()
    {
        switch (Plugin.State)
        {
            case GameState.DealerFirstCards when participants.DealerCards.Any():
                blackjack.SetDrawingRound();
                break;
            case GameState.DealerSecondCards when participants.DealerCards.Count == 2:
                Plugin.SwitchState(GameState.DealerRound);
                blackjack.CheckForRemainingPlayers();
                break;
        }

        ImGui.TextColored(_greenColor, $"Waiting for dealer roll ...");
        ImGui.TextColored(_greenColor, $"Dealer must draw a card with either /random 13 or /dice 13 respectively.");
    }

    public void RegistrationPanel()
    {
        if (ErrorMsg != string.Empty) { Helper.ErrorWindow(ref ErrorMsg); }

        if (participants.PlayerNameList.Any())
        {
            if (ImGui.Button("Close Registration"))
            {
                if (configuration.VenueDealer)
                {
                    Plugin.SwitchState(GameState.DealerFirstCards);
                }
                else
                {
                    blackjack.SetDrawingRound();
                }
                return;
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ImGui.TextColored(_greenColor,$"Awaiting more players ...");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.TextWrapped("Players can automatically enter by typing /random or /dice respectively while round registration is active.");
        ImGui.TextWrapped("Alternatively players can be manually entered by targetting the character and pressing 'Add Target' below.");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        if (ImGui.Button("Add Target")) { ErrorMsg = blackjack.TargetRegistration(); }
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        TableBetRender();
    }

    public void TableBetRender()
    {
        if (!participants.PlayerBets.Any()) return;

        ImGui.TextColored(_yellowColor, "Player Bets:");
        if (!ImGui.BeginTable("##bj_table", 3, ImGuiTableFlags.None))
            return;

        ImGui.TableSetupColumn("##bj_name", ImGuiTableColumnFlags.None, 0.65f);
        ImGui.TableSetupColumn("##bj_formatted", ImGuiTableColumnFlags.None, 0.3f);
        ImGui.TableSetupColumn("##bj_bet", ImGuiTableColumnFlags.None, 0.5f);

        var updateName = string.Empty;
        var newBet = -1;
        foreach (var (name, player) in participants.PlayerBets)
        {
            var currentBet = player.Bet;
            var participant = participants.FindPlayer(name);

            ImGui.TableNextColumn();
            if (Helper.SelectableDelete(participant, participants))
                break; // break because we deleted an entry

            ImGui.TableNextColumn();
            ImGui.Text($"{currentBet:N0}");

            ImGui.TableNextColumn();
            var p = ImGui.GetCursorPos();
            ImGui.SetCursorPos(new Vector2(p.X, p.Y-3));
            ImGui.PushItemWidth(100);
            ImGui.InputInt($"##playerBet{name}", ref currentBet, 0);

            if (currentBet == player.Bet) continue;
            updateName = name;
            newBet = currentBet;
        }

        if (updateName != string.Empty)
        {
            participants.PlayerBets[updateName].Bet = newBet;
        }

        ImGui.EndTable();
    }

    public void GameCardRender(Cards.Card card)
    {
        var s = Cards.ShowCard(card);
        ImGui.PushFont(plugin.FontManager.Font);
        ImGui.Text(s[0]);
        ImGui.PopFont();

        ImGui.SameLine();

        var p = ImGui.GetCursorPos();
        ImGui.SetCursorPos(new Vector2(p.X-70, p.Y+100));
        ImGui.PushFont(plugin.FontManager.Font1);
        ImGui.Text(s[1]);
        ImGui.PopFont();
    }

    private bool fieldVisible = false;
    public void DrawGameField()
    {
        if (!fieldVisible) return;
        if (Plugin.State is GameState.NotRunning or GameState.Crash) return;
        if (participants.PList.Count == 0) return;

        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(600, 600), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("DeathRoll Blackjack Visual", ref fieldVisible))
        {
            ImGui.Text($"Dealer: ");
            var orgCursor = ImGui.GetCursorPos();
            foreach (var card in participants.DealerCards.Select(x => x.Card))
            {
                var cursor = ImGui.GetCursorPos();
                GameCardRender(card);
                ImGui.SetCursorPos(new Vector2(cursor.X + 110, cursor.Y));
            }

            var currentX = orgCursor.X;
            foreach (var name in participants.PlayerNameList)
            {
                if (participants.PlayerNameList.First() != name)
                    currentX += 30;
                ImGui.SetCursorPos(new Vector2(currentX, orgCursor.Y + 250));
                ImGui.Text($"{participants.FindPlayer(name).GetDisplayName()}: ");
                foreach (var card in participants.FindAll(name).Select(x => x.Card))
                {
                    ImGui.SetCursorPos(new Vector2(currentX, orgCursor.Y + 280));
                    GameCardRender(card);
                    currentX += 110;
                }
            }
        }
        ImGui.End();
    }
}