using System.IO;

namespace DeathRoll.Bahamood;

public class Weapon : AnimatedSpriteHandler
{
    private readonly float ScaledWidth;
    private readonly float ScaledHeight;
    private readonly Vector2 WeaponPosition;

    public int Damage;
    public bool Reloading;

    private int FrameCounter;

    private bool Once;
    internal CachedSound WeaponSound = null!;

    private readonly TextureHandler.Sprite WeaponSprites;

    protected Weapon(Bahamood game, TextureHandler.Sprite sprites, float scale, float shift, int animationTime) : base(game, Vector2.Zero, sprites, scale, shift, animationTime)
    {
        WeaponSprites = sprites;

        ScaledWidth = WeaponSprites.SimpleImage.Width * Scale;
        ScaledHeight = WeaponSprites.SimpleImage.Height * Scale;
        WeaponPosition = new Vector2(Settings.HalfWidth - ScaledWidth / 2, Settings.Height - ScaledHeight);
    }

    private void AnimateShot()
    {
        if (!Reloading)
            return;

        if (!Once)
        {
            Once = true;
            AudioPlaybackEngine.Instance.PlaySound(WeaponSound);
        }

        Game.Player!.Shot = false;
        if (Trigger)
        {
            Image = WeaponSprites.WeaponImages[FrameCounter];
            FrameCounter++;

            if (FrameCounter == WeaponSprites.WeaponImages.Length)
            {
                Once = false;
                Reloading = false;
                FrameCounter = 0;
                Image = WeaponSprites.WeaponImages[FrameCounter];
            }
        }
    }

    public void Draw(ImDrawListPtr drawlist, Vector2 p)
    {
        var drawX = p.X + WeaponPosition.X;
        var drawY = p.Y + WeaponPosition.Y;
        drawlist.AddImage(Image.Tex.ImGuiHandle, new Vector2(drawX, drawY), new Vector2(drawX + ScaledWidth, drawY + ScaledHeight));
    }

    public override void Update()
    {
        CheckAnimationTime();
        AnimateShot();
    }
}

public class Shotgun : Weapon
{
    public Shotgun(Bahamood game)
        : base(game, game.SpriteManager.Shotgun, 0.4f, 0.0f, 130)
    {
        Damage = 50;
        WeaponSound = new CachedSound(Path.Combine(Plugin.PluginDir, @"Resources\Sound\Shotgun.aac"));
    }
}

public class Revolver : Weapon
{
    public Revolver(Bahamood game)
        : base(game, game.SpriteManager.Revolver, 0.5f, 0.0f, 150)
    {
        Damage = 100;
        WeaponSound = new CachedSound(Path.Combine(Plugin.PluginDir, @"Resources\Sound\Revolver.aac"));
    }
}