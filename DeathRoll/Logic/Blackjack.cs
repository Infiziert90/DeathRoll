using System;
using System.Collections.Generic;
using System.Linq;
using DeathRoll.Data;

namespace DeathRoll.Logic;

public class Blackjack
{
    private readonly Configuration configuration;
    private readonly Participants participants;

    public Blackjack(Configuration configuration, Participants participants)
    {
        this.configuration = configuration;
        this.participants = participants;
    }

    public void Parser(Roll roll)
    {
        switch (Plugin.State)
        {
            case GameState.Registration:
                Registration(roll);
                break;
            case GameState.Hit or GameState.DoubleDown or GameState.DrawSplit:
                RollParse(roll);
                break;
            case GameState.DrawFirstCards or GameState.DrawSecondCards:
                ParseFirstCards(roll);
                break;
            case GameState.DealerFirstCards or GameState.DealerSecondCards or GameState.DrawDealerCard:
                ParseDealerCards(roll);
                break;
            default:
                return;
        }
    }

    public void DealerAction()
    {
        if (!configuration.AutoDrawDealer)
        {
            Plugin.SwitchState(GameState.DrawDealerCard);
            return;
        }
        while (DealerCheckHand()) { GiveDealerCard(false); }
        DealerRound();
        if (Plugin.State != GameState.Done) Plugin.SwitchState(GameState.DealerDone);
    }

    public void PlayerAction()
    {
        // if (!participants.GetParticipant().Name.EndsWith(" Split"))
        // {
        //     if (!configuration.AutoDrawCard)
        //         return;
        // }

        if (!configuration.AutoDrawCard)
            return;

        var card = DrawCard();
        switch (Plugin.State)
        {
            case GameState.Hit:
                Hit(card);
                break;
            case GameState.DoubleDown:
                DoubleDown(card);
                break;
            default:
                return;
        }
    }

    private void RollParse(Roll roll)
    {
        // check if roll is out of 13 (13 cards) and check if current player has rolled
        if (roll.OutOf != 13)
            return;

        // Check if 'dealer draws all cards' is enabled. If so, spaghetti swap out the player var so it doesn't try to insert the dealer as a player
        if (!configuration.DealerDrawsAll)
        {
            var checkableName = participants.GetParticipantName();
            if (checkableName.EndsWith(" Split"))
                checkableName = checkableName.Split(" Split").First();

            if (checkableName != roll.PlayerName)
                return;
        }
        else
        {
            if (Plugin.LocalPlayer != roll.PlayerName)
                return;
        }

        // make sure that Participant has correct name
        // TODO Check if this is needed at all
        roll.PlayerName = participants.GetParticipantName();

        var card = new Cards.Card(roll.Rolled, DrawCard().Suit);
        switch (Plugin.State)
        {
            case GameState.Hit:
                Hit(card);
                return;
            case GameState.DoubleDown:
                DoubleDown(card);
                return;
            case GameState.DrawSplit:
                participants.SplitDraw.Add(card);
                return;
        }
    }

    private void ParseFirstCards(Roll roll)
    {
        if (roll.OutOf != 13) return;
        if (!configuration.DealerDrawsAll) { if (participants.GetParticipantName() != roll.PlayerName) return; }
        else
        {
            if (Plugin.LocalPlayer != roll.PlayerName) return;
            roll.PlayerName = participants.GetParticipantName();
        }

        participants.Add(new Participant(roll.PlayerName, new Cards.Card(roll.Rolled, DrawCard().Suit)));
        participants.NextParticipant();
    }

    private void ParseDealerCards(Roll roll)
    {
        if (roll.OutOf != 13) return;
        if (Plugin.LocalPlayer != roll.PlayerName) return;

        participants.DealerCards.Add(new Participant("", new Cards.Card(roll.Rolled, DrawCard().Suit)));
    }

    public void TakePeopleIntoNextRound()
    {
        var last = new List<string>(participants.PlayerNameList);
        participants.Reset();

        foreach (var name in last.Where(name => !name.EndsWith(" Split")))
        {
            participants.Add(new Participant(Roll.Dummy(name)));
            participants.PlayerBets[name] = new Participants.Player(configuration.DefaultBet, true);
        }
        Plugin.SwitchState(GameState.Registration);
    }

    public void EndMatch()
    {
        var dealerCards = CalculatePlayerCardValues(participants.DealerCards);
        foreach (var (name, player) in participants.PlayerBets)
        {
            if (!player.IsAlive) continue;

            var currentPlayer = participants.FindAll(name);
            var cards = CalculatePlayerCardValues(currentPlayer);

            if (cards != dealerCards)
            {
                player.Bet *= cards > dealerCards ? 2 : -1;
            }
        }
        Plugin.SwitchState(GameState.Done);
    }

