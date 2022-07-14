using ImGuiNET;

namespace DeathRoll.Gui;

public static class Helper
{
    // https://www.programcreek.com/cpp/?code=kswaldemar%2Frewind-viewer%2Frewind-viewer-master%2Fsrc%2Fimgui_impl%2Fimgui_widgets.cpp
    public static void ShowHelpMarker(string desc) {
        ImGui.TextDisabled("(?)");
        if (!ImGui.IsItemHovered()) return;
        
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(450.0f);
        ImGui.TextUnformatted(desc);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }
}