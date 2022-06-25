using System.Numerics;
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
        if (ImGui.BeginTabItem("General###general-tab"))
        {
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            var on = configuration.On;
            if (ImGui.Checkbox("On", ref on))
            {
                configuration.On = on;
                configuration.Save();
            }

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            ImGui.Text("Options:");
            ImGui.Dummy(new Vector2(0.0f, 5.0f));

            var deactivateOnClear = configuration.DeactivateOnClear;
            if (ImGui.Checkbox("Clear ends active round", ref deactivateOnClear))
            {
                configuration.DeactivateOnClear = deactivateOnClear;
                configuration.Save();
            }

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
}