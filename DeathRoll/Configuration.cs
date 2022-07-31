using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace DeathRoll;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public bool On { get; set; } = false;
    public bool Debug { get; set; } = false;
    
    public int GameMode { get; set; } = 0;
    
    public bool RerollAllowed { get; set; } = false;
    public bool OnlyRandom { get; set; } = false;
    public bool OnlyDice { get; set; } = false;

    public SortingType SortingMode { get; set; } = SortingType.Min;
    public int Nearest { get; set; } = 1;

    public bool ActiveBlocklist { get; set; } = false;
    public List<string> SavedBlocklist { get; set; } = new();

    public bool ActiveHighlighting { get; set; } = false;
    public bool UseFirstPlace { get; set; } = false;
    public Vector4 FirstPlaceColor { get; set; } = new(0.586f, 0.996f, 0.586f, 1.0f);
    public bool UseLastPlace { get; set; } = false;
    public Vector4 LastPlaceColor { get; set; } = new(0.980f, 0.245f, 0.245f, 1.0f);

    public List<Highlight> SavedHighlights { get; set; } = new();

    public bool UseTimer { get; set; } = false;
    public int DefaultHour { get; set; } = 0;
    public int DefaultMin { get; set; } = 30;
    public int DefaultSec { get; set; } = 0;
    public int Version { get; set; } = 0;

    [NonSerialized] private DalamudPluginInterface? pluginInterface;
    [NonSerialized] public bool DRandomizeNames = false;
    [NonSerialized] public bool DebugAllowDiceCheat = false;
    [NonSerialized] public bool AcceptNewPlayers = false;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}