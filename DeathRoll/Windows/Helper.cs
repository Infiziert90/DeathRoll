using Dalamud.Logging;
using DeathRoll.Data;

namespace DeathRoll.Windows;

public static class Helper
{
    public static readonly Vector4 White = new(1.0f, 1.0f, 1.0f, 1.0f);
    public static readonly Vector4 Green = new(0.0f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 Red = new(0.980f, 0.245f, 0.245f, 1.0f);
    public static readonly Vector4 Yellow = new(0.959f, 1.0f, 0.0f, 1.0f);
    public static readonly Vector4 SoftBlue = new(0.031f, 0.376f, 0.768f, 1.0f);
    public static readonly Vector4 KofiColor = new(0.12549f, 0.74902f, 0.33333f, 0.6f);

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