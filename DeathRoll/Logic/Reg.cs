using System.Text.RegularExpressions;
using Dalamud;

namespace DeathRoll.Logic;

public static class Reg
{
    private static readonly Regex DiceRollRegexEN = new(@"^Random! (?:\(1-(?<out>\d+)\) )?(?<roll>\d+)");
    private static readonly Regex RandomRollRegexEN = new(@"^Random! (?<player>[a-zA-Z'-]+.? [a-zA-Z'-]*.?|You) roll[s]? a (?<roll>\d+)(?: \(out of (?<out>\d+)\))?\.");

    private static readonly Regex DiceRollRegexDE = new(@"^Würfeln! (?:\(1-(?<out>\d+)\) )?(?<roll>\d+)");
    private static readonly Regex RandomRollRegexDE = new(@"^(?<player>[a-zA-Z'-]+.? [a-zA-Z'-]*.?|Du) (?:(?:würfelst|würfelt)|(?:hast|hat) mit dem|würfelt) (?<out>\d+)?(?:eine |-seitigen Würfel eine) (?<roll>\d+)(?:\.| gewürfelt!)");

    private static readonly Regex DiceRollRegexFR = new(@"^Lancer (?:de dé|d'un dé )(?<out>\d+)?! (?<roll>\d+)");
    private static readonly Regex RandomRollRegexFR = new(@"^(?:Lancer d'un dé (?<out>\d+)! )?(?<player>Vous|[a-zA-Z'-]+.? [a-zA-Z'-]*.?) (?:jetez|jette|obtenez un|obtient un) (?:les dés et (?:obtenez|obtient) )?(?<roll>\d+)!");

    private static readonly Regex DiceRollRegexJP = new(@"^ダイス！ (?:MAX(?<out>\d+)  )?(?<roll>\d+)");
    private static readonly Regex RandomRollRegexJP = new(@"^(?:(?<out>\d+)面)?ダイス！ (?<player>[a-zA-Z'-]+.? [a-zA-Z'-]*.?)は、(?<roll>\d+)を出した！");

    public static Match Match(string message, ClientLanguage language, bool dice)
    {
        var reg = language switch
        {
            ClientLanguage.English => !dice ? RandomRollRegexEN : DiceRollRegexEN,
            ClientLanguage.German => !dice ? RandomRollRegexDE : DiceRollRegexDE,
            ClientLanguage.French => !dice ? RandomRollRegexFR : DiceRollRegexFR,
            ClientLanguage.Japanese => !dice ? RandomRollRegexJP : DiceRollRegexJP,
            _ => RandomRollRegexEN
        };

        return reg.Match(message);
    }
}