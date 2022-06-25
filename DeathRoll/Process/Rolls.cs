using System;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Logging;

namespace DeathRoll.Process;

public class Rolls
{
    private readonly Configuration configuration;
    private readonly Participants participants;
    
    public Rolls(Configuration configuration, Participants participants)
    {
        this.configuration = configuration;
        this.participants = participants;
    }
    
    public void ParseRoll(bool dice, Match m, string playerName)
    {
        try
        {
            var parsedRoll = int.Parse(m.Groups[2].Value);
            var parsedOutOf = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : -1;
            if (dice) // adjusting to different reqex
                parsedOutOf = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : -1;

            switch (configuration.GameMode)
            {
                case 0:
                    NormalGameMode(playerName, parsedRoll, parsedOutOf);
                    break;
                case 1:
                    DeathRollGameMode(playerName, parsedRoll, parsedOutOf);
                    break;
            }
        }
        catch (FormatException e)
        {
            Plugin.Chat.PrintError("Unable to parse rolls.");
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
                if (configuration.DebugChat) PluginLog.Debug("Player already rolled, no overwrite allowed.");
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

        switch (configuration.CurrentMode)
        {
            case 0:
                participants.Min();
                break;
            case 1:
                participants.Max();
                break;
            case 2:
                participants.Nearest(configuration.Nearest);
                break;
        }
        
        participants.IsOutOfUsed = participants.PList.Exists(x => x.outOf > -1);
    }
    
    private void DeathRollGameMode(string playerName, int parsedRoll, int parsedOutOf)
    {
        // check if player is in playerList, if not, check if new players get accepted
        if (!participants.PlayerNameList.Exists(x => x == playerName))
        {
            if (!configuration.AcceptNewPlayers) return; 
            participants.PlayerNameList.Add(playerName);
        }
        
        // check if rolls exists, if not, add first roll with OutOf 1000
        if (participants.PList.Count == 0)
        {
            if (parsedOutOf == -1) parsedOutOf = 1000;
            participants.Add(new Participant(playerName, parsedRoll, parsedOutOf));
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
            participants.RoundDone = true;
            configuration.ActiveRound = false;
            configuration.AcceptNewPlayers = false;
            configuration.Save();
        }
    }
}