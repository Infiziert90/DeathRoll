namespace DeathRoll.Peggle.Objects;

public record Size(bool IsRadius, float Radius, Vector2 Lengths)
{
    public static Size DefaultCircle => new(true, 8.0f, Vector2.Zero);
}

public class BaseObject
{
    public Vector2 Position;
    public Vector2 Velocity;

    public Size Size;

    public bool Alive = true;

    internal uint Color;

    internal BaseObject(Vector2 position, Vector2 velocity, Size size, uint color = 0)
    {
        Position = position;
        Velocity = velocity;
        Color = color;

        Size = size;
    }

    internal void CollisionCheck()
    {
        if (Size.IsRadius)
        {
            var radius = Size.Radius;
            // Check left / right border
            if (Position.X < radius || Position.X > Settings.Width - radius)
                Velocity.X = -Velocity.X;
            // Check top border
            if (Position.Y < radius)
                Velocity.Y = -Velocity.Y;
            // Check bottom death border
            else if (Position.Y > Settings.Height + radius)
                Alive = false;
        }
    }

    public override string ToString()
    {
        return $"Position: {Position.X} {Position.Y}\nVelocity: {Velocity.X} {Velocity.Y}";
    }
}
