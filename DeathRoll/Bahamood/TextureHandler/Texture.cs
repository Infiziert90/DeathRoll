using System.IO;
using Dalamud.Interface.Internal;
using Dalamud.Utility;
using Lumina.Data.Files;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DeathRoll.Bahamood.TextureHandler;

public enum SpriteType
{
    Sprite = 0,
    AnimatedSprite = 1,
    NPC = 2,
    Weapon = 3,
    Collectable = 4,
}

public class Texture
{
    public readonly IDalamudTextureWrap Tex;

    public readonly int Width;
    public readonly int Height;

    public readonly bool SimpleTexture;

    public Texture(IDalamudTextureWrap tex, Vector2 uvMin, Vector2 uvMax)
    {
        Tex = tex;

        Width = (int) (tex.Width * (uvMax.X - uvMin.X));
        Height = (int) (tex.Height * (uvMax.Y - uvMin.Y));
    }

    public Texture(IDalamudTextureWrap tex)
    {
        Tex = tex;

        Width = tex.Width;
        Height = tex.Height;

        SimpleTexture = true;
    }

    public void Dispose() => Tex.Dispose();

    public static Texture FromFile(string file)
    {
        return new Texture(Plugin.PluginInterface.UiBuilder.LoadImage(file));
    }

    public static Texture GetFile(string path)
    {
        return new Texture(Plugin.PluginInterface.UiBuilder.LoadImage(Path.Combine(Plugin.PluginDir, path)));
    }

    public static Texture GetTex(string path)
    {
        var tex = Plugin.Data.GetFile<TexFile>(path)!;
        return new Texture(Plugin.TextureProvider.GetTexture(tex));
    }

    public static Texture GetTexSpecial(string path, Vector2 uvMin, Vector2 uvMax)
    {
        var tex = Plugin.Data.GetFile<TexFile>(path)!;
        return new Texture(Plugin.TextureProvider.GetTexture(tex), uvMin, uvMax);
    }

    public static Texture FromRaw(byte[] data, int width, int height, int channels)
    {
        return new Texture(Plugin.PluginInterface.UiBuilder.LoadImageRaw(data, width, height, channels));
    }

    private static Texture GetUldPart(string path, string tex, int index)
    {
        var uld = Plugin.PluginInterface.UiBuilder.LoadUld(path);
        if (!uld.Valid)
            Plugin.Log.Error("Not a valid ULD");
        var texture = uld.LoadTexturePart(tex, index);
        if (texture == null)
            Plugin.Log.Error("Not a valid texture");
        return new Texture(uld.LoadTexturePart(tex, index)!);
    }

    public static implicit operator nint(Texture t) => t.Tex.ImGuiHandle;
}

public class TextureCollection
{
    public bool IsDoor;
    public Texture[] Simple = null!;

    private Texture[] North = null!;
    private Texture[] East = null!;
    private Texture[] South = null!;
    private Texture[] West = null!;

    public bool UseDirection;

    public Texture[] GetDirection((int X, int Y) diff, bool vertHit)
    {
        var north = diff.Y <= 0;
        var east = diff.X <= 0;

        if (north && east)
            return vertHit ? East : North;
        if (!north && east)
            return vertHit ? East : South;
        if (!north && !east)
            return vertHit ? West : South;

        return vertHit ? West : North;
    }

    public static TextureCollection SimpleCol(Texture[] textures, bool isDoor = false)
    {
        return new TextureCollection
        {
            Simple = textures,
            IsDoor = isDoor
        };
    }

    public static TextureCollection WestFacing(Texture[] west, Texture[] others)
    {
        return new TextureCollection
        {
            Simple = west,
            West = west,

            East = others,
            South = others,
            North = others,
            UseDirection = true
        };
    }

    public static TextureCollection EastFacing(Texture[] east, Texture[] others)
    {
        return new TextureCollection
        {
            Simple = east,
            East = east,

            West = others,
            South = others,
            North = others,
            UseDirection = true
        };
    }

    public static TextureCollection Facings(Texture[] north, Texture[] east, Texture[] south, Texture[] west)
    {
        return new TextureCollection
        {
            Simple = north,
            North = north,
            East = east,
            South = south,
            West = west,

            UseDirection = true
        };
    }
}

