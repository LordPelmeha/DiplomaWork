using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class GraphGenerator
{
    public static RoomGraph Generate(DungeonSettings settings)
    {
        var rnd = new System.Random(settings.seed);
        int margin = settings.roomMaxSize;
        int wLimit = settings.mapWidth - margin;
        int hLimit = settings.mapHeight - margin;

        float minDist = settings.roomMaxSize;
        int maxAttemptsPerNode = 100;

        var nodes = new List<Vector2Int>();
        bool placed = false;
        while (!placed)
        {
            while (nodes.Count < settings.roomCount)
            {

                for (int attempt = 0; attempt < maxAttemptsPerNode; attempt++)
                {
                    int x = rnd.Next(margin, wLimit);
                    int y = rnd.Next(margin, hLimit);
                    var candidate = new Vector2Int(x, y);

                    bool tooClose = nodes.Any(p => (p - candidate).sqrMagnitude < minDist * minDist);
                    if (!tooClose)
                    {
                        nodes.Add(candidate);
                        placed = true;
                        break;
                    }
                }
            }
            if (!placed) minDist -= 1;
        }

        var edges = new HashSet<Edge>();
        var used = new HashSet<int> { 0 };
        var unused = new HashSet<int>(Enumerable.Range(1, nodes.Count - 1));

        while (unused.Count > 0)
        {
            float bestDist = float.MaxValue;
            int bestU = -1, bestV = -1;
            foreach (int u in used)
                foreach (int v in unused)
                {
                    var diff = nodes[u] - nodes[v];
                    float d = diff.x * diff.x + diff.y * diff.y;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestU = u; bestV = v;
                    }
                }
            edges.Add(new Edge(bestU, bestV));
            used.Add(bestV);
            unused.Remove(bestV);
        }

        for (int i = 0; i < nodes.Count; i++)
        {
            var nearest = Enumerable.Range(0, nodes.Count)
                .Where(j => j != i)
                .OrderBy(j =>
                {
                    var diff = nodes[i] - nodes[j];
                    return diff.x * diff.x + diff.y * diff.y;
                })
                .Take(settings.neighborCount);

            foreach (int j in nearest)
                edges.Add(new Edge(i, j));
        }
        return new RoomGraph(nodes, edges.ToList());
    }
}
