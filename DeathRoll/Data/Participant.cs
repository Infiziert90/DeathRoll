using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace DeathRoll;

public class Participants
{
    public List<Participant> PList = new();
    
    public void DeleteEntry(string name)
    {
        PList.RemoveAll(x => x.name == name);
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
    
    public void Update(Configuration configuration)
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
}

public class Participant
{
    public readonly string name;
    public readonly int outOf;
    public readonly int roll;

    public bool hasHighlight;
    public Vector4 highlightColor;
    public string randomName;

    private readonly List<string> FirstName = new()
        {"Absolutely", "Completely", "Undoubtedly", "More or less", "Assuredly", "Utterly", "Kind of"};
    private readonly List<string> Surnames = new()
        {"Fake", "Sus", "Imposter", "Pseudo"};

    public Participant(string name, int roll, int outOf, bool hasHighlight, Vector4 highlightColor)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = hasHighlight;
        this.highlightColor = highlightColor;

        randomName = GetRandomizedName();
    }

    public Participant(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        hasHighlight = false;
        highlightColor = new Vector4(0, 0, 0, 0);

        randomName = GetRandomizedName();
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
}