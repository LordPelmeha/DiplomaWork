using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Планирование спавна декораций: деревья, фонари.
    /// Заполняет SpawnPlan данными, не создавая Unity-объекты.
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

            // Считаем расстояние от дорог для каждой клетки
            var distanceFromRoad = ComputeDistanceFromRoad(roads);

            // Спавн деревьев
            int treeCount = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Проверяем, что это трава
                    if (ground.Get(x, y) != TileType.Grass)
                        continue;

                    // Проверяем расстояние от дороги
                    int dist = distanceFromRoad[x, y];
                    if (dist < config.decorationMinDistanceFromRoad)
                        continue;

                    if (config.decorationMaxDistanceFromRoad > 0 &&
                        dist > config.decorationMaxDistanceFromRoad)
                        continue;

                    // Проверяем лимит
                    if (config.maxTreeCount > 0 && treeCount >= config.maxTreeCount)
                        break;

                    // Шанс спавна
                    if (rng.Next(0, 100) < config.treeSpawnChance)
                    {
                        // Выбираем случайное дерево из списка
                        int treePrefabId = SelectPrefabById(config.treePrefabIds, config.treePrefabWeights, rng);
                        world.SpawnPlan.Add(treePrefabId, new Vector2Int(x, y), 0);
                        treeCount++;
                    }
                }
            }

            // Спавн фонарей (вдоль дорог, с фиксированным интервалом)
            var lampPositions = SpawnLampsAlongRoads(roads, ground, config);    

            foreach (var lampPos in lampPositions)
            {
                int lampPrefabId = SelectPrefabById(config.lampPrefabIds, config.lampPrefabWeights, rng);
                world.SpawnPlan.Add(lampPrefabId, lampPos, 0);
            }

            // Спавн скамеек (внутри участков, ограждённых дорогами)
            SpawnBenchesInEnclosedAreas(roads, ground, config, rng, world.SpawnPlan);
        }

        /// <summary>
        /// Находит позиции для фонарей вдоль дорог.
        /// Фонари спавняются алгоритмически с фиксированным интервалом.
        /// </summary>
        private List<Vector2Int> SpawnLampsAlongRoads(TileLayer roads, TileLayer ground, WorldGenConfig config)
        {
            var lampPositions = new List<Vector2Int>();
            int width = roads.Width;
            int height = roads.Height;

            // 1. Собираем ВСЕ отрезки дорог
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
                // Завершаем отрезок в конце строки
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
                // Завершаем отрезок в конце столбца
                if (segmentStart != -1 && height - segmentStart >= config.minRoadSegmentLength)
                {
                    allSegments.Add((new Vector2Int(x, segmentStart), new Vector2Int(x, height - 1), false));
                }
            }

            // 2. Спавним фонари для каждого отрезка
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
                
                if (segmentCount <= 3)
                {
                    Debug.Log($"[DecorationPlanStep] Segment {segmentCount}: start={segment.start}, end={segment.end}, horizontal={segment.isHorizontal}, spawned={spawned}");
                }
            }
            
            Debug.Log($"[DecorationPlanStep] Total segments: {segmentCount}");
            Debug.Log($"[DecorationPlanStep] Total lamps spawned: {totalSpawned}");
            
            // Временная отладка - покажем первые 10 позиций
            int showCount = Mathf.Min(10, lampPositions.Count);
            for (int i = 0; i < showCount; i++)
            {
                Debug.Log($"[DecorationPlanStep] Lamp[{i}] = {lampPositions[i]}");
            }

            return lampPositions;
        }

        /// <summary>
        /// Спавнит фонари для отрезка дороги.
        /// Фонари спавняются алгоритмически с фиксированным интервалом.
        /// </summary>
        private void SpawnLampsForSegment(TileLayer roads, TileLayer ground,
            Vector2Int start, Vector2Int end, bool isHorizontal,
            List<Vector2Int> lampPositions,
            int lampInterval, int minSegmentLength)
        {
            int length = isHorizontal ? (end.x - start.x + 1) : (end.y - start.y + 1);

            // Минимальная длина для спавна фонарей
            if (length < minSegmentLength)
                return;

            // Спавним фонари с фиксированным интервалом
            // Начинаем с половины интервала от начала, чтобы фонари были красиво распределены
            int halfInterval = lampInterval / 2;
            int spawnedInSegment = 0;

            if (isHorizontal)
            {
                // Горизонтальный отрезок
                int startX = start.x + halfInterval;
                int endX = end.x - halfInterval;
                
                for (int x = startX; x <= endX; x += lampInterval)
                {
                    var cell = new Vector2Int(x, start.y);

                    // Спавним пару фонарей сверху и снизу
                    SpawnLampPair(roads, ground, cell, lampPositions, new Vector2Int(0, 1));
                    spawnedInSegment++;
                }
            }
            else
            {
                // Вертикальный отрезок
                int startY = start.y + halfInterval;
                int endY = end.y - halfInterval;
                
                for (int y = startY; y <= endY; y += lampInterval)
                {
                    var cell = new Vector2Int(start.x, y);

                    // Спавним пару фонарей слева и справа
                    SpawnLampPair(roads, ground, cell, lampPositions, new Vector2Int(1, 0));
                    spawnedInSegment++;
                }
            }
        }

        /// <summary>
        /// Спавнит пару фонарей по обеим сторонам от клетки дороги.
        /// </summary>
        private void SpawnLampPair(TileLayer roads, TileLayer ground, Vector2Int roadCell,
            List<Vector2Int> lampPositions, Vector2Int perpendicular)
        {
            // Позиции для фонарей по обеим сторонам
            var pos1 = roadCell + perpendicular;
            var pos2 = roadCell - perpendicular;

            // Отладка - проверяем, что в этих клетках
            // Debug.Log($"[SpawnLampPair] roadCell={roadCell}, pos1={pos1}, pos2={pos2}");
            // Debug.Log($"[SpawnLampPair] ground[pos1]={ground.Get(pos1)}, roads[pos1]={roads.Get(pos1)}");
            // Debug.Log($"[SpawnLampPair] ground[pos2]={ground.Get(pos2)}, roads[pos2]={roads.Get(pos2)}");

            // Проверяем первую позицию - должна быть НЕ дорогой
            if (ground.InBounds(pos1.x, pos1.y) &&
                roads.Get(pos1) != TileType.Road &&
                !lampPositions.Contains(pos1))
            {
                lampPositions.Add(pos1);
            }

            // Проверяем вторую позицию - должна быть НЕ дорогой
            if (ground.InBounds(pos2.x, pos2.y) &&
                roads.Get(pos2) != TileType.Road &&
                !lampPositions.Contains(pos2))
            {
                lampPositions.Add(pos2);
            }
        }

        /// <summary>
        /// Спавнит скамейки внутри участков, ограждённых дорогами.
        /// 1 скамейка на отрезок дороги, внутри "кольца".
        /// </summary>
        private void SpawnBenchesInEnclosedAreas(TileLayer roads, TileLayer ground, 
            WorldGenConfig config, System.Random rng, SpawnPlan spawnPlan)
        {
            int width = roads.Width;
            int height = roads.Height;
            
            // Находим все "внутренние" участки (enclosed areas)
            var enclosedAreas = FindEnclosedAreas(roads, ground);
            
            Debug.Log($"[DecorationPlanStep] Found {enclosedAreas.Count} enclosed areas");

            // Для каждого участка находим ближайший отрезок дороги и спавним скамейку
            int benchCount = 0;
            int benchPrefabId = config.benchPrefabId; // ID скамейки из конфига

            foreach (var area in enclosedAreas)
            {
                // Находим центр участка
                Vector2Int center = FindAreaCenter(area);
                
                // Находим ближайшую дорогу к этому участку
                Vector2Int? nearestRoad = FindNearestRoad(roads, area);
                
                if (nearestRoad.HasValue)
                {
                    // Спавним скамейку на траве рядом с дорогой
                    var benchPos = FindBenchPosition(roads, ground, nearestRoad.Value, rng);
                    
                    if (benchPos != null)
                    {
                        spawnPlan.Add(benchPrefabId, benchPos.Value, 0);
                        benchCount++;
                        Debug.Log($"[DecorationPlanStep] Bench spawned at {benchPos.Value}");
                    }
                }
            }

            Debug.Log($"[DecorationPlanStep] Spawned {benchCount} benches");
        }

        /// <summary>
        /// Находит все замкнутые участки (enclosed areas) на карте.
        /// </summary>
        private List<List<Vector2Int>> FindEnclosedAreas(TileLayer roads, TileLayer ground)
        {
            int width = roads.Width;
            int height = roads.Height;
            var visited = new bool[width, height];
            var enclosedAreas = new List<List<Vector2Int>>();

            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            // Проходим по всем клеткам травы
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (ground.Get(x, y) == TileType.Grass && !visited[x, y])
                    {
                        // BFS для нахождения связной области травы
                        var area = new List<Vector2Int>();
                        var queue = new Queue<Vector2Int>();
                        queue.Enqueue(new Vector2Int(x, y));
                        visited[x, y] = true;
                        bool touchesMapEdge = false;

                        while (queue.Count > 0)
                        {
                            var cell = queue.Dequeue();
                            area.Add(cell);

                            foreach (var dir in directions)
                            {
                                int nx = cell.x + dir.x;
                                int ny = cell.y + dir.y;

                                if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                                {
                                    // Достигли границы карты
                                    touchesMapEdge = true;
                                    continue;
                                }

                                if (visited[nx, ny])
                                    continue;

                                // Если дорога - это граница участка (не проходим)
                                if (roads.Get(nx, ny) == TileType.Road)
                                    continue;

                                // Если трава - добавляем в область
                                if (ground.Get(nx, ny) == TileType.Grass)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }

                        // Добавляем только замкнутые области (не касающиеся края карты)
                        if (!touchesMapEdge && area.Count >= 4)
                        {
                            enclosedAreas.Add(area);
                        }
                    }
                }
            }

            return enclosedAreas;
        }

        /// <summary>
        /// Находит центр участка.
        /// </summary>
        private Vector2Int FindAreaCenter(List<Vector2Int> area)
        {
            int sumX = 0, sumY = 0;
            foreach (var cell in area)
            {
                sumX += cell.x;
                sumY += cell.y;
            }
            return new Vector2Int(sumX / area.Count, sumY / area.Count);
        }

        /// <summary>
        /// Находит ближайшую дорогу к участку.
        /// </summary>
        private Vector2Int? FindNearestRoad(TileLayer roads, List<Vector2Int> area)
        {
            int minDist = int.MaxValue;
            Vector2Int? nearestRoad = null;
            Vector2Int center = FindAreaCenter(area);

            // Ищем ближайшую дорогу к любой клетке участка
            foreach (var cell in area)
            {
                Vector2Int[] directions = {
                    Vector2Int.up, Vector2Int.down,
                    Vector2Int.left, Vector2Int.right
                };

                foreach (var dir in directions)
                {
                    int nx = cell.x + dir.x;
                    int ny = cell.y + dir.y;

                    if (roads.InBounds(nx, ny) && roads.Get(nx, ny) == TileType.Road)
                    {
                        int dist = Mathf.Abs(nx - center.x) + Mathf.Abs(ny - center.y);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            nearestRoad = new Vector2Int(nx, ny);
                        }
                    }
                }
            }

            return nearestRoad;
        }

        /// <summary>
        /// Находит позицию для скамейки рядом с дорогой.
        /// </summary>
        private Vector2Int? FindBenchPosition(TileLayer roads, TileLayer ground, 
            Vector2Int roadCell, System.Random rng)
        {
            // Ищем клетки травы рядом с дорогой
            var candidates = new List<Vector2Int>();

            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                int nx = roadCell.x + dir.x;
                int ny = roadCell.y + dir.y;

                if (ground.InBounds(nx, ny) && 
                    ground.Get(nx, ny) == TileType.Grass &&
                    roads.Get(nx, ny) != TileType.Road)
                {
                    candidates.Add(new Vector2Int(nx, ny));
                }
            }

            if (candidates.Count > 0)
            {
                // Выбираем случайную позицию из подходящих
                var chosen = candidates[rng.Next(0, candidates.Count)];
                Debug.Log($"[FindBenchPosition] Found {candidates.Count} candidates, chose {chosen}");
                return chosen;
            }

            Debug.LogWarning($"[FindBenchPosition] No candidates found near {roadCell}");
            return null;
        }

        /// <summary>
        /// Вычисляет расстояние каждой клетки от ближайшей дороги через BFS.
        /// </summary>
        private int[,] ComputeDistanceFromRoad(TileLayer roads)
        {
            int width = roads.Width;
            int height = roads.Height;
            var distance = new int[width, height];

            // Инициализируем максимальным значением
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    distance[x, y] = int.MaxValue;

            // BFS от всех дорог
            var queue = new Queue<(int x, int y, int dist)>();

            // Добавляем все дороги в очередь
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
        /// Выбирает ID префаба на основе весов.
        /// </summary>
        private int SelectPrefabById(int[] prefabIds, int[] weights, System.Random rng)
        {
            if (prefabIds == null || prefabIds.Length == 0)
                return 1000; // Default

            if (prefabIds.Length == 1)
                return prefabIds[0];

            // Если веса не заданы или не совпадают по длине, используем равномерное распределение
            if (weights == null || weights.Length != prefabIds.Length)
            {
                return prefabIds[rng.Next(0, prefabIds.Length)];
            }

            // Выбираем по весам
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
