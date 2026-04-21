using System.Collections.Generic;
using System.Linq;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Validation;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Детерминированный "ремонт" мира после генерации.
    /// Исправляет проблемы со связностью дорог и другие критические ошибки.
    /// </summary>
    public sealed class RepairStep : IWorldGenStep
    {
        public string Key => "Repair";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var rng = seed.CreateRng(Key);
            var validationResult = WorldValidator.Validate(world);

            if (validationResult.IsValid)
                return;

            // Игнорируем ошибки графа, если он не требуется (только дороги)
            bool hasRoads = world.Roads != null;
            bool hasGraphErrors = validationResult.Errors.Any(e => e.Contains("graph", System.StringComparison.OrdinalIgnoreCase));
            
            if (hasRoads && hasGraphErrors)
            {
                // Исправляем только проблемы с дорогами
                FixRoadConnectivity(world, rng);
            }
            else if (hasRoads)
            {
                FixRoadConnectivity(world, rng);
            }
        }

        /// <summary>
        /// Исправляет проблемы со связностью дорог, добавляя соединительные дороги.
        /// </summary>
        private void FixRoadConnectivity(WorldData world, System.Random rng)
        {
            var roads = world.Roads;
            int width = roads.Width;
            int height = roads.Height;

            // Находим все компоненты связности дорог
            var components = FindRoadComponents(roads);

            if (components.Count <= 1)
                return; // Всё связано

            Debug.Log($"[RepairStep] Found {components.Count} disconnected road components. Connecting...");

            // Соединяем компоненты детерминированно
            ConnectComponents(world, components, rng);
        }

        /// <summary>
        /// Находит все компоненты связности дорог через BFS.
        /// </summary>
        private List<List<Vector2Int>> FindRoadComponents(TileLayer roads)
        {
            int width = roads.Width;
            int height = roads.Height;
            var visited = new bool[width, height];
            var components = new List<List<Vector2Int>>();

            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (roads.Get(x, y) == TileType.Road && !visited[x, y])
                    {
                        var component = new List<Vector2Int>();
                        var queue = new Queue<Vector2Int>();
                        queue.Enqueue(new Vector2Int(x, y));
                        visited[x, y] = true;

                        while (queue.Count > 0)
                        {
                            var cell = queue.Dequeue();
                            component.Add(cell);

                            foreach (var dir in directions)
                            {
                                int nx = cell.x + dir.x;
                                int ny = cell.y + dir.y;

                                if (nx >= 0 && nx < width && ny >= 0 && ny < height &&
                                    !visited[nx, ny] && roads.Get(nx, ny) == TileType.Road)
                                {
                                    visited[nx, ny] = true;
                                    queue.Enqueue(new Vector2Int(nx, ny));
                                }
                            }
                        }

                        components.Add(component);
                    }
                }
            }

            return components;
        }

        /// <summary>
        /// Соединяет компоненты связности дорог, добавляя соединительные дороги.
        /// </summary>
        private void ConnectComponents(WorldData world, List<List<Vector2Int>> components, System.Random rng)
        {
            var roads = world.Roads;
            var connected = new HashSet<int> { 0 };
            var unconnected = new HashSet<int>();

            for (int i = 1; i < components.Count; i++)
                unconnected.Add(i);

            int maxAttempts = components.Count * 10;
            int attempts = 0;

            while (unconnected.Count > 0 && attempts < maxAttempts)
            {
                attempts++;

                // Находим ближайшую пару между connected и unconnected
                int bestConnected = -1;
                int bestUnconnected = -1;
                float bestDist = float.MaxValue;
                Vector2Int bestFrom = default;
                Vector2Int bestTo = default;

                foreach (int cIdx in connected)
                {
                    foreach (int uIdx in unconnected)
                    {
                        var (dist, from, to) = FindClosestPoints(components[cIdx], components[uIdx], roads);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestConnected = cIdx;
                            bestUnconnected = uIdx;
                            bestFrom = from;
                            bestTo = to;
                        }
                    }
                }

                if (bestConnected == -1)
                    break;

                // Прокладываем дорогу между точками
                CarveRoad(world, bestFrom, bestTo);

                // Перемещаем unconnected в connected
                connected.Add(bestUnconnected);
                unconnected.Remove(bestUnconnected);
            }

            if (unconnected.Count > 0)
            {
                Debug.LogWarning($"[RepairStep] Could not connect {unconnected.Count} components after {maxAttempts} attempts");
            }
        }

        /// <summary>
        /// Находит ближайшую пару точек между двумя компонентами.
        /// </summary>
        private (int distance, Vector2Int from, Vector2Int to) FindClosestPoints(
            List<Vector2Int> componentA, List<Vector2Int> componentB, TileLayer roads)
        {
            int bestDist = int.MaxValue;
            Vector2Int bestFrom = default;
            Vector2Int bestTo = default;

            foreach (var a in componentA)
            {
                foreach (var b in componentB)
                {
                    int dist = Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestFrom = a;
                        bestTo = b;
                    }
                }
            }

            return (bestDist, bestFrom, bestTo);
        }

        /// <summary>
        /// Прокладывает дорогу между двумя точками (L-образный путь).
        /// </summary>
        private void CarveRoad(WorldData world, Vector2Int from, Vector2Int to)
        {
            var roads = world.Roads;

            // L-образный путь
            Vector2Int pivot = new Vector2Int(to.x, from.y);

            // Горизонтальный сегмент
            int startX = Mathf.Min(from.x, pivot.x);
            int endX = Mathf.Max(from.x, pivot.x);
            for (int x = startX; x <= endX; x++)
            {
                if (roads.InBounds(x, from.y))
                    roads.Set(x, from.y, TileType.Road);
            }

            // Вертикальный сегмент
            int startY = Mathf.Min(pivot.y, to.y);
            int endY = Mathf.Max(pivot.y, to.y);
            for (int y = startY; y <= endY; y++)
            {
                if (roads.InBounds(pivot.x, y))
                    roads.Set(pivot.x, y, TileType.Road);
            }
        }
    }
}
