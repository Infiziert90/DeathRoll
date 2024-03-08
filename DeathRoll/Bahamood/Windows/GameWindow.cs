using Dalamud.Interface.Windowing;

namespace DeathRoll.Bahamood.Windows;

public class GameWindow : Window, IDisposable
{
    private readonly Bahamood Game;

    public  Vector2 LastPos = Vector2.Zero;

    private (bool, bool, bool, ImGuiMouseCursor) Original;

    public GameWindow(Bahamood game) : base("Window###Bahamood")
    {
        Size = new Vector2(Settings.Width, Settings.Height);

        Game = game;
    }

    public void Dispose()
    {

    }

    public override void OnOpen()
    {
        var io = ImGui.GetIO();
        Original = (io.WantCaptureKeyboard, io.WantTextInput, io.WantCaptureMouse, ImGui.GetMouseCursor());
    }

    public override void OnClose()
    {
        if (Game.Running)
            Game.Stop();

        var io = ImGui.GetIO();
        io.WantCaptureKeyboard = Original.Item1;
        io.WantTextInput = Original.Item2;
        io.WantCaptureMouse = Original.Item3;

        ImGui.SetMouseCursor(Original.Item4);
    }

    public override void PreDraw()
    {
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(0.0f,0.0f));
        var io = ImGui.GetIO();
        io.WantCaptureKeyboard = true;
        io.WantTextInput = true;
        io.WantCaptureMouse = true;

        if (Game.HoldingAlt || Game.CurrentState is State.MainMenu or State.Credits)
        {
            Flags = ImGuiWindowFlags.None;
        }
        else
        {
            Flags = ImGuiWindowFlags.NoMove;
            ImGui.SetMouseCursor(ImGuiMouseCursor.None);
        }
    }

    public override void Draw()
    {
        Game.Draw();

        LastPos = ImGui.GetWindowPos();
    }

    public override void PostDraw()
    {
        ImGui.PopStyleVar();
    }
}