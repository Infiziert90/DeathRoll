using System.IO;

namespace DeathRoll.Bahamood;

public class Level
{
    protected readonly Bahamood Game;

    internal Map Map = null!;
    internal ObjectHandler ObjectHandler = null!;
    internal Pathfinder Pathfinder = null!;

    internal string LevelName = string.Empty;

    private bool PlayOnce;
    private readonly CachedSound Theme;

    public readonly Vector2 StartPos;
    public readonly float StartAngle;

    protected Level(Bahamood game, Vector2 startPos, float startAngle, string theme)
    {
        Game = game;
        StartPos = startPos;
        StartAngle = startAngle;
        Theme = new CachedSound(Path.Combine(Plugin.PluginDir, theme));
    }

    private bool CheckVictory()
    {
        return !ObjectHandler.AnyAlive;
    }

    public void Update()
    {
        if (!PlayOnce)
        {
            PlayOnce = true;
            AudioPlaybackEngine.Instance.Stop();
            AudioPlaybackEngine.Instance.PlaySound(Theme);
            AudioPlaybackEngine.Instance.FadeIn();
        }

        ObjectHandler.Update();

        if (CheckVictory())
            Game.NextLevel();
    }
}

public class LimsaStage1 : Level
{
    private const int _ = 0;
private readonly int[,] MiniMap =
{
    {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, 8, 8, 8, 8, _, _, _, 2, 2, 2, _, _, 1},
    {1, _, _, 12, _, _, 6, _, _, _, _, _, 2, _, _, 1},
    {1, _, _, 13, _, _, 7, _, _, _, _, _, 2, _, _, 1},
    {1, _, _, 11, _, _, 6, _, _, _, _, _, _, _, _, 1},
    {1, _, _, 9, 9, 9, 9, _, _, _, _, _, _, _, _, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, 4, _, _, _, 4, _, _, _, _, _, _, _, 1},
    {1, 1, 3, 1, 3, 1, 1, 1, 3, 3, _, _, 3, 1, 1, 1},
    {1, 1, 1, 1, 1, 1, 1, 1, 1, 3, _, _, 3, 1, 1, 1},
    {1, 1, 1, 1, 1, 1, 1, 1, 1, 3, _, _, 3, 1, 1, 1},
    {1, 1, 3, 1, 1, 1, 1, 1, 1, 3, _, _, 3, 1, 1, 1},
    {1, 4, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, 14, _, _, _, _, _, 3, 4, _, 4, 3, _, 1},
    {1, _, _, 15, _, _, _, _, _, _, 3, _, 3, _, _, 1},
    {1, _, _, 16, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
    {1, 4, _, _, _, _, _, _, 4, _, _, 4, _, _, _, 1},
    {1, 1, 3, 3, _, _, 3, 3, 1, 3, 3, 1, 3, 1, 1, 1},
    {1, 1, 1, 3, _, _, 3, 1, 1, 1, 1, 1, 1, 1, 1, 1},
    {1, 3, 3, 4, _, _, 4, 3, 3, 3, 3, 3, 3, 3, 3, 1},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, _, _, 5, _, _, _, 5, _, _, _, 5, _, _, _, 3},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 3},
    {3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3},
};

    public LimsaStage1(Bahamood game) : base(game, new Vector2(1.5f, 5.0f), 0.0f, @"Resources\Sound\Limsa.aac")
    {
        LevelName = "Limsa Stage 1";

        Map = new Map(MiniMap);
        ObjectHandler = new ObjectHandler();
        Pathfinder = new Pathfinder(this);

        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(10.5f, 3.5f)));
        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(5.5f, 4.75f)));
        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(7.5f, 5.5f)));
        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(9.5f, 7.5f)));

        ObjectHandler.AddNPC(new Miqo(Game, new Vector2(10.5f, 5.5f)));
        ObjectHandler.AddNPC(new Miqo(Game, new Vector2(8.5f, 5.5f)));
        ObjectHandler.AddNPC(new Miqo(Game, new Vector2(14.5f, 26.5f)));
        ObjectHandler.AddNPC(new SwordMonster(Game, new Vector2(5.5f, 15.5f)));
        ObjectHandler.AddNPC(new SwordMonster(Game, new Vector2(6.5f, 15.5f)));

        ObjectHandler.AddPickup(new CollectableRevolver(Game, new Vector2(4.5f, 4.5f)));

        ObjectHandler.AddPickup(new CollectableHealth(Game, new Vector2(3.5f, 3.5f)));
        ObjectHandler.AddPickup(new CollectableHealth(Game, new Vector2(14.5f, 1.5f)));
        ObjectHandler.AddPickup(new CollectableHealth(Game, new Vector2(10.5f, 10.5f)));
    }
}

public class LimsaStage2 : Level
{
    private const int _ = 0;
    private readonly int[,] MiniMap =
    {
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
        {1, _, _, 2, 2, 2, 2, _, _, _, 2, 5, 2, _, _, 1},
        {1, _, _, 2, 2, 2, 2, _, _, _, 2, 5, 2, _, _, 1},
        {1, _, _, 2, 2, 2, 2, _, _, _, 2, 5, 2, _, _, 1},
        {1, _, _, 2, 2, 2, 2, _, _, _, 2, 5, 2, _, _, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, _, 1, 1, 1, 1, 1, 1, 1},
        {1, _, _, _, _, _, _, _, _, 1, 1, 1, 1, 1, 1, 1},
        {1, _, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, _, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
        {1, _, 4, 4, _, _, _, _, _, _, _, 3, _, _, _, 1},
        {1, _, 4, 4, _, _, _, _, _, _, _, 3, _, _, _, 1},
        {1, _, 4, 4, _, _, _, _, _, 3, 3, 3, 3, 3, _, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 1},
        {1, _, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
        {1, _, _, _, _, _, _, _, _, _, _, _, _, _, _, 5},
        {1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1},
    };

    public LimsaStage2(Bahamood game) : base(game, new Vector2(1.5f, 1.5f), 0.0f, @"Resources\Sound\Limsa.aac")
    {
        LevelName = "Limsa Stage 2";

        Map = new Map(MiniMap);
        ObjectHandler = new ObjectHandler();
        Pathfinder = new Pathfinder(this);

        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(1.5f, 1.5f)));
        ObjectHandler.AddSprite(new FatCat(Game, new Vector2(1.5f, 6.5f)));

        ObjectHandler.AddNPC(new Miqo(Game, new Vector2(9.5f, 2.5f)));
        ObjectHandler.AddNPC(new SwordMonster(Game, new Vector2(14.5f, 18.5f)));
    }
}