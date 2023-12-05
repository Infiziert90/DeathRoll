using DeathRoll.Logic;
using static DeathRoll.Data.Cards;

namespace DeathRoll.Data;

public enum BlackjackActions
{
    None,
    Hit,
    Bust,
    Stay,
    Split,
    Blackjack,
    Surrender,
    DoubleDown,
    FullHand
}

public class BlackjackPlayer
{
    public readonly string Name;
    public readonly string AnonName;
    public readonly bool IsSplit;

    public bool CanSplit;
    public bool IsAlive = true;
    public BlackjackActions LastAction = BlackjackActions.None;

    public int Bet;
    public readonly List<Card> Cards = new();

    public string DisplayName => !DebugConfig.RandomizeNames ? Name.Replace("\uE05D", "\uE05D ") : AnonName;

    public BlackjackPlayer(string name, int bet)
    {
        Bet = bet;
        Name = name;
        AnonName = Utils.GenerateHashedName(name);
    }

    public BlackjackPlayer(BlackjackPlayer player)
    {
        Bet = player.Bet;
        Name = $"{player.Name} Split";
        AnonName = $"{player.AnonName} Split";

        IsSplit = true;
    }

    public int CalculateCardValues()
    {
        var cards = 0;
        var hasAce = false;
        foreach (var card in Cards.Where(c => !c.IsHidden))
        {
            cards += card.Value;
            if (card.IsAce)
                hasAce = true;
        }

        return !hasAce ? cards : cards - 1 < 11 ? cards + 10 : cards;
    }
}

public class Blackjack
{
    private readonly Plugin Plugin;

    public BlackjackPlayer Dealer = new("Dealer", 0);
    public List<BlackjackPlayer> Players = new();
    public int CurrentPlayerIndex;

    public BlackjackPlayer CurrentPlayer => Players[CurrentPlayerIndex];

    public Blackjack(Plugin plugin)
    {
        Plugin = plugin;
    }

    public void Reset()
    {
        Players.Clear();
        CurrentPlayerIndex = 0;
        Dealer = new BlackjackPlayer("Dealer", 0);
    }

    private void NextPlayer()
    {
        CurrentPlayerIndex++;
        if (CurrentPlayerIndex < Players.Count)
        {
            Plugin.SwitchState(GameState.PlayerRound);

            if (!IsPlayerDone())
                return;

            NextPlayer();
            return;
        }

        CurrentPlayerIndex = 0;
        if (Plugin.Configuration.VenueDealer)
        {
            Plugin.SwitchState(GameState.DealerSecondCards);
            return;
        }

        Dealer.Cards.Last().IsHidden = false;
        Plugin.SwitchState(GameState.DealerRound);

        CheckForRemainingPlayers();
    }

    private void FirstPlayer()
    {
        CurrentPlayerIndex = 0;
        if (IsPlayerDone())
        {
            NextPlayer();
            return;
        }

        Plugin.SwitchState(GameState.PlayerRound);
    }

    public void FinishDrawingRound(GameState state)
    {
        Plugin.SwitchState(state);
    }

    public void SetDrawingRound()
    {
        if (!Plugin.Configuration.VenueDealer)
            GiveDealerCard(false);

        if (Plugin.Configuration.AutoDrawOpening)
        {
            Plugin.SwitchState(GameState.PrepareRound);
            return;
        }

        Plugin.SwitchState(CurrentPlayer.Cards.Count == 0 ? GameState.DrawFirstCards : GameState.DrawSecondCards);
    }

    public void PreparePlayers()
    {
        if (!Plugin.Configuration.VenueDealer)
            GiveDealerCard(true);

        CheckIfPlayersCanSplits();
        FirstPlayer();
    }

    public void StartRound()
    {
        GiveEachPlayerOneCard();
        GiveEachPlayerOneCard();
    }

    public void Parser(Roll roll)
    {
        switch (Plugin.State)
        {
            case GameState.Registration:
                Registration(roll);
                break;
            case GameState.Hit or GameState.DoubleDown or GameState.DrawSplit or GameState.FillDraw:
                RollParse(roll);
                break;
            case GameState.DrawFirstCards or GameState.DrawSecondCards:
                ParseStartingCards(roll);
                break;
            case GameState.DealerFirstCards or GameState.DealerSecondCards or GameState.DrawDealerCard:
                ParseDealerCards(roll);
                break;
            default:
                return;
        }
    }

    #region Roll Parsing

    private void Registration(Roll roll)
    {
        // check if registration roll is correct or if player is already in list
        if (roll.OutOf != -1)
            return;

        if (Players.Exists(p => p.Name == roll.PlayerName))
            return;

        Players.Add(new BlackjackPlayer(roll.PlayerName, Plugin.Configuration.DefaultBet));
    }

