using System;
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
using DeathRoll.Logic;

namespace DeathRoll;

public sealed class Plugin : IDalamudPlugin
{
    private readonly PluginCommandManager<Plugin> commandManager;
    private readonly ClientState clientState;
    private readonly Reg reg = new();
    public static Participants? Participants;
    public static GameState State = GameState.NotRunning;
    
    [PluginService] public static DataManager Data { get; private set; } = null!;
    [PluginService] public static ChatGui Chat { get; private set; } = null!;
    [PluginService] public static Framework Framework { get; private set; } = null!;

    public string Name => "Death Roll Helper";

    private DalamudPluginInterface PluginInterface { get; init; }
    private Configuration Configuration { get; init; }
    private PluginUI PluginUi { get; init; }
    private Rolls Rolls { get; init; }
    
    public Plugin(
        [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
        [RequiredVersion("1.0")] CommandManager commands,
        [RequiredVersion("1.0")] ClientState clientState)
    {
        PluginInterface = pluginInterface;
        this.clientState = clientState;

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);
        
        Participants = new Participants(Configuration);
        Rolls = new Rolls(Configuration, Participants);
        
        PluginUi = new PluginUI(Configuration, Participants, Rolls);
        commandManager = new PluginCommandManager<Plugin>(this, commands);

        Chat.ChatMessage += OnChatMessage;
        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

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
        // TODO check makes no sense with Tournament Mode
        if (!Configuration.On || State is GameState.NotRunning or GameState.Done or GameState.Crash) return;
        var xivChatType = (ushort) type;
        var channel = xivChatType & 0x7F;
        
        if (Configuration.Debug)
        {
            PluginLog.Debug("Chat Event fired.");
            PluginLog.Debug($"Sender: {sender}.");
            PluginLog.Debug($"ID: {id}.");
            PluginLog.Debug($"ChatType: {type}.");
            PluginLog.Debug($"Channel: {channel}.");
            PluginLog.Debug($"Content: {message}.");
            PluginLog.Debug($"Language: {clientState.ClientLanguage}.");
        }
        
        // 2122 = Random Roll 8266 = different Player Random roll?
        // Dice Roll: FC, LS, CWLS, Party
        if (!Enum.IsDefined(typeof(DeathRollChatTypes), xivChatType) && channel != 74) return;

        var dice = channel != 74;
        switch (dice)
        {
            case true when Configuration.OnlyRandom: // only /random is accepted
            case false when Configuration.OnlyDice: // only /dice is accepted
                return;
        }

        var m = reg.Match(message.ToString(), clientState.ClientLanguage, dice);
        if (!m.Success) return;

        var local = clientState?.LocalPlayer;
        if (local == null || local.HomeWorld.GameData?.Name == null)
        {
            PluginLog.Information("Unable to fetch character name.");
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
                if (Configuration.Debug) PluginLog.Debug($"message: {payload}");
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
                    if (Configuration.Debug) PluginLog.Debug($"Sender: {payload}");
                    playerName = payload switch
                    {
                        PlayerPayload playerPayload => playerPayload.DisplayedName,
                        _ => playerName
                    };
                }
        }

        if (Configuration.ActiveBlocklist && Configuration.SavedBlocklist.Contains(playerName))
        {
            if (Configuration.Debug) PluginLog.Debug("Blocked player tried to roll.");
            return;
        }


        // dice always needs the autoTranslate payload
        // if not has a player just written the exact string
        if (dice && !Configuration.DebugAllowDiceCheat && diceCommand != 3)
        {
            Chat.Print($"{playerName} tried to cheat~");
            return;
        }

        if (Configuration.Debug)
        {
            PluginLog.Debug($"Extracted Player Name: {playerName}.");
            PluginLog.Debug($"Message: {message}\n    Matches: 1: {m.Groups["player"]} 2: {m.Groups["roll"]} 3: {m.Groups["out"].Success} {m.Groups["out"]}");
        }

        Rolls.ParseRoll(m, playerName);
    }

    public static void SwitchState(GameState newState)
    {
        State = newState;
        if (newState is GameState.NotRunning) Participants?.Reset();
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