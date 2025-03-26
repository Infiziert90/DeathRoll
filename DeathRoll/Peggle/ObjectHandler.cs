using DeathRoll.Peggle.Objects;

namespace DeathRoll.Peggle;

public class ObjectHandler
{
    private PlayBall Ball = PlayBall.CreateBall();
    private readonly List<Peg> Targets = [];

    private bool HasMouseBeenDown;

    public ObjectHandler()
    {
        var purplePegs = 2;
        var orangePegs = 5;
        var greenPegs = 10;

        var pegDiameter = 8.0f * 2;
        
        var offsetX = 100f;
        var offsetY = 200f;
        for (var i = 0; i < 25; i++)
        {
            for (var j = 0; j < 40; j++)
            {
                Targets.Add(new Peg(new Vector2(offsetX, offsetY)));
                offsetX += pegDiameter;
            }

            offsetX = 100f;
            offsetY += pegDiameter;
        }

        var rng = new Random();
        for (var i = 0; i < purplePegs; i++)
            Targets[rng.Next(0, Targets.Count)].ChangeType(PegType.Purple);
        
        for (var i = 0; i < orangePegs; i++)
            Targets[rng.Next(0, Targets.Count)].ChangeType(PegType.Orange);
        
        for (var i = 0; i < greenPegs; i++)
            Targets[rng.Next(0, Targets.Count)].ChangeType(PegType.Green);
    }

    public void Update()
    {
        // No ball, so we don't need to update anything
        if (!Ball.Alive)
            return;
        
        Ball.Update();
        foreach (var target in Targets)
            target.Update(Ball);
    }

    public void Draw(ImDrawListPtr drawlist, Vector2 screenPos)
    {
        if (Ball.Alive)
            Ball.Draw(drawlist, screenPos);
        else
            Ball.DeadBallDraw(drawlist, screenPos);
        
        foreach (var target in Targets)
            target.Draw(drawlist, screenPos);
    }

    public void HandleMouseReleased()
    {
        if (!HasMouseBeenDown)
            return;
        
        if (!Ball.Alive)
        {
            Ball = PlayBall.CreateBall();
        }
        else if (!Ball.Fired)
        {
            HasMouseBeenDown = false;
            Ball.Fire();
        }
    }

    public void HandleMouseDown()
    {
        HasMouseBeenDown = true;
        
        if (!Ball.Fired)
            Ball.IncreaseVelocity();
    }
}
