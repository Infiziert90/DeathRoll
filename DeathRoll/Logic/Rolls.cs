using System;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Logging;
using DeathRoll.Data;

namespace DeathRoll.Logic;

public class Rolls
{
    private readonly Configuration configuration;
    private readonly Participants participants;
    public readonly SimpleTournament SimpleTournament;
    public readonly Blackjack Blackjack;
    
    public Rolls(Configuration configuration, Participants participants)
    {
        this.configuration = configuration;
        this.participants = participants;
        SimpleTournament = new SimpleTournament(configuration, participants);
        Blackjack = new Blackjack(configuration, participants);
    }
    
    public void ParseRoll(Roll roll)
    {
        if (configuration.Debug)
        {
            PluginLog.Information($"Extracted Player Name: {roll.PlayerName}.");
            PluginLog.Information($"Regex: Roll {roll.Rolled} OutOf {roll.OutOf}");
        }
        
        try
        {
            switch (configuration.GameMode)
            {
                case GameModes.Venue:
                    NormalGameMode(roll);
                    break;
                case GameModes.DeathRoll:
                    DeathRollGameMode(roll);
                    break;                
                case GameModes.Tournament:
                    SimpleTournament.Parser(roll);
                    break;
                case GameModes.Blackjack:
                    Blackjack.Parser(roll);
                    break;
                default:
                    return;
            }
        }
        catch (FormatException e)
        {
            Plugin.Chat.PrintError("Unable to parse roll.");
            PluginLog.Error(e.ToString());
        }
    }

    private void NormalGameMode(Roll roll)
    {
        var exists = participants.PList.Exists(x => x.Name == roll.PlayerName);
        switch (configuration.RerollAllowed)
        {
            case false when exists:
            {
                if (configuration.Debug) PluginLog.Information("Player already rolled, no reroll allowed.");
                return;
            }
            case true when exists:
                participants.DeleteEntry(roll.PlayerName);
                break;
        }
        
        Highlight? highlight = null;
        if (configuration is { ActiveHighlighting: true, SavedHighlights.Count: > 0 })
            highlight = configuration.SavedHighlights.FirstOrDefault(hl => hl.Matches(roll.Rolled));

        participants.Add(highlight != null
            ? new Participant(roll, highlight)
            : new Participant(roll));

        participants.UpdateSorting();
        participants.IsOutOfUsed = participants.PList.Any(x => x.OutOf > -1);
    }
    
    private void DeathRollGameMode(Roll roll)
    {
        // check that player not in playerList and new players not get accepted
        if (participants.PlayerNameList.All(x => x != roll.PlayerName) && !configuration.AcceptNewPlayers)
            return;

        // check if rolls exists, if not, add first roll with OutOf 1000
        if (participants.PList.Count == 0)
        {
            if (roll.OutOf == -1) roll.OutOf = 1000;
            participants.Add(new Participant(roll));
            return;
        }
        
        // check if same player and check if OutOf is correct
        // deathroll rules: first roll /random all following /random {PreviousNumber}
        if (roll.PlayerName == participants.Last.Name || participants.Last.Roll != roll.OutOf) return;
        
        participants.Add(new Participant(roll));
        if (roll.Rolled >= 2) return;
        
        // player lost this round
        configuration.AcceptNewPlayers = false;
        configuration.Save();
        Plugin.SwitchState(GameState.Done);
    }
}

public class Roll
{
    public int Rolled = 1000;
    public int OutOf = 1000;
    public string PlayerName;

    private Roll(string name) { PlayerName = name; }
    
    public Roll(Match m, string playerName)
    {
        Rolled = int.Parse(m.Groups["roll"].Value);
        OutOf = m.Groups["out"].Success ? int.Parse(m.Groups["out"].Value) : -1;
        PlayerName = playerName;
    }

    public static Roll Dummy(string name = "Unknown") => new(name);
}