using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public class RollTable
{
    private PluginUI pluginUi;
    private Configuration configuration;
    
    public bool IsOutOfUsed;
    private Vector4 _defaultColor = new Vector4(1.0f,1.0f,1.0f,1.0f);
    
    public RollTable(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.configuration;
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