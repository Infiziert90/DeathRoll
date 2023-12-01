namespace DeathRoll.Data;

public enum GameModes
{
    Venue = 0,
    DeathRoll = 1,
    Tournament = 2,
    Blackjack = 3,
    TripleT = 4,
    Minesweeper = 5,
}

public enum Difficulty : byte
{
    Easy = 0,
    Medium = 1,
    Hard = 2
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
            GameModes.Minesweeper => "Minesweeper",
            _ => "Unknown"
        };
    }
}

public static class DifficultyExtensions
{
    public static string String(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy AI",
            Difficulty.Medium => "Medium AI",
            Difficulty.Hard => "Hard AI",
            _ => "Unknown"
        };
    }

    public static string Name(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => "Easy",
            Difficulty.Medium => "Medium",
            Difficulty.Hard => "Hard",
            _ => "Unknown"
        };
    }

    public static (int Rows, int Cols, int Mines)[] GridSizes(this Difficulty difficulty)
    {
        return difficulty switch
        {
            Difficulty.Easy => new [] { (8, 8, 10), (9, 9, 10) },
            Difficulty.Medium => new [] { (16, 16, 40) },
            Difficulty.Hard => new [] { (16, 30, 99) },
            _ => new [] { (8, 8, 10) },
        };
    }
}