namespace DeathRoll.Bahamood.TextureHandler;

public static class TextureManager
{
    // XIV Textures
    public static readonly Texture GarleanFlag = Texture.GetTexSpecial(XivPaths.GarleanFlag, Vector2.Zero, new Vector2(0.72f, 1.0f));
    public static readonly Texture LimsaRock1 = Texture.GetTex(XivPaths.LimsaRock1);
    public static readonly Texture LimsaStuc1 = Texture.GetTex(XivPaths.LimsaStuc1);
    public static readonly Texture LimsaStpv1 = Texture.GetTex(XivPaths.LimsaStpv1);
    public static readonly Texture LimsaWall1 = Texture.GetTex(XivPaths.LimsaWall1);
    public static readonly Texture LimsaWood3 = Texture.GetTex(XivPaths.LimsaWood3);

    // Special XIV Textures
    public static readonly DoorTexture LimsaDoor = DoorTexture.GetDoor(XivPaths.LimsaRedDoor, 0, (int)(0.250f * 1024), (int)(0.485f * 1024), (int)((0.485f - 0.250f) * 1024));

    // General Textures
    public static readonly Texture SkyTexture = Texture.GetFile(@"Resources\Textures\Sky.png");
    public static readonly Texture BloodScreen = Texture.GetFile(@"Resources\Textures\BloodScreen.png");
    public static readonly Texture GameOver = Texture.GetFile(@"Resources\Textures\GameOver.png");

    // Self-made Textures
    public static readonly Texture Vines1 = Texture.GetFile(@"Resources\Textures\v1.png");
    public static readonly Texture Vines2 = Texture.GetFile(@"Resources\Textures\v2.png");
    public static readonly Texture VinesLeft = Texture.GetFile(@"Resources\Textures\v3L.png");
    public static readonly Texture VinesRight = Texture.GetFile(@"Resources\Textures\v3R.png");

    public static void Dispose()
    {
        GarleanFlag.Dispose();
        LimsaRock1.Dispose();
        LimsaStuc1.Dispose();
        LimsaStpv1.Dispose();
        LimsaWall1.Dispose();
        LimsaWood3.Dispose();

        LimsaDoor.Dispose();

        SkyTexture.Dispose();
        BloodScreen.Dispose();
        GameOver.Dispose();

        Vines1.Dispose();
        Vines2.Dispose();
        VinesLeft.Dispose();
        VinesRight.Dispose();
    }
}

public class SpriteManager
{
    public readonly BunnySprite Bunny = new();
    public readonly SwordMonsterSprite SwordMonster = new();

    public readonly FatCatSprite FatCat = new();

    public readonly ShotgunSprite Shotgun = new();
    public readonly RevolverSprite Revolver = new();

    public readonly CollectableHealthSprite CollectableHealth = new();
    public readonly CollectableRevolverSprite CollectableRevolver = new();

    public void Dispose()
    {
        Bunny.Dispose();
        SwordMonster.Dispose();

        FatCat.Dispose();

        Shotgun.Dispose();
        Revolver.Dispose();

        CollectableHealth.Dispose();
        CollectableRevolver.Dispose();
    }
}
