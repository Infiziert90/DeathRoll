using System;
using System.Linq;
using System.Numerics;
using DeathRoll.Data;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui;

public class SimpleTournamentMode
{
    private readonly Vector4 _redColor = new(0.980f, 0.245f, 0.245f, 1.0f);
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private readonly Vector4 _yellowColor = new(0.959f, 1.0f, 0.0f, 1.0f);

    private string ErrorMsg = string.Empty;
    
    private readonly Configuration configuration;
    private readonly Participants participants;
    private readonly SimpleTournament spTourn;

    private readonly PluginUI pluginUi;

    private bool shuffled = false;
    private int debugNumber = 8;
    private bool lastSeenSetting = false;

    private bool bracketVisible = false;
    private bool matchVisible = false;
    
    public SimpleTournamentMode(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
        spTourn = pluginUi.Rolls.SimpleTournament;
    }

    public void MainRender()
    {
        RenderControlPanel();
        ImGui.Dummy(new Vector2(0.0f, 10.0f));   
        
        switch (Plugin.State)
        {
            case GameState.Registration:
                RegistrationPanel();
                break;
            case GameState.Shuffling:
                ShufflingPreviewPanel();
                break;
            case GameState.Prepare:
                PreparePhaseRender();
                break;
            case GameState.Done:
                RenderWinnerPanel();
                break;
        }
    }

