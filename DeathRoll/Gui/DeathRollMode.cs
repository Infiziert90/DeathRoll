using System.Linq;
using System.Numerics;
using DeathRoll.Data;
using ImGuiNET;

namespace DeathRoll.Gui;

public class DeathRollMode
{
    private readonly Vector4 _redColor = new(0.980f, 0.245f, 0.245f, 1.0f);
    private readonly Configuration configuration;
    private readonly Participants participants;
    private readonly PluginUI pluginUi;

    public const string HowToPlay = @"Deahtroll is a game of slowly lowering the celling while trying to not hit 1.
It can be played by as many people as want, but whoever of them reaches 1 first, lost.

Example:
Player 1: /random (Result 926)
Player 2: /random 926 (Result 666)
Player 3: /random 666 (Result 42)
Player 1: /random 42 (Result 12)
Player 2: /random 12 (Result 1)

Player 2 lost";
    
    public DeathRollMode(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
    }

    public void MainRender()
    {
        RenderControlPanel();

        if (Plugin.State is GameState.Done)
        {
            ImGui.Dummy(new Vector2(0.0f, 10.0f));
            RenderLoserPanel();  
        }
        
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ParticipantRender();
    }

    public void RenderLoserPanel()
    {
        ImGui.TextColored(_redColor, $"{participants.Last.GetDisplayName()} lost!!!");
    }

    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) 
            pluginUi.SettingsVisible = true;
        
        var spacing = ImGui.GetScrollMaxY() == 0 ? 80.0f : 95.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        if (ImGui.Button("New Round"))
        {
            configuration.AcceptNewPlayers = true;
            participants.Reset();
            Plugin.SwitchState(GameState.Match);
        }
        
        var acceptNewPlayers = configuration.AcceptNewPlayers;
        if (!ImGui.Checkbox("Accept New Players", ref acceptNewPlayers)) 
            return;
        
        configuration.AcceptNewPlayers = acceptNewPlayers;
        configuration.Save();
    }
    
    public void ParticipantRender()
    {
        if (ImGui.BeginChild("Content##DeathRoll", new Vector2(0, -20), false, 0))
        {
            if (ImGui.BeginTable("##rolls", 3))
            {
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 3.0f);
                ImGui.TableSetupColumn("Roll");
                ImGui.TableSetupColumn("Out Of");

                ImGui.TableHeadersRow();
                foreach (var (participant, idx) in participants.PList.Select((value, i) => (value, i)))
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(participant.GetDisplayName());

                    ImGui.TableNextColumn();
                    ImGui.Text(participant.Roll.ToString());
            
                    ImGui.TableNextColumn();
                    ImGui.Text(participant.OutOf.ToString());
                }

                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
        
        if (ImGui.BeginChild("BottomBar##DeathRoll", new Vector2(0, 0), false, 0))
        {
            Helper.ShowHelpMarker(HowToPlay, "How to play?", false);
        }
        ImGui.EndChild();

    }
}