namespace DeathRoll.Bahamood;

public class Pathfinder
{
    private readonly Level Level;

    private readonly (int X, int Y)[] Ways = { (-1, 0), (0, -1), (1, 0), (0, 1), (-1, -1), (1, -1), (1, 1), (-1, 1) };
    private Dictionary<(int, int), List<(int, int)>> Graph = new();
    private Dictionary<(int, int), (int, int)?> Visited = new();

    public Pathfinder(Level level)
    {
        Level = level;
        GetGraph();
    }

    public (int, int) GetPath((int, int) start, (int, int) goal)
    {
        Visited = BFS(start, goal, Graph);
        var path = new List<(int, int)> { goal };
        var step = Visited.GetValueOrDefault(goal, start);

        while (step != null && step != start)
        {
            path.Add(step.Value);
            step = Visited[step.Value];
        }

        return path[^1];
    }

    private Dictionary<(int, int), (int, int)?> BFS((int, int) start, (int, int) goal, Dictionary<(int, int), List<(int, int)>> graph)
    {
        var queue = new Queue<(int, int)>();
        queue.Enqueue(start);
        var visited = new Dictionary<(int, int), (int, int)?>();
        visited[start] = null;

        while (queue.Count > 0)
        {
            var curNode = queue.Dequeue();
            if (curNode == goal)
                break;

            var nextNodes = graph[curNode];
            foreach (var nextNode in nextNodes)
            {
                if (!visited.ContainsKey(nextNode) && !Level.ObjectHandler.CurrentPositions.Contains(nextNode))
                {
                    visited[nextNode] = curNode;
                    queue.Enqueue(nextNode);
                }
            }
        }

        return visited;
    }

    private List<(int, int)> GetNextNodes(int x, int y)
    {
        var l = new List<(int, int)>();
        foreach (var (dX, dY) in Ways.Where(d =>!Level.Map.WorldMap.ContainsKey((x + d.X, y + d.Y))))
            l.Add((x + dX, y + dY));

        return l;
    }

    private void GetGraph()
    {
        foreach (var (y, x) in Level.Map.ForGenerator())
        {
            if (Level.Map.MiniMap[y, x] == 0)
            {
                var l = Graph.GetValueOrDefault((x, y), new List<(int, int)>());
                l.AddRange(GetNextNodes(x, y));

                Graph[(x, y)] = l;
            }
        }
    }

}