using System.Text.RegularExpressions;
using Dalamud;

namespace DeathRoll.Logic;

public static class Reg
{
    private static Regex diceRollRegexEN = new(@"^Random! (?:\(1-(?<out>\d+)\) )?(?<roll>\d+)");
    private static Regex randomRollRegexEN = new(@"^Random! (?<player>[a-zA-Z'-]+ [a-zA-Z'-]*|You) roll[s]? a (?<roll>\d+)(?: \(out of (?<out>\d+)\))?\.");
    
    private static Regex diceRollRegexDE = new(@"^Würfeln! (?:\(1-(?<out>\d+)\) )?(?<roll>\d+)");
    private static Regex randomRollRegexDE = new(@"^(?<player>[a-zA-Z'-]+ [a-zA-Z'-]*|Du) (?:(?:würfelst|würfelt)|(?:hast|hat) mit dem|würfelt) (?<out>\d+)?(?:eine |-seitigen Würfel eine) (?<roll>\d+)(?:\.| gewürfelt!)");
    
    private static Regex diceRollRegexFR = new(@"^Lancer (?:de dé|d'un dé )(?<out>\d+)? ! (?<roll>\d+)");
    private static Regex randomRollRegexFR = new(@"^(?:Lancer d'un dé (?<out>\d+) ! )?(?<player>Vous|[a-zA-Z'-]+ [a-zA-Z'-]*) (?:jetez|jette|obtenez un|obtient un) (?:les dés et (?:obtenez|obtient) )?(?<roll>\d+) !");

    public static Match Match(string message, ClientLanguage language, bool dice)
    {
        var reg = language switch
        {
            ClientLanguage.English => !dice ? randomRollRegexEN : diceRollRegexEN,
            ClientLanguage.German => !dice ? randomRollRegexDE : diceRollRegexDE,
            ClientLanguage.French => !dice ? randomRollRegexFR : diceRollRegexFR,
            _ => randomRollRegexEN
        };
        return reg.Match(message);
    } 
}