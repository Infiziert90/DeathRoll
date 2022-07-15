namespace DeathRoll.Logic;

public enum GameState
{
    NotRunning = 0,
    Match = 1,
    Done = 2,
    
    // tournament
    Registration = 101,
    Shuffling = 102,
    Prepare = 103,
    Crash = 199,
}