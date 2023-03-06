using System.Numerics;
using System.Text.RegularExpressions;

namespace DeathRoll;

public partial class Highlight
{
    private Regex? _compiled;
    public Vector4 Color;

    public string Regex = null!;
    public Regex CompiledRegex => _compiled ??= Compile();
    // and clear _compiled to null when Regex changes
    
    [GeneratedRegex("\\A(?!x)x")]
    private static partial Regex Unmatchable();
    
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

    public bool Matches(int number) => CompiledRegex.Match(number.ToString()).Success;
    
    private Regex Compile()
    {
        try
        {
            return new Regex(Regex);
        }
        catch (RegexParseException)
        {
            return Unmatchable();
        }
    }
}