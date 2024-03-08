using System.Runtime.InteropServices;
using DeathRoll.Windows;

namespace DeathRoll.Bahamood;

public partial class Player
{
    private readonly Bahamood Game;

    private bool SkipNextMouse;
    private float AnimationTimer = 400.0f;

    public Vector2 Position;
    public float Angle;
    public float Rel;

    public bool Shot;
    public bool ReceivedDamage;
    public int Health = Settings.PlayerHealth;

    public bool CloseToDoor;
    private (int, int) DoorPos = (-1, -1);

    public Weapon CurrentWeapon;
    public readonly Queue<Weapon> OtherWeapons;

    public Player(Bahamood game)
    {
        Game = game;
        OtherWeapons = new Queue<Weapon>();
        CurrentWeapon = new Shotgun(Game);
    }

    private void CheckGameOver()
    {
        if (Health > 0)
            return;

        Game.Accumulator = 0.0f;
        Game.CurrentState = State.Dead;
    }

    public void GetDamage(int damage)
    {
        Health -= damage;
        ReceivedDamage = true;
        AnimationTimer = 400.0f;

        CheckGameOver();
    }

    private void CheckDamageAnimation()
    {
        AnimationTimer -= Bahamood.DeltaTime;
        if (AnimationTimer < 1)
            ReceivedDamage = false;
    }

    private void SingleFireEvent()
    {
        if (ImGui.GetIO().MouseClicked[0] && !CurrentWeapon.Reloading)
        {
            Shot = true;
            CurrentWeapon.Reloading = true;
        }
    }

    private void WeaponSwitchEvent()
    {
        if (!OtherWeapons.Any())
            return;

        if (ImGui.GetIO().MouseWheel != 0 && !CurrentWeapon.Reloading)
        {
            OtherWeapons.Enqueue(CurrentWeapon);
            CurrentWeapon = OtherWeapons.Dequeue();
        }
    }

    private void Movement()
    {
        var sinA = Math.Sin(Angle);
        var cosA = Math.Cos(Angle);
        var dX = 0.0;
        var dY = 0.0;

        var speed = Settings.PlayerSpeed * Bahamood.DeltaTime;
        var speedSin = speed * sinA;
        var speedCos = speed * cosA;

        if (ImGui.IsKeyDown(ImGuiKey.W))
        {
            dX += speedCos;
            dY += speedSin;
        }
        if (ImGui.IsKeyDown(ImGuiKey.S))
        {
            dX += -speedCos;
            dY += -speedSin;
        }
        if (ImGui.IsKeyDown(ImGuiKey.A))
        {
            dX += speedSin;
            dY += -speedCos;
        }
        if (ImGui.IsKeyDown(ImGuiKey.D))
        {
            dX += -speedSin;
            dY += speedCos;
        }

        CheckWallCollision(dX, dY);

        // if (ImGui.IsKeyDown(ImGuiKey.LeftArrow))
        // {
        //     Angle -= Settings.PlayerRotSpeed * Game.DeltaTime;
        // }
        //
        // if (ImGui.IsKeyDown(ImGuiKey.RightArrow))
        // {
        //     Angle += Settings.PlayerRotSpeed * Game.DeltaTime;
        // }

        Angle = (float) Utils.Mod(Angle, Math.Tau);
    }

    private bool CheckWall(int x, int y)
    {
        return !Game.CurrentLevel!.Map.WorldMap.ContainsKey((x, y));
    }

    private void CheckWallCollision(double dx, double dy)
    {
        var scale = Settings.PlayerSize / Bahamood.DeltaTime;
        if (CheckWall((int)(Position.X + dx * scale), (int)Position.Y))
            Position.X += (float) dx;
        if (CheckWall((int)Position.X, (int)(Position.Y + dy * scale)))
            Position.Y += (float) dy;
    }

