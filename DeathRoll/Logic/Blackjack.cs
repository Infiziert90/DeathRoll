using DeathRoll.Data;

namespace DeathRoll.Logic;

public class Blackjack
{
    private readonly Plugin Plugin;

    public Blackjack(Plugin plugin)
    {
        Plugin = plugin;
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
                ParseStartingCards(roll);
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
        if (!Plugin.Configuration.AutoDrawDealer)
        {
            Plugin.SwitchState(GameState.DrawDealerCard);
            return;
        }

        while (DealerCheckHand())
            GiveDealerCard(false);
        DealerRound();

        if (Plugin.State != GameState.Done)
            Plugin.SwitchState(GameState.DealerDone);
    }

    public void PlayerAction()
    {
        // if (!participants.GetParticipant().Name.EndsWith(" Split"))
        // {
        //     if (!configuration.AutoDrawCard)
        //         return;
        // }

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

    private void RollParse(Roll roll)
    {
        // check if roll is out of 13 (13 cards) and check if current player has rolled
        if (roll.OutOf != 13)
            return;

        // Check if 'dealer draws all cards' is enabled. If so, spaghetti swap out the player var so it doesn't try to insert the dealer as a player
        if (Plugin.Configuration.DealerDrawsAll)
        {
            if (Plugin.LocalPlayer != roll.PlayerName)
                return;
        }
        else
        {
            var checkableName = Plugin.Participants.GetParticipantName();
            if (checkableName.EndsWith(" Split"))
                checkableName = checkableName.Split(" Split").First();

            if (checkableName != roll.PlayerName)
                return;
        }

        // make sure that Participant has correct name
        // TODO Check if this is needed at all
        roll.PlayerName = Plugin.Participants.GetParticipantName();

        var card = new Cards.Card(roll.Result, DrawCard().Suit);
        switch (Plugin.State)
        {
            case GameState.Hit:
                Hit(card);
                return;
            case GameState.DoubleDown:
                DoubleDown(card);
                return;
            case GameState.DrawSplit:
                Plugin.Participants.SplitDraw.Add(card);
                return;
        }
    }

    private void ParseStartingCards(Roll roll)
    {
        if (roll.OutOf != 13)
            return;

        if (Plugin.Configuration.DealerDrawsAll)
        {
            if (Plugin.LocalPlayer != roll.PlayerName)
                return;

            roll.PlayerName = Plugin.Participants.GetParticipantName();
        }
        else
        {
            if (Plugin.Participants.GetParticipantName() != roll.PlayerName)
                return;
        }

        Plugin.Participants.Add(new Participant(roll.PlayerName, new Cards.Card(roll.Result, DrawCard().Suit)));
        if (!Plugin.Configuration.StartingDraw || Plugin.State is GameState.DrawSecondCards)
            Plugin.Participants.NextParticipant();
    }

    private void ParseDealerCards(Roll roll)
    {
        if (roll.OutOf != 13)
            return;

        if (Plugin.LocalPlayer != roll.PlayerName)
            return;

        Plugin.Participants.DealerCards.Add(new Participant("", new Cards.Card(roll.Result, DrawCard().Suit)));
    }

    public void TakePeopleIntoNextRound()
    {
        var last = new List<string>(Plugin.Participants.PlayerNameList);
        Plugin.Participants.Reset();

        foreach (var name in last.Where(name => !name.EndsWith(" Split")))
        {
            Plugin.Participants.Add(new Participant(Roll.Dummy(name)));
            Plugin.Participants.PlayerBets[name] = new Participants.Player(Plugin.Configuration.DefaultBet, true);
        }
        Plugin.SwitchState(GameState.Registration);
    }

    public void EndMatch()
    {
        var dealerCards = CalculateCardValues(Plugin.Participants.DealerCards);
        foreach (var (name, player) in Plugin.Participants.PlayerBets)
        {
            if (!player.IsAlive)
                continue;

            var currentPlayer = Plugin.Participants.FindAll(name);
            var cards = CalculateCardValues(currentPlayer);

            if (cards != dealerCards)
                player.Bet *= cards > dealerCards ? 2 : -1;
        }

        Plugin.SwitchState(GameState.Done);
    }

    public bool DealerCheckHand()
    {
        var cards = CalculateCardValues(Plugin.Participants.DealerCards);
        var hasAce = Plugin.Participants.DealerCards.Any(x => x.Card.IsAce);
        var check = Plugin.Configuration.DealerRule switch
        {
            DealerRules.DealerHard17 => cards < 17,
            DealerRules.DealerHard16 => cards < 16,
            DealerRules.DealerSoft17 => !hasAce ? cards < 17: HasDealerSoftHand() <= 17 || cards < 17,
            DealerRules.DealerSoft16 => !hasAce ? cards < 16: HasDealerSoftHand() <= 16 || cards < 16,
            _ => cards < 16
        };

        return check;
    }

    private void DealerBust()
    {
        Plugin.Participants.DealerAction = "Bust";

        foreach (var player in Plugin.Participants.PlayerBets.Values.Where(x => x.IsAlive))
        {
            player.Bet *= 2;
            player.IsAlive = false;
        }
        Plugin.SwitchState(GameState.Done);
    }

    private void DealerBlackjack()
    {
        Plugin.Participants.DealerAction = "Blackjack";

        foreach (var player in Plugin.Participants.PlayerBets.Values.Where(x => x.IsAlive))
        {
            player.Bet *= -1;
            player.IsAlive = false;
        }
        Plugin.SwitchState(GameState.Done);
    }

    public void DealerRound()
    {
        var cards = CalculateCardValues(Plugin.Participants.DealerCards);
        switch (cards)
        {
            case > 21:
                DealerBust();
                break;
            case 21:
                if (!Plugin.Configuration.StartingBlackjack || Plugin.Participants.DealerCards.Count == 2)
                    DealerBlackjack();
                break;
        }
    }

    public void CheckForRemainingPlayers()
    {
        if (!Plugin.Participants.PlayerBets.Values.Any(x => x.IsAlive))
        {
            Plugin.SwitchState(GameState.Done);
            return;
        }

        DealerRound();
    }

    private void Hit(Cards.Card card)
    {
        var currentPlayer = Plugin.Participants.GetParticipant().Name;
        Plugin.Participants.SetLastPlayerAction("Hit");

        Plugin.Participants.Add(new Participant(currentPlayer, card));
        if (!CheckPlayerCards())
        {
            NextPlayer();
            return;
        }
        Plugin.SwitchState(GameState.PlayerRound);
    }

    private void DoubleDown(Cards.Card card)
    {
        var currentPlayer = Plugin.Participants.GetParticipant().Name;
        Plugin.Participants.PlayerBets[currentPlayer].Bet *= 2;
        Plugin.Participants.SetLastPlayerAction("Double Down");

        Plugin.Participants.Add(new Participant(currentPlayer, card));
        CheckPlayerCards();
        NextPlayer();
    }

    public void Split()
    {
        if (Plugin.Configuration is { VenueDealer: false, AutoDrawCard: true })
        {
            Plugin.Participants.SplitDraw.Add(DrawCard());
            Plugin.Participants.SplitDraw.Add(DrawCard());
        }

        if (Plugin.Participants.SplitDraw.Count != 2)
        {
            Plugin.SwitchState(GameState.DrawSplit);
            return;
        }

        var cards = Plugin.Participants.FindAllWithIndex();
        var currentPlayer = Plugin.Participants.GetParticipant().Name;
        var splitName = $"{currentPlayer} Split";

        var card1 = Plugin.Participants.SplitDraw[0];
        var card2 = Plugin.Participants.SplitDraw[1];

        var tmp = new Participant(splitName, cards[1].Card);
        Plugin.Participants.PList[Plugin.Participants.GetCurrentIndex() + Plugin.Participants.PlayerNameList.Count] = tmp;
        cards[1] = tmp;

        Plugin.Participants.Add(new Participant(currentPlayer, card1));

        Plugin.Participants.PlayerNameList.Add(splitName);
        Plugin.Participants.PList.Insert(Plugin.Participants.PlayerNameList.Count-1, new Participant(splitName, card2));

        var player = Plugin.Participants.PlayerBets[currentPlayer];
        player.LastAction = "Split";

        Plugin.Participants.PlayerBets[splitName] = new Participants.Player(player.Bet, true);

        Plugin.Participants.GetParticipant().CanSplit = false;
        Plugin.Participants.SplitDraw.Clear();
        Plugin.SwitchState(GameState.PlayerRound);
    }

    public void Stay()
    {
        Plugin.Participants.SetLastPlayerAction("Stay");

        NextPlayer();
    }

    public void Surrender()
    {
        var currentPlayer = Plugin.Participants.GetParticipant().Name;
        var player = Plugin.Participants.PlayerBets[currentPlayer];
        player.Bet = (player.Bet / 2) * -1;
        player.IsAlive = false;
        player.LastAction = "Surrender";

        NextPlayer();
    }

    private void NextPlayer()
    {
        Plugin.Participants.NextParticipant();

        if (Plugin.Participants.HasMoreParticipants())
        {
            Plugin.SwitchState(GameState.PlayerRound);
            if (CheckPlayerCards())
                return;
            NextPlayer();
            return;
        }

        if (Plugin.Configuration.VenueDealer)
        {
            Plugin.SwitchState(GameState.DealerSecondCards);
            return;
        }

        Plugin.Participants.DealerCards[0].Card.IsHidden = false;
        Plugin.SwitchState(GameState.DealerRound);
        CheckForRemainingPlayers();
    }

    private void FirstPlayer()
    {
        if (!CheckPlayerCards())
        {
            NextPlayer();
            return;
        }
        Plugin.SwitchState(GameState.PlayerRound);
    }

    private bool CheckPlayerCards()
    {
        var currentPlayer = Plugin.Participants.FindAll(Plugin.Participants.GetParticipant().Name);
        var cards = CalculateCardValues(currentPlayer);

        var player = Plugin.Participants.PlayerBets[currentPlayer[0].Name];
        switch (cards)
        {
            case > 21:
                player.Bet *= -1;
                player.IsAlive = false;
                player.LastAction = "Bust";
                return false;
            case 21:
                player.IsAlive = false;
                if (!Plugin.Configuration.StartingBlackjack || currentPlayer.Count == 2)
                {
                    player.Bet += (int) (player.Bet * ((float) 3 / 2));
                    player.LastAction = "Blackjack";
                }
                else
                {
                    player.LastAction = "Stay";
                }
                return false;
        }
        return true;
    }

    public void FinishDrawingRound(GameState state)
    {
        Plugin.Participants.ResetParticipant();

        Plugin.SwitchState(state);
    }

    public void SetDrawingRound()
    {
        if (!Plugin.Configuration.VenueDealer) GiveDealerCard(true);

        if (Plugin.Configuration.AutoDrawOpening)
        {
            Plugin.SwitchState(GameState.PrepareRound);
            return;
        }

        Plugin.SwitchState(Plugin.Participants.FindAllWithIndex().Count == 1 ? GameState.DrawFirstCards : GameState.DrawSecondCards);
    }

    public void PreparePlayers()
    {
        if (!Plugin.Configuration.VenueDealer) GiveDealerCard(false);
        Plugin.Participants.DeleteRangeFromStart(Plugin.Participants.PlayerNameList.Count);
        CheckIfPlayersCanSplits();
        FirstPlayer();
    }

    public void StartRound()
    {
        GiveEachPlayerOneCard();
        GiveEachPlayerOneCard();

        Plugin.Participants.ResetParticipant();
    }

    public string TargetRegistration()
    {
        // get target
        var name = Plugin.GetTargetName();
        if (name == string.Empty)
            return "Target not found.";

        // check if registration roll is correct or if player is already in list
        if (Plugin.Participants.PlayerNameList.Exists(x => x == name))
            return "Target already registered.";

        Plugin.Participants.Add(new Participant(Roll.Dummy(name)));
        Plugin.Participants.PlayerBets[name] = new Participants.Player(Plugin.Configuration.DefaultBet, true);
        return string.Empty;
    }

    private void Registration(Roll roll)
    {
        // check if registration roll is correct or if player is already in list
        if (roll.OutOf != -1)
            return;

        if (Plugin.Participants.PlayerNameList.Exists(x => x == roll.PlayerName))
            return;

        Plugin.Participants.Add(new Participant(roll));
        Plugin.Participants.PlayerBets[roll.PlayerName] = new Participants.Player(Plugin.Configuration.DefaultBet, true);
    }

    // internal game mechanics
    private readonly Random RNG = new(unchecked(Environment.TickCount * 31));
    private Cards.Card DrawCard()
    {
        return new Cards.Card(RNG.Next(1, 14), RNG.Next(0, 4), false);
    }

    private void CheckIfPlayersCanSplits()
    {
        foreach (var cards in Plugin.Participants.PlayerNameList.Select(player => Plugin.Participants.FindAll(player)).Where(cards => cards[0].Card.Rank == cards[1].Card.Rank))
            cards[0].CanSplit = true;
    }

    private void GiveEachPlayerOneCard()
    {
        foreach (var player in Plugin.Participants.PlayerNameList)
        {
            var card = DrawCard();
            Plugin.Participants.Add(new Participant(player, card));
        }
    }

    private void GiveDealerCard(bool isHidden)
    {
        var card = DrawCard();
        card.IsHidden = isHidden;
        Plugin.Participants.DealerCards.Add(new Participant("", card));

    }

    public static int CalculateCardValues(IEnumerable<Participant> playersHand)
    {
        var cards = 0;
        var hasAce = false;
        foreach (var card in playersHand.Select(x => x.Card))
        {
            cards += card.Value;
            if (card.IsAce)
                hasAce = true;
        }

        return !hasAce ? cards : cards - 1 < 11 ? cards + 10 : cards;
    }

    private int HasDealerSoftHand()
    {
        return Plugin.Participants.DealerCards.Select(x => x.Card).Sum(card => card.Value) + 10;
    }
}