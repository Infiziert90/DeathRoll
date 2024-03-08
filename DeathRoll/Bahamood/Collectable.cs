using System.Diagnostics.CodeAnalysis;

namespace DeathRoll.Bahamood;

public class Collectable : SpriteHandler
{
    private bool PickedUp;
    public required Action PickupAction;

    protected Collectable(Bahamood game, Vector2 pos, TextureHandler.Sprite sprites, float scale, float shift) : base(game, pos, sprites, scale, shift)
    {

    }

    private bool CheckIfWalkedOver()
    {
        return Vector2.Distance(Position, Game.Player.Position) < 0.5;
    }

    public override void Update()
    {
        if (PickedUp)
            return;

        if (CheckIfWalkedOver())
        {
            PickedUp = true;
            PickupAction.Invoke();
        }

        base.Update();
    }
}

public class CollectableRevolver : Collectable
{
    [SetsRequiredMembers]
    public CollectableRevolver(Bahamood game, Vector2 pos, float scale = 0.5f, float shift = 0.67f)
        : base(game, pos, game.SpriteManager.CollectableRevolver, scale, shift)
    {
        PickupAction = () => Game.Player.OtherWeapons.Enqueue(new Revolver(Game));
    }
}

public class CollectableHealth : Collectable
{
    [SetsRequiredMembers]
    public CollectableHealth(Bahamood game, Vector2 pos, float scale = 0.4f, float shift = 0.9f)
        : base(game, pos, game.SpriteManager.CollectableHealth, scale, shift)
    {
        PickupAction = () => Game.Player.Health += 25;
    }
}