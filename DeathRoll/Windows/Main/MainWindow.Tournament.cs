using Dalamud.Interface.Utility;
using DeathRoll.Data;
using DeathRoll.Logic;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    public readonly SimpleTournament Tournament;

    private bool Shuffled;
    private int DebugNumber = 8;

    private void TournamentMode()
    {
        RenderControlPanel();
        ImGui.Dummy(new Vector2(0.0f, 10.0f));

        switch (Plugin.State)
        {
            case GameState.Registration:
                RegistrationPanel();
                break;
            case GameState.Shuffling:
                ShufflingPreviewPanel();
                break;
            case GameState.Prepare:
                PreparePhase();
                break;
            case GameState.Done:
                RenderWinnerPanel();
                break;
        }
    }

    private void RenderWinnerPanel()
    {
        ImGuiHelpers.ScaledDummy(30.0f);

        Helper.SetTextCenter("WINNER", Helper.Yellow);
        Helper.SetTextCenter("WINNER", Helper.Yellow);
        Helper.SetTextCenter("GOAT", Helper.Yellow);
        Helper.SetTextCenter("DINNER", Helper.Yellow);
        Helper.SetTextCenter($"{Plugin.Participants.Winner.GetDisplayName()}", Helper.Yellow);

        ImGuiHelpers.ScaledDummy(210.0f);

        if (ImGui.Button("Show Bracket"))
            Plugin.OpenBracket();
    }

    private void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings"))
            Plugin.ConfigWindow.IsOpen = true;

        var spacing = ImGui.GetScrollMaxY() == 0 ? 120.0f : 135.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        switch (Plugin.State)
        {
            case GameState.NotRunning:
                if (!ImGui.Button("Start Tournament"))
                    return;
                Tournament.Reset();
                Plugin.SwitchState(GameState.Registration);
                break;
            case GameState.Crash:
                if (!ImGui.Button("Force Stop Tournament"))
                    return;
                Tournament.Reset();
                Plugin.ClosePlayWindows();
                break;
            default:
                if (!ImGui.Button("Stop Tournament"))
                    return;
                Tournament.Reset();
                Plugin.ClosePlayWindows();
                break;
        }
    }

    private void PreparePhase()
    {
        if (ImGui.Button("Show Bracket"))
            Plugin.OpenBracket();

        if (Tournament.Player2.Name != "Byes")
        {
            ImGui.TextUnformatted("Next Match:");
            ImGui.TextUnformatted($"{Tournament.Player1.GetDisplayName()} vs {(Tournament.Player2.GetDisplayName())}");

            if (ImGui.Button("Begin Match"))
            {
                Plugin.OpenMatch();
                Plugin.SwitchState(GameState.Match);
            }

            if (DebugConfig.Debug)
            {
                if (ImGui.Button("Auto Win Match"))
                    Tournament.AutoWin();
            }
        }
        else
        {
            ImGui.Text($"{Tournament.Player1.GetDisplayName()} got lucky and automatically won~");

            if (ImGui.Button("Continue to next Round"))
                Tournament.ForfeitWin(Tournament.Player1);
        }
    }

    public void VsPanel()
    {
        Helper.SetTextCenter($"{Tournament.Player1.GetDisplayName()} vs {Tournament.Player2.GetDisplayName()}");
    }

    private void ShufflingPreviewPanel()
    {
        ImGui.TextColored(Helper.Green,$"Pls shuffle at least once~");
        if (ImGui.Button("Shuffle"))
        {
            Tournament.Shuffle();
            Shuffled = true;
        }

        if (!Shuffled)
            return;

        ImGui.SameLine();
        if (ImGui.Button("Begin Tournament"))
        {
            Shuffled = false;
            Tournament.GenerateBrackets();
            Tournament.CalculateNextMatch();
            return;
        }

        ImGuiHelpers.ScaledDummy(10.0f);
        if (ImGui.CollapsingHeader("Shuffled Entry List", ImGuiTreeNodeFlags.DefaultOpen))
        {
            foreach (var name in Tournament.InternalParticipants.PlayerNameList.Select(playerName => Tournament.InternalParticipants.LookupDisplayName(playerName)))
                ImGui.Selectable($"{name}");
        }
    }

    private void RegistrationPanel()
    {
        if (Plugin.Participants.PlayerNameList.Count > 2)
            if (ImGui.Button("Close Registration"))
                Plugin.SwitchState(GameState.Shuffling);

        if (DebugConfig.Debug)
        {
            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.Text("Number of players to generate:");

            ImGui.SliderInt("##s", ref DebugNumber, 3, 128);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                DebugNumber = Math.Clamp(DebugNumber, 3, 128);
            }

            ImGuiHelpers.ScaledDummy(5.0f);
            if (ImGui.Button("Auto"))
            {
                Tournament.AutoRegistration(DebugNumber);
                Plugin.SwitchState(GameState.Shuffling);
            }
        }

        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TextColored(Helper.Green,
            Plugin.Participants.PlayerNameList.Count <= 2
                ? $"{3 - Plugin.Participants.PlayerNameList.Count} more players are necessary ..."
                : $"Awaiting more players ...");
        ImGuiHelpers.ScaledDummy(10.0f);
        ImGui.TextWrapped("Players can automatically enter by typing /random or /dice respectively while round registration is active.");
        ImGui.TextWrapped("Alternatively players can be manually entered by targeting the character and pressing 'Add Target' below.");
        AddTargetButton();
        PlayerListPanel();
    }

    private void PlayerListPanel()
    {
        if (!Plugin.Participants.PlayerNameList.Any())
            return;

        if (ImGui.CollapsingHeader("Players", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.BeginTable("##TournamentTable", 1))
            {
                ImGui.TableSetupColumn("##Playername");

                foreach (var participant in Plugin.Participants.PList)
                {
                    ImGui.TableNextColumn();
                    if (Helper.SelectableDelete(participant, Plugin.Participants))
                        break; // break because we deleted an entry
                }

                ImGui.EndTable();
            }
        }
    }
}