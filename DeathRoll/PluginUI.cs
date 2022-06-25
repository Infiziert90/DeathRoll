using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using DeathRoll.Gui;
using ImGuiNET;

namespace DeathRoll;

// It is good to have this be disposable in general, in case you ever need it
// to do any cleanup
public class PluginUI : IDisposable
{
    private readonly ImGuiColorEditFlags _flags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha;
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private Vector4 _newColor = new(0.6f, 0.6f, 0.6f, 1.0f);
    private string _newRegex = string.Empty;
    public Participants Participants;
    public Configuration Configuration;

    private bool settingsVisible;

    // this extra bool exists for ImGui, since you can't ref a property
    private bool visible;

    // passing in the image here just for simplicityw
    public PluginUI(Configuration configuration, Participants p)
    {
        this.Configuration = configuration;
        this.Participants = p;
        
        Blocklist = new Blocklist(configuration);
        GeneralSettings = new GeneralSettings(configuration);
        RollTable = new RollTable(this);

        // needs RollTable
        TimerSetting = new TimerSetting(configuration, RollTable);
    }

    private Blocklist Blocklist { get; }
    private GeneralSettings GeneralSettings { get; }
    public RollTable RollTable { get; init; }
    public TimerSetting TimerSetting { get; init; }

    public bool Visible
    {
        get => visible;
        set => visible = value;
    }

    public bool SettingsVisible
    {
        get => settingsVisible;
        set => settingsVisible = value;
    }

    public void Dispose()
    {
    }

    public void UpdateParticipants()
    {
        if (!Configuration.ActiveHighlighting || Participants.PList.Count == 0) return;

        foreach (var roll in Participants.PList)
        {
            var hasMatch = false;
            foreach (var highlight in Configuration.SavedHighlights)
            {
                if (highlight.CompiledRegex.Match(roll.roll.ToString()).Success)
                {
                    hasMatch = true;
                    roll.hasHighlight = true;
                    roll.highlightColor = highlight.Color;
                    break;
                }

                ;
            }

            if (hasMatch) continue;
            roll.hasHighlight = false;
        }
    }

    public void Draw()
    {
        // This is our only draw handler attached to UIBuilder, so it needs to be
        // able to draw any windows we might have open.
        // Each method checks its own visibility/state to ensure it only draws when
        // it actually makes sense.
        // There are other ways to do this, but it is generally best to keep the number of
        // draw delegates as low as possible.

        DrawMainWindow();
        DrawSettingsWindow();
    }

    public void DrawMainWindow()
    {
        if (!Visible) return;

        ImGui.SetNextWindowSize(new Vector2(375, 480), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(375, 480), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("DeathRoll Helper", ref visible))
        {
            RollTable.RenderControlPanel();

            ImGui.Spacing();

            if (Participants.PList.Count > 0)
            {
                RollTable.RenderRollTable();
                ImGui.Dummy(new Vector2(0.0f, 60.0f));
                RollTable.RenderDeletionDropdown();
            }
        }

        ImGui.End();
    }

