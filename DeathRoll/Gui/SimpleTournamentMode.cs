using System;
using System.Linq;
using System.Numerics;
using DeathRoll.Logic;
using ImGuiNET;

namespace DeathRoll.Gui;

public class SimpleTournamentMode
{
    private readonly Vector4 _redColor = new(0.980f, 0.245f, 0.245f, 1.0f);
    private readonly Vector4 _greenColor = new(0.0f, 1.0f, 0.0f, 1.0f);
    private readonly Vector4 _yellowColor = new(0.959f, 1.0f, 0.0f, 1.0f);
    private readonly Configuration configuration;
    private readonly Participants participants;
    private readonly SimpleTournament spTourn;

    private readonly PluginUI pluginUi;

    private bool shuffled = false;
    private int debugNumber = 8;

    private bool bracketVisible = false;
    private bool matchVisible = false;
    
    public SimpleTournamentMode(PluginUI pluginUi)
    {
        this.pluginUi = pluginUi;
        configuration = pluginUi.Configuration;
        participants = pluginUi.Participants;
        spTourn = pluginUi.Rolls.simpleTournament;
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
        
        var windowWidth = ImGui.GetWindowSize().X;
        var textWidth1   = ImGui.CalcTextSize("WINNER").X;
        var textWidth2   = ImGui.CalcTextSize("GOAT").X;
        var textWidth3   = ImGui.CalcTextSize("DINNER").X;
        var textWidth4   = ImGui.CalcTextSize($"{participants.Winner.fName}").X;
        
        ImGui.SetCursorPosX((windowWidth - textWidth1) * 0.5f);
        ImGui.TextColored(_yellowColor, $"WINNER");
        ImGui.SetCursorPosX((windowWidth - textWidth1) * 0.5f);
        ImGui.TextColored(_yellowColor, $"WINNER");
        ImGui.SetCursorPosX((windowWidth - textWidth2) * 0.5f);
        ImGui.TextColored(_yellowColor, $"GOAT");
        ImGui.SetCursorPosX((windowWidth - textWidth3) * 0.5f);
        ImGui.TextColored(_yellowColor, $"DINNER");
        ImGui.SetCursorPosX((windowWidth - textWidth4) * 0.5f);
        ImGui.TextColored(_yellowColor, $"{participants.Winner.fName}");
        
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
        {
            bracketVisible = true;
        }

        if (!spTourn.CalculationDone)
        {
            ImGui.TextUnformatted("Preparing Next Match...");
        }
        else
        {
            if (spTourn.Player2.name != "Byes")
            {
                ImGui.Text("Next Match:");
                ImGui.Text($"{spTourn.Player1.fName} vs {(spTourn.Player2.fName)}");
                
                if (ImGui.Button("Begin Match"))
                {
                    matchVisible = true;
                    Plugin.SwitchState(GameState.Match);
                }

                if (!configuration.Debug) return;
                if (ImGui.Button("Auto Win Match"))
                {
                    spTourn.AutoWin();
                }
            }
            else
            {
                ImGui.Text("No other player available.");
                ImGui.Text($"{spTourn.Player1.fName} automatically wins");
                
                if (ImGui.Button("Continue to next Round"))
                {
                    spTourn.ForfeitWin(spTourn.Player1);
                }
            }
        }
    }

    public void DrawGeneratedBracket()
    {
        if (!bracketVisible) return;
        if (Plugin.State is GameState.NotRunning or GameState.Crash) return;
        
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
                ImGui.Dummy(new Vector2(0.0f, 10.0f));
                pluginUi.DeathRollMode.RenderWinnerPanel();  
            }
            else
            {
                ImGui.Text($"Player 1: {spTourn.Player1.fName}");
                ImGui.Text($"Player 2: {spTourn.Player2.fName}");
            }
        
            ImGui.Dummy(new Vector2(0.0f, 10.0f));
            pluginUi.DeathRollMode.ParticipantRender();

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
                ImGui.Dummy(new Vector2(0.0f, 20.0f));
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
        foreach (var name in spTourn.TParticipants.PlayerNameList.Select(playerName => 
                     spTourn.TParticipants.FindPlayer(playerName).GetUsedName(configuration.DebugRandomPn)))
        {
            ImGui.Selectable($"{name}");
        }
    }
    
    public void RegistrationPanel()
    {
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
        ImGui.Text($"Players can enter by rolling /random or /dice once.");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        Helper.PlayerListRender("Entry List", configuration.DebugRandomPn, participants, ImGuiTreeNodeFlags.DefaultOpen);
    }
}