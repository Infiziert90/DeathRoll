using System;
using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public partial class RollTable
{
    private PluginUI pluginUi;
    private Configuration configuration;
    public Timers Timers;
    
    public bool IsOutOfUsed;
    private Vector4 _defaultColor = new Vector4(1.0f,1.0f,1.0f,1.0f);

    public RollTable(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        this.configuration = pluginUi.configuration;
        this.Timers = new Timers(pluginUi);
    }

    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings"))
        {
            this.pluginUi.SettingsVisible = true;
        }

        var spacing = ImGui.GetScrollY() == 0 ? 45.0f : 70.0f;
        ImGui.SameLine(ImGui.GetWindowWidth()-spacing);
        
        if (ImGui.Button("Clear"))
        {
            if (configuration.DeactivateOnClear) configuration.ActiveRound = false;
            this.pluginUi.Participants.Clear();
        }

        if (configuration.UseTimer)
        {
            this.Timers.RenderTimer();  
        }
        
        var activeRound = this.configuration.ActiveRound;
        if (ImGui.Checkbox("Active Round", ref activeRound))
        {
            this.configuration.ActiveRound = activeRound;
            this.configuration.Save();
        }
        
        ImGui.SameLine();
        
        var allowReroll = this.configuration.RerollAllowed;
        if (ImGui.Checkbox("Rerolling is allowed", ref allowReroll))
        {
            this.configuration.RerollAllowed = allowReroll;
            this.configuration.Save();
        }
        
        var current = configuration.CurrentMode;
        var nearest = configuration.Nearest;
        ImGui.RadioButton("min", ref current, 0); ImGui.SameLine();
        ImGui.RadioButton("max", ref current, 1); ImGui.SameLine();
        ImGui.RadioButton("nearest to", ref current, 2);
        if (current == 2)
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(40.0f);
            if (ImGui.InputInt("##nearestinput", ref nearest, 0, 0))
            {
                nearest = Math.Clamp(nearest, 1, 999);
            }
        }

        if (current != configuration.CurrentMode || nearest != configuration.Nearest)
        {
            configuration.CurrentMode = current;
            configuration.Nearest = nearest;
            
            switch(current)
            {
                case 0: this.pluginUi.Min();break;
                case 1: this.pluginUi.Max();break;
                case 2: this.pluginUi.Nearest();break;
            }
            
            configuration.Save();
        }
    }

    public void RenderRollTable()
    {
        if (!ImGui.BeginTable("##rolls", IsOutOfUsed ? 3 : 2)) return;
        ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.None, 3.0f);
        ImGui.TableSetupColumn("Roll");
        if (IsOutOfUsed) ImGui.TableSetupColumn("Out Of");
                    
        ImGui.TableHeadersRow();
        foreach (var participant in pluginUi.Participants)
        {
            var hCheck = configuration.ActiveHightlighting && participant.hasHighlight;
            var color = hCheck ? participant.highlightColor : _defaultColor;
            var name = !configuration.DebugRandomizedPlayerNames
                ? participant.GetReadableName()
                : participant.randomName;
            ImGui.TableNextColumn();
            ImGui.TextColored(color, name);
                            
            ImGui.TableNextColumn();
            ImGui.TextColored(color,participant.roll.ToString());
                            
            if (IsOutOfUsed)
            { 
                ImGui.TableNextColumn();
                ImGui.TextColored(color, participant.outOf != -1 ? participant.outOf.ToString() : "");
            };
        }
        ImGui.EndTable();
    }

    public void RenderDeletionDropdown()
    {
        var deletion = "";
        if (ImGui.CollapsingHeader("Remove Player from List"))
        {
            foreach (var participant in pluginUi.Participants)
            {
                var name = !configuration.DebugRandomizedPlayerNames
                    ? participant.GetReadableName()
                    : participant.randomName;
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
        }

        if (deletion != "")
        {
            pluginUi.DeleteEntry(deletion);
        }
    }
}