using DeathRoll.Logic;

namespace DeathRoll.Data;

public class Participants
{
    private readonly Configuration Configuration;

    public List<Participant> PList = new();
    public List<string> PlayerNameList = new();

    // venue
    public bool IsOutOfUsed;

    // deathroll
    public Participant Last = new(Roll.Dummy());
    public Participant Winner = new(Roll.Dummy());

    // tournament
    public List<string> NextRound = new();
    public bool RoundDone;

    public Participants(Configuration configuration)
    {
        Configuration = configuration;
    }

    public void Add(Participant p)
    {
        if (p.Name == "Byes" || !PlayerNameList.Exists(x => x == p.Name))
            PlayerNameList.Add(p.Name);

        PList.Add(p);
        Last = p;
        Winner = PList[^(PList.Count == 1 ? 1 : 2)]; // we only ever take last if there is one entry
    }

    public void DeleteEntry(string name)
    {
        PList.RemoveAll(x => x.Name == name);
        PlayerNameList.RemoveAll(x => x == name);
    }

    public void UpdateSorting()
    {
        PList = Configuration.SortingMode switch
        {
            SortingType.Min => PList.OrderBy(x => x.Roll).ToList(),
            SortingType.Max => PList.OrderByDescending(x => x.Roll).ToList(),
            SortingType.Nearest => PList.OrderBy(x => Math.Abs(Configuration.Nearest - x.Roll)).ToList(),
            _ => PList
        };
    }

    public void Update()
    {
        if (Configuration is { ActiveHighlighting: false, SavedHighlights.Count: 0 }) return;

        foreach (var roll in PList)
        {
            var highlight = Configuration.SavedHighlights.FirstOrDefault(hl => hl.Matches(roll.Roll));
            if (highlight == null)
                roll.UpdateColor(false);
            else
                roll.UpdateColor(true, highlight.Color);
        }
    }

    public Participant FindPlayer(string playerName) => PList.First(x => x.Name == playerName);

    public List<Participant> FindAll(string playerName) => PList.FindAll(x => x.Name == playerName);

    public Participant GetWithIndex(int idx) => FindPlayer(PlayerNameList[idx]);

    public string LookupDisplayName(string playerName) => FindPlayer(playerName).GetDisplayName();

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
    public readonly string Name;
    public readonly int Roll;
    public readonly int OutOf;

    public bool HasHighlight;
    public Vector4 HighlightColor = new(0, 0, 0, 0);

    public Participant(Roll newRoll, Highlight? highlight = null)
    {
        Name = newRoll.PlayerName;
        Roll = newRoll.Result;
        OutOf = newRoll.OutOf;

        if (highlight == null)
            return;

        HasHighlight = true;
        HighlightColor = highlight.Color;
    }

    public void UpdateColor(bool hasHl, Vector4 hlColor = new())
    {
        HasHighlight = hasHl;
        HighlightColor = hlColor;
    }

    public string GetDisplayName() => !DebugConfig.RandomizeNames ? FName : Utils.GenerateHashedName(Name);

    private string FName => Name.Replace("\uE05D", "\uE05D ");
}

public enum SortingType
{
   Min = 0,
   Max = 1,
   Nearest = 2,
}