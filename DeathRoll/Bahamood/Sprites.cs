using DeathRoll.Bahamood.TextureHandler;

namespace DeathRoll.Bahamood;

public class SpriteHandler
{
    internal readonly Bahamood Game;

    internal Vector2 Position;
    internal readonly float Scale;
    private readonly float Shift;

    internal int SpriteHalfWidth;
    private readonly float ImageRatio;
    private readonly int ImageHalfWidth;

    internal float ScreenX;
    internal float Distance = 1;
    private float NormDistance = 1;

    internal float ThetA;

    internal Texture Image;

    protected SpriteHandler(Bahamood game, Vector2 pos, Sprite sprites, float scale, float shift)
    {
        Game = game;
        Position = pos;
        Scale = scale;
        Shift = shift;

        Image = sprites.SimpleImage;
        ImageHalfWidth = Image.Width / 2;
        ImageRatio = Image.Width / (float) Image.Height;
    }

    public virtual void Update()
    {
        GetSprite();
    }

    private void GetSpriteProjection()
    {
        var proj = Settings.ScreenDist / NormDistance * Scale;
        var (projWidth, projHeight) = (proj * ImageRatio, proj);

        // TODO: transform it here

        SpriteHalfWidth = (int) Math.Floor(projWidth / 2);
        var heightShift = projHeight * Shift;
        var pos = GetPos(new Vector2(ScreenX - SpriteHalfWidth, Settings.HalfHeight - (int) Math.Floor(projHeight / 2) + heightShift));
        var size = new Vector2(projWidth, projHeight);

        Game.Raycasting.ObjectsToRender.Add(new RenderObject(NormDistance, new []{ Image }, size, pos));
    }

    internal void GetSprite()
    {
        var diff = Position - Game.Player!.Position;
        ThetA = (float) Math.Atan2(diff.Y, diff.X);

        var delta = ThetA - Game.Player.Angle;
        if ((diff.X > 0 && Game.Player.Angle > Math.PI) || diff is { X: < 0, Y: < 0 })
            delta += (float) Math.Tau;

        var deltaRays = delta / Settings.DeltaAngle;
        ScreenX = (Settings.HalfNumRays + deltaRays) * Settings.Scale;
        Distance = float.Hypot(diff.X, diff.Y);
        NormDistance = Distance * (float) Math.Cos(delta);
        if (-ImageHalfWidth < ScreenX && ScreenX < (Settings.Width + ImageHalfWidth) && NormDistance > 0.5)
            GetSpriteProjection();

    }

    private (int X, int Y) GetPos(Vector2 pos) => ((int) pos.X, (int) pos.Y);
}

public class AnimatedSpriteHandler : SpriteHandler
{
    private int AnimationCounter;
    private readonly int AnimationTime;

    internal bool Trigger;
    private float Accumulator;

    private readonly Sprite Animations;

    protected AnimatedSpriteHandler(Bahamood game, Vector2 pos, Sprite sprites, float scale, float shift, int animationTime)
        : base(game, pos, sprites, scale, shift)
    {
        Animations = sprites;
        AnimationTime = animationTime;
    }

    public override void Update()
    {
        base.Update();
        CheckAnimationTime();
        Animate(Animations.AnimatedImages, ref AnimationCounter);
    }

    internal void Animate(Texture[] images, ref int counter)
    {
        if (!Trigger)
            return;

        if (counter >= images.Length)
            counter = 0;

        Image = images[counter];
        counter++;
    }

    internal void CheckAnimationTime()
    {
        Trigger = false;

        Accumulator += Bahamood.DeltaTime;
        if (Accumulator > AnimationTime)
        {
            Accumulator = 0.0f;
            Trigger = true;
        }
    }
}

public class FatCat : AnimatedSpriteHandler
{
    public FatCat(Bahamood game, Vector2 pos, float scale = 0.5f, float shift = 0.5f, int animationTime = 60)
        : base(game, pos, game.SpriteManager.FatCat, scale, shift, animationTime)
    {

    }
}