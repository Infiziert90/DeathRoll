namespace DeathRoll.Bahamood;

public class ObjectHandler
{
    private readonly List<NPC> NPCs = new();
    private readonly List<SpriteHandler> Sprites = new();
    private readonly List<Collectable> Pickup = new();

    public List<(int, int)> CurrentPositions = new();

    public void Update()
    {
        CurrentPositions = NPCs.Where(n => n.Alive).Select(n => n.MapPos).ToList();

        foreach (var sprite in Sprites)
            sprite.Update();

        foreach (var npc in NPCs)
            npc.Update();

        foreach (var pickup in Pickup)
            pickup.Update();
    }

    public void AddSprite(SpriteHandler spriteHandler)
    {
        Sprites.Add(spriteHandler);
    }

    public void AddNPC(NPC npc)
    {
        NPCs.Add(npc);
    }

    public void AddPickup(Collectable collectable)
    {
        Pickup.Add(collectable);
    }

    public bool AnyAlive => NPCs.Any(p => p.Alive);
}