    public bool DealerCheckHand()
    {
        var cards = CalculatePlayerCardValues(participants.DealerCards);
        var hasAce = participants.DealerCards.Any(x => x.Card.IsAce);
        var check = configuration.DealerRule switch
        {
            DealerRules.DealerHard17 => cards < 17,
            DealerRules.DealerHard16 => cards < 16,
            DealerRules.DealerSoft17 => !hasAce ? cards < 17: HasDealerSoftHand() <= 17 || cards < 17,
            DealerRules.DealerSoft16 => !hasAce ? cards < 16: HasDealerSoftHand() <= 16 || cards < 16,
            _ => cards < 16
        };

        return check;
    }

    public void DealerBust()
    {
        participants.DealerAction = "Bust";

        foreach (var player in participants.PlayerBets.Values.Where(x => x.IsAlive))
        {
            player.Bet *= 2;
            player.IsAlive = false;
        }
        Plugin.SwitchState(GameState.Done);
    }

    public void DealerBlackjack()
    {
        participants.DealerAction = "Blackjack";

        foreach (var player in participants.PlayerBets.Values.Where(x => x.IsAlive))
        {
            player.Bet *= -1;
            player.IsAlive = false;
        }
        Plugin.SwitchState(GameState.Done);
    }

    public void DealerRound()
    {
        var cards = CalculatePlayerCardValues(participants.DealerCards);
        switch (cards)
        {
            case > 21:
                DealerBust();
                break;
            case 21:
                DealerBlackjack();
                break;
        }
    }

    public void CheckForRemainingPlayers()
    {
        if (!participants.PlayerBets.Values.Any(x => x.IsAlive))
        {
            Plugin.SwitchState(GameState.Done);
            return;
        }

        DealerRound();
    }

    public void Hit(Cards.Card card)
    {
        var currentPlayer = participants.GetParticipant().Name;
        participants.SetLastPlayerAction("Hit");

        participants.Add(new Participant(currentPlayer, card));
        if (!CheckPlayerCards())
        {
            NextPlayer();
            return;
        }
        Plugin.SwitchState(GameState.PlayerRound);
    }

    public void DoubleDown(Cards.Card card)
    {
        var currentPlayer = participants.GetParticipant().Name;
        participants.PlayerBets[currentPlayer].Bet *= 2;
        participants.SetLastPlayerAction("Double Down");

        participants.Add(new Participant(currentPlayer, card));
        CheckPlayerCards();
        NextPlayer();
    }

    public void Split()
    {
        if (!configuration.VenueDealer && configuration.AutoDrawCard)
        {
            participants.SplitDraw.Add(DrawCard());
            participants.SplitDraw.Add(DrawCard());
        }

        if (participants.SplitDraw.Count != 2)
        {
            Plugin.SwitchState(GameState.DrawSplit);
            return;
        }

        var cards = participants.FindAllWithIndex();
        var currentPlayer = participants.GetParticipant().Name;
        var splitName = $"{currentPlayer} Split";

        var card1 = participants.SplitDraw[0];
        var card2 = participants.SplitDraw[1];

        var tmp = new Participant(splitName, cards[1].Card);
        participants.PList[participants.GetCurrentIndex() + participants.PlayerNameList.Count] = tmp;
        cards[1] = tmp;

        participants.Add(new Participant(currentPlayer, card1));

        participants.PlayerNameList.Add(splitName);
        participants.PList.Insert(participants.PlayerNameList.Count-1, new Participant(splitName, card2));

        var player = participants.PlayerBets[currentPlayer];
        player.LastAction = "Split";

        participants.PlayerBets[splitName] = new Participants.Player(player.Bet, true);

        participants.GetParticipant().CanSplit = false;
        participants.SplitDraw.Clear();
        Plugin.SwitchState(GameState.PlayerRound);
    }

    public void Stay()
    {
        participants.SetLastPlayerAction("Stay");

        NextPlayer();
    }

    public void Surrender()
    {
        var currentPlayer = participants.GetParticipant().Name;
        var player = participants.PlayerBets[currentPlayer];
        player.Bet = (player.Bet / 2) * -1;
        player.IsAlive = false;
        player.LastAction = "Surrender";

        NextPlayer();
    }

