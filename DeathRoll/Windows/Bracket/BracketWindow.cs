using Dalamud.Interface.Windowing;
using DeathRoll.Data;

namespace DeathRoll.Windows.Bracket;

public class BracketWindow : Window, IDisposable
{
    private readonly Plugin Plugin;

    private bool UsedNameSetting;

    public BracketWindow(Plugin plugin) : base("Bracket")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(600, 600),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (Plugin.State is GameState.NotRunning or GameState.Crash)
            return;

        if (UsedNameSetting != DebugConfig.RandomizeNames)
        {
            UsedNameSetting = DebugConfig.RandomizeNames;
            Plugin.MainWindow.SimpleTournament.FillBracketTable();
        }

        if (ImGui.BeginTable("##Brackets", Plugin.MainWindow.SimpleTournament.LastStage))
        {
            foreach (var idx in Enumerable.Range(0, Plugin.MainWindow.SimpleTournament.LastStage))
                ImGui.TableSetupColumn($"Stage {idx+1}");

            ImGui.TableHeadersRow();
            for (var idx = 0; idx < Plugin.MainWindow.SimpleTournament.InternalBrackets[0].Count * 2 - 1; idx++)
            {
                for (var stage = 0; stage < Plugin.MainWindow.SimpleTournament.LastStage; stage++)
                {
                    if (stage >= Plugin.MainWindow.SimpleTournament.FilledBrackets[idx].Count)
                        break;

                    if (Plugin.MainWindow.SimpleTournament.FilledBrackets[idx][stage] == "x")
                        break;

                    ImGui.TableNextColumn();
                    if (Plugin.MainWindow.SimpleTournament.FilledBrackets[idx][stage] != "  ")
                        ImGui.Text(Plugin.MainWindow.SimpleTournament.FilledBrackets[idx][stage]);
                }
                ImGui.TableNextRow();
            }
            ImGui.EndTable();
        }
    }
}