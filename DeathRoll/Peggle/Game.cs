using System.Diagnostics;
using DeathRoll.Peggle.Windows;

namespace DeathRoll.Peggle;

public enum State
{
    Menu = 0,
    Playing = 1,
    Dead = 2,
}

public class Peggle
{
    public readonly Plugin Plugin;
    public const string Version = "0.0.1.0";

    public readonly GameWindow Window;
    public readonly EditorWindow EditorWindow;

    public readonly Renderer Renderer;
    public readonly ObjectHandler ObjectHandler;

    public State CurrentState = State.Menu;

    public bool Running = true;
    public static float DeltaTimeMil => ImGui.GetIO().DeltaTime;
    public static float DeltaTimeSec => DeltaTimeMil * 1000;

    public double UpdateTime;
    public double DrawTime;
    private readonly Stopwatch Watch = new();


    public Peggle(Plugin plugin)
    {
        Plugin = plugin;

        Renderer = new Renderer(this);
        ObjectHandler = new ObjectHandler();

        Window = new GameWindow(this);
        EditorWindow = new EditorWindow();
    }

    private void Update()
    {
        Watch.Restart();
        if (ImGui.IsMouseDown(ImGuiMouseButton.Left))
            ObjectHandler.HandleMouseDown();
        
        if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            ObjectHandler.HandleMouseReleased();

        ObjectHandler.Update();
        Watch.Stop();
        UpdateTime = Watch.Elapsed.TotalMilliseconds;
    }

    public void Draw()
    {
        Watch.Restart();
        switch (CurrentState)
        {
            case State.Menu:
                Renderer.DrawMainMenu();
                break;
            case State.Playing:
                Renderer.Draw();
                break;
            case State.Dead:
                Renderer.DrawGameOver();
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

        if (CurrentState == State.Playing)
        {
            Update();
        }
    }

    public void Stop()
    {
        Running = false;
        Window.IsOpen = false;

        CurrentState = State.Menu;
    }

    public void Play()
    {
        Running = true;
        CurrentState = State.Playing;
    }

    public void Dispose()
    {

    }
}
