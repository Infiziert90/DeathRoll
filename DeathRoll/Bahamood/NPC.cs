namespace DeathRoll.Bahamood;

public class NPC : AnimatedSpriteHandler
{
    public bool Alive = true;

    internal int Hitbox = 10;
    internal int Health = 100;
    internal float Speed = 0.015f;
    internal int AttackDistance = Random.Shared.Next(3, 6);
    internal int AttackDamage = 10;
    internal float Accuracy = 0.15f;

    internal bool DisableAI;

    private bool InPain;
    private bool RayValue;
    private bool PlayerSearch;

    private int DeathCounter;
    private int WalkCounter;
    private int AttackCounter;
    private int PainCounter;
    private int IdleCounter;

    private readonly TextureHandler.Sprite Sprites;

    protected NPC(Bahamood game, Vector2 pos, TextureHandler.Sprite sprites, float scale, float shift, int animationTime)
        : base(game, pos, sprites, scale, shift, animationTime)
    {
        Sprites = sprites;
    }

    public override void Update()
    {
        CheckAnimationTime();
        GetSprite();
        Logic();
    }

    private bool CheckWall(int x, int y)
    {
        return !Game.CurrentLevel!.Map.WorldMap.ContainsKey((x, y));
    }

    private void CheckWallCollision(double dx, double dy)
    {
        if (CheckWall((int)(Position.X + dx * Hitbox), (int)Position.Y))
            Position.X += (float) dx;
        if (CheckWall((int)Position.X, (int)(Position.Y + dy * Hitbox)))
            Position.Y += (float) dy;
    }

    private void Movement()
    {
        var nextPos = Game.CurrentLevel!.Pathfinder.GetPath(MapPos, Game.Player!.MapPos);
        var (nextX, nextY) = nextPos;

        if (!Game.CurrentLevel.ObjectHandler.CurrentPositions.Contains(nextPos))
        {
            var angle = Math.Atan2(nextY + 0.5 - Position.Y, nextX + 0.5 - Position.X);
            var dX = Math.Cos(angle) * Speed;
            var dY = Math.Sin(angle) * Speed;
            CheckWallCollision(dX, dY);
        }
    }

    private void Attack()
    {
        if (!Trigger)
            return;

        if (Random.Shared.NextSingle() < Accuracy)
            Game.Player!.GetDamage(AttackDamage);
    }

    private void AnimeDeath()
    {
        if (Alive)
            return;

        if (Trigger && DeathCounter < Sprites.DeathImages.Length - 1)
        {
            Image = Sprites.DeathImages[DeathCounter];

            DeathCounter++;
        }
    }

    private void AnimatePain()
    {
        Animate(Sprites.PainImages, ref PainCounter);
        if (Trigger)
            InPain = false;
    }

    private void CheckIfHit()
    {
        if (!RayValue || !Game.Player!.Shot)
            return;

        if (Settings.HalfWidth - SpriteHalfWidth < ScreenX && ScreenX < Settings.HalfWidth + SpriteHalfWidth)
        {
            Game.Player.Shot = false;

            InPain = true;
            Health -= Game.Player.CurrentWeapon.Damage;

            if (Health < 1)
                Alive = false;
        }
    }

    private void Logic()
    {
        if (DisableAI)
        {
            Animate(Sprites.IdleImages, ref PainCounter);
            return;
        }

        if (Alive)
        {
            RayValue = RayCast();

            CheckIfHit();
            if (InPain)
            {
                AnimatePain();
            }
            else if (RayValue)
            {
                PlayerSearch = true;

                if (Distance < AttackDistance)
                {
                    Animate(Sprites.AttackImages, ref AttackCounter);
                    Attack();
                }
                else
                {
                    Animate(Sprites.WalkImages, ref WalkCounter);
                    Movement();
                }
            }
            else if (PlayerSearch)
            {
                Animate(Sprites.WalkImages, ref WalkCounter);
                Movement();
            }
            else
            {
                Animate(Sprites.IdleImages, ref IdleCounter);
            }
        }
        else
        {
            AnimeDeath();
        }
    }

    public (int X, int Y) MapPos => ((int)Position.X, (int)Position.Y);

    private bool RayCast()
    {
        if (Game.Player!.MapPos == MapPos)
            return true;

        var (wallDistV, wallDistH) = (0.0, 0.0);
        var (playerDistV, playerDistH) = (0.0, 0.0);

        var (oX, oY) = Game.Player.Pos;
        var (mapX, mapY) = Game.Player.MapPos;

        var sinA = Math.Sin(ThetA);
        var cosA = Math.Cos(ThetA);

        // horizontals
        var (horY, dY) = sinA > 0 ? (mapY + 1, 1.0) : (mapY - 1e-6, -1.0);
        var depthHor = (horY - oY) / sinA;
        var horX = oX + depthHor * cosA;

        var deltaDepth = dY / sinA;
        var dX = deltaDepth * cosA;

        for (var j = 0; j < Settings.MaxDepth; j++)
        {
            var tileHor = ((int)horX, (int)horY);
            if (tileHor == MapPos)
            {
                playerDistH = depthHor;
                break;
            }

            if (Game.CurrentLevel!.Map.WorldMap.ContainsKey(tileHor))
            {
                wallDistH = depthHor;
                break;
            }

            horX += dX;
            horY += dY;
            depthHor += deltaDepth;
        }

        // verticals
        (var vertX, dX) = cosA > 0 ? (mapX + 1, 1.0) : (mapX - 1e-6, -1.0);
        var depthVert = (vertX - oX) / cosA;
        var vertY = oY + depthVert * sinA;

        deltaDepth = dX / cosA;
        dY = deltaDepth * sinA;

        for (var j = 0; j < Settings.MaxDepth; j++)
        {
            var tileVert = ((int) vertX, (int) vertY);
            if (tileVert == MapPos)
            {
                playerDistV = depthVert;
                break;
            }

            if (Game.CurrentLevel!.Map.WorldMap.ContainsKey(tileVert))
            {
                wallDistV = depthVert;
                break;
            }

            vertX += dX;
            vertY += dY;
            depthVert += deltaDepth;
        }

        var playerDist = Math.Max(playerDistV, playerDistH);
        var wallDist = Math.Max(wallDistV, wallDistH);

        return 0 < playerDist && playerDist < wallDist || wallDist == 0.0;
    }
}

public class Miqo : NPC
{
    public Miqo(Bahamood game, Vector2 pos, float scale = 0.8f, float shift = 0.2f, int animationTime = 100, bool disabled = false)
        : base(game, pos, game.SpriteManager.Bunny, scale, shift, animationTime)
    {
        AttackDistance = 3;
        Health = 50;
        AttackDamage = 10;
        Speed = 0.01f;
        Accuracy = 0.25f;

        DisableAI = disabled;
        Hitbox = 20;
    }
}

public class SwordMonster : NPC
{
    public SwordMonster(Bahamood game, Vector2 pos, float scale = 1.2f, float shift = 0f, int animationTime = 50, bool disabled = false)
        : base(game, pos, game.SpriteManager.SwordMonster, scale, shift, animationTime)
    {
        AttackDistance = 2;
        Health = 50;
        AttackDamage = 25;
        Speed = 0.03f;
        Accuracy = 0.35f;

        DisableAI = disabled;
        Hitbox = 20;
    }
}