using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Разбиение пространства на кварталы (блоки) между дорогами.
    /// Определяет области, где можно размещать здания.
    /// </summary>
    public sealed class BlockLayoutStep : IWorldGenStep
    {
        public string Key => "Blocks";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var rng = seed.CreateRng(Key);

            if (world.Roads == null)
                throw new InvalidOperationException("BlockLayoutStep: world.Roads is null. Add StreetCarvingStep before BlockLayoutStep.");

            var roads = world.Roads;
            int width = roads.Width;
            int height = roads.Height;

            // Находим "островки" земли, окружённые дорогами
            // Это будут кварталы для застройки
            var visited = new bool[width, height];
            var blocks = new List<RectInt>();

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Пропускаем дороги и уже посещённые клетки
                    if (roads.Get(x, y) == TileType.Road || visited[x, y])
                        continue;

                    // BFS для нахождения связной области
                    var block = FindBlock(roads, visited, new Vector2Int(x, y), rng);
                    if (block.HasValue)
                    {
                        var r = block.Value;
                        // Минимальный размер квартала - 3x3 клетки
                        if (r.width >= 3 && r.height >= 3)
                        {
                            // Квартал должен быть полностью внутри карты (не касаться границ)
                            // Это гарантирует, что он окружён дорогами со всех сторон
                            if (r.xMin > 0 && r.yMin > 0 && r.xMax < width && r.yMax < height)
                            {
                                blocks.Add(r);
                            }
                        }
                    }
                }
            }

            Debug.Log($"[BlockLayoutStep] Found {blocks.Count} interior blocks (districts) out of total components");

            // Сохраняем найденные блоки в world data
            world.Blocks = blocks;
        }

        /// <summary>
        /// Находит связную область земли (квартал) через BFS.
        /// </summary>
        private RectInt? FindBlock(TileLayer roads, bool[,] visited, Vector2Int startPos, System.Random rng)
        {
            int width = roads.Width;
            int height = roads.Height;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startPos);
            visited[startPos.x, startPos.y] = true;

            int minX = startPos.x, maxX = startPos.x;
            int minY = startPos.y, maxY = startPos.y;
            int count = 0;

            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                count++;

                foreach (var dir in directions)
                {
                    int nx = current.x + dir.x;
                    int ny = current.y + dir.y;

                    if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                        continue;

                    if (visited[nx, ny])
                        continue;

                    // Пропускаем дороги
                    if (roads.Get(nx, ny) == TileType.Road)
                        continue;

                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));

                    minX = Mathf.Min(minX, nx);
                    maxX = Mathf.Max(maxX, nx);
                    minY = Mathf.Min(minY, ny);
                    maxY = Mathf.Max(maxY, ny);
                }
            }

            // Возвращаем ограничивающий прямоугольник
            int blockWidth = maxX - minX + 1;
            int blockHeight = maxY - minY + 1;

            return new RectInt(minX, minY, blockWidth, blockHeight);
        }
    }
}
