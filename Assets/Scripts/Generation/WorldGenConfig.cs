using UnityEngine;
using Diploma.Core;

namespace Diploma.Generation
{
    [CreateAssetMenu(menuName = "Diploma/World Gen Config", fileName = "WorldGenConfig")]
    public sealed class WorldGenConfig : ScriptableObject
    {
        [Header("Base")]
        [Tooltip("Размер карты в клетках по горизонтали")]
        [Min(2)] public int mapSizeX = 128;
        [Tooltip("Размер карты в клетках по вертикали")]
        [Min(2)] public int mapSizeY = 128;

        [Tooltip("Размер карты в клетках (вычисляется из mapSizeX и mapSizeY)")]
        public Vector2Int MapSize
        {
            get => new Vector2Int(mapSizeX, mapSizeY);
            set
            {
                mapSizeX = value.x;
                mapSizeY = value.y;
            }
        }

        [Header("Graph / Districts (используем позже)")]
        [Tooltip("Количество районов на карте")]
        [Min(1)] public int districtCount = 8;
        [Tooltip("Количество дополнительных связей между районами (снижает изоляцию кластеров)")]
        [Min(0)] public int extraEdges = 3;

        [Header("Roads (используем позже)")]
        [Tooltip("Радиус расширения дороги в клетках (влияет на ширину дороги)")]
        [Min(1)] public int roadRadius = 1;

        [Header("Buildings (используем позже)")]
        [Tooltip("Минимальное расстояние (в клетках) от зданий до дорог и объектов окружения")]
        [Min(0)] public int minBuildingGap = 1;
        [Tooltip("Минимальное расстояние (в клетках) между зданиями (от стены до стены)")]
        [Min(0)] public int minBuildingDistance = 1;
        [Tooltip("ID префабов зданий для спавна (можно несколько)")]
        public int[] buildingPrefabIds = new int[] { 2000 };
        [Tooltip("Веса для каждого типа здания (должны совпадать по количеству с buildingPrefabIds)")]
        public int[] buildingPrefabWeights = new int[] { 1 };

        [Header("Terrain Smoothing")]
        [Tooltip("Шанс сглаживания границы между дорогой и травой (0-100%)")]
        [Range(0, 100)] public int terrainSmoothChance = 30;

        [Header("Biomes (Perlin Noise)")]
        [Tooltip("Масштаб шума (больше = крупные биомы)")]
        [Range(0.01f, 0.2f)] public float biomeScale = 0.05f;
        [Tooltip("Количество октав (больше = больше деталей)")]
        [Range(1, 6)] public int biomeOctaves = 4;
        [Tooltip("Персистентность (влияние старших октав)")]
        [Range(0.1f, 1f)] public float biomePersistence = 0.5f;
        [Tooltip("Лакунарность (рост частоты между октавами)")]
        [Range(1f, 4f)] public float biomeLacunarity = 2f;
        
        [Header("Biome Thresholds")]
        [Tooltip("Порог камня (0-1)")]
        [Range(0f, 1f)] public float biomeStoneThreshold = 0.15f;
        [Tooltip("Порог земли (0-1)")]
        [Range(0f, 1f)] public float biomeDirtThreshold = 0.35f;
        [Tooltip("Порог цветов (0-1)")]
        [Range(0f, 1f)] public float biomeFlowerThreshold = 0.55f;
        [Tooltip("Порог песка (0-1)")]
        [Range(0f, 1f)] public float biomeSandThreshold = 0.85f;

        [Header("District placement (Graph)")]
        [Tooltip("Внутренний отступ от края карты при размещении центров районов (в клетках)")]
        [Min(0)] public int districtMargin = 8;
        [Tooltip("Минимальное расстояние между центрами двух районов (в клетках)")]
        [Min(0)] public int districtMinDistance = 10;
        [Tooltip("Шаг сетки при размещении кандидатов центров (в клетках)")]
        [Min(1)] public int districtGridStep = 6;
        [Tooltip("Максимальное число дорог, сходящихся в одном узле графа")]
        [Range(1, 4)] public int maxNodeDegree = 4;

