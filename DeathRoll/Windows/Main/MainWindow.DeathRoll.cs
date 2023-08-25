using Dalamud.Interface.Components;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private const string HowToPlay = "Deahtroll is a game of slowly lowering the celling while trying to not hit 1." +
                                     "\nIt can be played by as many people as want, but whoever of them reaches 1 first, lost." +
                                     "\n\nExample:" +
                                     "\nPlayer 1: /random (Result 926)" +
                                     "\nPlayer 2: /random 926 (Result 666)" +
                                     "\nPlayer 3: /random 666 (Result 42)" +
                                     "\nPlayer 1: /random 42 (Result 12)" +
                                     "\nPlayer 2: /random 12 (Result 1)" +
                                     "\n\nPlayer 2 lost";

    private void DeathRollMode()
    {
        DeathRollPanel();

        if (Plugin.State is GameState.Done)
        {
            ImGui.Dummy(new Vector2(0.0f, 10.0f));
            DeathRollLoserPanel();
        }

        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        DeathRollParticipantRender();
    }

    private void DeathRollPanel()
    {
        if (ImGui.Button("Show Settings"))
            Plugin.OpenConfig();

        var spacing = ImGui.GetScrollMaxY() == 0 ? 80.0f : 95.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        if (ImGui.Button("New Round"))
        {
            Configuration.AcceptNewPlayers = true;
            Plugin.Participants.Reset();
            Plugin.SwitchState(GameState.Match);
        }

        if (ImGui.Checkbox("Accept New Players", ref Configuration.AcceptNewPlayers))
            Configuration.Save();
    }

    public void DeathRollLoserPanel()
    {
        ImGui.TextColored(Helper.Red, $"{Plugin.Participants.Last.GetDisplayName()} lost!!!");
    }

    public void DeathRollParticipantRender()
    {
        if (ImGui.BeginChild("Content", new Vector2(0, -20), false, 0))
        {
            if (ImGui.BeginTable("##rolls", 3))
            {
                ImGui.TableSetupColumn("Name", 0, 3.0f);
                ImGui.TableSetupColumn("Roll");
                ImGui.TableSetupColumn("Out Of");

                ImGui.TableHeadersRow();
                foreach (var participant in Plugin.Participants.PList)
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

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            ImGui.Text("How to play");
            ImGuiComponents.HelpMarker(HowToPlay);
        }
        ImGui.EndChild();
    }
}