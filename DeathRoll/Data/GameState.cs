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
    DrawFirstCards = 201,
    DrawSecondCards = 202,
    PlayerRound = 203,
    Hit = 204,
    DoubleDown = 205,
    DrawSplit = 206,
    DealerRound = 207,
    DealerFirstCards = 208,
    DealerSecondCards = 209,
    DrawDealerCard = 210,
    DealerDone = 211,
}