    public void NextPlayer()
    {
        participants.NextParticipant();

        if (participants.HasMoreParticipants())
        {
            Plugin.SwitchState(GameState.PlayerRound);
            if (CheckPlayerCards()) return;
            NextPlayer();
            return;
        }

        if (configuration.VenueDealer)
        {
            Plugin.SwitchState(GameState.DealerSecondCards);
            return;
        }

        participants.DealerCards[0].Card.IsHidden = false;
        Plugin.SwitchState(GameState.DealerRound);
        CheckForRemainingPlayers();
    }

    public void FirstPlayer()
    {
        if (!CheckPlayerCards())
        {
            NextPlayer();
            return;
        }
        Plugin.SwitchState(GameState.PlayerRound);
    }

    public bool CheckPlayerCards()
    {
        var currentPlayer = participants.FindAll(participants.GetParticipant().Name);
        var cards = CalculatePlayerCardValues(currentPlayer);

        var player = participants.PlayerBets[currentPlayer[0].Name];
        switch (cards)
        {
            case > 21:
                player.Bet *= -1;
                player.IsAlive = false;
                player.LastAction = "Bust";
                return false;
            case 21:
                player.Bet += (int) (player.Bet * ((float) 3 / 2));
                player.IsAlive = false;
                player.LastAction = "Blackjack";
                return false;
        }
        return true;
    }

    public void FinishDrawingRound(GameState state)
    {
        participants.ResetParticipant();

        Plugin.SwitchState(state);
    }

    public void SetDrawingRound()
    {
        if (!configuration.VenueDealer) GiveDealerCard(true);

        if (configuration.AutoDrawOpening)
        {
            Plugin.SwitchState(GameState.PrepareRound);
            return;
        }

        Plugin.SwitchState(participants.FindAllWithIndex().Count == 1 ? GameState.DrawFirstCards : GameState.DrawSecondCards);
    }

    public void PreparePlayers()
    {
        if (!configuration.VenueDealer) GiveDealerCard(false);
        participants.DeleteRangeFromStart(participants.PlayerNameList.Count);
        CheckIfPlayersCanSplits();
        FirstPlayer();
    }

    public void StartRound()
    {
        GiveEachPlayerOneCard();
        GiveEachPlayerOneCard();

        participants.ResetParticipant();
    }

    public string TargetRegistration()
    {
        // get target
        var name = Plugin.GetTargetName();
        if (name == string.Empty) return "Target not found.";

        // check if registration roll is correct or if player is already in list
        if (participants.PlayerNameList.Exists(x => x == name)) return "Target already registered.";

        participants.Add(new Participant(Roll.Dummy(name)));
        participants.PlayerBets[name] = new Participants.Player(configuration.DefaultBet, true);
        return string.Empty;
    }

    public void Registration(Roll roll)
    {
        // check if registration roll is correct or if player is already in list
        if (roll.OutOf != -1) return;
        if (participants.PlayerNameList.Exists(x => x == roll.PlayerName)) return;

        participants.Add(new Participant(roll));
        participants.PlayerBets[roll.PlayerName] = new Participants.Player(configuration.DefaultBet, true);
    }

    // internal game mechanics
    private Random rng = new Random(unchecked(Environment.TickCount * 31));
    public Cards.Card DrawCard()
    {
        return new Cards.Card(rng.Next(1, 14), rng.Next(0, 4), false);
    }

    public void CheckIfPlayersCanSplits()
    {
        foreach (var cards in participants.PlayerNameList.Select(player =>
                     participants.FindAll(player)).Where(cards => cards[0].Card.Rank == cards[1].Card.Rank))
        {
            cards[0].CanSplit = true;
        }
    }

    public void GiveEachPlayerOneCard()
    {
        foreach (var player in participants.PlayerNameList)
        {
            var card = DrawCard();
            participants.Add(new Participant(player, card));
        }
    }

    public void GiveDealerCard(bool isHidden)
    {
        var card = DrawCard();
        card.IsHidden = isHidden;
        participants.DealerCards.Add(new Participant("", card));

    }

    public int CalculatePlayerCardValues(List<Participant> playersHand)
    {
        var cards = 0;
        var hasAce = false;
        foreach (var card in playersHand.Select(x => x.Card))
        {
            cards += card.Value;
            if (card.IsAce) hasAce = true;
        }

        return !hasAce ? cards : cards - 1 < 11 ? cards + 10 : cards;
    }

    public int HasDealerSoftHand()
    {
        return participants.DealerCards.Select(x => x.Card).Sum(card => card.Value) + 10;
    }
}

public enum DealerRules
{
    DealerHard17 = 0,
    DealerSoft17 = 1,
    DealerHard16 = 2,
    DealerSoft16 = 3
}