public class DoorTexture
{
    public Texture Full = null!;
    public Texture Left = null!;
    public Texture Right = null!;

    public static DoorTexture GetDoor(string path, int top, int left, int bottom, int right)
    {
        var tex = Plugin.Data.GetFile<TexFile>(path)!;

        var width = tex.Header.Width;
        var height = tex.Header.Height;

        using var doorRight = Image.LoadPixelData<Rgba32>(tex.GetRgbaImageData(), width, height);
        doorRight.Mutate(d => d
            .Crop(new Rectangle(left, top, right, bottom))
            .Resize(256, 512));

        using var doorLeft = doorRight.Clone(d => d.Flip(FlipMode.Horizontal));

        using var fullTexture = new Image<Rgba32>(512, 512);
        fullTexture.Mutate(x => x
            .DrawImage(doorLeft, new Point(0, 0), 1f)
            .DrawImage(doorRight, new Point(256, 0), 1f));

        using var leftTexture = new Image<Rgba32>(512, 512);
        leftTexture.Mutate(x => x.DrawImage(doorLeft, new Point(0, 0), 1f));

        using var rightTexture = new Image<Rgba32>(512, 512);
        rightTexture.Mutate(x => x.DrawImage(doorRight, new Point(256, 0), 1f));

        return new DoorTexture
        {
            Full = Texture.FromRaw(fullTexture.ImageToRaw(), 512, 512, 4),
            Left = Texture.FromRaw(leftTexture.ImageToRaw(), 512, 512, 4),
            Right = Texture.FromRaw(rightTexture.ImageToRaw(), 512, 512, 4),
        };
    }

    public void Dispose()
    {
        Full.Dispose();
        Left.Dispose();
        Right.Dispose();
    }
}

public class Sprite
{
    private const string SpritePath = @"Resources\Sprites";

    public readonly Texture SimpleImage;
    public readonly Texture[] AnimatedImages = Array.Empty<Texture>();

    public readonly Texture[] AttackImages = Array.Empty<Texture>();
    public readonly Texture[] DeathImages = Array.Empty<Texture>();
    public readonly Texture[] IdleImages = Array.Empty<Texture>();
    public readonly Texture[] PainImages = Array.Empty<Texture>();
    public readonly Texture[] WalkImages = Array.Empty<Texture>();

    public readonly Texture[] WeaponImages = Array.Empty<Texture>();


    protected Sprite(string path, SpriteType type)
    {
        path = $@"{SpritePath}\{path}";
        SimpleImage = Texture.GetFile(path);

        switch (type)
        {
            case SpriteType.Collectable:
            case SpriteType.Sprite:
                // These types use the SimpleImage that all others also need
                break;
            case SpriteType.AnimatedSprite:
                AnimatedImages = LoadImages(path);
                break;
            case SpriteType.NPC:
                var pathDir = Path.GetDirectoryName(path)!;

                AttackImages = LoadImages($@"{pathDir}\Attack\");
                DeathImages = LoadImages($@"{pathDir}\Death\");
                IdleImages = LoadImages($@"{pathDir}\Idle\");
                PainImages = LoadImages($@"{pathDir}\Pain\");
                WalkImages = LoadImages($@"{pathDir}\Walk\");
                break;
            case SpriteType.Weapon:
                WeaponImages = LoadImages(path);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    private static Texture[] LoadImages(string pathDir)
    {
        var dir = new FileInfo(Path.Combine(Plugin.PluginDir, pathDir)).Directory!;
        return dir.EnumerateFiles().Select(f => Texture.FromFile(f.FullName)).ToArray();
    }

    public void Dispose()
    {
        SimpleImage.Dispose();

        foreach (var tex in AnimatedImages)
            tex.Dispose();

        foreach (var tex in AttackImages)
            tex.Dispose();

        foreach (var tex in DeathImages)
            tex.Dispose();

        foreach (var tex in IdleImages)
            tex.Dispose();

        foreach (var tex in PainImages)
            tex.Dispose();

        foreach (var tex in WalkImages)
            tex.Dispose();

        foreach (var tex in WeaponImages)
            tex.Dispose();
    }
}