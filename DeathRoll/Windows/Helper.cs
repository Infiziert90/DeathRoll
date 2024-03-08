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

    // Minesweeper
    public static readonly Vector4 Black = new(0, 0, 0, 1);
    public static readonly Vector4 LighterGrey = new(0.8f, 0.8f, 0.8f, 1.0f);
    public static readonly Vector4 DarkBlue = new(0.0f, 0.431f, 0.722f, 1.0f);
    public static readonly Vector4 DarkGreen = new(0.059f, 0.49f, 0.0f, 1.0f);
    public static readonly Vector4 DarkViolet = new(0.541f, 0.173f, 0.788f, 1.0f);
    public static readonly Vector4 DarkBrown = new(0.659f, 0.376f, 0.0f, 1.0f);
    public static readonly Vector4 DarkCyan = new(0.0f, 0.49f, 0.49f, 1.0f);
    public static readonly Vector4 DarkGrey = new(0.5f, 0.5f, 0.5f, 1.0f);
    public static readonly Vector4 DarkRed = new(0.722f, 0.169f, 0.169f, 1.0f);

    // Bahamood
    public static readonly uint MapGrey = Vec4ToUintColor(new Vector4(0.662f, 0.662f, 0.662f, 1));
    public static readonly uint PlayerYellow = Vec4ToUintColor(new Vector4(1.0f, 1.0f, 0.0f, 1));
    public static readonly uint PlayerGreen = Vec4ToUintColor(new Vector4(0.0f, 1.0f, 0.0f, 1));
    public static readonly uint RaycastWhite = Vec4ToUintColor(new Vector4(1.0f, 1.0f, 1.0f, 1));
    public static readonly uint NumberRed = Vec4ToUintColor(Red);
    public static readonly uint Background = Vec4ToUintColor(Black);

    public static bool SelectableDelete(Participant participant, Participants participants, int idx = 0, Vector4 color = new())
    {
        var deletion = "";

        try
        {
            if (color.W != 0) ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Selectable($"{participant.GetDisplayName()}##selectable{idx}");
            if (ImGui.IsItemClicked(ImGuiMouseButton.Right) && ImGui.GetIO().KeyShift)
                deletion = participant.Name;
            if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                ImGui.SetClipboardText(participant.Name);
            if (color.W != 0) ImGui.PopStyleColor();

            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 35.0f);
                ImGui.TextUnformatted("Left-click to copy name.");
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
            Plugin.Log.Error(e.Message);
            return true;
        }

        return false;
    }

    public static bool CenterButton(string text)
    {
        var buttonStyle = ImGui.GetStyle().ButtonTextAlign.X;
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X - buttonStyle) * 0.5f);
        return ImGui.Button(text);
    }

    public static void CenterNextButton(string text)
    {
        var buttonStyle = ImGui.GetStyle().ButtonTextAlign.X;
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X - buttonStyle) * 0.5f);
    }

    public static void SetTextCenter(string text, Vector4 color = default)
    {
        ImGui.SetCursorPosX((ImGui.GetWindowSize().X - ImGui.CalcTextSize(text).X) * 0.5f);

        // Alpha 0 means empty color
        if (color.W == 0)
            ImGui.TextUnformatted(text);
        else
            ImGui.TextColored(color, text);
    }

    public static void TableCenterText(string text, Vector4 color = default)
    {
        var pos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(pos with { X = pos.X + (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) * 0.5f });
        // Alpha 0 means empty color
        if (color.W == 0)
            ImGui.TextUnformatted(text);
        else
            ImGui.TextColored(color, text);
    }

    public static void TableDummy(string text)
    {
        var pos = ImGui.GetCursorPos();
        ImGui.SetCursorPos(pos with { X = pos.X + (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X)});
        ImGui.Dummy(ImGui.CalcTextSize(text));
    }

    private static float Saturate(float f) => f < 0.0f ? 0.0f : f > 1.0f ? 1.0f : f;
    private static uint FloatToUintSat(float val) => (uint) (Saturate(val) * 255.0f + 0.5f);

    public static uint Vec4ToUintColor(Vector4 i)
    {
        var o = FloatToUintSat(i.X) << 0;
        o |= FloatToUintSat(i.Y) << 8;
        o |= FloatToUintSat(i.Z) << 16;
        o |= FloatToUintSat(i.W) << 24;

        return o;
    }
}