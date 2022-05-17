using System.Numerics;

namespace DeathRoll;

public class Participant
{
    public readonly string name;
    public readonly int roll;
    public readonly int outOf;
    public bool hasHighlight;
    public Vector4 highlightColor;

    public Participant(string name, int roll, int outOf, bool hasHighlight, Vector4 highlightColor)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = hasHighlight;
        this.highlightColor = highlightColor;
    }    
    
    public Participant(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = false;
        this.highlightColor = new Vector4(0, 0, 0, 0);
    }
}