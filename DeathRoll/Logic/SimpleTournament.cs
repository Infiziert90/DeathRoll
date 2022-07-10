using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;

namespace DeathRoll.Logic;

public enum TournamentStates
{
    NotRunning = 0,
    Registration = 1,
    Shuffling = 2,
    Prepare = 3,
    NextStage = 4,
    Match = 5,
    Done = 6,
    Crash = 99,
}

public class SimpleTournament
{
    private readonly Configuration configuration;
    private readonly Participants participants;
    
    public Participants TParticipants;
    public TournamentStates TS = TournamentStates.NotRunning;
    
    public List<List<string>> TestBrackets = new ();

    private int currentIndex = 0;
    private int currentStage = 0;
    public int LastStage = 1;
    public List<int> StageDepth = new List<int>();
    
    public Participant Player1;
    public Participant Player2;
    
    public bool CalculationDone = false;
    
    public SimpleTournament(Configuration configuration, Participants participants)
    {
        this.configuration = configuration;
        this.participants = participants;
        
        TParticipants = new Participants(configuration);
    }

    public void SwitchState(TournamentStates newState)
    {
        TS = newState;
        
        switch (newState)
        {
            case TournamentStates.Prepare:
                CalculateNextMatch();
                break;
            case TournamentStates.NextStage:
                PrepareNextStage();
                break;
        }
    }
    
    public void Parser(string playerName, int parsedRoll, int parsedOutOf)
    {
        switch (TS)
        {
            case TournamentStates.Registration:
                Registration(playerName, parsedRoll, parsedOutOf);
                break;
            case TournamentStates.Match:
                MatchGameMode(playerName, parsedRoll, parsedOutOf);
                return;
            default:
                return;
        }
    }

    public void NextMatch()
    {
        TParticipants.NextRound.Add(participants.Winner.name);
        FillCurrentStageBrackets();
        
        currentIndex += 2;
        SwitchState(TournamentStates.Prepare);
    }
    
    public void ForfeitWin(Participant winner)
    {
        TParticipants.NextRound.Add(winner.name);
        participants.Winner = winner;
        FillCurrentStageBrackets();
        
        currentIndex += 2;
        SwitchState(TournamentStates.Prepare);
    }

    private void MatchGameMode(string playerName, int parsedRoll, int parsedOutOf)
    {
        // check if player is in playerList, if not, check if new players get accepted
        PluginLog.Debug($"New Roll: PN {playerName} Roll {parsedRoll} OutOf {parsedOutOf}");
        if (!participants.PlayerNameList.Exists(x => x == playerName)) return;
        
        if (participants.PList.Count == 0)
        {
            if (parsedOutOf == -1) parsedOutOf = 1000;
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
            return;
        }
        
        PluginLog.Debug($"New Roll: Last PN {participants.Last.name} Last Roll {participants.Last.roll}");
        if (playerName == participants.Last.name || participants.Last.roll != parsedOutOf) return;
        
        PluginLog.Debug($"Got Past it.");
        if (parsedRoll >= 2)
        {
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
        }
        else
        {
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
            participants.RoundDone = true;
        }
    }
    
    public void Registration(string playerName, int parsedRoll, int parsedOutOf)
    {
        // check if registration roll is correct or if player is already in list
        if (parsedOutOf != -1) return;
        if (participants.PlayerNameList.Exists(x => x == playerName)) return;
        
        participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
    }

    public void Shuffle()
    {
        var rng = new Random(unchecked(Environment.TickCount * 31));

        var n = participants.PlayerNameList.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (participants.PlayerNameList[k], participants.PlayerNameList[n]) = (participants.PlayerNameList[n], participants.PlayerNameList[k]);
        }

