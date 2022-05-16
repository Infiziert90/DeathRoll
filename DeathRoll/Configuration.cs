using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Logging;

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
        public int CurrentMode { get; set; } = 0;
        public int Nearest { get; set; } = 1;
        public int NumberOfTables { get; set; } = 1;

        public bool ActiveHightlighting { get; set; } = false;
        
        public List<Highlight> SavedHighlights { get; set; } = new();
        
        public class Highlight
        {
            public string Regex;
            public Vector4 Color;
            private Regex? _compiled = null;
            public Regex CompiledRegex => this._compiled ??= new Regex(this.Regex);
            // and clear _compiled to null when Regex changes

            public Highlight() { }
            
            public Highlight(string r, Vector4 c)
            {
                if (r == null)
                {
                    PluginLog.Error("Regex String is null?");
                    r = "";
                }
                this.Regex = r;
                this.Color = c;
            }

            public void Update(string r, Vector4 c)
            {
                this.Regex = r;
                this.Color = c;
                this._compiled = null;
            }
            
        }
        // the below exist just to make saving less cumbersome

        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

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
