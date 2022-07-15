using System.Numerics;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui;

public class GeneralSettings
{
    private readonly Configuration configuration;

    public GeneralSettings(Configuration configuration)
    {
        this.configuration = configuration;
    }

    public void RenderGeneralSettings()
    {
        if (!ImGui.BeginTabItem("General###general-tab")) return;
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        var on = configuration.On;
        if (ImGui.Checkbox("On", ref on))
        {
            configuration.On = on;
            configuration.Save();
        }
        
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Game Mode:");
        ImGui.SameLine();
        Helper.ShowHelpMarker("Venue: For games like Truth and Dare\nTournament: DeathRoll with a bracket system");
        
        var gameMode = configuration.GameMode;
        ImGui.RadioButton("Venue", ref gameMode, 0);
        ImGui.SameLine();
        ImGui.RadioButton("DeathRoll", ref gameMode, 1);
        ImGui.RadioButton("Simple Tournament", ref gameMode, 2);

        if (gameMode != configuration.GameMode)
        {
            Plugin.SwitchState(GameState.NotRunning);
            
            configuration.GameMode = gameMode;
            configuration.Save();
        }
        
        if (configuration.GameMode == 0)
        {
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            ImGui.Text("Game Mode Options:");
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            
            var allowReroll = configuration.RerollAllowed;
            if (ImGui.Checkbox("Reroll allowed", ref allowReroll))
            {
                configuration.RerollAllowed = allowReroll;
                configuration.Save();
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Options:");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));

        var onlyRandom = configuration.OnlyRandom;
        if (ImGui.Checkbox("Accept only /random", ref onlyRandom))
        {
            configuration.OnlyRandom = onlyRandom;
            configuration.OnlyDice = false;
            configuration.Save();
        }

        var onlyDice = configuration.OnlyDice;
        if (ImGui.Checkbox("Accept only /dice", ref onlyDice))
        {
            configuration.OnlyDice = onlyDice;
            configuration.OnlyRandom = false;
            configuration.Save();
        }

        var verboseChatlog = configuration.DebugChat;
        if (ImGui.Checkbox("Debug", ref verboseChatlog))
        {
            configuration.DebugChat = verboseChatlog;
            configuration.DebugRandomPn = false;
            configuration.DebugAllowDiceCheat = false;
            configuration.Save();
        }

        if (verboseChatlog)
        {
            ImGui.Dummy(new Vector2(15.0f, 0.0f));
            ImGui.SameLine();
            var randomizePlayers = configuration.DebugRandomPn;
            if (ImGui.Checkbox("Randomize names", ref randomizePlayers))
                configuration.DebugRandomPn = randomizePlayers;

            ImGui.Dummy(new Vector2(15.0f, 0.0f));
            ImGui.SameLine();
            var allowDiceCheat = configuration.DebugAllowDiceCheat;
            if (ImGui.Checkbox("Allow dice cheat", ref allowDiceCheat))
                configuration.DebugAllowDiceCheat = allowDiceCheat;
        }

        ImGui.EndTabItem();
    }
}