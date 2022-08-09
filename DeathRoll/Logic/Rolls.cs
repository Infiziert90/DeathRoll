using System;
using System.Linq;
using System.Numerics;
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
    
    public void ParseRoll(Match m, string playerName)
    {
        try
        {
            var parsedRoll = int.Parse(m.Groups["roll"].Value);
            var parsedOutOf = m.Groups["out"].Success ? int.Parse(m.Groups["out"].Value) : -1;

            switch (configuration.GameMode)
            {
                case GameModes.Venue:
                    NormalGameMode(playerName, parsedRoll, parsedOutOf);
                    break;
                case GameModes.DeathRoll:
                    DeathRollGameMode(playerName, parsedRoll, parsedOutOf);
                    break;                
                case GameModes.Tournament:
                    SimpleTournament.Parser(playerName, parsedRoll, parsedOutOf);
                    break;
                case GameModes.Blackjack:
                    Blackjack.Parser(playerName, parsedRoll, parsedOutOf);
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

    public void NormalGameMode(string playerName, int parsedRoll, int parsedOutOf)
    {
        var exists = participants.PList.Exists(x => x.name == playerName);
        switch (configuration.RerollAllowed)
        {
            case false when exists:
            {
                if (configuration.Debug) PluginLog.Debug("Player already rolled, no reroll allowed.");
                return;
            }
            case true when exists:
                participants.DeleteEntry(playerName);
                break;
        }
        
        var hasHighlight = false;
        var hightlightColor = new Vector4();
        if (configuration.ActiveHighlighting && configuration.SavedHighlights.Count > 0)
            foreach (var highlight in configuration.SavedHighlights.Where(highlight => 
                         highlight.CompiledRegex.Match(parsedRoll.ToString()).Success))
            {
                hasHighlight = true;
                hightlightColor = highlight.Color;
                break;
            }

        participants.Add(hasHighlight
            ? new Participant(playerName, parsedRoll, parsedOutOf, hightlightColor)
            : new Participant(playerName, parsedRoll, parsedOutOf));

        participants.UpdateSorting();
        participants.IsOutOfUsed = participants.PList.Exists(x => x.outOf > -1);
    }
    
    private void DeathRollGameMode(string playerName, int parsedRoll, int parsedOutOf)
    {
        // check if player is in playerList, if not, check if new players get accepted
        if (!participants.PlayerNameList.Exists(x => x == playerName))
        {
            if (!configuration.AcceptNewPlayers) return;
        }
        
        // check if rolls exists, if not, add first roll with OutOf 1000
        if (participants.PList.Count == 0)
        {
            if (parsedOutOf == -1) parsedOutOf = 1000;
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
            return;
        }
        
        // check if same player and check if OutOf is correct
        // deathroll rules -> first rolls /random -> all others /random {PreviousNumber}
        if (playerName == participants.Last.name || participants.Last.roll != parsedOutOf) return;
        
        // everything above 1 is ongoing
        if (parsedRoll >= 2)
        {
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
        }
        else // player lost this round
        {
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
            configuration.AcceptNewPlayers = false;
            configuration.Save();
            Plugin.SwitchState(GameState.Done);
        }
    }
}