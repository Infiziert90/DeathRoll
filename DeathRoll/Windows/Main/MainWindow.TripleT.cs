using Dalamud.Interface.Utility;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private bool HeaderOpen;

    private void TripleTMode()
    {
        TripleTPanel();

        if (Plugin.TripleT.Winner != null)
        {
            ImGuiHelpers.ScaledDummy(10.0f);
            TripleTWinnerPanel();
        }

        var textHeight = ImGui.CalcTextSize("XXXX").Y * 2.0f; // giving space for 6.0 lines
        var optionHeight = (HeaderOpen ? -65 : 0) * ImGuiHelpers.GlobalScale;
        if (ImGui.BeginChild("GameField", new Vector2(0, -textHeight + optionHeight)))
            TripleTFieldPanel();
        ImGui.EndChild();

        if (ImGui.BeginChild("GameOptions", new Vector2(0,0)))
            TripleTOptions();
        ImGui.EndChild();
    }

    private void TripleTPanel()
    {
        if (ImGui.Button("Show Settings"))
            Plugin.OpenConfig();


        var spacing = ImGui.GetScrollMaxY() == 0 ? 85.0f : 120.0f;
        ImGui.SameLine(ImGui.GetWindowWidth() - spacing);

        if (ImGui.Button("New Round"))
            Plugin.TripleT.NewRound(Configuration);
    }

    private void TripleTWinnerPanel()
    {
        if (Plugin.TripleT.Winner!.Player.Symbol != PlayerSymbol.None)
            Helper.SetTextCenter($"{Plugin.TripleT.Winner!.Player.Playername} won!!!", Helper.Red);
        else
            Helper.SetTextCenter($"It's a tie!!!", Helper.Red);
    }

    private void TripleTFieldPanel()
    {
        ImGuiHelpers.ScaledDummy(10.0f);

        var currentPlayer = Plugin.TripleT.CurrentPlayer;
        if (Plugin.TripleT.Winner == null)
        {
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Current Player:");
            ImGui.SameLine();
            ImGui.TextUnformatted($"{currentPlayer.Playername} ({currentPlayer.Symbol.String()})");
            if (Plugin.TripleT.CalculatingAIMove)
            {
                ImGui.SameLine();
                ImGui.Text($"Thinking {new string('.', (int)((DateTime.Now - Plugin.TripleT.ComputeStart).TotalMilliseconds / 500) % 5)}");
            }
            ImGuiHelpers.ScaledDummy(10.0f);
        }

        var fieldSize = Plugin.TripleT.Board.GameField.FieldSize;
        if (ImGui.BeginTable("##rolls", fieldSize, ImGuiTableFlags.BordersInnerV))
        {
            for (var row = 0; row < fieldSize; row++)
            {
                for (var col = 0; col < fieldSize; col++)
                {
                    ImGui.TableNextColumn();
                    ImGui.PushFont(Plugin.FontManager.SourceCode36);
                    try
                    {
                        var field = Plugin.TripleT.Board.GameField.Field[row, col];

                        ImGui.PushID($"{row}{col}");
                        if (field != PlayerSymbol.None)
                        {
                            if (Plugin.TripleT.Winner == null)
                            {
                                Helper.TableCenterText(field.String());
                            }
                            else switch (Plugin.TripleT.Winner.Type)
                            {
                                case WinType.Row when Plugin.TripleT.Winner.Row == row:
                                case WinType.Col when Plugin.TripleT.Winner.Col == col:
                                case WinType.Diag when Plugin.TripleT.Board.IsDiag(row, col):
                                case WinType.Anti when Plugin.TripleT.Board.IsAnti(row, col):
                                    Helper.TableCenterText(field.String(), ImGuiColors.HealerGreen);
                                    break;
                                case WinType.Tie:
                                default:
                                    Helper.TableCenterText(field.String());
                                    break;
                            }
                        }
                        else
                        {
                            ImGui.BeginGroup();
                            Helper.TableDummy("X");
                            ImGui.EndGroup();
                        }
                        ImGui.PopID();

                        if (ImGui.IsItemClicked() && !Plugin.TripleT.CurrentPlayer.IsAI)
                            Plugin.TripleT.MakeMove(row, col);
                    }
                    catch (Exception e)
                    {
                        ImGui.TextUnformatted("Error");
                        Plugin.Log.Error(e, "Something went wrong in UI draw");
                    }
                    ImGui.PopFont();

                    if (row < fieldSize - 1)
                        ImGui.Separator();
                }
            }

            ImGui.EndTable();
        }
    }

    private void TripleTOptions()
    {
        HeaderOpen = ImGui.CollapsingHeader("Options");
        if (!HeaderOpen)
            return;

        var save = false;
        var longText = "Username";
        var width = ImGui.CalcTextSize(longText).X + (20.0f * ImGuiHelpers.GlobalScale);

        ImGui.TextColored(ImGuiColors.DalamudViolet, longText);
        ImGui.SameLine(width);
        save |= ImGui.InputTextWithHint("##UsernameInput", "Your Username ...", ref Configuration.Username, 32);

        // Planned for the future, AI too slow for it atm
        // ImGui.TextColored(ImGuiColors.DalamudViolet, "Field Size");
        // ImGui.SameLine(width);
        // save |= ImGui.SliderInt("##FieldSizeSlider", ref Configuration.FieldSize, 3, 3);

        ImGui.TextColored(ImGuiColors.DalamudViolet, "Difficulty");
        ImGui.SameLine(width);
        if (ImGui.BeginCombo($"##DifficultyCombo", Configuration.Difficulty.Name()))
        {
            foreach (var difficulty in (Difficulty[]) Enum.GetValues(typeof(Difficulty)))
            {
                if (!ImGui.Selectable(difficulty.Name()))
                    continue;

                save = true;
                Configuration.Difficulty = difficulty;
            }

            ImGui.EndCombo();
        }

        if (save)
            Configuration.Save();
    }
}