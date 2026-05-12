using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Генерация биомов с помощью Multi-Octave Perlin Noise.
    /// Создаёт визуально читаемые области с плавными переходами.
    /// </summary>
    public sealed class BiomeNoiseStep : IWorldGenStep
    {
        public string Key => "BiomeNoise";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            if (world.Ground == null)
                return;

            var rng = seed.CreateRng(Key);

            // Получаем seed-смещение для детерминизма
            float seedOffsetX = rng.Next(0, 10000);
            float seedOffsetY = rng.Next(0, 10000);

            GenerateBiomes(world, config, seedOffsetX, seedOffsetY);
        }

        /// <summary>
        /// Генерирует биомы на основе Multi-Octave Perlin Noise.
        /// </summary>
        private void GenerateBiomes(WorldData world, WorldGenConfig config, float seedOffsetX, float seedOffsetY)
        {
            var ground = world.Ground;
            int width = ground.Width;
            int height = ground.Height;

            // Параметры Multi-Octave Perlin Noise
            int octaves = config.biomeOctaves;
            float persistence = config.biomePersistence;
            float lacunarity = config.biomeLacunarity;
            float baseScale = config.biomeScale;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Вычисляем Multi-Octave Perlin Noise (результат [0,1])
                    float noiseValue = CalculateMultiOctaveNoise(
                        x, y, octaves, persistence, lacunarity, baseScale, seedOffsetX, seedOffsetY);

                    // Определяем тип тайла на основе шума
                    TileType tileType = NoiseToTileType(noiseValue, config);

                    // Устанавливаем тайл
                    ground.Set(x, y, tileType);
                }
            }
        }

        /// <summary>
        /// Вычисляет Multi-Octave Perlin Noise.
        /// Каждая октава добавляет детали на разных масштабах.
        /// Возвращает значение в диапазоне [0, 1].
        /// </summary>
        private float CalculateMultiOctaveNoise(
            int x, int y,
            int octaves,
            float persistence,
            float lacunarity,
            float baseScale,
            float seedOffsetX,
            float seedOffsetY)
        {
            float total = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float maxAmplitude = 0f;

            for (int i = 0; i < octaves; i++)
            {
                // Координаты с текущей частотой и seed-смещением
                float sampleX = (x * baseScale * frequency) + seedOffsetX;
                float sampleY = (y * baseScale * frequency) + seedOffsetY;

                // Mathf.PerlinNoise возвращает [0, 1]
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);

                total += perlinValue * amplitude;
                maxAmplitude += amplitude;

                // Увеличиваем частоту и уменьшаем амплитуду для следующей октавы
                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // Нормализуем в [0, 1]
            return total / maxAmplitude;
        }

        /// <summary>
        /// Преобразует значение шума в тип тайла для биома.
        /// </summary>
        private TileType NoiseToTileType(float noiseValue, WorldGenConfig config)
        {
            // Определяем пороги биомов
            float stoneThreshold = config.biomeStoneThreshold;
            float dirtThreshold = config.biomeDirtThreshold;
            float flowerThreshold = config.biomeFlowerThreshold;
            float sandThreshold = config.biomeSandThreshold;

            // Определяем тип на основе значения шума
            if (noiseValue < stoneThreshold)
                return TileType.Stone;
            else if (noiseValue < dirtThreshold)
                return TileType.Dirt;
            else if (noiseValue < flowerThreshold)
                return TileType.Flower;
            else if (noiseValue < sandThreshold)
                return TileType.Grass; // Основная трава (самая распространённая)
            else
                return TileType.Sand;
        }
    }
}
