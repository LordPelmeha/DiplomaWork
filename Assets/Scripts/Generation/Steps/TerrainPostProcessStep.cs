using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Сглаживание границ между дорогами и землёй.
    /// Использует cellular automata для создания плавных переходов.
    /// </summary>
    public sealed class TerrainPostProcessStep : IWorldGenStep
    {
        public string Key => "TerrainPostProcess";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            if (world.Ground == null || world.Roads == null)
                return;

            var rng = seed.CreateRng(Key);

            // Сглаживаем границы между дорогами и травой
            SmoothTerrain(world, rng);
        }

        /// <summary>
        /// Сглаживает границы между дорогами и травой.
        /// </summary>
        private void SmoothTerrain(WorldData world, System.Random rng)
        {
            var ground = world.Ground;
            var roads = world.Roads;
            int width = ground.Width;
            int height = ground.Height;

            // Проходим по всем клеткам и сглаживаем границы
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Если это граница между дорогой и травой
                    if (ground.Get(x, y) == TileType.Grass && IsNearRoad(roads, x, y))
                    {
                        // С шансом из конфига меняем на dirt для плавного перехода
                        if (rng.NextDouble() * 100 < world.SmoothChance)
                        {
                            ground.Set(x, y, TileType.Dirt);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, есть ли рядом дорога.
        /// </summary>
        private bool IsNearRoad(TileLayer roads, int x, int y)
        {
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right,
                new Vector2Int(1, 1), new Vector2Int(-1, 1),
                new Vector2Int(1, -1), new Vector2Int(-1, -1)
            };

            foreach (var dir in directions)
            {
                int nx = x + dir.x;
                int ny = y + dir.y;

                if (roads.InBounds(nx, ny) && roads.Get(nx, ny) == TileType.Road)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
