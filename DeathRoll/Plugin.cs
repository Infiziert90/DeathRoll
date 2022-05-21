using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.ClientState;
using System;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using System.Numerics;
using DeathRoll.Attributes;


namespace DeathRoll
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static DataManager Data { get; private set; } = null!;
        [PluginService] public static ChatGui Chat { get; private set; } = null!;
        
        public string Name => "Death Roll Helper";
        
        Regex randomRollRegex = new Regex(@"^Random! ([a-zA-Z'-]+ [a-zA-Z'-]*|You) roll[s]? a (\d+)(?: \(out of (\d+)\))?.");
        Regex diceRollRegex = new Regex(@"^Random! (?:\(1-(\d+)\) )?(\d+)");
        
        
        private DalamudPluginInterface PluginInterface { get; init; }
        private Configuration Configuration { get; init; }
        private PluginUI PluginUi { get; init; }
        private ClientState clientState;
        
        private readonly PluginCommandManager<Plugin> commandManager;

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commands,
            [RequiredVersion("1.0")] ClientState clientState)
        {
            this.PluginInterface = pluginInterface;
            this.clientState = clientState;
            
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            
            this.PluginUi = new PluginUI(this.Configuration);
            
            this.commandManager = new PluginCommandManager<Plugin>(this, commands);
            
            Chat.ChatMessage += OnChatMessage;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }
        
        [Command("/drh")]
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
                    this.PluginUi.SettingsVisible = true;
                    break;
                default:
                    this.PluginUi.Visible = true;
                    break;
            }
        }
        
        [Command("/deathroll")]
        [HelpMessage("  ")]
        public void PluginCommandFallback(string command, string args)
        {
            PluginCommand(command, args);
        }
        
        public void Dispose()
        {
            Chat.ChatMessage -= OnChatMessage;
            this.PluginUi.Dispose();
            this.commandManager.Dispose();
        }
        
        private void OnChatMessage(XivChatType type, uint id, ref SeString sender, ref SeString message, ref bool handled)
        {
            if (!Configuration.On || !Configuration.ActiveRound) return;
            if (Configuration.DebugChat)
            {
                PluginLog.Debug(("Deathroll: Chat Event fired."));
                PluginLog.Debug(($"Deathroll: Sender: {sender}."));
                PluginLog.Debug(($"Deathroll: ID: {id}."));
                PluginLog.Debug(($"Deathroll: ChatType: {type}."));
            }
            
            var xivChatType = (ushort)type;
            var channel = (xivChatType & 0x7F);
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
                    if (Configuration.DebugChat) PluginLog.Debug($"Deathroll: message: {payload}");
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
                {
                    foreach (var payload in sender.Payloads)
                    {
                        if (Configuration.DebugChat) PluginLog.Debug($"Deathroll: Sender: {payload}");
                        playerName = payload switch
                        {
                            PlayerPayload playerPayload => playerPayload.DisplayedName,
                            _ => playerName
                        };
                    }
                }

            }

            if (Configuration.ActiveBlocklist && Configuration.SavedBlocklist.Contains(playerName))
            {
                if (Configuration.DebugChat) PluginLog.Debug($"Deathroll: Blocked player tried to roll.");
                return;
            }
            
            
            // dice always needs the autoTranslate payload
            // if not has a player just written the exact string
            if (dice && !Configuration.DebugAllowDiceCheat && diceCommand != 3)
            {
                Chat.Print($"Deathroll: {playerName} tried to cheat~");
                return;
            }
            
            var exists = PluginUi.Participants.Exists(x => x.name == playerName);
            switch (Configuration.RerollAllowed)
            {
                case false when exists:
                {
                    if (Configuration.DebugChat) PluginLog.Debug(($"Deathroll: Player already rolled, no overwrite allowed."));
                    return;
                }
                case true when exists:
                    PluginUi.DeleteEntry(playerName);
                    break;
            }

            if (Configuration.DebugChat)
            {
                PluginLog.Debug(($"Deathroll: Extracted Player Name: {playerName}."));
                PluginLog.Debug(($"Deathroll: Add message to viewer."));
                PluginLog.Debug($"Message: {message.ToString()}\n" +
                                $"    Matches: 1: {m.Groups[1]} 2: {m.Groups[2]} 3: {m.Groups[3].Success} {m.Groups[3]}");
            }

            try
            {
                var parsedRoll = Int32.Parse(m.Groups[2].Value);
                var parsedOutOf = m.Groups[3].Success ? Int32.Parse(m.Groups[3].Value) : -1;
                if (dice)
                {
                    // adjusting to different reqex
                    parsedOutOf = m.Groups[1].Success ? Int32.Parse(m.Groups[1].Value) : -1;
                }

                var hasHighlight = false;
                Vector4 hightlightColor = new Vector4();
                if (Configuration.ActiveHightlighting && Configuration.SavedHighlights.Count > 0)
                {
                    foreach (var highlight in Configuration.SavedHighlights)
                    {
                        if (highlight.CompiledRegex.Match(m.Groups[2].Value).Success)
                        {
                            hasHighlight = true;
                            hightlightColor = highlight.Color;
                            break;
                        };
                    }
                }

                PluginUi.Participants.Add(hasHighlight
                    ? new Participant(playerName, parsedRoll, parsedOutOf, true, hightlightColor)
                    : new Participant(playerName, parsedRoll, parsedOutOf));

                switch(Configuration.CurrentMode)
                {
                    case 0: PluginUi.Min();break;
                    case 1: PluginUi.Max();break;
                    case 2: PluginUi.Nearest();break;
                }
            }
            catch (FormatException  e)
            {
                Chat.PrintError("Deathroll: Unable to parse rolls, turning plugin off.");
                PluginLog.Error(e.ToString());
                TurnOff();
                return;
            }

            PluginUi.RollTable.IsOutOfUsed = PluginUi.Participants.Exists(x => x.outOf > -1);
            
            return;
        }
        
        private void TurnOff()
        {
            Configuration.On = false;
            Configuration.Save();
        }

        private void DrawUI()
        {
            this.PluginUi.Draw();
        }

        private void DrawConfigUI()
        {
            this.PluginUi.SettingsVisible = true;
        }
    }
}
