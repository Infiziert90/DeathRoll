using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using DeathRoll.Attributes;
using DeathRoll.Data;
using DeathRoll.Logic;
using DeathRoll.Windows.Bracket;
using DeathRoll.Windows.CardField;
using DeathRoll.Windows.Config;
using DeathRoll.Windows.Main;
using DeathRoll.Windows.Match;

namespace DeathRoll;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static ITargetManager TargetManager { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static INotificationManager Notification { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider GameInteropProvider { get; private set; } = null!;

    public static string PluginDir => PluginInterface.AssemblyLocation.DirectoryName!;

    private readonly WindowSystem WindowSystem = new("DeathRoll Helper");
    public MainWindow MainWindow { get; init; }
    public ConfigWindow ConfigWindow { get; init; }
    private MatchWindow MatchWindow { get; init; }
    private BracketWindow BracketWindow { get; init; }
    private CardFieldWindow CardFieldWindow { get; init; }

    public readonly Configuration Configuration;
    public readonly RollManager RollManager;
    public readonly FontManager FontManager;
    public readonly HookManager HookManager;

    public string LocalPlayer = string.Empty;
    public readonly Participants Participants;
    public GameState State = GameState.NotRunning;

    public readonly Uno Uno;
    public readonly Blackjack Blackjack;
    public readonly RoundInfo TripleT;
    public Minesweeper Minesweeper;
    public readonly Bahamood.Bahamood Bahamood;
    public readonly Peggle.Peggle Peggle;

    private readonly PluginCommandManager<Plugin> CommandManager;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        Participants = new Participants(Configuration);
        HookManager = new HookManager(this);
        RollManager = new RollManager(this);
        Uno = new Uno(this);
        Blackjack = new Blackjack(this);
        TripleT = new RoundInfo(Configuration);
        Minesweeper = new Minesweeper(Configuration.MinesweeperDif.GridSizes()[0]);
        Bahamood = new Bahamood.Bahamood(this);
        Peggle = new Peggle.Peggle(this);

        FontManager = new FontManager();

        MainWindow = new MainWindow(this);
        ConfigWindow = new ConfigWindow(this);
        MatchWindow = new MatchWindow(this);
        BracketWindow = new BracketWindow(this);
        CardFieldWindow = new CardFieldWindow(this);

        WindowSystem.AddWindow(MainWindow);
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MatchWindow);
        WindowSystem.AddWindow(BracketWindow);
        WindowSystem.AddWindow(CardFieldWindow);

        WindowSystem.AddWindow(Bahamood.Window);
        #if DEBUG
        WindowSystem.AddWindow(Bahamood.DebugWindow);
        #endif

        WindowSystem.AddWindow(Peggle.Window);
        WindowSystem.AddWindow(Peggle.EditorWindow);

        CommandManager = new PluginCommandManager<Plugin>(this, Commands);

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;

        Framework.Update += GameUpdate;
    }

    private void GameUpdate(IFramework framework)
    {
        Bahamood.Run();
        Peggle.Run();
    }

    public void Dispose()
    {
        Bahamood.Dispose();
        Peggle.Dispose();

        Framework.Update -= GameUpdate;

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        PluginInterface.UiBuilder.Draw -= DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi -= OpenConfig;

        FontManager.Dispose();
        CommandManager.Dispose();
        HookManager.Dispose();

        TripleT.Dispose();
    }

    [Command("/drh")]
    [Aliases("/deathroll")]
    [HelpMessage("Toggles UI\nArguments:\non - Turns on\noff - Turns off\nconfig - Opens config\ntimer - Toggles timer")]
    public void PluginCommand(string _, string args)
    {
        switch (args)
        {
            case "on":
                Configuration.On = true;
                Configuration.Save();
                break;
            case "off":
                Configuration.On = false;
                Configuration.Save();
                break;
            case "config":
                ConfigWindow.IsOpen = true;
                break;
            case "timer":
                if (MainWindow.IsTimerActive())
                    MainWindow.StopTimer();
                else
                    MainWindow.BeginTimer();
                break;
            case "bahamood":
                Bahamood.Running ^= true;
                Bahamood.Window.IsOpen ^= true;
                Bahamood.DebugWindow.IsOpen ^= true;
                break;
            default:
                MainWindow.IsOpen = true;
                break;
        }
    }

    public static string GetTargetName()
    {
        var target = TargetManager.SoftTarget ?? TargetManager.Target;
        if (target is not IPlayerCharacter pc || pc.HomeWorld.ValueNullable == null)
            return string.Empty;

        return $"{pc.Name}\uE05D{pc.HomeWorld.Value.Name}";
    }

    public void ProcessIncomingMessage(string fullName, int roll, int outOf)
    {
        if (!Configuration.On || State is GameState.NotRunning or GameState.Done or GameState.Crash)
            return;

        var local = ClientState.LocalPlayer;
        if (local?.HomeWorld.ValueNullable?.Name != null)
            LocalPlayer = $"{local.Name}\uE05D{local.HomeWorld.Value.Name}";

        if (Configuration.ActiveBlocklist && Configuration.SavedBlocklist.Contains(fullName.Replace("\uE05D", "@")))
        {
            Log.Information("Blocked player tried to roll.");
            return;
        }

        RollManager.ParseRoll(new Roll(fullName, roll, outOf));
    }

    public void SwitchState(GameState newState)
    {
        State = newState;
        if (newState is GameState.NotRunning)
            Participants.Reset();
    }

    #region UI Toggles
    private void DrawUI() => WindowSystem.Draw();
    public void OpenMain() => MainWindow.IsOpen = true;
    public void OpenConfig() => ConfigWindow.IsOpen = true;
    public void OpenMatch() => MatchWindow.IsOpen = true;
    public void OpenBracket() => BracketWindow.IsOpen = true;
    public void OpenCardField() => CardFieldWindow.IsOpen = true;

    public void ToggleCardField() => CardFieldWindow.IsOpen ^= true;

    public void ClosePlayWindows()
    {
        MatchWindow.IsOpen = false;
        BracketWindow.IsOpen = false;
        CardFieldWindow.IsOpen = false;
    }
    #endregion
}