    private void OpenDoor()
    {
        if (Game.CurrentLevel!.Map.WorldMap.TryGetValue(DoorPos, out var door))
            if (Game.Renderer.LoadedTextures[door].IsDoor)
                Game.CurrentLevel!.Map.WorldMap.Remove(DoorPos);

        DoorPos = (-1, -1);
        CloseToDoor = false;
    }

    private void CheckDoor()
    {
        CloseToDoor = false;

        var (oX, oY) = Game.Player.Pos;
        var (mapX, mapY) = Game.Player.MapPos;

        var sinA = Math.Sin(Angle);
        var cosA = Math.Cos(Angle);

        // horizontals
        var (horY, dY) = sinA > 0 ? (mapY + 1, 1.0) : (mapY - 1e-6, -1.0);
        var depthHor = (horY - oY) / sinA;
        var horX = oX + depthHor * cosA;

        var deltaDepth = dY / sinA;
        var dX = deltaDepth * cosA;

        for (var j = 0; j < 1; j++)
        {
            var tileHor = ((int)horX, (int)horY);
            if (Game.CurrentLevel!.Map.WorldMap.TryGetValue(tileHor, out var door))
            {
                if (Game.Renderer.LoadedTextures[door].IsDoor)
                {
                    DoorPos = tileHor;
                    CloseToDoor = true;
                    return;
                }
            }

            horX += dX;
            horY += dY;
            depthHor += deltaDepth;
        }

        // verticals
        (var vertX, dX) = cosA > 0 ? (mapX + 1, 1.0) : (mapX - 1e-6, -1.0);
        var depthVert = (vertX - oX) / cosA;
        var vertY = oY + depthVert * sinA;

        deltaDepth = dX / cosA;
        dY = deltaDepth * sinA;

        for (var j = 0; j < 1; j++)
        {
            var tileVert = ((int) vertX, (int) vertY);
            if (Game.CurrentLevel!.Map.WorldMap.TryGetValue(tileVert, out var door))
            {
                if (Game.Renderer.LoadedTextures[door].IsDoor)
                {
                    DoorPos = tileVert;
                    CloseToDoor = true;
                    return;
                }
            }

            vertX += dX;
            vertY += dY;
            depthVert += deltaDepth;
        }
    }

    private void MouseControl()
    {
        if (SkipNextMouse)
        {
            SkipNextMouse = false;
            return;
        }

        var io = ImGui.GetIO();

        var mousePos = io.MousePos;
        var windowPos = Game.Window.LastPos;

        if (mousePos.X - windowPos.X < Settings.MouseBorderLeft || mousePos.X - windowPos.X > Settings.MouseBorderRight)
        {
            SetCursorPos((int) windowPos.X + Settings.HalfWidth, (int) windowPos.Y + Settings.HalfHeight);
            SkipNextMouse = true;
        }

        Rel = Math.Max(-Settings.MouseMaxRel, Math.Min(Settings.MouseMaxRel, io.MouseDelta.X));
        Angle += Rel * Settings.MouseSensitivity * Bahamood.DeltaTime;
    }

    public void Draw(float scaling = 10.0f)
    {
        var drawlist = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();

        var drawX = p.X + Position.X * scaling;
        var drawY = p.Y + Position.Y * scaling;

        drawlist.AddLine(new Vector2(drawX, drawY), new Vector2(drawX + Settings.Height * (float) Math.Cos(Angle), drawY + + Settings.Height * (float) Math.Sin(Angle)), Helper.PlayerYellow);
        drawlist.AddCircleFilled(new Vector2(drawX, drawY), 5.0f, Helper.PlayerGreen);
    }

    public void Update()
    {
        Movement();

        if (!Game.HoldingAlt)
            MouseControl();

        SingleFireEvent();
        WeaponSwitchEvent();
        CheckDamageAnimation();
        CheckDoor();

        if (CloseToDoor && ImGui.IsKeyDown(ImGuiKey.F))
            OpenDoor();
    }

    public (float X, float Y) Pos => (Position.X, Position.Y);
    public (int X, int Y) MapPos => ((int) Position.X, (int) Position.Y);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool SetCursorPos(int x, int y);
}