    private void ParseStartingCards(Roll roll)
    {
        if (roll.OutOf != 13)
            return;

        if (Plugin.Configuration.DealerDrawsAll)
        {
            if (Plugin.LocalPlayer != roll.PlayerName)
                return;
        }
        else
        {
            if (CurrentPlayer.Name != roll.PlayerName)
                return;
        }

        CurrentPlayer.Cards.Add(new Card(roll.Result, DrawCard().Suit));
        if (!Plugin.Configuration.StartingDraw || CurrentPlayer.Cards.Count >= 2)
            CurrentPlayerIndex++;
    }

    private void ParseDealerCards(Roll roll)
    {
        if (roll.OutOf != 13)
            return;

        if (Plugin.LocalPlayer != roll.PlayerName)
            return;

        Dealer.Cards.Add(new Card(roll.Result, DrawCard().Suit));
    }

    private void RollParse(Roll roll)
    {
        if (roll.OutOf != 13)
            return;

        // Check if 'dealer draws all cards' is enabled.
        // If so, spaghetti swap out the player var so it doesn't try to insert the dealer as a player
        if (Plugin.Configuration.DealerDrawsAll)
        {
            if (Plugin.LocalPlayer != roll.PlayerName)
                return;
        }
        else
        {
            if (CurrentPlayer.IsSplit)
                roll.PlayerName = $"{roll.PlayerName} Split";

            if (CurrentPlayer.Name != roll.PlayerName)
                return;
        }

        var card = new Card(roll.Result, DrawCard().Suit);
        switch (Plugin.State)
        {
            case GameState.Hit:
                Hit(card);
                return;
            case GameState.DoubleDown:
                DoubleDown(card);
                return;
            case GameState.DrawSplit:
                AddSplit(card);
                return;
            case GameState.FillDraw:
                FillSplit(card);
                return;
        }
    }

    #endregion

    private void AddSplit(Card card)
    {
        CurrentPlayer.CanSplit = false;

        var split = new BlackjackPlayer(CurrentPlayer);
        var existingCard = CurrentPlayer.Cards.PopAt(1);

        split.Cards.Add(existingCard);
        split.Cards.Add(card);
        Players.Insert(CurrentPlayerIndex + 1, split);

        Plugin.SwitchState(GameState.FillDraw);
    }

    private void FillSplit(Card card)
    {
        CurrentPlayer.LastAction = BlackjackActions.Split;
        CurrentPlayer.Cards.Add(card);

        if (IsPlayerDone())
        {
            NextPlayer();
            return;
        }

        Plugin.SwitchState(GameState.PlayerRound);
    }

    public void Split()
    {
        if (Plugin.Configuration is { VenueDealer: true })
        {
            Plugin.SwitchState(GameState.DrawSplit);
            return;
        }

        var split = new BlackjackPlayer(CurrentPlayer);
        var existingCard = CurrentPlayer.Cards.PopAt(1);

        split.Cards.Add(existingCard);
        split.Cards.Add(DrawCard());
        Players.Add(split);

        CurrentPlayer.Cards.Add(DrawCard());

        Plugin.SwitchState(GameState.PlayerRound);
    }

    private void Hit(Card card)
    {
        CurrentPlayer.LastAction = BlackjackActions.Hit;
        CurrentPlayer.Cards.Add(card);

        if (IsPlayerDone())
        {
            NextPlayer();
            return;
        }

        Plugin.SwitchState(GameState.PlayerRound);
    }

    private void DoubleDown(Card card)
    {
        CurrentPlayer.Bet *= 2;
        CurrentPlayer.LastAction = BlackjackActions.DoubleDown;

        CurrentPlayer.Cards.Add(card);

        IsPlayerDone();
        NextPlayer();
    }

    public void Surrender()
    {
        CurrentPlayer.Bet = (CurrentPlayer.Bet / 2) * -1;
        CurrentPlayer.IsAlive = false;
        CurrentPlayer.LastAction = BlackjackActions.Surrender;

        NextPlayer();
    }

    public void Stay()
    {
        CurrentPlayer.LastAction = BlackjackActions.Stay;

        NextPlayer();
    }

