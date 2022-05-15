using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Network.Structures;
using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Game.ClientState;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Logging;
using System.Numerics;
using DeathRoll.Attributes;
using Lumina.Data.Parsing;


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
        
        [Command("/dr")]
        [HelpMessage("Toggles UI\nArguments:\non - Turns on\noff - Turns off\nconfig - Opens config")]
        public void PluginCommand(string command, string args)
        {
            if (args == "on")
            {
                Configuration.On = true;
                Configuration.Save();
            } 
            else if (args == "off")
            {
                Configuration.On = false;
                Configuration.Save();
            }            
            else if (args == "config")
            {
                this.PluginUi.SettingsVisible = true;
            }
            else
            {
                this.PluginUi.Visible = true;
            }
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
            
            // 2122 = Random Roll 8266 = different Player Radom roll?
            // Dice Roll: FC, LS, CWLS, Party
            if (!Enum.IsDefined(typeof(DeathRollChatTypes), xivChatType)) return;

            var (reg, dice) = xivChatType switch
            {
                2122 => (randomRollRegex, false),
                8266 => (randomRollRegex, false),
                _ => (diceRollRegex, true),
            };
            
            var m = reg.Match(message.ToString());
            if (!m.Success) return;
            
            if (clientState?.LocalPlayer == null)
            {
                TurnOff();
                Chat.PrintError("Deathroll: Unable to fetch character name.");
                return;
            }

            var autoTranslate = false;
            var playerName = clientState?.LocalPlayer.Name.ToString();
            if (sender.ToString() != playerName || dice)
            {
                // add item and message part payloads
                foreach (var payload in message.Payloads)
                {
                    if (Configuration.DebugChat)
                    {
                        PluginLog.Debug($"Deathroll: {payload.Type}");
                        PluginLog.Debug($"Deathroll: {payload}");
                        
                    }

                    switch (payload)
                    {
                        case PlayerPayload playerPayload:
                            playerName = playerPayload.DisplayedName;
                            break;
                        // case IconPayload:
                        //     autoTranslate = true;
                        //     break;
                    }
                }
            }
            
            // TODO: prevent cheating
            // dice always needs the autoTranslate payload
            // if not, a player just wrote the exact string
            // if (dice && !autoTranslate)
            // {
            //     Chat.Print($"Deathroll: {playerName} tried to cheat~");
            // }
            
            var exists = PluginUi.Participants.Exists(x => x.name == playerName);
            if (!Configuration.RerollAllowed && exists)
            {
                if (Configuration.DebugChat) PluginLog.Debug(($"Deathroll: Player already rolled, no overwrite allowed."));
                return;
            }

            if (Configuration.RerollAllowed && exists)
            {
                PluginUi.DeleteEntry(playerName);
            }

            if (Configuration.DebugChat)
            {
                PluginLog.Debug(($"Deathroll: Extracted Player Name: {playerName}."));
                PluginLog.Debug(($"Deathroll: Add message to viewer."));
                var testString = $"Message: {message.ToString()}\n" +
                                 $"    Matches: 1: {m.Groups[1]} 2: {m.Groups[2]} 3: {m.Groups[3].Success} {m.Groups[3]}";
                PluginUi.CurrentText = testString;
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
                    ? new Roll(playerName, parsedRoll, parsedOutOf, hasHighlight, hightlightColor)
                    : new Roll(playerName, parsedRoll, parsedOutOf));

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

            PluginUi.IsOutOfUsed = PluginUi.Participants.Exists(x => x.outOf > -1);
            
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
