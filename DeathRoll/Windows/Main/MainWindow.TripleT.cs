using System.Threading.Tasks;
using Dalamud.Interface.Utility;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow
{
    private bool HeaderOpen;
    private string RoomKey = string.Empty;

    private void TripleTMode()
    {
        var scaledSpacing = 20.0f * ImGuiHelpers.GlobalScale;
        var letterHeight = ImGui.CalcTextSize("X").Y;
        var buttonHeight = letterHeight + scaledSpacing;
        if (ImGui.BeginChild("MainContent", new Vector2(0, -buttonHeight)))
        {
            if (Configuration.OnlineMode)
                TripleTOnlinePanel();
            else
                TripleTPanel();

            if (Plugin.TripleT.Winner != null)
            {
                ImGuiHelpers.ScaledDummy(10.0f);
                TripleTWinnerPanel();
            }

            var headerHeight = letterHeight + scaledSpacing;
            var textHeight = letterHeight * 4.5f; // giving space for 3 lines

            var spaceNeeded = headerHeight + (HeaderOpen ? textHeight : 0);
            if (ImGui.BeginChild("GameField", new Vector2(0, -spaceNeeded)))
                TripleTFieldPanel();
            ImGui.EndChild();

            if (ImGui.BeginChild("GameOptions", new Vector2(0, 0)))
                TripleTOptions();
            ImGui.EndChild();
        }
        ImGui.EndChild();

        ImGui.Separator();
        ImGuiHelpers.ScaledDummy(1.0f);

        if (ImGui.BeginChild("BottomBar", new Vector2(0, 0), false, 0))
        {
            Plugin.TripleT.GetEmptyRoomCount();
            ImGui.TextUnformatted($"Empty Rooms: {Plugin.TripleT.OpenRooms}");
        }
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

    private void TripleTOnlinePanel()
    {
        if (Plugin.TripleT.Room == null)
        {
            ImGuiHelpers.ScaledDummy(20.0f);

            var text = "Create Room";
            Helper.CenterNextButton(text);
            if (ImGui.Button(text))
                Plugin.TripleT.CreateRoom(Configuration, false);

            text = "Join Random Room";
            Helper.CenterNextButton(text);
            if (ImGui.Button(text))
                Plugin.TripleT.JoinRoom(Configuration, false);

            ImGuiHelpers.ScaledDummy(20.0f);

            text = "Create Private Room";
            Helper.CenterNextButton(text);
            if (ImGui.Button(text))
                Plugin.TripleT.CreateRoom(Configuration, true);

            text = "Join Private Room";
            Helper.CenterNextButton(text);
            ImGui.Button(text);
            JoinPopup();
        }
        else
        {
            if (ImGui.Button("Leave Room"))
                Plugin.TripleT.Reset();

            if (Plugin.TripleT is { IsHost: true, Board.BoardDone: true })
            {
                if (Plugin.TripleT.TimeLeft > 0)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Replay"))
                        Task.Run(async() => { await Plugin.TripleT.SendReplaySignal(); });
                }
                else
                {
                    // Trigger a timeout for the host
                    Plugin.TripleT.Reset();
                }
            }
        }

        if (Plugin.TripleT.Room != null)
        {
            if (ImGui.Selectable($"Current Room: {Plugin.TripleT.Room.Identifier}"))
                ImGui.SetClipboardText(Plugin.TripleT.Room.Identifier);

            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Click to copy room key");
        }
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

        if (Configuration.OnlineMode && Plugin.TripleT.Room == null)
            return;

        if (Configuration.OnlineMode && Plugin.TripleT.Awaiting == OnlineAwait.Replay)
            ImGui.TextColored(ImGuiColors.DalamudViolet, $"Waiting for replay indicator (Timeout in {Plugin.TripleT.TimeLeft} seconds)");

        var currentPlayer = Plugin.TripleT.CurrentPlayer;
        if (Plugin.TripleT.Winner == null)
        {
            if (!Configuration.OnlineMode)
            {
                ImGui.TextColored(ImGuiColors.DalamudViolet, "Current Player:");
                ImGui.SameLine();
                ImGui.TextUnformatted($"{currentPlayer.Playername} ({currentPlayer.Symbol.String()})");
                if (Plugin.TripleT.CalculatingAIMove)
                {
                    ImGui.SameLine();
                    ImGui.Text($"Thinking {new string('.', (int)((DateTime.Now - Plugin.TripleT.ComputeStart).TotalMilliseconds / 500) % 5)}");
                }
            }
            else
            {
                var text = Plugin.TripleT.Awaiting switch
                {
                    OnlineAwait.Join => "Waiting for another player to join ...",
                    OnlineAwait.Move => $"Waiting for {currentPlayer.Playername} ({currentPlayer.Symbol.String()}) to move ...",
                    OnlineAwait.Start => $"Waiting for start indicator ...",
                    _ => $"It's your turn {currentPlayer.Playername} ({currentPlayer.Symbol.String()})",
                };

                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudViolet);
                ImGui.TextUnformatted(text);
                ImGui.PopStyleColor();
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
                    Plugin.FontManager.SourceCode36.Push();
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

                        if (ImGui.IsItemClicked())
                        {
                            if (Configuration.OnlineMode)
                            {
                                if (Plugin.TripleT.MySymbol == Plugin.TripleT.CurrentPlayer.Symbol)
                                    Plugin.TripleT.MakeOnlineMove(row, col);
                            }
                            else
                            {
                                if (!Plugin.TripleT.CurrentPlayer.IsAI)
                                    Plugin.TripleT.MakeMove(row, col);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        ImGui.TextUnformatted("Error");
                        Plugin.Log.Error(e, "Something went wrong in UI draw");
                    }
                    Plugin.FontManager.SourceCode36.Pop();

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

        ImGui.AlignTextToFramePadding();
        ImGui.TextColored(ImGuiColors.DalamudViolet, "Online");
        ImGui.SameLine(width);
        if (ImGui.Checkbox("##OnlineCheckbox", ref Configuration.OnlineMode))
        {
            save = true;
            Plugin.TripleT.NewRound(Configuration);
        }

        if (!Configuration.OnlineMode)
        {
            ImGui.AlignTextToFramePadding();
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
        }
        else
        {
            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, longText);
            ImGui.SameLine(width);
            save |= ImGui.InputTextWithHint("##UsernameInput", "Your Username ...", ref Configuration.Username, 32);

            ImGui.AlignTextToFramePadding();
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Field Size");
            ImGui.SameLine(width);
            save |= ImGui.SliderInt("##FieldSizeSlider", ref Configuration.FieldSizeOnline, 3, 5);
        }

        if (save)
            Configuration.Save();
    }

    private bool JoinPopup()
    {
        ImGui.SetNextWindowSize(new Vector2(200 * ImGuiHelpers.GlobalScale, 90 * ImGuiHelpers.GlobalScale));
        if (!ImGui.BeginPopupContextItem("JoinPopup", ImGuiPopupFlags.None))
            return false;

        ImGui.BeginChild("JoinPopupChild", Vector2.Zero, false);

        var ret = false;

        ImGuiHelpers.ScaledDummy(3.0f);
        ImGui.SetNextItemWidth(180 * ImGuiHelpers.GlobalScale);
        ImGui.InputTextWithHint("##JoinPopupInput", "Room Key ...", ref RoomKey, 36, ImGuiInputTextFlags.AutoSelectAll);
        ImGuiHelpers.ScaledDummy(3.0f);

        if (ImGui.Button("Join"))
        {
            Plugin.TripleT.JoinRoom(Configuration, true, RoomKey);
            ret = true;
        }

        // ImGui issue #273849, children keep popups from closing automatically
        if (ret)
            ImGui.CloseCurrentPopup();

        ImGui.EndChild();
        ImGui.EndPopup();

        return ret;
    }
}