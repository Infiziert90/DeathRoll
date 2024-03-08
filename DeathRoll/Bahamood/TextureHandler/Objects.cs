namespace DeathRoll.Bahamood.TextureHandler;

// NPC
public class BunnySprite : Sprite
{
    public BunnySprite() : base(@"NPC\Bunny\0.png", SpriteType.NPC)
    {

    }
}

public class SwordMonsterSprite : Sprite
{
    public SwordMonsterSprite() : base(@"NPC\SwordMonster\0.png", SpriteType.NPC)
    {

    }
}

// Animated Sprites
public class FatCatSprite : Sprite
{
    public FatCatSprite() : base(@"Animated\FatCat\0.png", SpriteType.AnimatedSprite)
    {

    }
}

// Weapons
public class ShotgunSprite : Sprite
{
    public ShotgunSprite() : base(@"Weapon\Shotgun\0.png", SpriteType.Weapon)
    {

    }
}

public class RevolverSprite : Sprite
{
    public RevolverSprite() : base(@"Weapon\Revolver\0.png", SpriteType.Weapon)
    {

    }
}

// Collectables
public class CollectableRevolverSprite : Sprite
{
    public CollectableRevolverSprite() : base(@"Pickup\Revolver.png", SpriteType.Collectable)
    {

    }
}

public class CollectableHealthSprite : Sprite
{
    public CollectableHealthSprite() : base(@"Pickup\Health.png", SpriteType.Collectable)
    {

    }
}