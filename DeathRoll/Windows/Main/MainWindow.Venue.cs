using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private void Venue()
    {
        ControlPanel();
        ImGuiHelpers.ScaledDummy(5);
        VenueRollTable();
    }

    private void ControlPanel()
    {
        if (ImGui.Button("Show Settings"))
            Plugin.OpenConfig();

        var spacing = ImGui.GetScrollMaxY() == 0 ? 85.0f : 120.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        if (Plugin.State is GameState.Match)
        {
            if (ImGui.Button(IsTimerActive() ? "Stop Round" : "Stop Timer"))
            {
                StopTimer();
                Plugin.SwitchState(GameState.Done);
            }
        }
        else
        {
            if (ImGui.Button("Start Round"))
            {
                Plugin.Participants.Reset();
                Plugin.SwitchState(GameState.Match);
            }
        }

        ImGuiHelpers.ScaledDummy(5);
        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(5);

        if (Configuration.UseTimer)
            Timer();

        ImGui.TextUnformatted("Sorting:");

        var current = (int) Configuration.SortingMode;
        var nearest = Configuration.Nearest;
        ImGui.RadioButton("Min", ref current, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Max", ref current, 1);
        ImGui.SameLine();
        ImGui.RadioButton("Nearest To", ref current, 2);
        if (current == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40.0f);
            if (ImGui.InputInt("##NearestInput", ref nearest, 0, 0)) nearest = Math.Clamp(nearest, 1, 999);
        }

        if (current == (int) Configuration.SortingMode && nearest == Configuration.Nearest)
            return;

        Configuration.SortingMode = (SortingType) current;
        Configuration.Nearest = nearest;
        Configuration.Save();
        Plugin.Participants.UpdateSorting();
    }

    private void VenueRollTable()
    {
        if (ImGui.BeginTable("##VenueRollTable", Plugin.Participants.IsOutOfUsed ? 3 : 2))
        {
            ImGui.TableSetupColumn("Name", 0, 3.0f);
            ImGui.TableSetupColumn("Roll");
            if (Plugin.Participants.IsOutOfUsed)
                ImGui.TableSetupColumn("Out Of");

            ImGui.TableHeadersRow();
            foreach (var (participant, idx) in Plugin.Participants.PList.Select((value, i) => (value, i)))
            {
                var last = Plugin.Participants.PList.Count - 1;
                var color = Helper.White;
                if (Configuration.ActiveHighlighting)
                {
                    if (participant.HasHighlight)
                        color = participant.HighlightColor;
                    else if (idx == 0 && Configuration.UseFirstPlace)
                        color = Configuration.FirstPlaceColor;
                    else if (idx == last && Configuration.UseLastPlace)
                        color = Configuration.LastPlaceColor;
                }

                ImGui.TableNextColumn();
                if (Helper.SelectableDelete(participant, Plugin.Participants, idx, color))
                    break; // break because we deleted an entry

                ImGui.TableNextColumn();
                ImGui.TextColored(color, participant.Roll.ToString());

                if (!Plugin.Participants.IsOutOfUsed)
                    continue;

                ImGui.TableNextColumn();
                ImGui.TextColored(color, participant.OutOf != -1 ? participant.OutOf.ToString() : "");
            }

            ImGui.EndTable();
        }
    }
}