using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DeathRoll;

public class Participants
{
    private readonly Configuration configuration;
    
    public List<Participant> PList = new();
    public List<string> PlayerNameList = new();
    public List<string> NextRound = new();
    
    public bool IsOutOfUsed = false;
    
    // deathroll
    public bool RoundDone = false;
    public Participant Last = new("Unknown", 1000, 1000);
    public Participant Winner = new("Unknown", 1000, 1000);

    public Participants(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Add(Participant p)
    {
        PList.Add(p);
        if (!PlayerNameList.Exists(x => x == p.name))
        {
            PlayerNameList.Add(p.name);
        }
        Last = p;
        if (PList.Count == 1)
        {
            Winner = p;
            return;
        }
        Winner = PList[^2];
    }
    
    public Participant? FindPlayer(string playerName)
    {
        if (!configuration.DRandomizeNames)
        {
            return PList.Find(x => x.name == playerName) ?? PList.Find(x => x.fName == playerName);
        }

        return PList.Find(x => x.randomName == playerName);
    }

    public Participant GetWithIndex(int idx)
    {
        return FindPlayer(PlayerNameList[idx]);
    }
    
    public void DeleteEntry(string name)
    {
        PList.RemoveAll(x => x.name == name);
        PlayerNameList.RemoveAll(x => x == name);
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
            foreach (var hl in configuration.SavedHighlights.Where(hl =>
                         hl.CompiledRegex.Match(roll.roll.ToString()).Success))
            {
                roll.UpdateColor(true, hl.Color);
                break;
            }
        }
    }

    public void Reset()
    {
        PList.Clear();
        PlayerNameList.Clear();
        NextRound.Clear();
        IsOutOfUsed = false;
        RoundDone = false;
    }
}

public class Participant
{
    public readonly string name;
    public readonly int outOf;
    public readonly int roll;

    public bool hasHighlight;
    public Vector4 highlightColor;
    public string randomName;
    public string fName;

    private readonly List<string> FirstName = new()
        {"Absolutely", "Completely", "Undoubtedly", "More or less", "Assuredly", "Utterly", "Kind of"};
    private readonly List<string> Surnames = new()
        {"Fake", "Sus", "Imposter", "Pseudo"};

    public Participant(string name, int roll, int outOf, Vector4 highlightColor)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = true;
        this.highlightColor = highlightColor;
        
        randomName = GetRandomizedName();
        fName = GetReadableName();
    }

    public Participant(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        hasHighlight = false;
        highlightColor = new Vector4(0, 0, 0, 0);
        
        randomName = GetRandomizedName();
        fName = GetReadableName();
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

    public string GetReadableName()
    {
        return name.Replace("\uE05D", "\uE05D ");
    }

    public string GetRandomizedName()
    {
        var random = new Random();
        return $"{FirstName[random.Next(FirstName.Count)]} {Surnames[random.Next(Surnames.Count)]}";
    }

    public string GetUsedName(bool useRandomPn)
    {
        return !useRandomPn ? GetReadableName() : randomName;
    }
}

public enum SortingType
{
   Min = 0,
   Max = 1,
   Nearest = 2,
}