using Dalamud.Configuration;
using Dalamud.Plugin;
using DeathRoll.Data;

namespace DeathRoll;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public bool On = false;
    public bool Debug = false;

    public GameModes GameMode = 0;

    public bool RerollAllowed = false;
    public bool OnlyRandom = false;
    public bool OnlyDice = false;
    public bool TimerResets = false;

    public SortingType SortingMode = SortingType.Min;
    public int Nearest = 1;

    public bool ActiveBlocklist = false;
    public List<string> SavedBlocklist = new();

    public bool ActiveHighlighting = false;
    public bool UseFirstPlace = false;
    public bool UseLastPlace = false;
    public Vector4 FirstPlaceColor = new(0.586f, 0.996f, 0.586f, 1.0f);
    public Vector4 LastPlaceColor = new(0.980f, 0.245f, 0.245f, 1.0f);

    public List<Highlight> SavedHighlights = new();

    public bool UseTimer = false;
    public int DefaultHour = 0;
    public int DefaultMin = 30;
    public int DefaultSec = 0;

    public bool AutoDrawCard = true;
    public bool AutoDrawOpening = true;
    public bool AutoDrawDealer = true;
    public bool DealerDrawsAll = false;
    public bool VenueDealer = false;
    public bool StartingDraw = false;
    public bool StartingBlackjack = false;
    public int BlackjackMode = 0;
    public bool AutoOpenField = true;
    public int DefaultBet = 250000;
    public DealerRules DealerRule = DealerRules.DealerHard16;

    [NonSerialized] private DalamudPluginInterface? PluginInterface;
    [NonSerialized] public bool AcceptNewPlayers = false;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}