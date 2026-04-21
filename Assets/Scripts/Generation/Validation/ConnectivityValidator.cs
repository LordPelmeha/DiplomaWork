using System.Collections.Generic;
using UnityEngine;
using Diploma.Generation.Model;

namespace Diploma.Generation.Validation
{
    /// <summary>
    /// Проверка связности дорожной сети через BFS.
    /// </summary>
    public static class ConnectivityValidator
    {
        /// <summary>
        /// Проверяет, что все тайлы дорог соединены в одну компоненту связности.
        /// </summary>
        public static void ValidateConnectivity(WorldData world, ValidationResult result)
        {
            if (world.Roads == null)
            {
                result.AddError("Roads layer is null, cannot validate connectivity");
                return;
            }

            var roads = world.Roads;
            int width = roads.Width;
            int height = roads.Height;

            // Находим первый тайл дороги
            Vector2Int? startPos = null;
            int totalRoadTiles = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (roads.Get(x, y) == TileType.Road)
                    {
                        totalRoadTiles++;
                        if (!startPos.HasValue)
                        {
                            startPos = new Vector2Int(x, y);
                        }
                    }
                }
            }

            // Если дорог нет — это не ошибка, просто предупреждение
            if (totalRoadTiles == 0)
            {
                result.AddWarning("No road tiles found in the world");
                return;
            }

            // BFS от первого тайла дороги
            var visited = new bool[width, height];
            int visitedRoadTiles = 0;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startPos.Value);
            visited[startPos.Value.x, startPos.Value.y] = true;

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                visitedRoadTiles++;

                foreach (var dir in directions)
                {
                    int nx = current.x + dir.x;
                    int ny = current.y + dir.y;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visited[nx, ny])
                        continue;

                    if (roads.Get(nx, ny) == TileType.Road)
                    {
                        visited[nx, ny] = true;
                        queue.Enqueue(new Vector2Int(nx, ny));
                    }
                }
            }

            // Проверяем, все ли тайлы дорог посещены
            if (visitedRoadTiles != totalRoadTiles)
            {
                int disconnectedCount = totalRoadTiles - visitedRoadTiles;
                result.AddError(
                    $"Road network is disconnected: {disconnectedCount} road tiles " +
                    $"are not reachable from the main network " +
                    $"(visited {visitedRoadTiles} of {totalRoadTiles})");
            }
        }

        /// <summary>
        /// Проверяет, что все узлы графа достижимы из первого узла.
        /// </summary>
        public static void ValidateGraphConnectivity(DistrictGraph graph, ValidationResult result)
        {
            if (graph.Nodes.Count == 0)
                return;

            // Строим список смежности
            var adjacency = new List<int>[graph.Nodes.Count];
            for (int i = 0; i < graph.Nodes.Count; i++)
            {
                adjacency[i] = new List<int>();
            }

            foreach (var edge in graph.Edges)
            {
                adjacency[edge.a].Add(edge.b);
                adjacency[edge.b].Add(edge.a);
            }

            // BFS от узла 0
            var visited = new bool[graph.Nodes.Count];
            var queue = new Queue<int>();
            queue.Enqueue(0);
            visited[0] = true;
            int visitedCount = 1;

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (var neighbor in adjacency[current])
                {
                    if (!visited[neighbor])
                    {
                        visited[neighbor] = true;
                        visitedCount++;
                        queue.Enqueue(neighbor);
                    }
                }
            }

            if (visitedCount != graph.Nodes.Count)
            {
                int isolatedCount = graph.Nodes.Count - visitedCount;
                result.AddError(
                    $"District graph is disconnected: {isolatedCount} nodes " +
                    $"are not reachable from node 0");
            }
        }
    }
}
