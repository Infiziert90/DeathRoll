namespace DeathRoll.Data;

public static class Cards
{
    public const string BaseCard = @"
┌─────────┐
│{0}{1}       │
│         │
│         │
│         │
│         │
│         │
│       {1}{0}│
└─────────┘";        
    public const string SuitCard = @"{0}";
    
    public const string BlankCard = @"
┌─────────┐
│         │
│         │
│         │
│         │
│         │
│         │
│         │
└─────────┘";

    public static string[] ShowCard(Card card)
    {
        var space = card.Rank != 10 ? " " : "";
        var (rank, suit) = ParseRankAndSuit(card);
        
        return new[]
        {
            !card.IsHidden ? string.Format(BaseCard, rank, space) : BlankCard, 
            !card.IsHidden ? string.Format(SuitCard, suit) : "",
        };
    }
    
    public static string ShowCardSimple(Card card)
    {
        var (rank, suit) = ParseRankAndSuit(card);
        
        return !card.IsHidden ? $"{rank} {suit}" : "Hidden";
    }

    private static (string, string) ParseRankAndSuit(Card card)
    {
        return (
            card.Rank switch
            {
                1 => "A",
                11 => "J",
                12 => "Q",
                13 => "K",
                _ => card.Rank.ToString()
            }, 
            card.Suit switch
            {
                0 => "♠",
                1 => "♥",
                2 => "♦",
                3 => "♣",
                _ => "♠",
            }
        );
    }

    public class Card
    {
        public int Rank;
        public int Value;
        public int Suit;
        public bool IsHidden;
        public bool IsAce;

        public Card (int rank, int suit, bool isHidden = false)
        {
            Rank = rank;
            Suit = suit;
            IsHidden = isHidden;

            Value = rank switch
            {
                > 10 => 10,
                _ => rank
            };
            IsAce = rank == 1;
        }
    }
}