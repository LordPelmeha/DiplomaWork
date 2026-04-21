using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Спавн игрока и выхода в мире.
    /// Работает только с данными, не создаёт Unity-объекты.
    /// </summary>
    public static class SpawnPointService
    {
        /// <summary>
        /// Находит точку спавна игрока (ближайшая к первому узлу графа).
        /// </summary>
        public static Vector2Int FindPlayerSpawn(WorldData world, SeedContext seed)
        {
            if (world.Graph == null || world.Graph.Nodes.Count == 0)
                return new Vector2Int(1, 1);

            var rng = seed.CreateRng("PlayerSpawn");

            // Начинаем от первого узла графа
            var startNode = world.Graph.Nodes[0];
            var startPos = startNode.position;

            // Ищем свободную клетку рядом с узлом
            var spawnPos = FindValidGroundCell(world, startPos, rng);
            return spawnPos;
        }

        /// <summary>
        /// Находит точку выхода (самая удалённая от спавна игрока).
        /// </summary>
        public static Vector2Int FindExitSpawn(WorldData world, Vector2Int playerSpawn, SeedContext seed)
        {
            if (world.Graph == null || world.Graph.Nodes.Count == 0)
                return new Vector2Int(world.Size.x - 2, world.Size.y - 2);

            var rng = seed.CreateRng("ExitSpawn");

            // Находим самый удалённый узел графа
            int farIdx = 0;
            float maxDistSqr = -1f;

            for (int i = 0; i < world.Graph.Nodes.Count; i++)
            {
                var nodePos = world.Graph.Nodes[i].position;
                var delta = nodePos - playerSpawn;
                float distSqr = delta.x * delta.x + delta.y * delta.y;

                if (distSqr > maxDistSqr)
                {
                    maxDistSqr = distSqr;
                    farIdx = i;
                }
            }

            var farNode = world.Graph.Nodes[farIdx];

            // Ищем свободную клетку рядом с удалённым узлом
            var exitPos = FindValidGroundCell(world, farNode.position, rng);
            return exitPos;
        }

        /// <summary>
        /// Находит свободную клетку земли рядом с заданной позицией.
        /// </summary>
        private static Vector2Int FindValidGroundCell(WorldData world, Vector2Int center, System.Random rng)
        {
            var ground = world.Ground;
            var roads = world.Roads;
            var walls = world.Walls;

            int searchRadius = 5;
            var candidates = new List<Vector2Int>();

            for (int dx = -searchRadius; dx <= searchRadius; dx++)
            {
                for (int dy = -searchRadius; dy <= searchRadius; dy++)
                {
                    int x = center.x + dx;
                    int y = center.y + dy;

                    if (!ground.InBounds(x, y))
                        continue;

                    // Проверяем, что это земля (не дорога, не стена)
                    if (ground.Get(x, y) == TileType.Grass &&
                        roads.Get(x, y) != TileType.Road &&
                        walls.Get(x, y) != TileType.Wall)
                    {
                        candidates.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (candidates.Count > 0)
            {
                return candidates[rng.Next(0, candidates.Count)];
            }

            // Если не нашли, возвращаем центр
            return center;
        }
    }
}
