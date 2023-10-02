using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;

namespace DeathRoll.Windows.Config;

public partial class ConfigWindow
{
    private string NewBlocklistEntry = string.Empty;

    private void Blocklist()
    {
        if (ImGui.BeginTabItem("Blocklist"))
        {
            ImGuiHelpers.ScaledDummy(5.0f);

            var changed = false;
            changed |= ImGui.Checkbox("Blocklist active", ref Configuration.ActiveBlocklist);

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(ImGuiColors.DalamudViolet, "Blocked Players:");

            var width = ImGui.GetContentRegionAvail().X - 20.0f * ImGuiHelpers.GlobalScale;
            if (ImGui.BeginTable("##BlocklistTable", 2, 0, new Vector2(width, 0)))
            {
                ImGui.TableSetupColumn("##BlocklistPlus", 0, 0.13f);
                ImGui.TableSetupColumn("##BlocklistName");
                //ImGui.TableHeadersRow();

                var deletionIdx = -1;
                var updateIdx = -1;
                var newBlk = string.Empty;
                foreach (var (item, idx) in Configuration.SavedBlocklist.Select((value, i) => (value, i)))
                {
                    var currentBlocklistEntry = item;

                    ImGui.TableNextColumn();
                    ImGui.Indent(10.0f);
                    if (ImGuiComponents.IconButton(idx, FontAwesomeIcon.Trash))
                        deletionIdx = idx;

                    ImGui.TableNextColumn();
                    ImGui.PushItemWidth(-1);
                    ImGui.InputTextWithHint($"##blName{idx}", "", ref currentBlocklistEntry, 255);
                    ImGui.Unindent(10.0f);

                    if (currentBlocklistEntry == item)
                        continue;

                    updateIdx = idx;
                    newBlk = currentBlocklistEntry;
                }

                if (deletionIdx != -1)
                {
                    changed = true;
                    Configuration.SavedBlocklist.RemoveAt(deletionIdx);
                }

                if (updateIdx != -1)
                {
                    changed = true;
                    Configuration.SavedBlocklist[updateIdx] = newBlk.Replace("@", "\uE05D");
                }

                // new blocklist entry last
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Indent(10.0f);
                if (ImGuiComponents.IconButton(FontAwesomeIcon.Check))
                {
                    changed = true;
                    Configuration.SavedBlocklist.Add(NewBlocklistEntry.Replace("@", "\uE05D"));

                    NewBlocklistEntry = string.Empty;
                }

                ImGui.TableNextColumn();
                ImGui.PushItemWidth(-1);
                ImGui.InputTextWithHint("##Playername", "Name...", ref NewBlocklistEntry, 255);
                ImGui.Unindent(10.0f);

                ImGui.EndTable();
            }

            ImGuiHelpers.ScaledDummy(5.0f);
            ImGui.TextColored(Helper.Green, "Syntax: Player Name@World");

            if (changed)
                Configuration.Save();

            ImGui.EndTabItem();
        }
    }
}