    public void DrawSettingsWindow()
    {
        if (!SettingsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(260, 310), ImGuiCond.Always);
        if (ImGui.Begin("DRH Config", ref settingsVisible, ImGuiWindowFlags.NoResize))
            if (ImGui.BeginTabBar("##settings-tabs"))
            {
                // Renders General Settings UI
                GeneralSettings.RenderGeneralSettings();

                // Renders Timer Settings Tab
                TimerSetting.RenderTimerSettings();

                // Renders Highlight Settings Tab
                if (ImGui.BeginTabItem("Highlight###hl-tab"))
                {
                    var activeHightlighting = Configuration.ActiveHighlighting;
                    if (ImGui.Checkbox("Highlighting active", ref activeHightlighting))
                    {
                        Configuration.ActiveHighlighting = activeHightlighting;
                        Configuration.Save();
                    }

                    ImGui.Dummy(new Vector2(0.0f, 5.0f));
                    ImGui.Text("Default Highlights:");

                    var firstPlaceColor = Configuration.FirstPlaceColor;
                    ImGui.ColorEdit4("##firstplacecolor", ref firstPlaceColor, _flags);
                    ImGui.SameLine();
                    var useFirstPlace = Configuration.UseFirstPlace;
                    if (ImGui.Checkbox("First place", ref useFirstPlace))
                    {
                        Configuration.UseFirstPlace = useFirstPlace;
                        Configuration.Save();
                    }

                    var lastPlaceColor = Configuration.LastPlaceColor;
                    ImGui.ColorEdit4("##lastplacecolor", ref lastPlaceColor, _flags);
                    ImGui.SameLine();
                    var useLastPlace = Configuration.UseLastPlace;
                    if (ImGui.Checkbox("Last place", ref useLastPlace))
                    {
                        Configuration.UseLastPlace = useLastPlace;
                        Configuration.Save();
                    }

                    if (firstPlaceColor != Configuration.FirstPlaceColor ||
                        lastPlaceColor != Configuration.LastPlaceColor)
                    {
                        firstPlaceColor.W = 1; // fix alpha
                        lastPlaceColor.W = 1; // fix alpha

                        Configuration.FirstPlaceColor = firstPlaceColor;
                        Configuration.LastPlaceColor = lastPlaceColor;
                        Configuration.Save();
                    }

                    ImGui.Dummy(new Vector2(0.0f, 5.0f));
                    ImGui.Text("Custom Highlights:");

                    if (!ImGui.BeginTable("##hl_table", 3, ImGuiTableFlags.None))
                        return;

                    ImGui.TableSetupColumn("##hl_buttons", ImGuiTableColumnFlags.None, 0.12f);
                    ImGui.TableSetupColumn("##hl_regexheader");
                    ImGui.TableSetupColumn("##hl_colorheader", ImGuiTableColumnFlags.None, 0.12f);
                    //ImGui.TableHeadersRow();

                    if (Configuration.SavedHighlights?.Count > 0)
                    {
                        var deletionIdx = -1;
                        var updateIdx = -1;
                        var _newReg = string.Empty;
                        var _newCol = new Vector4();
                        foreach (var (item, idx) in Configuration.SavedHighlights.Select((value, i) => (value, i)))
                        {
                            var _currentRegex = item.Regex;
                            var _currentColor = item.Color;

                            ImGui.TableNextColumn();
                            ImGui.PushFont(UiBuilder.IconFont);
                            if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##hl_delbtn{idx}"))
                                deletionIdx = idx;
                            ImGui.PopFont();

                            ImGui.TableNextColumn();
                            ImGui.PushItemWidth(200.0f);
                            ImGui.InputTextWithHint($"##regex{idx}", "", ref _currentRegex, 255);

                            ImGui.TableNextColumn();
                            ImGui.ColorEdit4($"##hl_oldColor{idx}", ref _currentColor, _flags);

                            if (_currentRegex != item.Regex || _currentColor != item.Color)
                            {
                                updateIdx = idx;
                                _newCol = _currentColor;
                                _newReg = _currentRegex;
                            }
                        }

                        if (deletionIdx != -1)
                        {
                            Configuration.SavedHighlights.RemoveAt(deletionIdx);
                            Configuration.Save();
                            UpdateParticipants();
                        }

                        if (updateIdx != -1)
                        {
                            _newCol.W = 1.0f; // fix alpha

                            Configuration.SavedHighlights[updateIdx].Update(_newReg, _newCol);
                            Configuration.Save();
                            UpdateParticipants();
                        }
                    }

                    // new highlight entry last
                    ImGui.TableNextColumn();
                    
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Check.ToIconString()}##hl_plusbtn"))
                    {
                        _newColor.W = 1; // fix alpha being 0

                        Configuration.SavedHighlights?.Add(new Highlight(_newRegex, _newColor));
                        Configuration.Save();
                        RestoreDefaults();
                        UpdateParticipants();
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

                // Renders Blocklist UI
                Blocklist.RenderBlocklistTab();

                ImGui.EndTabBar();
            }

        ImGui.End();
    }

    public void RestoreDefaults()
    {
        _newRegex = string.Empty;
        _newColor = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
    }
}