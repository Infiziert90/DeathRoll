using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DeathRoll.Data;

namespace DeathRoll.Windows.Match;

public class MatchWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    public MatchWindow(Plugin plugin) : base("Match###DeathRoll")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(385, 400),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (Plugin.State is GameState.NotRunning or GameState.Crash)
            return;

        if (Plugin.Participants.RoundDone)
            Plugin.MainWindow.DeathRollLoserPanel();
        else
            Plugin.MainWindow.VsPanel();

        if (ImGui.BeginChild("Content##SimpleTournament", new Vector2(0, -60 * ImGuiHelpers.GlobalScale), false, 0))
        {
            ImGuiHelpers.ScaledDummy(10.0f);
            Plugin.MainWindow.DeathRollParticipantRender();
        }
        ImGui.EndChild();

        if (ImGui.BeginChild("BottomBar##SimpleTournament", new Vector2(0, 0), false, 0))
        {
            if (Plugin.Participants.RoundDone)
            {
                if (ImGui.Button("End Match"))
                {
                    IsOpen = false;
                    Plugin.MainWindow.Tournament.NextMatch();
                }
            }
            else
            {
                ImGui.TextColored(ImGuiColors.DalamudOrange, $"Player isn't responding? Forfeit the match~");

                if (ImGui.Button("Forfeit to P1"))
                {
                    IsOpen = false;
                    Plugin.MainWindow.Tournament.ForfeitWin(Plugin.MainWindow.Tournament.Player1);
                }

                ImGui.SameLine();

                if (ImGui.Button("Forfeit to P2"))
                {
                    IsOpen = false;
                    Plugin.MainWindow.Tournament.ForfeitWin(Plugin.MainWindow.Tournament.Player2);
                }
            }
        }
        ImGui.EndChild();
    }
}