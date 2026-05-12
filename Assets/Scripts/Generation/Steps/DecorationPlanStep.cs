using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Планирование спавна декораций: деревья, фонари, скамейки.
    /// Заполняет SpawnPlan данными, не создавая Unity-объектов.
    /// </summary>
    public sealed class DecorationPlanStep : IWorldGenStep
    {
        public string Key => "Decorations";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var rng = seed.CreateRng(Key);

            if (world.Roads == null)
                throw new InvalidOperationException("DecorationPlanStep: world.Roads is null.");

            if (world.Ground == null)
                throw new InvalidOperationException("DecorationPlanStep: world.Ground is null.");

            var roads = world.Roads;
            var ground = world.Ground;
            int width = roads.Width;
            int height = roads.Height;

            Debug.Log($"[DecorationPlanStep] Start: mapSize={width}x{height}, BlocksCount={world.Blocks?.Count ?? 0}");

            // Считаем расстояние от дорог для каждой клетки
            var distanceFromRoad = ComputeDistanceFromRoad(roads);

            // Спавн деревьев (на всей карте, в пределах расстояния от дороги)
            int treeCount = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Проверяем, что это земля (Grass или Dirt)
                    TileType tile = ground.Get(x, y);
                    if (tile != TileType.Grass && tile != TileType.Dirt)
                        continue;

                    // Проверяем расстояние от дороги
                    int dist = distanceFromRoad[x, y];
                    if (dist < config.decorationMinDistanceFromRoad)
                        continue;

                    if (config.decorationMaxDistanceFromRoad > 0 &&
                        dist > config.decorationMaxDistanceFromRoad)
                        continue;

                    // Проверяем расстояние до зданий (используем minBuildingDistance для декораций)
                    if (!IsCellAwayFromBuildings(new Vector2Int(x, y), world, config.minBuildingDistance))
                        continue;

                    // Проверяем лимит
                    if (config.maxTreeCount > 0 && treeCount >= config.maxTreeCount)
                        break;

                    // Шанс спавна
                    if (rng.Next(0, 100) < config.treeSpawnChance)
                    {
                        int treePrefabId = SelectPrefabById(config.treePrefabIds, config.treePrefabWeights, rng);
                        world.SpawnPlan.Add(treePrefabId, new Vector2Int(x, y), 0);
                        treeCount++;
                    }
                }
            }
            Debug.Log($"[DecorationPlanStep] Trees spawned: {treeCount}");

            // Спавн фонарей вдоль дорог
            var lampPositions = SpawnLampsAlongRoads(roads, ground, config);
            // Фильтруем позиции, которые слишком близко к зданиям (используем minBuildingDistance)
            lampPositions.RemoveAll(pos => !IsCellAwayFromBuildings(pos, world, config.minBuildingDistance));
            foreach (var lampPos in lampPositions)
            {
                int lampPrefabId = SelectPrefabById(config.lampPrefabIds, config.lampPrefabWeights, rng);
                world.SpawnPlan.Add(lampPrefabId, lampPos, 0);
            }
            Debug.Log($"[DecorationPlanStep] Lamps spawned: {lampPositions.Count}");

            // Спавн скамеек внутри кварталов, вдоль дорог
            SpawnBenchesInBlocks(world, config, rng, world.SpawnPlan);
        }

        /// <summary>
        /// Спавнит скамейки внутри кварталов (блоков) ТОЛЬКО на земле (Grass/Dirt) рядом с дорогой.
        /// </summary>
        private void SpawnBenchesInBlocks(WorldData world, WorldGenConfig config,
            System.Random rng, SpawnPlan spawnPlan)
        {
            if (world.Blocks == null || world.Blocks.Count == 0)
            {
                Debug.LogWarning("[DecorationPlanStep] No blocks found for bench spawning.");
                return;
            }

            if (world.Roads == null || world.Ground == null)
            {
                Debug.LogWarning("[DecorationPlanStep] Roads or Ground layer is null.");
                return;
            }

            int benchCount = 0;
            int benchPrefabId = config.benchPrefabId;
            var roads = world.Roads;
            var ground = world.Ground;

            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var block in world.Blocks)
            {
                if (block.width < 3 || block.height < 3)
                {
                    Debug.Log($"[DecorationPlanStep] Block too small: {block}");
                    continue;
                }

                var benchCandidates = new List<Vector2Int>();

                // Перебираем все клетки внутри прямоугольника блока
                for (int x = block.xMin; x < block.xMax; x++)
                {
                    for (int y = block.yMin; y < block.yMax; y++)
                    {
                        // Проверяем, что это земля (Grass или Dirt)
                        TileType tile = ground.Get(x, y);
                        if (tile != TileType.Grass && tile != TileType.Dirt)
                            continue;

                        // Проверяем, есть ли среди соседей дорога
                        bool adjacentToRoad = false;
                        foreach (var dir in directions)
                        {
                            int nx = x + dir.x;
                            int ny = y + dir.y;

                            if (roads.InBounds(nx, ny) && roads.Get(nx, ny) == TileType.Road)
                            {
                                adjacentToRoad = true;
                                break;
                            }
                        }

                        if (adjacentToRoad)
                        {
                            Vector2Int cellPos = new Vector2Int(x, y);
                             // Проверяем, что клетка достаточно далеко от зданий
                             if (IsCellAwayFromBuildings(cellPos, world, config.minBuildingDistance))
                            {
                                benchCandidates.Add(cellPos);
                            }
                        }
                    }
                }

                Debug.Log($"[DecorationPlanStep] Block {block}: candidates={benchCandidates.Count}");

                if (benchCandidates.Count > 0)
                {
                    int randomIndex = rng.Next(0, benchCandidates.Count);
                    Vector2Int benchPos = benchCandidates[randomIndex];

                    spawnPlan.Add(benchPrefabId, benchPos, 0);
                    benchCount++;

                    Debug.Log($"[DecorationPlanStep] Bench spawned in block at {benchPos}");
                }
            }

            Debug.Log($"[DecorationPlanStep] Benches spawned total: {benchCount} of {world.Blocks.Count} blocks");
        }

        /// <summary>
        /// Находит позиции для фонарей вдоль дорог.
        /// </summary>
        private List<Vector2Int> SpawnLampsAlongRoads(TileLayer roads, TileLayer ground, WorldGenConfig config)
        {
            var lampPositions = new List<Vector2Int>();
            int width = roads.Width;
            int height = roads.Height;

            var allSegments = new List<(Vector2Int start, Vector2Int end, bool isHorizontal)>();

            // Горизонтальные отрезки
            for (int y = 0; y < height; y++)
            {
                int segmentStart = -1;
                for (int x = 0; x < width; x++)
                {
                    if (roads.Get(x, y) == TileType.Road)
                    {
                        if (segmentStart == -1)
                            segmentStart = x;
                    }
                    else
                    {
                        if (segmentStart != -1 && x - segmentStart >= config.minRoadSegmentLength)
                        {
                            allSegments.Add((new Vector2Int(segmentStart, y), new Vector2Int(x - 1, y), true));
                        }
                        segmentStart = -1;
                    }
                }
                if (segmentStart != -1 && width - segmentStart >= config.minRoadSegmentLength)
                {
                    allSegments.Add((new Vector2Int(segmentStart, y), new Vector2Int(width - 1, y), true));
                }
            }

            // Вертикальные отрезки
            for (int x = 0; x < width; x++)
            {
                int segmentStart = -1;
                for (int y = 0; y < height; y++)
                {
                    if (roads.Get(x, y) == TileType.Road)
                    {
                        if (segmentStart == -1)
                            segmentStart = y;
                    }
                    else
                    {
                        if (segmentStart != -1 && y - segmentStart >= config.minRoadSegmentLength)
                        {
                            allSegments.Add((new Vector2Int(x, segmentStart), new Vector2Int(x, y - 1), false));
                        }
                        segmentStart = -1;
                    }
                }
                if (segmentStart != -1 && height - segmentStart >= config.minRoadSegmentLength)
                {
                    allSegments.Add((new Vector2Int(x, segmentStart), new Vector2Int(x, height - 1), false));
                }
            }

            int segmentCount = 0;
            int totalSpawned = 0;
            foreach (var segment in allSegments)
            {
                int beforeCount = lampPositions.Count;
                SpawnLampsForSegment(roads, ground, segment.start, segment.end, segment.isHorizontal,
                    lampPositions, config.lampInterval, config.minRoadSegmentLength);
                int spawned = lampPositions.Count - beforeCount;
                totalSpawned += spawned;
                segmentCount++;
            }

            Debug.Log($"[DecorationPlanStep] Lamp segments: {segmentCount}, total lamps: {totalSpawned}");
            return lampPositions;
        }

        /// <summary>
        /// Спавнит фонари для отрезка дороги.
        /// </summary>
        private void SpawnLampsForSegment(TileLayer roads, TileLayer ground,
            Vector2Int start, Vector2Int end, bool isHorizontal,
            List<Vector2Int> lampPositions,
            int lampInterval, int minSegmentLength)
        {
            int length = isHorizontal ? (end.x - start.x + 1) : (end.y - start.y + 1);

            if (length < minSegmentLength)
                return;

            int halfInterval = lampInterval / 2;

            if (isHorizontal)
            {
                int startX = start.x + halfInterval;
                int endX = end.x - halfInterval;
                
                for (int x = startX; x <= endX; x += lampInterval)
                {
                    var cell = new Vector2Int(x, start.y);
                    SpawnLampPair(roads, ground, cell, lampPositions, new Vector2Int(0, 1));
                }
            }
            else
            {
                int startY = start.y + halfInterval;
                int endY = end.y - halfInterval;
                
                for (int y = startY; y <= endY; y += lampInterval)
                {
                    var cell = new Vector2Int(start.x, y);
                    SpawnLampPair(roads, ground, cell, lampPositions, new Vector2Int(1, 0));
                }
            }
        }

        /// <summary>
        /// Спавнит пару фонарей по обеим сторонам от клетки дороги.
        /// </summary>
        private void SpawnLampPair(TileLayer roads, TileLayer ground, Vector2Int roadCell,
            List<Vector2Int> lampPositions, Vector2Int perpendicular)
        {
            var pos1 = roadCell + perpendicular;
            var pos2 = roadCell - perpendicular;

            // Первый фонарь
            if (ground.InBounds(pos1.x, pos1.y) &&
                roads.Get(pos1) != TileType.Road &&
                !lampPositions.Contains(pos1))
            {
                lampPositions.Add(pos1);
            }

            // Второй фонарь
            if (ground.InBounds(pos2.x, pos2.y) &&
                roads.Get(pos2) != TileType.Road &&
                !lampPositions.Contains(pos2))
            {
                lampPositions.Add(pos2);
            }
        }

        /// <summary>
        /// Вычисляет расстояние каждой клетки от ближайшей дороги через BFS.
        /// </summary>
        private int[,] ComputeDistanceFromRoad(TileLayer roads)
        {
            int width = roads.Width;
            int height = roads.Height;
            var distance = new int[width, height];

            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distance[x, y] = int.MaxValue;

            var queue = new Queue<(int x, int y, int dist)>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (roads.Get(x, y) == TileType.Road)
                    {
                        distance[x, y] = 0;
                        queue.Enqueue((x, y, 0));
                    }
                }
            }

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var (x, y, dist) = queue.Dequeue();

                foreach (var dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (distance[nx, ny] > dist + 1)
                    {
                        distance[nx, ny] = dist + 1;
                        queue.Enqueue((nx, ny, dist + 1));
                    }
                }
            }

            return distance;
        }

        /// <summary>
        /// Проверяет, что клетка находится на расстоянии не менее minGap от всех зданий.
        /// </summary>
        private bool IsCellAwayFromBuildings(Vector2Int cellPos, WorldData world, int minGap)
        {
            foreach (var building in world.Buildings)
            {
                // Расширяем прямоугольник здания на minGap со всех сторон
                var expanded = new RectInt(
                    building.position.x - minGap,
                    building.position.y - minGap,
                    building.width + 2 * minGap,
                    building.height + 2 * minGap
                );

                if (expanded.Contains(cellPos))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Выбирает ID префаба на основе весов.
        /// </summary>
        private int SelectPrefabById(int[] prefabIds, int[] weights, System.Random rng)
        {
            if (prefabIds == null || prefabIds.Length == 0)
                return 1000;

            if (prefabIds.Length == 1)
                return prefabIds[0];

            if (weights == null || weights.Length != prefabIds.Length)
            {
                return prefabIds[rng.Next(0, prefabIds.Length)];
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            int randomValue = rng.Next(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < prefabIds.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                {
                    return prefabIds[i];
                }
            }

            return prefabIds[prefabIds.Length - 1];
        }
    }
}