    private void RenderWinnerPanel()
    {
        ImGui.Dummy(new Vector2(0.0f, 30.0f));
        
        Helper.SetTextCenter("WINNER", _yellowColor);
        Helper.SetTextCenter("WINNER", _yellowColor);
        Helper.SetTextCenter("GOAT", _yellowColor);
        Helper.SetTextCenter("DINNER", _yellowColor);
        Helper.SetTextCenter($"{participants.Winner.GetDisplayName()}", _yellowColor);
        
        ImGui.Dummy(new Vector2(0.0f, 210.0f));
        
        if (ImGui.Button("Show Bracket"))
        {
            bracketVisible = true;
        }
    }

    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) pluginUi.SettingsVisible = true;
        
        var spacing = ImGui.GetScrollMaxY() == 0 ? 120.0f : 135.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        switch (Plugin.State)
        {
            case GameState.NotRunning:
                if (!ImGui.Button("Start Tournament")) return;
                spTourn.Reset();
                Plugin.SwitchState(GameState.Registration);
                return;            
            case GameState.Crash:
                if (!ImGui.Button("Force Stop Tournament")) return;
                spTourn.Reset();
                bracketVisible = false;
                matchVisible = false;
                return;
            default:
                if (!ImGui.Button("Stop Tournament")) return;
                spTourn.Reset();
                bracketVisible = false;
                matchVisible = false;
                return;
        }
    }

    public void PreparePhaseRender()
    {
        if (ImGui.Button("Show Bracket"))
            bracketVisible = true;

        if (spTourn.Player2.Name != "Byes")
        {
            ImGui.TextUnformatted("Next Match:");
            ImGui.TextUnformatted($"{spTourn.Player1.GetDisplayName()} vs {(spTourn.Player2.GetDisplayName())}");
            
            if (ImGui.Button("Begin Match"))
            {
                matchVisible = true;
                Plugin.SwitchState(GameState.Match);
            }

            if (configuration.Debug)
            {
                if (ImGui.Button("Auto Win Match"))
                    spTourn.AutoWin();
            }
        }
        else
        {
            ImGui.Text($"{spTourn.Player1.GetDisplayName()} got lucky and automatically won~");
            
            if (ImGui.Button("Continue to next Round"))
                spTourn.ForfeitWin(spTourn.Player1);
        }
    }

    public void DrawGeneratedBracket()
    {
        if (!bracketVisible) 
            return;
        
        if (Plugin.State is GameState.NotRunning or GameState.Crash) 
            return;
        
        if (lastSeenSetting != DebugConfig.RandomizeNames)
        {
            lastSeenSetting = DebugConfig.RandomizeNames;
            spTourn.FillBracketTable();
        }
        
        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(600, 600), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("DeathRoll Tournament Bracket", ref bracketVisible))
        {
            if (!ImGui.BeginTable("##Brackets", spTourn.LastStage)) return;
            foreach (var idx in Enumerable.Range(0, spTourn.LastStage))
            {
                ImGui.TableSetupColumn($"Stage {idx+1}");
            }
            
            ImGui.TableHeadersRow();
            for (var idx = 0; idx < spTourn.InternalBrackets[0].Count*2-1; idx++)
            {
                for (var stage = 0; stage < spTourn.LastStage; stage++)
                {
                    if (stage >= spTourn.FilledBrackets[idx].Count) break;
                    if (spTourn.FilledBrackets[idx][stage] == "x") break;
                    ImGui.TableNextColumn();
                    if (spTourn.FilledBrackets[idx][stage] != "  ") ImGui.Text(spTourn.FilledBrackets[idx][stage]);
                }
                ImGui.TableNextRow();
            }
            ImGui.EndTable();
        }
        ImGui.End();
    }
    
    public void DrawMatch()
    {
        if (!matchVisible) return;
        if (Plugin.State is GameState.NotRunning or GameState.Crash) return;

        ImGui.SetNextWindowSize(new Vector2(385, 400), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(385, 400), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("DeathRoll Match", ref matchVisible))
        {
            if (participants.RoundDone)
            {
                pluginUi.DeathRollMode.RenderLoserPanel();  
            }
            else
            {
                Helper.SetTextCenter($"{spTourn.Player1.GetDisplayName()} vs {spTourn.Player2.GetDisplayName()}");
            }

            if (ImGui.BeginChild("Content", new Vector2(0, -60), false, 0))
            {
                ImGui.Dummy(new Vector2(0.0f, 10.0f));
                pluginUi.DeathRollMode.ParticipantRender();
            }
            ImGui.EndChild();

            if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
            {
                if (participants.RoundDone)
                {
                    if (ImGui.Button("End Match"))
                    {
                        spTourn.NextMatch();
                        matchVisible = false;
                    }  
                }
                else
                {
                    ImGui.Text($"Player isn't responding? Forfeit the match~");
                
                    if (ImGui.Button("Forfeit to P1"))
                    {
                        spTourn.ForfeitWin(spTourn.Player1);
                        matchVisible = false;
                    }  
                
                    ImGui.SameLine();
                
                    if (ImGui.Button("Forfeit to P2"))
                    {
                        spTourn.ForfeitWin(spTourn.Player2);
                        matchVisible = false;
                    }  
                }
            }
            ImGui.EndChild();
        }

        ImGui.End();
    }
    
    public void ShufflingPreviewPanel()
    {
        ImGui.TextColored(_greenColor,$"Pls shuffle at least once~");
        if (ImGui.Button("Shuffle"))
        {
            spTourn.Shuffle();
            shuffled = true;
        }

        if (!shuffled) return;
        
        ImGui.SameLine();
        if (ImGui.Button("Begin Tournament"))
        {
            shuffled = false;
            spTourn.GenerateBrackets();
            spTourn.CalculateNextMatch();
            return;
        }  
            
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        if (!ImGui.CollapsingHeader("Shuffled Entry List", ImGuiTreeNodeFlags.DefaultOpen)) return;
        foreach (var name in spTourn.TParticipants.PlayerNameList.Select(
                     playerName => spTourn.TParticipants.LookupDisplayName(playerName)))
            ImGui.Selectable($"{name}");
    }
    
    public void RegistrationPanel()
    {
        if (ErrorMsg != string.Empty) { Helper.ErrorWindow(ref ErrorMsg); }
        
        if (participants.PlayerNameList.Count > 2)
        {
            if (ImGui.Button("Close Registration"))
            {
                Plugin.SwitchState(GameState.Shuffling);
            }
        }

        if (configuration.Debug)
        {
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            ImGui.Text("Number of players to generate:");
                        
            ImGui.SliderInt("##s", ref debugNumber, 3, 128);
            if (ImGui.IsItemDeactivatedAfterEdit())
            {
                debugNumber = Math.Clamp(debugNumber, 3, 128);
            }
                        
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            if (ImGui.Button("Auto"))
            {
                spTourn.AutoRegistration(debugNumber);
                Plugin.SwitchState(GameState.Shuffling);
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ImGui.TextColored(_greenColor,
            participants.PlayerNameList.Count <= 2
                ? $"{3 - participants.PlayerNameList.Count} more players are necessary ..."
                : $"Awaiting more players ...");
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ImGui.TextWrapped("Players can automatically enter by typing /random or /dice respectively while round registration is active.");
        ImGui.TextWrapped("Alternatively players can be manually entered by targeting the character and pressing 'Add Target' below.");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        if (ImGui.Button("Add Target")) { ErrorMsg = spTourn.TargetRegistration(); }
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        PlayerListRender();
    }
    
    private void PlayerListRender()
    {
        if (!participants.PlayerNameList.Any()) return;

        if (ImGui.CollapsingHeader("Players", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.BeginTable("##tournament_table", 1, ImGuiTableFlags.None))
            {
                ImGui.TableSetupColumn("##tournament_name");

                foreach (var participant in participants.PList)
                {
                    ImGui.TableNextColumn();
                    if (Helper.SelectableDelete(participant, participants))
                        break; // break because we deleted an entry
                }
            
                ImGui.EndTable();
            }
        }
    }
}