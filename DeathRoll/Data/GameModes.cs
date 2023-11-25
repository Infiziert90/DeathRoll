namespace DeathRoll.Data;

public enum GameModes
{
    Venue = 0,
    DeathRoll = 1,
    Tournament = 2,
    Blackjack = 3,
    TripleT = 4,
}

public static class GameModeUtils
{
    public static readonly string[] ListOfNames = Enum.GetValues<GameModes>().Select(n => n.GetName()).ToArray();

    public static string GetName(this GameModes n)
    {
        return n switch
        {
            GameModes.Venue => "Venue",
            GameModes.DeathRoll => "DeathRoll",
            GameModes.Tournament => "Tournament",
            GameModes.Blackjack => "Blackjack",
            GameModes.TripleT => "Tic-Tac-Toe",
            _ => "Unknown"
        };
    }
}