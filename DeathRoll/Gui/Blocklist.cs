using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace DeathRoll.Gui;

public class Blocklist
{
    private Configuration configuration;
    private string _newBlocklistEntry = string.Empty;
    private Vector4 _greenColor = new Vector4(0.0f, 1.0f, 0.0f,1.0f);
    
    public Blocklist(Configuration configuration)
    {
        this.configuration = configuration;
    }
    
    public void RenderBlocklistTab()
    {
        if (!ImGui.BeginTabItem($"Blocklist###blocklist-tab")) return;
        
        var activeBlocklist = this.configuration.ActiveBlocklist;
        if (ImGui.Checkbox("Blocklist active", ref activeBlocklist))
        {
            this.configuration.ActiveBlocklist = activeBlocklist;
            this.configuration.Save();
        }
            
        ImGui.Dummy(new Vector2(0.0f, 5.0f));
        
        if (!ImGui.BeginTable("##highlighttable", 2, ImGuiTableFlags.None))
            return;

        ImGui.TableSetupColumn("##block_plusbutton", ImGuiTableColumnFlags.None, 0.10f);
        ImGui.TableSetupColumn("##block_nameheader");
        //ImGui.TableHeadersRow();

        if (configuration.SavedBlocklist?.Count > 0)
        {
            var deletionIdx = -1;
            var updateIdx = -1;
            var _newBlk = string.Empty;
            foreach (var (item, idx) in configuration.SavedBlocklist.Select((value, i) => ( value, i )))
            {
                var _currentBlocklistEntry = item;
            
                ImGui.TableNextColumn();
                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##blocklist_delbtn{idx}"))
                {
                    deletionIdx = idx;
                }
                ImGui.PopFont();
            
                ImGui.TableNextColumn();
                ImGui.PushItemWidth(220.0f);
                ImGui.InputTextWithHint($"##blocklistname{idx}", "", ref _currentBlocklistEntry, 255);
                    
                if ( _currentBlocklistEntry != item)
                {
                    updateIdx = idx;
                    _newBlk = _currentBlocklistEntry;
                };
            }
        
            if (deletionIdx != -1)
            {
                configuration.SavedBlocklist.RemoveAt(deletionIdx);
                configuration.Save();
            }
            
            if (updateIdx != -1)
            {
                configuration.SavedBlocklist[updateIdx] = _newBlk.Replace("@", "\uE05D");
                configuration.Save();
            }
        }

        // new blocklist entry last
        ImGui.TableNextColumn();
        ImGui.PushFont(UiBuilder.IconFont);
        if (ImGui.Button($"{FontAwesomeIcon.Check.ToIconString()}##blocklist_plusbtn"))
        {
            configuration.SavedBlocklist?.Add(_newBlocklistEntry.Replace("@", "\uE05D"));
            configuration.Save();
            _newBlocklistEntry = string.Empty;
        }
        ImGui.PopFont();
        
        ImGui.TableNextColumn();
        ImGui.PushItemWidth(220.0f);
        ImGui.InputTextWithHint("##playername", "Name...", ref _newBlocklistEntry, 255);

        ImGui.EndTable();
        
        ImGui.TextColored(_greenColor, "Syntax: Player Name@World");
        
        
        
        
        
        ImGui.EndTabItem();
    }
}