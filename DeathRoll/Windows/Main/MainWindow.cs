using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using DeathRoll.Data;
using DeathRoll.Logic;

namespace DeathRoll.Windows.Main;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    public MainWindow(Plugin plugin) : base("Main##DeathRoll")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
        Blackjack = Plugin.RollManager.Blackjack;
        Tournament = Plugin.RollManager.SimpleTournament;

        RestoreTimerDefaults();
    }

    public void Dispose()
    {
        StopTimer();
    }

    public override void Draw()
    {
        switch (Configuration.GameMode)
        {
            case GameModes.Venue:
                VenueMode();
                break;
            case GameModes.DeathRoll:
                DeathRollMode();
                break;
            case GameModes.Tournament:
                TournamentMode();
                break;
            case GameModes.Blackjack:
                BlackjackMode();
                break;
            case GameModes.TripleT:
                TripleTMode();
                break;
            default:
                ImGui.Text("Not Implemented!");
                break;
        }
    }

    private void AddTargetButton()
    {
        ImGuiHelpers.ScaledDummy(5.0f);
        if (ImGui.Button("Add Target"))
        {
            var result = TargetRegistration();
            if (result != string.Empty)
                Plugin.PluginInterface.UiBuilder.AddNotification(result, "DeathRoll Helper", NotificationType.Error);
        }
        ImGuiHelpers.ScaledDummy(10.0f);
    }

    private string TargetRegistration()
    {
        var name = Plugin.GetTargetName();
        if (name == string.Empty)
            return "Target not found.";

        if (Plugin.Participants.PlayerNameList.Exists(x => x == name))
            return "Target already registered.";

        Plugin.Participants.Add(new Participant(Roll.Dummy(name)));
        Plugin.Participants.PlayerBets[name] = new Participants.Player(Plugin.Configuration.DefaultBet, true);

        return string.Empty;
    }
}