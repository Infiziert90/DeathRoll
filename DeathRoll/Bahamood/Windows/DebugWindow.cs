using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;

namespace DeathRoll.Bahamood.Windows;

public class DebugWindow : Window, IDisposable
{
    private readonly Bahamood Game;

    public DebugWindow(Bahamood game) : base("DebugWindow##Bahamood")
    {
        SizeConstraints = new WindowSizeConstraints()
        {
            MinimumSize = new Vector2(200, 500),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Game = game;
    }

    public void Dispose()
    {

    }

    public override void OnClose()
    {
        if (Game.Running)
            Game.Stop();
    }

    public override void Draw()
    {
        if (Game.CurrentLevel == null)
            return;

        ImGui.TextUnformatted($"X: {Game.Player.Position.X}");
        ImGui.TextUnformatted($"Y: {Game.Player.Position.Y}");
        ImGui.TextUnformatted($"Angle: {Game.Player.Angle}");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted($"Mouse X: {ImGui.GetIO().MousePos.X}");
        ImGui.TextUnformatted($"Window X: {Game.Window.LastPos.X}");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted($"Player Rel: {Game.Player.Rel}");
        ImGui.TextUnformatted($"SkyOffset: {Game.Renderer.SkyOffset}");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted($"Delta Angle: {Settings.DeltaAngle}");
        ImGui.TextUnformatted($"FOV: {Settings.FieldOfView}");
        ImGui.TextUnformatted($"Half FOV: {Settings.HalfFoV}");
        ImGui.TextUnformatted($"Scale: {Settings.Scale}");
        ImGui.TextUnformatted($"Screen Dist: {Settings.ScreenDist}");
        ImGui.TextUnformatted($"Num of Rays: {Settings.NumRays}");

        ImGuiHelpers.ScaledDummy(5.0f);

        ImGui.TextUnformatted($"ImGui DeltaTime: {Bahamood.DeltaTime}");
        ImGui.TextUnformatted($"Update Time: {Game.UpdateTime}");
        ImGui.TextUnformatted($"Draw Time: {Game.DrawTime}");

        if (Game.CurrentState != State.Playing)
            return;

        Game.CurrentLevel.Map.Draw();
        Game.Player.Draw();
        Game.Raycasting.RayCastLines();
    }
}