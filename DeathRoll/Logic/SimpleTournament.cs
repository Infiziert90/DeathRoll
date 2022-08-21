using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Logging;
using DeathRoll.Data;

namespace DeathRoll.Logic;

public class SimpleTournament
{
    private readonly Configuration configuration;
    private readonly Participants participants;
    
    public Participants TParticipants;
    
    public List<List<Participant>> InternalBrackets = new();
    public List<List<string>> FilledBrackets = new();

    private int currentIndex = 0;
    private int currentStage = 0;
    public int LastStage = 1;
    public List<int> StageDepth = new();
    
    public Participant Player1 = new("Unknown", 1000, 1000);
    public Participant Player2 = new("Unknown", 1000, 1000);
    
    public SimpleTournament(Configuration configuration, Participants participants)
    {
        this.configuration = configuration;
        this.participants = participants;
        
        TParticipants = new Participants(configuration);
    }

    public void Parser(string playerName, int parsedRoll, int parsedOutOf)
    {
        switch (Plugin.State)
        {
            case GameState.Registration:
                Registration(playerName, parsedRoll, parsedOutOf);
                break;
            case GameState.Match:
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
        CalculateNextMatch();
    }
    
    public void ForfeitWin(Participant winner)
    {
        TParticipants.NextRound.Add(winner.name);
        participants.Winner = winner;
        FillCurrentStageBrackets();
        
        currentIndex += 2;
        CalculateNextMatch();
    }

    private void MatchGameMode(string playerName, int parsedRoll, int parsedOutOf)
    {
        // check if player is in playerList, if not, check if new players get accepted
        if (!participants.PlayerNameList.Exists(x => x == playerName)) return;
        
        if (participants.PList.Count == 0)
        {
            if (parsedOutOf == -1) parsedOutOf = 1000;
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
            return;
        }
        
        if (playerName == participants.Last.name || participants.Last.roll != parsedOutOf) return;
        
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
    
    public string TargetRegistration()
    {
        // get target
        var name = Plugin.GetTargetName();
        if (name == string.Empty) return "Target not found.";
        
        // check if player is already in list
        if (participants.PlayerNameList.Exists(x => x == name)) return "Target already registered.";
        
        participants.Add(new Participant(name, 1000, 1000));
        return string.Empty;
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
    }

    public void GenerateBrackets()
    {
        var count = TParticipants.PlayerNameList.Count;
        var neededPlayers = (int)Math.Pow(2, Math.Ceiling(Math.Log2(count)));
        var stages = (int)Math.Log2(neededPlayers)+1;
        var magicNumber = neededPlayers - 1;
        
        for (var i = 0; i < stages; i++)
        {
            InternalBrackets.Add(new List<Participant>());
        }
        
        // fill with byes if need
        for (var i = count; i < neededPlayers; i++)
        {
            TParticipants.Add(new Participant("Byes", -1, -1));
        }
        
        foreach (var (name, idx) in TParticipants.PlayerNameList.Select((value, i) => (value, i)))
        {
            InternalBrackets[currentStage].Add(TParticipants.GetWithIndex(idx));
            InternalBrackets[currentStage].Add(TParticipants.GetWithIndex(magicNumber-idx));
            if (idx == neededPlayers / 2 - 1) break;
        }
        
        // Set LastStage for later
        LastStage = stages;
        CalculateDepth();
        FillBracketTable();
    }

    public void FillCurrentStageBrackets()
    {
        InternalBrackets[currentStage+1] = new List<Participant>();
        
        foreach (var name in TParticipants.NextRound)
        {
            InternalBrackets[currentStage+1].Add(TParticipants.FindPlayer(name));
        }

        FillBracketTable();
    }
    
    public void CalculateNextMatch()
    {
        Plugin.SwitchState(GameState.Prepare);
        
        try
        {
            if (currentIndex >= InternalBrackets[currentStage].Count)
            {
                PrepareNextStage();
                return;  
            }
            
            (Player1, Player2) = (InternalBrackets[currentStage][currentIndex], InternalBrackets[currentStage][currentIndex+1]);

            var tmp = new Dictionary<string, string>(participants.UsedDebugNames);
            participants.Reset();
            participants.UsedDebugNames = new Dictionary<string, string>(tmp);
            participants.PlayerNameList.Add(Player1.name);
            participants.PlayerNameList.Add(Player2.name);
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
                Plugin.SwitchState(GameState.Done);
                return;
            }
            
            TParticipants.PlayerNameList = new List<string>(TParticipants.NextRound);
            TParticipants.NextRound.Clear();
            participants.PList.Clear();
            participants.PlayerNameList.Clear();
            
            currentStage += 1;
            CalculateNextMatch();
        } 
        catch (Exception e)
        {
            PluginLog.Error("Exception triggered.");
            PluginLog.Error($"{e}");
            Reset();
            Plugin.SwitchState(GameState.Crash);
        }
    }

    public void Reset()
    {
        Plugin.SwitchState(GameState.NotRunning);
        participants.Reset();
        TParticipants.Reset();
        InternalBrackets.Clear();
        FilledBrackets.Clear();

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
    
    public void FillBracketTable()
    {
        FilledBrackets.Clear();
        var nestedIDX = new List<int>();
        for (var i = 0; i < LastStage; i++) nestedIDX.Add(i == 0 ? -1 : 0);

        for (var idx = 0; idx < InternalBrackets[0].Count * 2; idx++)
        {
            FilledBrackets.Add(new List<string>());
            for (var stage = 0; stage < LastStage; stage++)
            {
                if (nestedIDX[stage] >= InternalBrackets[stage].Count) break;
                switch (stage)
                {
                    case 0 when idx % 2 == 0:
                        FilledBrackets[idx].Add(InternalBrackets[stage][idx / 2].GetDisplayName());
                        break;
                    case 0 when idx % 2 == 1:
                        // fill the cell with spaces to give it font height
                        FilledBrackets[idx].Add("    ");
                        break;
                    case 1 when idx != 1 && idx % 4 != 1:
                        // skip this cell later
                        FilledBrackets[idx].Add("x");
                        break;
                    case 1:
                        FilledBrackets[idx].Add(InternalBrackets[stage][nestedIDX[stage]].GetDisplayName());
                        nestedIDX[stage]++;
                        break;
                    default:
                    {
                        var stageDepth = StageDepth[stage];
                        if (idx != stageDepth && idx != (stageDepth * 2 + 2) * nestedIDX[stage] + stageDepth)
                        {
                            // skip this cell later
                            FilledBrackets[idx].Add("x");
                            break;
                        }
                        
                        FilledBrackets[idx].Clear();
                        for (var s = 0; s < stage; s++) FilledBrackets[idx].Add("  ");
                        FilledBrackets[idx].Add(InternalBrackets[stage][nestedIDX[stage]].GetDisplayName());
                        nestedIDX[stage]++;
                        break;
                    }
                }
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
    
    public void AutoRegistration(int n)
    {
        for (var i = 1; i <= n; i++)
        {
            participants.Add(new Participant($"Real Player {i}", 1000, 1000));
        }
    }
}