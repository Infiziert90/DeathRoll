using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using DeathRoll.Data;

namespace DeathRoll.Windows.Main;

public partial class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    private readonly Configuration Configuration;

    public MainWindow(Plugin plugin) : base("Main")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 480),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin = plugin;
        Configuration = plugin.Configuration;
        Backend = Plugin.RollManager.Blackjack;
        SimpleTournament = Plugin.RollManager.SimpleTournament;

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
                Venue();
                break;
            case GameModes.DeathRoll:
                DeathRoll();
                break;
            case GameModes.Tournament:
                Tournament();
                break;
            case GameModes.Blackjack:
                Blackjack();
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
            var result = SimpleTournament.TargetRegistration();
            if (result != string.Empty)
                Plugin.PluginInterface.UiBuilder.AddNotification(result, "DeathRoll Helper", NotificationType.Error);
        }
        ImGuiHelpers.ScaledDummy(10.0f);
    }
}