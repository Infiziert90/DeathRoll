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
    public Participant Last;
    public Participant Winner;

    public Participants(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void Add(Participant p)
    {
        PList.Add(p);
        PlayerNameList.Add(p.name);
        Last = p;
        if (PList.Count == 1)
        {
            Winner = p;
            return;
        }
        Winner = PList[^2];
    }
    
    public Participant FindPlayer(string playerName)
    {
        return PList.Find(x => x.name == playerName) ?? PList.Find(x => x.fName == playerName);
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

    public void Min()
    {
        PList = PList.OrderBy(x => x.roll).ToList();
    }

    public void Max()
    {
        PList = PList.OrderByDescending(x => x.roll).ToList();
    }

    public void Nearest(int nearest)
    {
        PList = PList.OrderBy(x => Math.Abs(nearest - x.roll)).ToList();
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
        
        Init();
    }

    public Participant(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        hasHighlight = false;
        highlightColor = new Vector4(0, 0, 0, 0);

        Init();
    }

    private void Init()
    {
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

    public string GetUsedName(bool randomPn)
    {
        return !randomPn ? GetReadableName() : randomName;
    }
}