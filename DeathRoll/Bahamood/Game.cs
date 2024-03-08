using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using DeathRoll.Bahamood.Windows;

namespace DeathRoll.Bahamood;

public enum State
{
    Playing = 0,
    Dead = 1,
    Victory = 2,

    NextStage = 98,
    MainMenu = 99,
    Credits = 100,
}

public class Bahamood
{
    public readonly Plugin Plugin;
    public const string Version = "0.0.1.0";

    public readonly GameWindow Window;
    public readonly DebugWindow DebugWindow;

    public Player Player;
    public int CurrentLevelIdx;
    public Level? CurrentLevel;

    public readonly Renderer Renderer;
    public readonly Raycasting Raycasting;
    public readonly TextureHandler.SpriteManager SpriteManager;

    public static float DeltaTime => ImGui.GetIO().DeltaTime * 1000;

    public bool Running;
    public bool HoldingAlt;

    public State CurrentState;
    private bool Loading;

    public float Accumulator;
    private float StateSwitchDelay = 2000.0f;

    public double UpdateTime;
    public double DrawTime;
    private readonly Stopwatch Watch = new();

    private bool Once;
    private readonly CachedSound MenuTheme;

    public Bahamood(Plugin plugin)
    {
        Plugin = plugin;
        MenuTheme = new CachedSound(Path.Combine(Plugin.PluginDir, @"Resources\Sound\MainMenu.aac"));

        Renderer = new(this);
        Raycasting = new(this);
        SpriteManager = new();

        NewGame();

        Window = new GameWindow(this);
        DebugWindow = new DebugWindow(this);
    }

    private void NewGame()
    {
        CurrentState = State.MainMenu;

        Once = false;
        Loading = false;
        Accumulator = 0.0f;
        CurrentLevelIdx = 0;
        Player = new(this);
    }

    public void NextLevel()
    {
        Accumulator = 0.0f;
        CurrentLevelIdx++;

        InitLevel();
    }

    public void InitLevel()
    {
        CurrentState = State.NextStage;

        CurrentLevel = null;
        Loading = true;
        Task.Run(() =>
        {
            CurrentLevel = StageSelection(CurrentLevelIdx);

            if (CurrentLevel == null)
                CurrentState = State.Victory;

            Loading = false;
        });
    }

    private void Update()
    {
        Watch.Restart();
        HoldingAlt = ImGui.GetIO().KeyAlt;

        Player!.Update();
        Raycasting.Update();
        CurrentLevel!.Update();
        Player.CurrentWeapon.Update();
        Watch.Stop();
        UpdateTime = Watch.Elapsed.TotalMilliseconds;
    }

    public void Draw()
    {
        Watch.Restart();
        switch (CurrentState)
        {
            case State.MainMenu:
                Renderer.DrawMainMenu();
                break;
            case State.Credits:
                Renderer.DrawCredits();
                break;
            case State.NextStage:
                Renderer.DrawNextStage();
                break;
            case State.Playing:
                Renderer.Draw();
                break;
            case State.Dead:
                Renderer.DrawGameOver();
                break;
            case State.Victory:
                Renderer.DrawVictory();
                break;
        }
        Watch.Stop();
        DrawTime = Watch.Elapsed.TotalMilliseconds;
    }

    public void Run()
    {
        if (!Running)
            return;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            Stop();
            return;
        }

        if (CurrentState is State.MainMenu or State.Credits)
        {
            if (!Once)
            {
                Once = true;
                AudioPlaybackEngine.Instance.Stop();
                AudioPlaybackEngine.Instance.PlaySound(MenuTheme);
                AudioPlaybackEngine.Instance.FadeIn();
            }

            return;
        }

        if (CurrentState == State.Playing)
        {
            Update();
        }
        else
        {
            if (Loading)
                return;

            Accumulator += DeltaTime;
            if (Accumulator < StateSwitchDelay)
                return;

            if (CurrentState == State.Victory)
            {
                NewGame();
                return;
            }

            if (CurrentState == State.NextStage)
            {
                CurrentState = State.Playing;
                Player!.Position = CurrentLevel!.StartPos;
                Player.Angle = CurrentLevel.StartAngle;
            }
            else
            {
                NewGame();
            }
        }
    }

    public void Stop()
    {
        AudioPlaybackEngine.Instance.Stop();
        Running = false;
        Window.IsOpen = false;
        DebugWindow.IsOpen = false;

        NewGame();
    }

    public void Dispose()
    {
        AudioPlaybackEngine.Instance.Dispose();

        SpriteManager.Dispose();
        TextureHandler.TextureManager.Dispose();
    }

    private Level? StageSelection(int stage)
    {
        var s = stage switch
        {
            0 => 0,
            1 => 1,

            _ => -1,
        };

        if (s == -1)
            return null;

        return s switch
        {
            0 => new LimsaStage1(this),
            1 => new LimsaStage2(this),

            _ => throw new ArgumentOutOfRangeException()
        };
    }
}