        TParticipants.PList = new List<Participant>(participants.PList);
        TParticipants.PlayerNameList = new List<string>(participants.PlayerNameList);
        GenerateBrackets();
    }

    public void GenerateBrackets()
    {
        var count = TParticipants.PlayerNameList.Count;
        var neededPlayers = (int)Math.Pow(2, Math.Ceiling(Math.Log2(count)));
        var stages = (int)Math.Log2(neededPlayers)+1;
        var magicNumber = neededPlayers - 1;
        
        for (var i = 0; i < stages; i++)
        {
            TestBrackets.Add(new List<string>());
        }
        
        // fill with byes if need
        for (var i = count; i < neededPlayers; i++)
        {
            TParticipants.Add(new Participant("Byes", -1, -1));
        }
        
        foreach (var (name, idx) in TParticipants.PlayerNameList.Select((value, i) => (value, i)))
        {
            TestBrackets[currentStage].Add(TParticipants.GetWithIndex(idx).fName);
            TestBrackets[currentStage].Add(TParticipants.GetWithIndex(magicNumber-idx).fName);
            if (idx == neededPlayers / 2 - 1) break;
        }
        
        // Set LastStage for later
        LastStage = stages;
        CalculateDepth();
    }

    public void FillCurrentStageBrackets()
    {
        TestBrackets[currentStage+1] = new List<string>();

        foreach (var name in TParticipants.NextRound)
        {
            PluginLog.Debug($"Next Round PN {name}");
        }
        
        foreach (var name in TParticipants.NextRound)
        {
            TestBrackets[currentStage+1].Add(TParticipants.FindPlayer(name).fName);
        }
    }
    
    public void CalculateNextMatch()
    {
        try
        {
            CalculationDone = false;
            
            if (currentIndex >= TestBrackets[currentStage].Count)
            {
                SwitchState(TournamentStates.NextStage);
                return;  
            }
            
            (Player1, Player2) = (
                TParticipants.FindPlayer(TestBrackets[currentStage][currentIndex]), 
                TParticipants.FindPlayer(TestBrackets[currentStage][currentIndex+1])
                );
            
            participants.Reset();
            participants.PlayerNameList.Add(Player1.name);
            participants.PlayerNameList.Add(Player2.name);
            
            CalculationDone = true;
        } 
        catch (Exception e)
        {
            PluginLog.Error("Exception triggered.");
            PluginLog.Error($"{e}");
            Reset();
        }
    }
    
    public void PrepareNextStage()
    {
        try
        {
            currentIndex = 0;
            if (currentIndex + 1 == TParticipants.NextRound.Count)
            {
                SwitchState(TournamentStates.Done);
                return;
            }
            
            TParticipants.PlayerNameList = new List<string>(TParticipants.NextRound);
            TParticipants.NextRound.Clear();
            participants.PList.Clear();
            participants.PlayerNameList.Clear();

            currentStage += 1;
            SwitchState(TournamentStates.Prepare);
        } 
        catch (Exception e)
        {
            PluginLog.Error("Exception triggered.");
            PluginLog.Error($"{e}");
            Reset();
            TS = TournamentStates.Crash;
        }
    }

    public void Reset()
    {
        TS = TournamentStates.NotRunning;
        participants.Reset();
        TParticipants.Reset();
        TestBrackets.Clear();

        currentIndex = 0;
        currentStage = 0;
        LastStage = 1;
    }
    
    // Prefill
    public void CalculateDepth()
    {
        // fill 0 and 1 stage which are not needed
        StageDepth.Add(-1);
        StageDepth.Add(-1);
        
        var previousDepth = 0;
        for (var i = 2; i < LastStage; i++)
        {
            if (i == 2)
            {
                StageDepth.Add(3);
                previousDepth = 3;
            }
            else
            {
                var calculatedDepth = previousDepth * 2 + 1; 
                StageDepth.Add(calculatedDepth);
                previousDepth = calculatedDepth;
            }
        }
    }
    
    // Code for testing
    public void AutoWin()
    {
        var rng = new Random(Environment.TickCount);
        participants.Add(new Participant(rng.Next(0, 2) == 0 ? participants.PlayerNameList[0] : participants.PlayerNameList[1], -1, -1));
        participants.Add(new Participant(rng.Next(0, 2) == 0 ? participants.PlayerNameList[0] : participants.PlayerNameList[1], -1, -1));
        NextMatch();
    }
    
    public void AutoRegistration()
    {
        for (var i = 1; i <= 8; i++)
        {
            participants.Add(new Participant($"Real Player {i}", 1000, 1000));
        }
    }
}