    public void PlayerAction()
    {
        if (!Plugin.Configuration.AutoDrawCard)
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

    public void DealerAction()
    {
        if (!Plugin.Configuration.AutoDrawDealer)
        {
            Plugin.SwitchState(GameState.DrawDealerCard);
            return;
        }

        while (IsDealerDone())
            GiveDealerCard(false);

        DealerRound();

        if (Plugin.State != GameState.Done)
            Plugin.SwitchState(GameState.DealerDone);
    }

    public void DealerRound()
    {
        switch (Dealer.CalculateCardValues())
        {
            case > 21:
                DealerBust();
                break;
            case 21:
                if (!Plugin.Configuration.StartingBlackjack || Dealer.Cards.Count == 2)
                    DealerBlackjack();
                break;
        }
    }

    private void DealerBust()
    {
        Dealer.LastAction = BlackjackActions.Bust;

        foreach (var player in Players.Where(x => x.IsAlive))
        {
            player.Bet *= 2;
            player.IsAlive = false;
        }

        Plugin.SwitchState(GameState.Done);
    }

    private void DealerBlackjack()
    {
        Dealer.LastAction = BlackjackActions.Blackjack;

        foreach (var player in Players.Where(x => x.IsAlive))
        {
            player.Bet *= -1;
            player.IsAlive = false;
        }

        Plugin.SwitchState(GameState.Done);
    }

    private bool IsPlayerDone()
    {
        var cards = CurrentPlayer.CalculateCardValues();
        switch (cards)
        {
            case > 21:
                CurrentPlayer.Bet *= -1;
                CurrentPlayer.IsAlive = false;
                CurrentPlayer.LastAction = BlackjackActions.Bust;
                return true;
            case 21:
                CurrentPlayer.IsAlive = false;
                if (!Plugin.Configuration.StartingBlackjack || CurrentPlayer.Cards.Count == 2)
                {
                    CurrentPlayer.Bet += (int)(CurrentPlayer.Bet * ((float)3 / 2));
                    CurrentPlayer.LastAction = BlackjackActions.Blackjack;
                }
                else
                {
                    CurrentPlayer.LastAction = BlackjackActions.FullHand;
                }

                return true;
        }

        return false;
    }

    private void GiveDealerCard(bool isHidden)
    {
        Dealer.Cards.Add(DrawCard(isHidden));
    }

    public bool IsDealerDone()
    {
        var cards = Dealer.CalculateCardValues();
        var hasAce = Dealer.Cards.Any(c => c.IsAce);

        return Plugin.Configuration.DealerRule switch
        {
            DealerRules.DealerHard17 => cards < 17,
            DealerRules.DealerHard16 => cards < 16,
            DealerRules.DealerSoft17 => !hasAce ? cards < 17 : HasDealerSoftHand() <= 17 || cards < 17,
            DealerRules.DealerSoft16 => !hasAce ? cards < 16 : HasDealerSoftHand() <= 16 || cards < 16,
            _ => cards < 16
        };
    }

    private int HasDealerSoftHand()
    {
        return Dealer.Cards.Sum(c => c.Value) + 10;
    }

    public void CheckForRemainingPlayers()
    {
        if (!Players.Any(x => x.IsAlive))
        {
            Plugin.SwitchState(GameState.Done);
            return;
        }

        DealerRound();
    }

    public void EndMatch()
    {
        var dealerCards = Dealer.CalculateCardValues();
        foreach (var player in Players)
        {
            if (!player.IsAlive)
                continue;

            var cards = player.CalculateCardValues();
            if (cards != dealerCards)
                player.Bet *= cards > dealerCards ? 2 : -1;
        }

        Plugin.SwitchState(GameState.Done);
    }

    public void TakePeopleIntoNextRound()
    {
        var l = new List<BlackjackPlayer>();
        foreach (var player in Players.Where(p => !p.IsSplit))
            l.Add(new BlackjackPlayer(player.Name, Plugin.Configuration.DefaultBet));
        Players = l;

        Dealer = new BlackjackPlayer("Dealer", 0);
        Plugin.SwitchState(GameState.Registration);
    }

    #region Internal

    private readonly Random RNG = new(unchecked(Environment.TickCount * 31));

    private Card DrawCard(bool isHidden = false)
    {
        return new Card(RNG.Next(1, 14), RNG.Next(0, 4), isHidden);
    }

    private void GiveEachPlayerOneCard()
    {
        foreach (var player in Players)
            player.Cards.Add(DrawCard());
    }

    private void CheckIfPlayersCanSplits()
    {
        foreach (var player in Players.Where(p => p.Cards[0].Rank == p.Cards[1].Rank))
            player.CanSplit = true;
    }

    #endregion
}

public static class BlackjackActionsExtensions
{
    public static string Name(this BlackjackActions action)
    {
        return action switch
        {
            BlackjackActions.None => "",
            BlackjackActions.Hit => "Hit",
            BlackjackActions.Bust => "Bust",
            BlackjackActions.Stay => "Stay",
            BlackjackActions.Split => "Split",
            BlackjackActions.FullHand => "Full Hand",
            BlackjackActions.Blackjack => "Blackjack",
            BlackjackActions.Surrender => "Surrender",
            BlackjackActions.DoubleDown => "Double Down",
            _ => "Unknown"
        };
    }
}