namespace DeathRoll.Data;

public enum DealerRules
{
    DealerHard17 = 0,
    DealerSoft17 = 1,
    DealerHard16 = 2,
    DealerSoft16 = 3
}

public static class RuleUtils
{
    public static readonly string[] ListOfNames = Enum.GetValues<DealerRules>().Select(n => n.GetName()).ToArray();

    public static string GetName(this DealerRules n)
    {
        return n switch
        {
            DealerRules.DealerHard17 => "Hard17",
            DealerRules.DealerHard16 => "Hard16",
            DealerRules.DealerSoft16 => "Soft17",
            DealerRules.DealerSoft17 => "Soft16",
            _ => "Unknown"
        };
    }
}