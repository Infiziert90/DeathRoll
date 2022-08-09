namespace DeathRoll.Data;

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
    
    // blackjack
    PrepareRound = 200,
    PlayerRound = 201,
    DealerRound = 203,
    Hit = 204,
    DoubleDown = 205,
    DrawFirstCards = 206,
    DrawSecondCards = 207,
}