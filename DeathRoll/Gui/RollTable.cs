using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public class RollTable
{
    private readonly Vector4 _defaultColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private readonly Configuration configuration;
    private readonly Participants participants;

    private readonly PluginUI pluginUi;
    public Timers Timers;

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
        
        if (participants.PList.Count <= 0) return;
        
        ImGui.Spacing();
        RenderRollTable();
        ImGui.Dummy(new Vector2(0.0f, 60.0f));
        RenderDeletionDropdown();
    }
    
    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) pluginUi.SettingsVisible = true;

        var spacing = ImGui.GetScrollMaxY() == 0 ? 45.0f : 70.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        if (ImGui.Button("Clear"))
        {
            if (configuration.DeactivateOnClear) configuration.ActiveRound = false;
            participants.PList.Clear();
        }

        if (configuration.UseTimer) Timers.RenderTimer();

        var activeRound = configuration.ActiveRound;
        if (ImGui.Checkbox("Active Round", ref activeRound))
        {
            configuration.ActiveRound = activeRound;
            configuration.Save();
        }

        ImGui.SameLine();

        var allowReroll = configuration.RerollAllowed;
        if (ImGui.Checkbox("Reroll allowed", ref allowReroll))
        {
            configuration.RerollAllowed = allowReroll;
            configuration.Save();
        }

        var current = configuration.CurrentMode;
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

        if (current == configuration.CurrentMode && nearest == configuration.Nearest) return;
        configuration.CurrentMode = current;
        configuration.Nearest = nearest;
        configuration.Save();
        
        switch (current)
        {
            case 0:
                participants.Min();
                break;
            case 1:
                participants.Max();
                break;
            case 2:
                participants.Nearest(configuration.Nearest);
                break;
        }
    }

    public void RenderRollTable()
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
                if (participant.hasHighlight)
                    color = participant.highlightColor;
                else if (idx == 0 && configuration.UseFirstPlace)
                    color = configuration.FirstPlaceColor;
                else if (idx == last && configuration.UseLastPlace)
                    color = configuration.LastPlaceColor;
            }

            var name = participant.GetUsedName(configuration.DebugRandomPn);

            ImGui.TableNextColumn();
            ImGui.TextColored(color, name);

            ImGui.TableNextColumn();
            ImGui.TextColored(color, participant.roll.ToString());

            if (participants.IsOutOfUsed)
            {
                ImGui.TableNextColumn();
                ImGui.TextColored(color, participant.outOf != -1 ? participant.outOf.ToString() : "");
            }
        }

        ImGui.EndTable();
    }

    public void RenderDeletionDropdown()
    {
        var deletion = "";
        if (ImGui.CollapsingHeader("Player List"))
            foreach (var participant in participants.PList)
            {
                var name = participant.GetUsedName(configuration.DebugRandomPn);
                ImGui.Selectable($"{name}");
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyShift)
                    deletion = participant.name;

                if (!ImGui.IsItemHovered()) continue;
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted("Hold Shift and right-click to delete.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

        if (deletion != "") participants.DeleteEntry(deletion);
    }
}