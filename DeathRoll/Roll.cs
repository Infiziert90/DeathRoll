using System.Numerics;
using System.Security.Cryptography;

namespace DeathRoll;

public class Roll
{
    public readonly string name;
    public readonly int roll;
    public readonly int outOf;
    public bool hasHighlight;
    public Vector4 highlightColor;

    public Roll(string name, int roll, int outOf, bool hasHighlight, Vector4 highlightColor)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = hasHighlight;
        this.highlightColor = highlightColor;
    }    
    
    public Roll(string name, int roll, int outOf)
    {
        this.name = name;
        this.roll = roll;
        this.outOf = outOf;
        this.hasHighlight = false;
        this.highlightColor = new Vector4(0, 0, 0, 0);
    }
}