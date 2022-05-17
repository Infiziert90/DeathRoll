using System.Numerics;
using ImGuiNET;

namespace DeathRoll.Gui;

public class GeneralSettings
{
    private Configuration configuration;
    
    public GeneralSettings(Configuration configuration)
    {
        this.configuration = configuration;
    }
    
    public void RenderGeneralSettings()
    {
        if (ImGui.BeginTabItem($"General###general-tab"))
        {
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            var on = this.configuration.On;
            if (ImGui.Checkbox("On", ref on))
            {
                this.configuration.On = on;
                this.configuration.Save();
            }

            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            ImGui.Text("Options:");

            // TODO implement multiple tables (allows min, max, nearest at the same time)
            ImGui.Dummy(new Vector2(0.0f, 5.0f));
            
            //ImGui.Text("Number of tables:");
            // ImGui.SliderInt("##numberoftables", ref this._numberOfTables, 1, 3);
            // if (ImGui.IsItemDeactivatedAfterEdit())
            // {
            //     this._numberOfTables = Math.Clamp(this._numberOfTables, 1, 3);
            //     if (this._numberOfTables != this.configuration.NumberOfTables)
            //     {
            //         this.configuration.NumberOfTables = _numberOfTables;
            //         this.configuration.Save();
            //     }
            // }

            var deactivateOnClear = this.configuration.DeactivateOnClear;
            if (ImGui.Checkbox("Clear ends active round", ref deactivateOnClear))
            {
                this.configuration.DeactivateOnClear = deactivateOnClear;
                this.configuration.Save();
            }
            
            ImGui.Dummy(new Vector2(0.0f, 40.0f));
            var verboseChatlog = this.configuration.DebugChat;
            if (ImGui.Checkbox("Debug", ref verboseChatlog))
            {
                this.configuration.DebugChat = verboseChatlog;
                this.configuration.Save();
            }

            ImGui.EndTabItem();
        }
    }
}