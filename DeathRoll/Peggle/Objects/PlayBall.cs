using DeathRoll.Windows;

namespace DeathRoll.Peggle.Objects;

public class PlayBall : BaseObject
{
    public bool Fired;

    private float FireAngle;
    private Vector2 FirePosition;
    
    const int MaxVelocityMultiplier = 5;
    private int StartVelocityMultiplier = 1;
    
    private bool Trigger;
    private float Accumulator;
    private float SpinUpTime = 500f;
    
    public PlayBall(Vector2 position, Vector2 velocity, Size size) : base(position, velocity, size, Helper.PegSpeed1)
    {
        
    }
    
    public static PlayBall CreateBall() => new(new Vector2(Settings.HalfWidth, 0), Vector2.Zero, Size.DefaultCircle);

    public void Fire()
    {
        Fired = true;
        Position = FirePosition;

        var muzzleVelocity = Settings.MuzzleVelocity * StartVelocityMultiplier;
        var xOff = muzzleVelocity * MathF.Cos(FireAngle);
        var yOff = muzzleVelocity * MathF.Sin(FireAngle);
        Velocity = new Vector2(MathF.Abs(xOff) < 0.0001 ? 0 : xOff, MathF.Abs(yOff) < 0.0001 ? 0 : yOff);
    }

    public void IncreaseVelocity()
    {
        if (!Trigger)
        {
            CheckSpinUpTimer();
            return;
        }
        
        if (StartVelocityMultiplier < MaxVelocityMultiplier)
        {
            StartVelocityMultiplier++;
            Color = StartVelocityMultiplier switch
            {
                1 => Helper.PegSpeed1,
                2 => Helper.PegSpeed2,
                3 => Helper.PegSpeed3,
                4 => Helper.PegSpeed4,
                5 => Helper.PegSpeed5,
                
                _ => Helper.PegSpeed1,
            };

            Trigger = false;
        }
    }

    private void CheckSpinUpTimer()
    {
        Trigger = false;

        Accumulator += Peggle.DeltaTimeSec;
        if (Accumulator > SpinUpTime)
        {
            Accumulator = 0.0f;
            Trigger = true;
        }
    }

    public void Update()
    {
        if (!Fired)
            return;

        Velocity.Y += Peggle.DeltaTimeMil * Settings.Gravity;
        Position += Velocity * Peggle.DeltaTimeMil;

        CollisionCheck();
    }

    public void DeadBallDraw(ImDrawListPtr drawlist, Vector2 screenPos)
    {
        var text = "Click to spawn new ball!";
        var textWidth = ImGui.CalcTextSize(text).X;
        
        ImGui.SetCursorPos(new Vector2(Settings.HalfWidth - (textWidth / 2), 50));
        ImGui.TextUnformatted("Click to spawn new ball!");
    }
    
    public void Draw(ImDrawListPtr drawlist, Vector2 screenPos)
    {
        if (!Fired)
        {
            var cursor = ImGui.GetIO().MousePos - screenPos;

            var x = cursor.X - Position.X;
            var y = cursor.Y - Position.Y;
            var angle = MathF.Atan2(y, x);

            var cos = MathF.Cos(angle) * Settings.OffsetFromTop;
            var sin = MathF.Sin(angle) * Settings.OffsetFromTop;
            var pos = Position + new Vector2(cos, MathF.Max(sin, 0));

            FireAngle = angle;
            FirePosition = pos;

            drawlist.AddCircleFilled(pos + screenPos, Size.Radius, Color);
            return;
        }

        var top = new Vector2(Position.X - Size.Radius, Position.Y - Size.Radius);
        drawlist.AddCircleFilled(screenPos+top, Size.Radius, Color);
    }
}
