using System;
using System.Collections.Generic;
using System.Numerics;

namespace DeathRoll;

public class Participant
{
    public readonly string name;
    public readonly int roll;
    public readonly int outOf;
    public bool hasHighlight;
    public Vector4 highlightColor;
    public string randomName;

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
        this.hasHighlight = false;
        this.highlightColor = new Vector4(0, 0, 0, 0);
        
        randomName = GetRandomizedName();
    }

    public string GetReadableName()
    {
        return name.Replace("\uE05D", "\uE05D ");
    }

    private List<string> FirstName = new()
        {"Absolutely", "Completely", "Undoubtedly", "More or less", "Assuredly", "Utterly", "Kind of"};
    private List<string> Surnames = new()
        {"Fake", "Sus", "Imposter", "Pseudo"};
    public string GetRandomizedName()
    {
        var random = new Random();
        return $"{FirstName[random.Next(FirstName.Count)]} {Surnames[random.Next(Surnames.Count)]}";
    }
}