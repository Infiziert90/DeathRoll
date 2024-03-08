using DeathRoll.Windows;

namespace DeathRoll.Bahamood;

public class Map
{
    private readonly int Rows;
    private readonly int Cols;

    public readonly int[,] MiniMap;
    public readonly Dictionary<(int, int), int> WorldMap = new();

    public Map(int[,] miniMap)
    {
        MiniMap = miniMap;
        Rows = MiniMap.GetLength(0);
        Cols = MiniMap.GetLength(1);

        BuildMap();
    }

    private void BuildMap()
    {
        foreach (var (j, i) in ForGenerator())
        {
            var val = MiniMap[j, i];
            if (val > 0)
                WorldMap[(i, j)] = val;
        }
    }

    public void Draw(float scaling = 10.0f)
    {
        var drawlist = ImGui.GetWindowDrawList();
        var p = ImGui.GetCursorScreenPos();

        foreach (var (x, y) in WorldMap.Keys)
        {
            var drawX = p.X + x * scaling;
            var drawY = p.Y + y * scaling;
            drawlist.AddRect(new Vector2(drawX, drawY), new Vector2(drawX + scaling, drawY + scaling), Helper.MapGrey);
        }
    }

    public IEnumerable<(int Row, int Col)> ForGenerator()
    {
        for (var row = 0; row < Rows; row++)
            for (var col = 0; col < Cols; col++)
                yield return (row, col);
    }
}