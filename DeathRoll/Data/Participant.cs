using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using DeathRoll.Data;
using DeathRoll.Logic;

namespace DeathRoll;

public class Participants
{
    private readonly Configuration configuration;
    
    public List<Participant> PList = new();
    public List<string> PlayerNameList = new();

    // venue
    public bool IsOutOfUsed;
    
    // deathroll
    public Participant Last = new(Roll.Dummy());
    public Participant Winner = new(Roll.Dummy());
    
    // tournament
    public List<string> NextRound = new();
    public bool RoundDone;
    
    // blackjack
    public string DealerAction = "";
    public List<Participant> DealerCards = new();
    public Dictionary<string, Player> PlayerBets = new();
    public List<Cards.Card> SplitDraw = new();
    private int currentIndex;
    
    public Participants(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Add(Participant p)
    {
        if (p.Name == "Byes" || !PlayerNameList.Exists(x => x == p.Name))
        {
            PlayerNameList.Add(p.Name);
        }

        PList.Add(p);
        Last = p;
        Winner = PList[^(PList.Count == 1 ? 1 : 2)]; // we only ever take last if there is one entry
    }

    public void DeleteEntry(string name)
    {
        PList.RemoveAll(x => x.Name == name);
        PlayerNameList.RemoveAll(x => x == name);
        
        PlayerBets.Remove(name);
    }

    public void UpdateSorting()
    {
        PList = configuration.SortingMode switch
        {
            SortingType.Min => PList.OrderBy(x => x.Roll).ToList(),
            SortingType.Max => PList.OrderByDescending(x => x.Roll).ToList(),
            SortingType.Nearest => PList.OrderBy(x => Math.Abs(configuration.Nearest - x.Roll)).ToList(),
            _ => PList
        };
    }
    
    public void Update()
    {
        if (configuration is { ActiveHighlighting: false, SavedHighlights.Count: 0 }) return;
        
        foreach (var roll in PList)
        {
            var highlight = configuration.SavedHighlights.FirstOrDefault(hl => hl.Matches(roll.Roll));
            if (highlight == null)
                roll.UpdateColor(false);
            else
                roll.UpdateColor(true, highlight.Color);
        }
    }
    
    public Participant FindPlayer(string playerName) => PList.First(x => x.Name == playerName);
    
    public List<Participant> FindAll(string playerName) => PList.FindAll(x => x.Name == playerName);
    
    public Participant GetWithIndex(int idx) => FindPlayer(PlayerNameList[idx]);
    
    public string LookupDisplayName(string playerName) => FindPlayer(playerName).GetDisplayName();
    
    public void DeleteRangeFromStart(int range) => PList.RemoveRange(0, range);
    
    public void Reset()
    {
        PList.Clear();
        PlayerNameList.Clear();
        NextRound.Clear();
        
        IsOutOfUsed = false;
        RoundDone = false;
        
        DealerCards.Clear();
        PlayerBets.Clear();
        currentIndex = 0;
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

    public int GetCurrentIndex() => currentIndex;
    
    public Participant GetParticipant() => PList[currentIndex];
    
    public string GetParticipantName() => PlayerNameList[currentIndex];
    
    public List<Participant> FindAllWithIndex() => FindAll(PList[currentIndex].Name);
    
    public void NextParticipant() => currentIndex++;
    
    public void ResetParticipant() => currentIndex = 0;
    
    public bool HasMoreParticipants() => currentIndex < PlayerNameList.Count;
    
    public void SetLastPlayerAction(string action) => PlayerBets[GetParticipant().Name].LastAction = action;
}

public class Participant
{
    public readonly string Name;
    public readonly int Roll;
    public readonly int OutOf;

    public bool HasHighlight;
    public Vector4 HighlightColor = new(0, 0, 0, 0);
    
    //blackjack
    public readonly Cards.Card Card = new(1, 1);
    public bool CanSplit = false;
    
    public Participant(Roll newRoll, Highlight? highlight = null)
    {
        Name = newRoll.PlayerName;
        Roll = newRoll.Rolled;
        OutOf = newRoll.OutOf;

        if (highlight != null)
        {
            HasHighlight = true;
            HighlightColor = highlight.Color;
        }
    }
    
    public Participant(string name, Cards.Card card)
    {
        Name = name;
        Card = card;
    }
    
    public void UpdateColor(bool hasHl, Vector4 hlColor = new())
    {
        HasHighlight = hasHl;
        HighlightColor = hlColor;
    }

    private string GenerateHashedName()
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(Name));
        var sb = new StringBuilder(hash.Length * 2);

        foreach (var b in hash)
            sb.Append(b.ToString("X2"));

        return $"Player {sb.ToString()[..10]}";
    }
    
    public string GetDisplayName() => !DebugConfig.RandomizeNames ? FName : GenerateHashedName();
    
    private string FName => Name.Replace("\uE05D", "\uE05D ");
}

public enum SortingType
{
   Min = 0,
   Max = 1,
   Nearest = 2,
}