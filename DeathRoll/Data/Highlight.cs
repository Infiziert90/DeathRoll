using System.Numerics;
using System.Text.RegularExpressions;

namespace DeathRoll;

public class Highlight
{
    public string Regex;
    public Vector4 Color;
    private Regex? _compiled = null;
    public Regex CompiledRegex => this._compiled ??= Compile();
    // and clear _compiled to null when Regex changes

    public Highlight() { }
            
    public Highlight(string r, Vector4 c)
    {
        this.Regex = r;
        this.Color = c;
    }

    public void Update(string r, Vector4 c)
    {
        this.Regex = r;
        this.Color = c;
        this._compiled = null;
    }

    private Regex Compile()
    {
        try { return new Regex(this.Regex); }
        catch (RegexParseException) { return new Regex(@"\A(?!x)x"); } // unmatchable regex
    }
}