using DeathRoll.Windows;

namespace DeathRoll.Bahamood;

public record Result(float Depth, float ProjHeight, int Texture, float Offset, bool VertHit, (int X, int Y) Coords);

public class Raycasting
{
    private readonly Bahamood Game;
    private readonly List<Result> Results = new();

    public readonly List<RenderObject> ObjectsToRender = new();

    public Raycasting(Bahamood game)
    {
        Game = game;
    }

    private void GetObjectsToRender()
    {
        ObjectsToRender.Clear();
        foreach (var ((depth, projHeight, texture, offset, vertHit, coords), ray) in Results.Select((val, i) => (val, i)))
        {
            var wallCollection = Game.Renderer.LoadedTextures[texture];

            var textures = wallCollection.Simple;
            if (wallCollection.UseDirection)
            {
                var diff = (coords.X - Game.Player.MapPos.X, coords.Y - Game.Player.MapPos.Y);
                textures = wallCollection.GetDirection(diff, vertHit);
            }

            var size = new Vector2(Settings.Scale, projHeight);
            var wallPos = (ray * Settings.Scale, Settings.HalfHeight - (int)Math.Floor(projHeight / 2));

            var uvTop = new Vector2(offset, 0);
            var uvBottom = new Vector2(offset + Settings.Scale / (float) textures[0].Width + 0.00001f, 1);

            ObjectsToRender.Add(new RenderObject(depth, textures, size, wallPos, uvTop, uvBottom));
        }
    }

    private void RayCast()
    {
        Results.Clear();

        var (oX, oY) = Game.Player!.Pos;
        var (mapX, mapY) = Game.Player.MapPos;

        var rayAngle = Game.Player.Angle - Settings.HalfFoV + 0.0001;
        for (var ray = 0; ray < Settings.NumRays; ray++)
        {
            var sinA = Math.Sin(rayAngle);
            var cosA = Math.Cos(rayAngle);

            // horizontals
            var (horY, dY) = sinA > 0 ? (mapY + 1, 1.0) : (mapY - 1e-6, -1.0);
            var depthHor = (horY - oY) / sinA;
            var horX = oX + depthHor * cosA;

            var deltaDepth = dY / sinA;
            var dX = deltaDepth * cosA;

            var textureHor = 1;
            for (var j = 0; j < Settings.MaxDepth; j++)
            {
                if (Game.CurrentLevel!.Map.WorldMap.TryGetValue(((int) horX, (int) horY), out var value))
                {
                    textureHor = value;
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

            var textureVert = 1;
            for (var j = 0; j < Settings.MaxDepth; j++)
            {
                if (Game.CurrentLevel!.Map.WorldMap.TryGetValue(((int) vertX, (int) vertY), value: out var value))
                {
                    textureVert = value;
                    break;
                }

                vertX += dX;
                vertY += dY;
                depthVert += deltaDepth;
            }

            int texture;
            double depth;
            double offset;
            bool vertHit;
            (int X, int Y) coords;
            if (depthVert < depthHor)
            {
                depth = depthVert;
                texture = textureVert;
                vertHit = true;
                coords = ((int) vertX, (int) vertY);

                vertY = Utils.Mod(vertY, 1);
                offset = cosA > 0 ? vertY : 1 - vertY;
            }
            else
            {
                depth = depthHor;
                texture = textureHor;
                vertHit = false;
                coords = ((int) horX, (int) horY);

                horX = Utils.Mod(horX, 1);
                offset = sinA > 0 ? 1 - horX : horX;
            }

            // remove fish ball effect
            depth *= Math.Cos(Game.Player.Angle - rayAngle);

            // projection
            var projHeight = Settings.ScreenDist / (depth + 0.0001);
            Results.Add(new Result((float) depth, (float) projHeight, texture, (float) offset, vertHit, coords));

            rayAngle += Settings.DeltaAngle;
            rayAngle = (float) Utils.Mod(rayAngle, Math.Tau);
        }
    }

    public void RayCastLines()
    {
        var (oX, oY) = Game.Player!.Pos;
        var (mapX, mapY) = Game.Player.MapPos;

        // Imgui stuff
        var drawlist = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();

        var rayAngle = Game.Player.Angle - Settings.HalfFoV + 0.0001;
        for (var ray = 0; ray < Settings.NumRays; ray++)
        {
            var sinA = Math.Sin(rayAngle);
            var cosA = Math.Cos(rayAngle);

            // horizontals
            var (horY, dY) = sinA > 0 ? (mapY + 1, 1.0) : (mapY - 1e-6, -1.0);
            var depthHor = (horY - oY) / sinA;
            var horX = oX + depthHor * cosA;

            var deltaDepth = dY / sinA;
            var dX = deltaDepth * cosA;

            for (var j = 0; j < Settings.MaxDepth; j++)
            {
                var tileHor = ((int)horX, (int)horY);
                if (Game.CurrentLevel!.Map.WorldMap.ContainsKey(tileHor))
                    break;

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
                var tileVert = ((int)vertX, (int)vertY);
                if (Game.CurrentLevel!.Map.WorldMap.ContainsKey(tileVert))
                    break;
                vertX += dX;
                vertY += dY;
                depthVert += deltaDepth;
            }

            var depth = depthVert < depthHor ? depthVert : depthHor;

            var drawX = p.X + oX * 10.0f;
            var drawY = p.Y + oY * 10.0f;
            drawlist.AddLine(new Vector2(drawX, drawY), new Vector2((float) (drawX + 10 * depth * cosA), (float) (drawY + 10 * depth * sinA)), Helper.RaycastWhite);

            rayAngle += Settings.DeltaAngle;
        }
    }

    public void Update()
    {
        RayCast();
        GetObjectsToRender();
    }
}