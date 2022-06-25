using System;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using DeathRoll.Attributes;

namespace DeathRoll;

public sealed class Plugin : IDalamudPlugin
{
    private readonly PluginCommandManager<Plugin> commandManager;
    private readonly ClientState clientState;
    private readonly Regex diceRollRegex = new(@"^Random! (?:\(1-(\d+)\) )?(\d+)");
    private readonly Regex randomRollRegex =
        new(@"^Random! ([a-zA-Z'-]+ [a-zA-Z'-]*|You) roll[s]? a (\d+)(?: \(out of (\d+)\))?.");

    public Participants Participants;
    
    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commands,
        [RequiredVersion("1.0")] ClientState clientState)
    {
        PluginInterface = pluginInterface;
        this.clientState = clientState;
        
        Participants = new Participants();
        
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        
        PluginUi = new PluginUI(Configuration, Participants);
        commandManager = new PluginCommandManager<Plugin>(this, commands);

        Chat.ChatMessage += OnChatMessage;
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    [PluginService] public static DataManager Data { get; } = null!;
    [PluginService] public static ChatGui Chat { get; } = null!;
    [PluginService] public static Framework Framework { get; } = null!;


    private DalamudPluginInterface PluginInterface { get; }
    private Configuration Configuration { get; }
    private PluginUI PluginUi { get; }

    public string Name => "Death Roll Helper";

    public void Dispose()
    {
        Chat.ChatMessage -= OnChatMessage;
        PluginUi.Dispose();
        commandManager.Dispose();
    }

    [Command("/drh")]
    [Aliases("/deathroll")]
    [HelpMessage("Toggles UI\nArguments:\non - Turns on\noff - Turns off\nconfig - Opens config")]
    public void PluginCommand(string command, string args)
    {
        switch (args)
        {
            case "on":
                Configuration.On = true;
                Configuration.Save();
                break;
            case "off":
                Configuration.On = false;
                Configuration.Save();
                break;
            case "config":
                PluginUi.SettingsVisible = true;
                break;
            default:
                PluginUi.Visible = true;
                break;
        }
    }

    private void OnChatMessage(XivChatType type, uint id, ref SeString sender, ref SeString message, ref bool handled)
    {
        if (!Configuration.On || !Configuration.ActiveRound) return;
        if (Configuration.DebugChat)
        {
            PluginLog.Debug("Chat Event fired.");
            PluginLog.Debug($"Sender: {sender}.");
            PluginLog.Debug($"ID: {id}.");
            PluginLog.Debug($"ChatType: {type}.");
        }

        var xivChatType = (ushort) type;
        var channel = xivChatType & 0x7F;
        // 2122 = Random Roll 8266 = different Player Radom roll?
        // Dice Roll: FC, LS, CWLS, Party
        if (!Enum.IsDefined(typeof(DeathRollChatTypes), xivChatType) && channel != 74) return;

        Regex reg; bool dice;
        if (channel == 74) (reg, dice) = (randomRollRegex, false);
        else (reg, dice) = (diceRollRegex, true);

        if (dice && Configuration.OnlyRandom) return; // only /random is accepted
        if (!dice && Configuration.OnlyDice) return; // only /dice is accepted

        var m = reg.Match(message.ToString());
        if (!m.Success) return;

        var local = clientState?.LocalPlayer;
        if (local == null || local.HomeWorld.GameData?.Name == null)
        {
            TurnOff();
            Chat.PrintError("Deathroll: Unable to fetch character name.");
            return;
        }

        var diceCommand = 0;
        var playerName = $"{local.Name}\uE05D{local.HomeWorld.GameData.Name}";
        var isLocalPlayer = sender.ToString() == local.Name.ToString();
        if (!isLocalPlayer || dice)
        {
            var found = isLocalPlayer;
            foreach (var payload in message.Payloads) // try to get name and check for dice cheating
            {
                if (Configuration.DebugChat) PluginLog.Debug($"message: {payload}");
                switch (payload)
                {
                    case PlayerPayload playerPayload:
                        playerName = playerPayload.DisplayedName;
                        found = true;
                        break;
                    case IconPayload iconPayload:
                        switch (iconPayload.Icon)
                        {
                            case BitmapFontIcon.Dice:
                            case BitmapFontIcon.AutoTranslateBegin:
                            case BitmapFontIcon.AutoTranslateEnd:
                                diceCommand += 1;
                                break;
                        }

                        break;
                }
            }

            if (!found) // get playerName from payload
                foreach (var payload in sender.Payloads)
                {
                    if (Configuration.DebugChat) PluginLog.Debug($"Sender: {payload}");
                    playerName = payload switch
                    {
                        PlayerPayload playerPayload => playerPayload.DisplayedName,
                        _ => playerName
                    };
                }
        }

        if (Configuration.ActiveBlocklist && Configuration.SavedBlocklist.Contains(playerName))
        {
            if (Configuration.DebugChat) PluginLog.Debug("Blocked player tried to roll.");
            return;
        }


        // dice always needs the autoTranslate payload
        // if not has a player just written the exact string
        if (dice && !Configuration.DebugAllowDiceCheat && diceCommand != 3)
        {
            Chat.Print($"Deathroll: {playerName} tried to cheat~");
            return;
        }

        var exists = Participants.PList.Exists(x => x.name == playerName);
        switch (Configuration.RerollAllowed)
        {
            case false when exists:
            {
                if (Configuration.DebugChat) PluginLog.Debug("Player already rolled, no overwrite allowed.");
                return;
            }
            case true when exists:
                Participants.DeleteEntry(playerName);
                break;
        }

        if (Configuration.DebugChat)
        {
            PluginLog.Debug($"Extracted Player Name: {playerName}.");
            PluginLog.Debug("Add message to viewer.");
            PluginLog.Debug($"Message: {message}\n" +
                            $"    Matches: 1: {m.Groups[1]} 2: {m.Groups[2]} 3: {m.Groups[3].Success} {m.Groups[3]}");
        }

        try
        {
            var parsedRoll = int.Parse(m.Groups[2].Value);
            var parsedOutOf = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : -1;
            if (dice)
                // adjusting to different reqex
                parsedOutOf = m.Groups[1].Success ? int.Parse(m.Groups[1].Value) : -1;

            var hasHighlight = false;
            var hightlightColor = new Vector4();
            if (Configuration.ActiveHighlighting && Configuration.SavedHighlights.Count > 0)
                foreach (var highlight in Configuration.SavedHighlights)
                {
                    if (highlight.CompiledRegex.Match(m.Groups[2].Value).Success)
                    {
                        hasHighlight = true;
                        hightlightColor = highlight.Color;
                        break;
                    }

                    ;
                }

            Participants.PList.Add(hasHighlight
                ? new Participant(playerName, parsedRoll, parsedOutOf, true, hightlightColor)
                : new Participant(playerName, parsedRoll, parsedOutOf));

            switch (Configuration.CurrentMode)
            {
                case 0:
                    Participants.Min();
                    break;
                case 1:
                    Participants.Max();
                    break;
                case 2:
                    Participants.Nearest(Configuration.Nearest);
                    break;
            }
        }
        catch (FormatException e)
        {
            Chat.PrintError("Deathroll: Unable to parse rolls, turning plugin off.");
            PluginLog.Error(e.ToString());
            TurnOff();
            return;
        }

        PluginUi.RollTable.IsOutOfUsed = Participants.PList.Exists(x => x.outOf > -1);
    }

    private void TurnOff()
    {
        Configuration.On = false;
        Configuration.Save();
    }

    private void DrawUI()
    {
        PluginUi.Draw();
    }

    private void DrawConfigUI()
    {
        PluginUi.SettingsVisible = true;
    }
}