using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;

namespace DeathRoll
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool On { get; set; } = false;
        public bool DebugChat { get; set; } = false;
        
        public bool ActiveRound { get; set; } = false;
        public bool DeactivateOnClear { get; set; } = false;
        public bool RerollAllowed { get; set; } = false;
        public bool OnlyRandom { get; set; } = false;
        public bool OnlyDice { get; set; } = false;

        public int CurrentMode { get; set; } = 0;
        public int Nearest { get; set; } = 1;
        public int NumberOfTables { get; set; } = 1;

        public bool ActiveBlocklist { get; set; } = false;
        public List<string> SavedBlocklist { get; set; } = new();
        public bool ActiveHightlighting { get; set; } = false;
        
        public List<Highlight> SavedHighlights { get; set; } = new();
        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        [NonSerialized]
        public bool DebugRandomizedPlayerNames = false;
        public bool DebugAllowDiceCheat = false;
        
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
