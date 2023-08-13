using Dalamud.Interface.Components;
using DeathRoll.Data;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private const ImGuiColorEditFlags ColorFlags = ImGuiColorEditFlags.NoInputs | ImGuiColorEditFlags.NoAlpha;

    private Vector4 NewColor = new(0.6f, 0.6f, 0.6f, 1.0f);
    private string NewRegex = string.Empty;

    private void Highlight()
    {
        if (ImGui.BeginTabItem("Highlight"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            changed |= ImGui.Checkbox("Highlights Active", ref Configuration.ActiveHighlighting);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Default Highlights:");

            ImGui.Indent(10.0f);
            if (ImGui.ColorEdit4("##FirstPlaceColorEdit", ref Configuration.FirstPlaceColor, ColorFlags))
            {
                changed = true;
                Configuration.FirstPlaceColor.W = 1;
            }
            ImGui.SameLine();
            changed |= ImGui.Checkbox("First Place", ref Configuration.UseFirstPlace);

            if (ImGui.ColorEdit4("##LastPlaceColorEdit", ref Configuration.LastPlaceColor, ColorFlags))
            {
                changed = true;
                Configuration.LastPlaceColor.W = 1;
            }
            ImGui.SameLine();
            changed |= ImGui.Checkbox("Last Place", ref Configuration.UseLastPlace);
            ImGui.Unindent(10.0f);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Custom Highlights:");

            var width = ImGui.GetContentRegionAvail().X - 20.0f * ImGuiHelpers.GlobalScale;
            if (ImGui.BeginTable("##hlTable", 3, 0, new Vector2(width, 0)))
            {
                ImGui.TableSetupColumn("##hlDel", 0, 0.13f);
                ImGui.TableSetupColumn("##hlColor", 0, 0.08f);
                ImGui.TableSetupColumn("##hlRegex");
                //ImGui.TableHeadersRow();

                var deletionIdx = -1;
                var updateIdx = -1;
                var newReg = string.Empty;
                var newCol = new Vector4();
                foreach (var (item, idx) in Configuration.SavedHighlights.Select((value, i) => (value, i)))
                {
                    var currentRegex = item.Regex;
                    var currentColor = item.Color;

                    ImGui.TableNextColumn();
                    ImGui.Indent(10.0f);
                    if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                        deletionIdx = idx;

                    ImGui.TableNextColumn();
                    ImGui.ColorEdit4($"##hl_oldColor{idx}", ref currentColor, ColorFlags);

                    ImGui.TableNextColumn();
                    ImGui.SetNextItemWidth(-1);
                    ImGui.InputTextWithHint($"##regex{idx}", "", ref currentRegex, 255);
                    ImGui.Unindent(10.0f);

                    if (currentRegex == item.Regex && currentColor == item.Color)
                        continue;

                    updateIdx = idx;
                    newCol = currentColor;
                    newReg = currentRegex;
                }

                if (deletionIdx != -1)
                {
                    changed = true;
                    Configuration.SavedHighlights.RemoveAt(deletionIdx);
                    Plugin.Participants.Update();
                }
                else if (updateIdx != -1)
                {
                    newCol.W = 1.0f; // fix alpha

                    changed = true;
                    Configuration.SavedHighlights[updateIdx].Update(newReg, newCol);
                    Plugin.Participants.Update();
                }

                // new highlight entry last
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Indent(10.0f);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Check))
                {
                    NewColor.W = 1; // fix alpha being 0

                    changed = true;
                    Configuration.SavedHighlights.Add(new Highlight(NewRegex, NewColor));
                    RestoreDefaults();
                    Plugin.Participants.Update();
                }

                ImGui.TableNextColumn();
                ImGui.ColorEdit4("##hlNewColor", ref NewColor, ColorFlags);

                ImGui.TableNextColumn();
                ImGui.SetNextItemWidth(-1);
                ImGui.InputTextWithHint("##Regex", "Regex...", ref NewRegex, 255);
                ImGui.Unindent(10.0f);

                ImGui.EndTable();
            }

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(Helper.Green, "Simple Matching: ^YourNumber$");

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }

    private void RestoreDefaults()
    {
        NewRegex = string.Empty;
        NewColor = new Vector4(0.6f, 0.6f, 0.6f, 1.0f);
    }
}