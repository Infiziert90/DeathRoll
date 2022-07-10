using System.Collections.Generic;
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
        
        switch (spTourn.TS)
        {
            case TournamentStates.Registration:
                RegistrationPanel();
                break;
            case TournamentStates.Shuffling:
                ShufflingPreviewPanel();
                break;
            case TournamentStates.Prepare:
                PreparePhaseRender();
                break;
            case TournamentStates.Done:
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
    }

    public void RenderControlPanel()
    {
        if (ImGui.Button("Show Settings")) pluginUi.SettingsVisible = true;
        
        var spacing = ImGui.GetScrollMaxY() == 0 ? 120.0f : 135.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        switch (spTourn.TS)
        {
            case TournamentStates.NotRunning:
                if (!ImGui.Button("Start Tournament")) return;
                spTourn.SwitchState(TournamentStates.Registration);
                participants.Reset();
                return;            
            case TournamentStates.Crash:
                if (!ImGui.Button("Force Stop Tournament")) return;
                bracketVisible = false;
                matchVisible = false;
                spTourn.SwitchState(TournamentStates.NotRunning);
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
                    spTourn.SwitchState(TournamentStates.Match);
                }

                if (!configuration.DebugChat) return;
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

    public void DrawBracket()
    {
        if (!bracketVisible) return;
        if (spTourn.TS is TournamentStates.NotRunning or TournamentStates.Crash) return;
        
        ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowSizeConstraints(new Vector2(600, 600), new Vector2(float.MaxValue, float.MaxValue));
        if (ImGui.Begin("DeathRoll Tournament Bracket", ref bracketVisible))
        {
            if (!ImGui.BeginTable("##Bracket", spTourn.LastStage)) return;
            foreach (var idx in Enumerable.Range(0, spTourn.LastStage))
            {
                ImGui.TableSetupColumn($"Stage{idx+1}");
            }

            ImGui.TableHeadersRow();

            var nestedIdxList = new List<int>();
            nestedIdxList.Add(-1);
            for (var i = 1; i < spTourn.LastStage; i++)
            {
                nestedIdxList.Add(0);
            }

            for (var idx = 0; idx < spTourn.TestBrackets[0].Count*2; idx++)
            {
                for (var stage = 0; stage < spTourn.LastStage; stage++)
                {
                    if (nestedIdxList[stage] >= spTourn.TestBrackets[stage].Count) break;
                    switch (stage)
                    {
                        case 0 when idx % 2 == 0:
                            ImGui.TableNextColumn();
                            ImGui.Text(spTourn.TestBrackets[stage][idx/2]);
                            break;
                        case 0 when idx % 2 == 1:
                            ImGui.TableNextColumn();  
                            ImGui.Dummy(new Vector2(0.0f, 10.0f));
                            break;
                        case 1 when idx != 1 && idx % 4 != 1:
                            continue;
                        case 1:
                            ImGui.TableNextColumn();
                            ImGui.Text(spTourn.TestBrackets[stage][nestedIdxList[stage]]);
                            nestedIdxList[stage]++;
                            break;
                        default:
                        {
                            if (stage + 1 == spTourn.LastStage)
                            {
                                if (idx != spTourn.StageDepth[stage]) continue;
                                var n = 0;
                                for (; n < stage; n++) ImGui.TableNextColumn();
                                ImGui.Text(spTourn.TestBrackets[stage][nestedIdxList[stage]]);
                                nestedIdxList[stage]++;
                            }
                            else
                            {
                                var stageDepth = spTourn.StageDepth[stage];
                                if (idx != stageDepth && idx != (stageDepth * 2 + 2) * nestedIdxList[stage] + stageDepth)
                                    continue;
                                for (var n = 0; n < stage; n++) ImGui.TableNextColumn();
                                ImGui.Text(spTourn.TestBrackets[stage][nestedIdxList[stage]]);
                                nestedIdxList[stage]++;
                            }

                            break;
                        }
                    }
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
        if (spTourn.TS is TournamentStates.NotRunning or TournamentStates.Crash) return;

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
            spTourn.SwitchState(TournamentStates.Prepare);
            return;
        }  
            
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        var deletion = "";
        if (ImGui.CollapsingHeader("Shuffled Entry List", ImGuiTreeNodeFlags.DefaultOpen))
            foreach (var playerName in spTourn.TParticipants.PlayerNameList)
            {
                var participant = spTourn.TParticipants.FindPlayer(playerName);
                var name = participant.GetUsedName(configuration.DebugRandomPn);
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

        if (deletion != "") participants.DeleteEntry(deletion);
    }
    
    public void RegistrationPanel()
    {
        if (participants.PlayerNameList.Count > 2)
        {
            if (ImGui.Button("Close Registration"))
            {
                spTourn.SwitchState(TournamentStates.Shuffling);
            }
        }

        if (configuration.DebugChat)
        {
            if (ImGui.Button("Auto"))
            {
                spTourn.AutoRegistration();
                spTourn.SwitchState(TournamentStates.Shuffling);
            }
        }

        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ImGui.TextColored(_greenColor, $"Awaiting more players ...");
        ImGui.Dummy(new Vector2(0.0f, 10.0f));
        ImGui.Text($"Players can enter by rolling /random or /dice once.");
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        var deletion = "";
        if (ImGui.CollapsingHeader("Entry List", ImGuiTreeNodeFlags.DefaultOpen))
            foreach (var playerName in participants.PlayerNameList)
            {
                var participant = participants.FindPlayer(playerName);
                var name = participant.GetUsedName(configuration.DebugRandomPn);
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

        if (deletion != "") participants.DeleteEntry(deletion);
    }
}