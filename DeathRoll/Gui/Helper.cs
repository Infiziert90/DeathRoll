using System;
using System.Numerics;
using System.Text.Json;
using Dalamud.Logging;
using DeathRoll.Data;
using ImGuiNET;

namespace DeathRoll.Gui;

public static class Helper
{
    private const ImGuiWindowFlags Flags = ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize;

    // https://www.programcreek.com/cpp/?code=kswaldemar%2Frewind-viewer%2Frewind-viewer-master%2Fsrc%2Fimgui_impl%2Fimgui_widgets.cpp
    public static void ShowHelpMarker(string desc, string markerText = "(?)", bool disabled = true)
    {
        if (disabled) 
            ImGui.TextDisabled(markerText);
        else 
            ImGui.TextUnformatted(markerText);
        
        if (!ImGui.IsItemHovered()) return;
        
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(450.0f);
        ImGui.TextUnformatted(desc);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    public static void ErrorWindow(ref string msg)
    {
        if (ImGui.Begin("Error##popup_helper_error", Flags))
        {
            ImGui.Text(msg);
                
            ImGui.Spacing();
            ImGui.NextColumn();

            ImGui.Columns(1);
            ImGui.Separator();

            ImGui.NewLine();

            ImGui.SameLine(120);
            //click ok when finished adjusting
            if (ImGui.Button("OK", new Vector2(100, 0)))
                msg = string.Empty;
            

            ImGui.End();
        }
    }
    
    public static bool SelectableDelete(Participant participant, Participants participants, int idx = 0, Vector4 color = new())
    {
        var deletion = "";
        
        try
        {
            if (color.W != 0) ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Selectable($"{participant.GetDisplayName()}##selectable{idx}");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyShift)
                deletion = participant.Name;
            if (color.W != 0) ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted("Hold Shift and right-click to delete.");
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            if (deletion != "")
            {
                participants.DeleteEntry(deletion);
                return true;
            }

        }
        catch (NullReferenceException e)
        {
            PluginLog.Error(e.Message);
            foreach (var pname in participants.PlayerNameList) PluginLog.Information($"Name in cause: {pname}");
            foreach (var p in participants.PList) PluginLog.Information($"Participants: {JsonSerializer.Serialize(p)}");
            Plugin.SwitchState(GameState.NotRunning);
            return true;
        }

        return false;
    }

    public static void SetTextCenter(string text, Vector4 color = new())
    {
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X) * 0.5f);
        
        // Alpha 0 means empty color
        if (color.W == 0) 
            ImGui.TextUnformatted(text);
        else 
            ImGui.TextColored(color, text);
    }
}