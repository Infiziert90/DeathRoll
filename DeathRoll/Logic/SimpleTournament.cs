using Dalamud.Logging;
using DeathRoll.Data;

namespace DeathRoll.Logic;

public class SimpleTournament
{
    private readonly Plugin Plugin;

    public readonly Participants InternalParticipants;
    public readonly List<List<Participant>> InternalBrackets = new();
    public readonly List<List<string>> FilledBrackets = new();

    private readonly List<int> StageDepth = new();
    private int CurrentIndex;
    private int CurrentStage;
    public int LastStage = 1;

    public Participant Player1 = new(Roll.Dummy());
    public Participant Player2 = new(Roll.Dummy());

    public SimpleTournament(Plugin plugin)
    {
        Plugin = plugin;
        InternalParticipants = new Participants(plugin.Configuration);
    }

    public void Parser(Roll roll)
    {
        switch (Plugin.State)
        {
            case GameState.Registration:
                Registration(roll);
                return;
            case GameState.Match:
                MatchGameMode(roll);
                return;
            default:
                return;
        }
    }

    public void NextMatch()
    {
        InternalParticipants.NextRound.Add(Plugin.Participants.Winner.Name);
        FillCurrentStageBrackets();

        CurrentIndex += 2;
        CalculateNextMatch();
    }

    public void ForfeitWin(Participant winner)
    {
        InternalParticipants.NextRound.Add(winner.Name);
        Plugin.Participants.Winner = winner;
        FillCurrentStageBrackets();

        CurrentIndex += 2;
        CalculateNextMatch();
    }

    private void MatchGameMode(Roll roll)
    {
        // check if player is in playerList
        if (!Plugin.Participants.PlayerNameList.Exists(x => x == roll.PlayerName))
            return;

        if (Plugin.Participants.PList.Count == 0)
        {
            if (roll.OutOf == -1)
                roll.OutOf = 1000;

            Plugin.Participants.Add(new Participant(roll));
            return;
        }

        if (roll.PlayerName == Plugin.Participants.Last.Name || Plugin.Participants.Last.Roll != roll.OutOf)
            return;

        Plugin.Participants.Add(new Participant(roll));
        if (roll.Result >= 2)
            return;

        // player lost this round
        Plugin.Participants.RoundDone = true;
    }

    private void Registration(Roll roll)
    {
        // check if registration roll is correct or if player is already in list
        if (roll.OutOf != -1)
            return;

        if (Plugin.Participants.PlayerNameList.Exists(x => x == roll.PlayerName))
            return;

        Plugin.Participants.Add(new Participant(roll));
    }

    public void Shuffle()
    {
        var rng = new Random(unchecked(Environment.TickCount * 31));

        var n = Plugin.Participants.PlayerNameList.Count;
        while (n > 1)
        {
            n--;
            var k = rng.Next(n + 1);
            (Plugin.Participants.PlayerNameList[k], Plugin.Participants.PlayerNameList[n]) = (Plugin.Participants.PlayerNameList[n], Plugin.Participants.PlayerNameList[k]);
        }

        InternalParticipants.PList = new List<Participant>(Plugin.Participants.PList);
        InternalParticipants.PlayerNameList = new List<string>(Plugin.Participants.PlayerNameList);
    }

    public void GenerateBrackets()
    {
        var count = InternalParticipants.PlayerNameList.Count;
        var neededPlayers = (int) Math.Pow(2, Math.Ceiling(Math.Log2(count)));
        var stages = (int) Math.Log2(neededPlayers) + 1;
        var magicNumber = neededPlayers - 1;

        for (var i = 0; i < stages; i++)
            InternalBrackets.Add(new List<Participant>());

        // fill with byes if need
        for (var i = count; i < neededPlayers; i++)
            InternalParticipants.Add(new Participant(Roll.Dummy("Byes")));

        foreach (var (_, idx) in InternalParticipants.PlayerNameList.Select((value, i) => (value, i)))
        {
            InternalBrackets[CurrentStage].Add(InternalParticipants.GetWithIndex(idx));
            InternalBrackets[CurrentStage].Add(InternalParticipants.GetWithIndex(magicNumber-idx));
            if (idx == neededPlayers / 2 - 1)
                break;
        }

        // Set LastStage for later
        LastStage = stages;
        CalculateDepth();
        FillBracketTable();
    }

    private void FillCurrentStageBrackets()
    {
        InternalBrackets[CurrentStage+1] = new List<Participant>();
        foreach (var name in InternalParticipants.NextRound)
            InternalBrackets[CurrentStage+1].Add(InternalParticipants.FindPlayer(name));

        FillBracketTable();
    }

