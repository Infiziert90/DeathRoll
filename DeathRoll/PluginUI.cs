using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Logging;
using DeathRoll.Gui;

namespace DeathRoll
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public class PluginUI : IDisposable
    {
        public Configuration configuration;
        private Blocklist Blocklist { get; init; }
        private GeneralSettings GeneralSettings { get; init; }
        public RollTable RollTable { get; init; }
        
        public List<Participant> Participants = new List<Participant>();

        public void UpdateParticipants()
        {
            if (!configuration.ActiveHightlighting || Participants.Count == 0) return;

            foreach (var roll in Participants)
            {
                var hasMatch = false;
                foreach (var highlight in configuration.SavedHighlights)
                {
                    if (highlight.CompiledRegex.Match(roll.roll.ToString()).Success)
                    {
                        hasMatch = true;
                        roll.hasHighlight = true;
                        roll.highlightColor = highlight.Color;
                        break;
                    };
                }
                if (hasMatch) continue;
                roll.hasHighlight = false;
            }
        }

        // this extra bool exists for ImGui, since you can't ref a property
        private bool visible = false;
        public bool Visible
        {
            get { return this.visible; }
            set { this.visible = value; }
        }

        private bool settingsVisible = false;
        public bool SettingsVisible
        {
            get { return this.settingsVisible; }
            set { this.settingsVisible = value; }
        }

        private int _numberOfTables;
        private string _newRegex = String.Empty;
        private Vector4 _newColor = new Vector4(0.6f,0.6f,0.6f,1.0f);
        private Vector4 _greenColor = new Vector4(0.0f, 1.0f, 0.0f,1.0f);

        // passing in the image here just for simplicityw
        public PluginUI(Configuration configuration)
        {
            this.configuration = configuration;
            this.Blocklist = new Blocklist(configuration);
            this.GeneralSettings = new GeneralSettings(configuration);
            this.RollTable = new RollTable(this);
            _numberOfTables = configuration.NumberOfTables;
        }

        public void Dispose()
        {
            
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
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(375, 480), ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSizeConstraints(new Vector2(375, 480), new Vector2(float.MaxValue, float.MaxValue));
            if (ImGui.Begin("DeathRoll Helper", ref this.visible))
            {
                if (ImGui.Button("Show Settings"))
                {
                    SettingsVisible = true;
                }

                var spacing = ImGui.GetScrollY() == 0 ? 45.0f : 70.0f;
                ImGui.SameLine(ImGui.GetWindowWidth()-spacing);
                
                if (ImGui.Button("Clear"))
                {
                    if (configuration.DeactivateOnClear) configuration.ActiveRound = false;
                    Participants.Clear();
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
                        case 0: Min();break;
                        case 1: Max();break;
                        case 2: Nearest();break;
                    }
                    
                    configuration.Save();
                }

                ImGui.Spacing();
                
                if (Participants.Count > 0)
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
            if (!SettingsVisible)
            {
                return;
            }
            
            ImGui.SetNextWindowSize(new Vector2(260, 310), ImGuiCond.Always);
            if (ImGui.Begin("DeathRoll Helper Config", ref this.settingsVisible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse ))
            {

                if (ImGui.BeginTabBar("##settings-tabs"))
                {
                    // Renders General Settings UI
                    this.GeneralSettings.RenderGeneralSettings();
                    
                    if (ImGui.BeginTabItem($"Highlight###highlight-tab"))
                    {
                        var activeHightlighting = this.configuration.ActiveHightlighting;
                        if (ImGui.Checkbox("Highlighting active", ref activeHightlighting))
                        {
                            this.configuration.ActiveHightlighting = activeHightlighting;
                            this.configuration.Save();
                        }
                        
                        ImGui.Dummy(new Vector2(0.0f, 5.0f));
                        
                        if (!ImGui.BeginTable("##highlighttable", 3, ImGuiTableFlags.None))
                            return;

                        ImGui.TableSetupColumn("##plusbutton", ImGuiTableColumnFlags.None, 0.12f);
                        ImGui.TableSetupColumn("##regexheader");
                        ImGui.TableSetupColumn("##colorheader", ImGuiTableColumnFlags.None, 0.12f);
                        //ImGui.TableHeadersRow();

                        if (configuration.SavedHighlights?.Count > 0)
                        {
                            var deletionIdx = -1;
                            var updateIdx = -1;
                            var _newReg = string.Empty;
                            var _newCol = new Vector4();
                            foreach (var (item, idx) in configuration.SavedHighlights.Select((value, i) => ( value, i )))
                            {
                                var _currentRegex = item.Regex;
                                var _currentColor = item.Color;
                            
                                ImGui.TableNextColumn();
                                ImGui.PushFont(UiBuilder.IconFont);
                                if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##highlight_delbtn{idx}"))
                                {
                                    deletionIdx = idx;
                                }
                                ImGui.PopFont();
                            
                                ImGui.TableNextColumn();
                                ImGui.PushItemWidth(200.0f);
                                ImGui.InputTextWithHint($"##regex{idx}", "", ref _currentRegex, 255);
                            
                                ImGui.TableNextColumn();
                                ImGui.ColorEdit4($"##hightlightcolor{idx}", ref _currentColor,
                                    ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);
                                    
                                if (_currentRegex != item.Regex || _currentColor != item.Color)
                                {
                                    updateIdx = idx;
                                    _newCol = _currentColor;
                                    _newReg = _currentRegex;
                                };
                            }

                            if (deletionIdx != -1)
                            {
                                configuration.SavedHighlights.RemoveAt(deletionIdx);
                                configuration.Save();
                                UpdateParticipants();
                            }
                            
                            if (updateIdx != -1)
                            {
                                _newCol.W = 1.0f; // fix alpha
                                
                                configuration.SavedHighlights[updateIdx].Update(_newReg, _newCol);
                                configuration.Save();
                                UpdateParticipants();
                            }
                        }

                        // new highlight last
                        ImGui.TableNextColumn();
                        ImGui.PushFont(UiBuilder.IconFont);
                        if (ImGui.Button($"{FontAwesomeIcon.Check.ToIconString()}##highlight_plusbtn"))
                        {
                            _newColor.W = 1; // fix alpha being 0
                            
                            configuration.SavedHighlights?.Add(new Highlight(_newRegex, _newColor));
                            configuration.Save();
                            RestoreDefaults();
                            UpdateParticipants();
                        }
                        ImGui.PopFont();
                        
                        ImGui.TableNextColumn();
                        ImGui.PushItemWidth(200.0f);
                        ImGui.InputTextWithHint("##regex", "Regex...", ref _newRegex, 255);
                        
                        ImGui.TableNextColumn();
                        ImGui.ColorEdit4("##hightlightcolor", ref _newColor, ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha);

                        ImGui.EndTable();
                        ImGui.TextColored(_greenColor, "Simple number matching:\n^YourNumber$");
                        
                        ImGui.EndTabItem();
                    }
                    // Renders Blocklist UI
                    this.Blocklist.RenderBlocklistTab();
                    
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
        }

        public void RestoreDefaults()
        {
            _newRegex = string.Empty;
            _newColor = new Vector4(0.6f,0.6f,0.6f,1.0f);
        }
        
        public void DeleteEntry(string name)
        {
            Participants.RemoveAll(x => x.name == name);
        }
        
        public void Min()
        {
            Participants = Participants.OrderBy(x => x.roll).ToList();
        }        
        
        public void Max()
        {
            Participants = Participants.OrderByDescending(x => x.roll).ToList();
        }        
        
        public void Nearest()
        {
            Participants = Participants.OrderBy(x => Math.Abs(configuration.Nearest - x.roll)).ToList();
        }
    }
}
