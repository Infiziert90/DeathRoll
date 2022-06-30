using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace DeathRoll.Gui;

public class Highlights
{
    private readonly Configuration configuration;
    private readonly Participants participants;

    private const ImGuiColorEditFlags _flags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha;
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private Vector4 _newColor = new(0.6f, 0.6f, 0.6f, 1.0f);
    private string _newRegex = string.Empty;
    
    public Highlights(PluginUI pluginUi)
    {
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
    }

    public void RenderHightlightsTab()
    {
        if (!ImGui.BeginTabItem("Highlight###hl-tab")) return;
        
        var activeHightlighting = configuration.ActiveHighlighting;
        if (ImGui.Checkbox("Highlighting active", ref activeHightlighting))
        {
            configuration.ActiveHighlighting = activeHightlighting;
            configuration.Save();
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Default Highlights:");

        var firstPlaceColor = configuration.FirstPlaceColor;
        ImGui.ColorEdit4("##firstplacecolor", ref firstPlaceColor, _flags);
        ImGui.SameLine();
        var useFirstPlace = configuration.UseFirstPlace;
        if (ImGui.Checkbox("First place", ref useFirstPlace))
        {
            configuration.UseFirstPlace = useFirstPlace;
            configuration.Save();
        }

        var lastPlaceColor = configuration.LastPlaceColor;
        ImGui.ColorEdit4("##lastplacecolor", ref lastPlaceColor, _flags);
        ImGui.SameLine();
        var useLastPlace = configuration.UseLastPlace;
        if (ImGui.Checkbox("Last place", ref useLastPlace))
        {
            configuration.UseLastPlace = useLastPlace;
            configuration.Save();
        }

        if (firstPlaceColor != configuration.FirstPlaceColor ||
            lastPlaceColor != configuration.LastPlaceColor)
        {
            firstPlaceColor.W = 1; // fix alpha
            lastPlaceColor.W = 1; // fix alpha

            configuration.FirstPlaceColor = firstPlaceColor;
            configuration.LastPlaceColor = lastPlaceColor;
            configuration.Save();
        }

        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        ImGui.Text("Custom Highlights:");

        if (!ImGui.BeginTable("##hl_table", 3, ImGuiTableFlags.None))
            return;

        ImGui.TableSetupColumn("##hl_buttons", ImGuiTableColumnFlags.None, 0.12f);
        ImGui.TableSetupColumn("##hl_regexheader");
        ImGui.TableSetupColumn("##hl_colorheader", ImGuiTableColumnFlags.None, 0.12f);
        //ImGui.TableHeadersRow();

        if (configuration.SavedHighlights?.Count > 0)
        {
            var deletionIdx = -1;
            var updateIdx = -1;
            var _newReg = string.Empty;
            var _newCol = new Vector4();
            foreach (var (item, idx) in configuration.SavedHighlights.Select((value, i) => (value, i)))
            {
                var _currentRegex = item.Regex;
                var _currentColor = item.Color;

                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##hl_delbtn{idx}")) deletionIdx = idx;
                ImGui.PopFont();

                ImGui.TableNextColumn();
                ImGui.PushItemWidth(200.0f);
                ImGui.InputTextWithHint($"##regex{idx}", "", ref _currentRegex, 255);

                ImGui.TableNextColumn();
                ImGui.ColorEdit4($"##hl_oldColor{idx}", ref _currentColor, _flags);

                if (_currentRegex == item.Regex && _currentColor == item.Color) continue;
                updateIdx = idx;
                _newCol = _currentColor;
                _newReg = _currentRegex;
            }

            if (deletionIdx != -1)
            {
                configuration.SavedHighlights.RemoveAt(deletionIdx);
                configuration.Save();
                participants.Update();
            }

            if (updateIdx != -1)
            {
                _newCol.W = 1.0f; // fix alpha

                configuration.SavedHighlights[updateIdx].Update(_newReg, _newCol);
                configuration.Save();
                participants.Update();
            }
        }

        // new highlight entry last
        ImGui.TableNextColumn();
        
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Check.ToIconString()}##hl_plusbtn"))
        {
            _newColor.W = 1; // fix alpha being 0

            configuration.SavedHighlights?.Add(new Highlight(_newRegex, _newColor));
            configuration.Save();
            RestoreDefaults();
            participants.Update();
        }
        ImGui.PopFont();

        ImGui.TableNextColumn();
        ImGui.PushItemWidth(200.0f);
        ImGui.InputTextWithHint("##regex", "Regex...", ref _newRegex, 255);

        ImGui.TableNextColumn();
        ImGui.ColorEdit4("##hl_newColor", ref _newColor, _flags);

        ImGui.EndTable();
        ImGui.TextColored(_greenColor, "Simple number matching:\n^YourNumber$");

        ImGui.EndTabItem();
    }
    
    public void RestoreDefaults()
    {
        _newRegex = string.Empty;
        _newColor = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
    }
}