    public void CalculateNextMatch()
    {
        Plugin.SwitchState(GameState.Prepare);

        try
        {
            if (CurrentIndex >= InternalBrackets[CurrentStage].Count)
            {
                PrepareNextStage();
                return;
            }

            (Player1, Player2) = (InternalBrackets[CurrentStage][CurrentIndex], InternalBrackets[CurrentStage][CurrentIndex+1]);

            Plugin.Participants.Reset();
            Plugin.Participants.PlayerNameList.Add(Player1.Name);
            Plugin.Participants.PlayerNameList.Add(Player2.Name);
        }
        catch (Exception e)
        {
            PluginLog.Error("Exception triggered.");
            PluginLog.Error(e.Message);
            Reset();
        }
    }

    private void PrepareNextStage()
    {
        try
        {
            CurrentIndex = 0;
            if (CurrentIndex + 1 == InternalParticipants.NextRound.Count)
            {
                Plugin.SwitchState(GameState.Done);
                return;
            }

            InternalParticipants.PlayerNameList = new List<string>(InternalParticipants.NextRound);
            InternalParticipants.NextRound.Clear();
            Plugin.Participants.PList.Clear();
            Plugin.Participants.PlayerNameList.Clear();

            CurrentStage += 1;
            CalculateNextMatch();
        }
        catch (Exception e)
        {
            PluginLog.Error("Exception triggered.");
            PluginLog.Error(e.Message);
            Reset(true);
        }
    }

    public void Reset(bool crashed = false)
    {
        Plugin.SwitchState(!crashed ? GameState.NotRunning : GameState.Crash);
        Plugin.Participants.Reset();
        InternalParticipants.Reset();
        InternalBrackets.Clear();
        FilledBrackets.Clear();

        CurrentIndex = 0;
        CurrentStage = 0;
        LastStage = 1;
    }

    // Prefill
    private void CalculateDepth()
    {
        // fill 0 and 1 stage which are not needed
        StageDepth.Add(-1);
        StageDepth.Add(-1);

        var previousDepth = 0;
        for (var i = 2; i < LastStage; i++)
        {
            if (i == 2)
            {
                StageDepth.Add(3);
                previousDepth = 3;
            }
            else
            {
                var calculatedDepth = previousDepth * 2 + 1;
                StageDepth.Add(calculatedDepth);
                previousDepth = calculatedDepth;
            }
        }
    }

    public void FillBracketTable()
    {
        FilledBrackets.Clear();
        var nestedIdx = new List<int>();
        for (var i = 0; i < LastStage; i++) nestedIdx.Add(i == 0 ? -1 : 0);

        for (var idx = 0; idx < InternalBrackets[0].Count * 2; idx++)
        {
            FilledBrackets.Add(new List<string>());
            for (var stage = 0; stage < LastStage; stage++)
            {
                if (nestedIdx[stage] >= InternalBrackets[stage].Count) break;
                switch (stage)
                {
                    case 0 when idx % 2 == 0:
                        FilledBrackets[idx].Add(InternalBrackets[stage][idx / 2].GetDisplayName());
                        break;
                    case 0 when idx % 2 == 1:
                        // fill the cell with spaces to give it font height
                        FilledBrackets[idx].Add("    ");
                        break;
                    case 1 when idx != 1 && idx % 4 != 1:
                        // skip this cell later
                        FilledBrackets[idx].Add("x");
                        break;
                    case 1:
                        FilledBrackets[idx].Add(InternalBrackets[stage][nestedIdx[stage]].GetDisplayName());
                        nestedIdx[stage]++;
                        break;
                    default:
                    {
                        var stageDepth = StageDepth[stage];
                        if (idx != stageDepth && idx != (stageDepth * 2 + 2) * nestedIdx[stage] + stageDepth)
                        {
                            // skip this cell later
                            FilledBrackets[idx].Add("x");
                            break;
                        }

                        FilledBrackets[idx].Clear();
                        for (var s = 0; s < stage; s++) FilledBrackets[idx].Add("  ");
                        FilledBrackets[idx].Add(InternalBrackets[stage][nestedIdx[stage]].GetDisplayName());
                        nestedIdx[stage]++;
                        break;
                    }
                }
            }
        }
    }

    #region Testing
    public void AutoWin()
    {
        var rng = new Random(Environment.TickCount);
        Plugin.Participants.Add(new Participant(Roll.Dummy(Plugin.Participants.PlayerNameList[rng.Next(0, 2)])));
        Plugin.Participants.Add(new Participant(Roll.Dummy(Plugin.Participants.PlayerNameList[rng.Next(0, 2)])));
        NextMatch();
    }

    public void AutoRegistration(int n)
    {
        for (var i = 1; i <= n; i++)
            Plugin.Participants.Add(new Participant(Roll.Dummy($"Real Player {i}")));
    }
    #endregion
}