using System.Numerics;
using System.Text.RegularExpressions;

namespace DeathRoll;

public class Highlight
{
    private Regex? _compiled;
    public Vector4 Color;

    public string Regex;
    public Regex CompiledRegex => _compiled ??= Compile();
    // and clear _compiled to null when Regex changes

    public Highlight() { }

    public Highlight(string r, Vector4 c)
    {
        Regex = r;
        Color = c;
    }

    public void Update(string r, Vector4 c)
    {
        Regex = r;
        Color = c;
        _compiled = null;
    }

    private Regex Compile()
    {
        try
        {
            return new Regex(Regex);
        }
        catch (RegexParseException)
        {
            return new Regex(@"\A(?!x)x");
        } // unmatchable regex
    }
}