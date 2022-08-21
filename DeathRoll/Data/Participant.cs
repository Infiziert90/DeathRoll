using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DeathRoll.Data;

namespace DeathRoll;

public class Participants
{
    private readonly Configuration configuration;
    
    public List<Participant> PList = new();
    public List<string> PlayerNameList = new();
    
    public Dictionary<string, string> UsedDebugNames = new();

    // venue
    public bool IsOutOfUsed = false;
    
    // deathroll
    public Participant Last = new("Unknown", 1000, 1000);
    public Participant Winner = new("Unknown", 1000, 1000);
    
    // tournament
    public List<string> NextRound = new();
    public bool RoundDone = false;
    
    // blackjack
    public string DealerAction = "";
    public List<Participant> DealerCards = new();
    public Dictionary<string, Player> PlayerBets = new();
    private int CurrentIndex = 0;
    public List<Cards.Card> SplitDraw = new();
    
    public Participants(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Add(Participant p)
    {
        if (!PlayerNameList.Exists(x => x == p.name))
        {
            while (UsedDebugNames.ContainsValue(p.randomName)) // check that random name is unique
                p.GenerateRandomizedName();
            PlayerNameList.Add(p.name);
        }
        else
        {
            p.randomName = UsedDebugNames[p.name]; // set random name to existing name
        }

        UsedDebugNames[p.name] = p.randomName;
        PList.Add(p);
        Last = p;
        Winner = PList[^(PList.Count == 1 ? 1 : 2)]; // we only ever take last if there is one entry
    }
    
    public Participant FindPlayer(string playerName)
    {
        return PList.Find(x => x.name == playerName);
    }
    
    public List<Participant> FindAll(string playerName)
    {
        return PList.FindAll(x => x.name == playerName);
    }
    
    public List<Participant> FindAllWithIndex()
    {
        return FindAll(PList[CurrentIndex].name);
    }
    
    public Participant GetWithIndex(int idx)
    {
        return FindPlayer(PlayerNameList[idx]);
    }

    public string LookupDisplayName(string playerName)
    {
        return FindPlayer(playerName).GetDisplayName();
    }
    
    public void DeleteEntry(string name)
    {
        PList.RemoveAll(x => x.name == name);
        PlayerNameList.RemoveAll(x => x == name);
        
        UsedDebugNames.Remove(name);
        PlayerBets.Remove(name);
    }

    public void UpdateSorting()
    {
        PList = configuration.SortingMode switch
        {
            SortingType.Min => PList.OrderBy(x => x.roll).ToList(),
            SortingType.Max => PList.OrderByDescending(x => x.roll).ToList(),
            SortingType.Nearest => PList.OrderBy(x => Math.Abs(configuration.Nearest - x.roll)).ToList(),
            _ => PList
        };
    }
    
    public void Update()
    {
        if (!configuration.ActiveHighlighting || PList.Count == 0) return;

        foreach (var roll in PList)
        {
            roll.UpdateColor(false);
            foreach (var hl in configuration.SavedHighlights.Where(
                         hl => hl.CompiledRegex.Match(roll.roll.ToString()).Success))
            {
                roll.UpdateColor(true, hl.Color);
                break;
            }
        }
    }

    public void DeleteRangeFromStart(int range)
    {
        PList.RemoveRange(0, range);
    }
    
    public void Reset()
    {
        PList.Clear();
        PlayerNameList.Clear();
        UsedDebugNames.Clear();
        NextRound.Clear();
        
        IsOutOfUsed = false;
        RoundDone = false;
        
        DealerCards.Clear();
        PlayerBets.Clear();
        CurrentIndex = 0;
        DealerAction = "";
        SplitDraw.Clear();
    }
    
    // blackjack
    public class Player
    {
        public int Bet;
        public bool IsAlive;
        public string LastAction = "";

        public Player(int bet, bool isAlive)
        {
            Bet = bet;
            IsAlive = isAlive;
        }
    }

    public int GetCurrentIndex() => CurrentIndex;
    public Participant GetParticipant() => PList[CurrentIndex];
    public string GetParticipantName() => PlayerNameList[CurrentIndex];
    public void NextParticipant() => CurrentIndex++;
    public void ResetParticipant() => CurrentIndex = 0;
    public bool HasMoreParticipants() => CurrentIndex < PlayerNameList.Count;
}

public class Participant
{
    public string name;
    public string randomName = "";
    public string fName = "";
    
    public readonly int outOf;
    public readonly int roll;

    public bool hasHighlight;
    public Vector4 highlightColor;

    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    //blackjack
    public Cards.Card Card = new(1, 1, false);
    public bool CanSplit = false;
    
    public Participant(string name, int roll, int outOf, Vector4 highlightColor)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = true;
        this.highlightColor = highlightColor;
        
        GenerateRandomizedName();
        GenerateFancyName();
    }

    public Participant(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        hasHighlight = false;
        highlightColor = new Vector4(0, 0, 0, 0);
        
        GenerateRandomizedName();
        GenerateFancyName();
    }
    
    public Participant(string name, Cards.Card card)
    {
        this.name = name;
        Card = card;

        GenerateRandomizedName();
        GenerateFancyName();
    }
    
    public void UpdateColor(bool hasHl)
    {
        hasHighlight = hasHl;
    }
    
    public void UpdateColor(bool hasHl, Vector4 hlColor)
    {
        hasHighlight = hasHl;
        highlightColor = hlColor;
    }

    public void GenerateFancyName()
    {
        fName = name.Replace("\uE05D", "\uE05D ");
    }

    public void GenerateRandomizedName()
    {
        var random = new Random();
        randomName = "Player " + new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());
    }
    
    public string GetDisplayName()
    {
        return !DebugConfig.RandomizeNames ? fName : randomName;
    }
}

public enum SortingType
{
   Min = 0,
   Max = 1,
   Nearest = 2,
}