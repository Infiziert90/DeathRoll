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
            public Regex CompiledRegex;

            public Highlight(string r, Vector4 c)
            {
                if (r == null)
                {
                    System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
                    PluginLog.Error("Regex String is null?");
                    PluginLog.Error($"{t.ToString()}");
                    r = "";
                }
                this.Regex = r;
                this.Color = c;
                this.Compile(r);
            }

            public void Update(string r, Vector4 c)
            {
                this.Regex = r;
                this.Color = c;
                this.Compile(r);
            }

            private void Compile(string r)
            {
                CompiledRegex = new Regex(r);
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