        [Header("Decorations")]
        [Tooltip("Минимальное расстояние от дороги для спавна декораций")]
        [Min(0)] public int decorationMinDistanceFromRoad = 1;
        [Tooltip("Максимальное расстояние от дороги для спавна декораций (0 = без ограничений)")]
        [Min(0)] public int decorationMaxDistanceFromRoad = 0;
        
        [Header("Trees")]
        [Tooltip("ID префабов деревьев для спавна (можно несколько)")]
        public int[] treePrefabIds = new int[] { 1000 };
        [Tooltip("Веса для каждого типа дерева (должны совпадать по количеству с treePrefabIds)")]
        public int[] treePrefabWeights = new int[] { 1 };
        [Tooltip("Шанс спавна дерева в подходящей клетке (0-100%)")]
        [Range(0, 100)] public int treeSpawnChance = 15;
        [Tooltip("Максимальное количество деревьев (0 = без ограничений)")]
        [Min(0)] public int maxTreeCount = 0;
        
        [Header("Lamps")]
        [Tooltip("ID префабов фонарей (можно несколько)")]
        public int[] lampPrefabIds = new int[] { 1003 };
        [Tooltip("Веса для каждого типа фонарей")]
        public int[] lampPrefabWeights = new int[] { 1 };
        [Tooltip("Интервал между фонарями вдоль дороги (в клетках)")]
        [Min(2)] public int lampInterval = 8;
        [Tooltip("Минимальная длина отрезка дороги для спавна фонарей")]
        [Min(3)] public int minRoadSegmentLength = 5;
        
        [Header("Benches")]
        [Tooltip("ID префаба скамейки")]
        public int benchPrefabId = 1004;
        [Tooltip("Минимальное расстояние (в клетках) от скамеек до любых других объектов (зданий, декораций)")]
        [Min(0)] public int minBenchDistance = 1;

        public uint ComputeStableHash()
        {
            unchecked
            {

                uint h = 2166136261u; 

                 h = Mix(h, mapSizeX);
                h = Mix(h, mapSizeY);

                h = Mix(h, districtCount);
                h = Mix(h, extraEdges);

                h = Mix(h, roadRadius);

                h = Mix(h, minBuildingGap);
                h = Mix(h, minBuildingDistance);

                h = Mix(h, terrainSmoothChance);

                // Biome hashes
                h = Mix(h, Mathf.FloorToInt(biomeScale * 1000));
                h = Mix(h, biomeOctaves);
                h = Mix(h, Mathf.FloorToInt(biomePersistence * 100));
                h = Mix(h, Mathf.FloorToInt(biomeLacunarity * 10));
                h = Mix(h, Mathf.FloorToInt(biomeStoneThreshold * 100));
                h = Mix(h, Mathf.FloorToInt(biomeDirtThreshold * 100));
                h = Mix(h, Mathf.FloorToInt(biomeFlowerThreshold * 100));
                h = Mix(h, Mathf.FloorToInt(biomeSandThreshold * 100));

                h = Mix(h, districtMargin);
                h = Mix(h, districtMinDistance);

                h = Mix(h, districtGridStep);
                h = Mix(h, maxNodeDegree);

                h = Mix(h, decorationMinDistanceFromRoad);
                h = Mix(h, decorationMaxDistanceFromRoad);
                
                // Tree hashes
                h = Mix(h, treePrefabIds.Length);
                for (int i = 0; i < treePrefabIds.Length; i++)
                {
                    h = Mix(h, treePrefabIds[i]);
                    if (i < treePrefabWeights.Length)
                        h = Mix(h, treePrefabWeights[i]);
                }
                h = Mix(h, treeSpawnChance);
                h = Mix(h, maxTreeCount);
                
                // Lamp hashes
                h = Mix(h, lampPrefabIds.Length);
                for (int i = 0; i < lampPrefabIds.Length; i++)
                {
                    h = Mix(h, lampPrefabIds[i]);
                    if (i < lampPrefabWeights.Length)
                        h = Mix(h, lampPrefabWeights[i]);
                }
                h = Mix(h, lampInterval);
                h = Mix(h, minRoadSegmentLength);

                // Bench hash
                h = Mix(h, benchPrefabId);
                h = Mix(h, minBenchDistance);

                return h;
            }

            static uint Mix(uint acc, int value)
            {
                return StableHash.HashToUInt((int)acc, value.ToString());
            }
        }
    }
}