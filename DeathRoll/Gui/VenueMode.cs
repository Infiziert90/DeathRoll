using System;
using System.Linq;
using System.Numerics;
using DeathRoll.Data;
using ImGuiNET;

namespace DeathRoll.Gui;

public class RollTable
{
    private readonly Vector4 _defaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private readonly Configuration configuration;
    private readonly Participants participants;

    private readonly PluginUI pluginUi;
    public readonly Timers Timers;

    public RollTable(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
        Timers = new Timers(configuration);
    }

    public void MainRender()
    {
        RenderControlPanel();
        
        if (!participants.PList.Any()) return;
        
        ImGui.Spacing();
        RenderRollTable();
    }
    
    private void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) 
            pluginUi.SettingsVisible = true;

        var spacing = ImGui.GetScrollMaxY() == 0 ? 85.0f : 120.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);
        
        if (Plugin.State is GameState.Match)
        {
            if (ImGui.Button(!Timers.IsStopwatchRunning() ? "Stop Round" : "Stop Timer"))
            {
                Timers.StopTimer();
                Plugin.SwitchState(GameState.Done);
            }
        } 
        else
        {
            if (ImGui.Button("Start Round"))
            {
                participants.Reset();
                Plugin.SwitchState(GameState.Match);
            }
        }
        
        ImGui.Dummy(new Vector2(0.0f, 1.0f));

        if (configuration.UseTimer) 
            Timers.RenderTimer();
        
        ImGui.TextUnformatted("Sorting:");
        
        var current = (int) configuration.SortingMode;
        var nearest = configuration.Nearest;
        ImGui.RadioButton("min", ref current, 0);
        ImGui.SameLine();
        ImGui.RadioButton("max", ref current, 1);
        ImGui.SameLine();
        ImGui.RadioButton("nearest to", ref current, 2);
        if (current == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40.0f);
            if (ImGui.InputInt("##nearestinput", ref nearest, 0, 0)) nearest = Math.Clamp(nearest, 1, 999);
        }

        if (current == (int) configuration.SortingMode && nearest == configuration.Nearest) 
            return;
        
        configuration.SortingMode = (SortingType) current;
        configuration.Nearest = nearest;
        configuration.Save();
        participants.UpdateSorting();
    }

    private void RenderRollTable()
    {
        if (!ImGui.BeginTable("##rolls", participants.IsOutOfUsed ? 3 : 2)) return;
        
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 3.0f);
        ImGui.TableSetupColumn("Roll");
        if (participants.IsOutOfUsed) ImGui.TableSetupColumn("Out Of");

        ImGui.TableHeadersRow();
        foreach (var (participant, idx) in participants.PList.Select((value, i) => (value, i)))
        {
            var last = participants.PList.Count - 1;
            var color = _defaultColor;
            if (configuration.ActiveHighlighting)
            {
                if (participant.HasHighlight)
                    color = participant.HighlightColor;
                else if (idx == 0 && configuration.UseFirstPlace)
                    color = configuration.FirstPlaceColor;
                else if (idx == last && configuration.UseLastPlace)
                    color = configuration.LastPlaceColor;
            }

            ImGui.TableNextColumn();
            if (Helper.SelectableDelete(participant, participants, idx, color))
                break; // break because we deleted an entry

            ImGui.TableNextColumn();
            ImGui.TextColored(color, participant.Roll.ToString());

            if (!participants.IsOutOfUsed) 
                continue;
            
            ImGui.TableNextColumn();
            ImGui.TextColored(color, participant.OutOf != -1 ? participant.OutOf.ToString() : "");
        }

        ImGui.EndTable();
    }

    public void StartTimer()
    {
        if (configuration.TimerResets)
        {
            participants.Reset();
        }
        
        Timers.StartTimer();
    }
}