using System.Collections.Generic;
using Diploma.Core;

namespace Diploma.Generation.Model
{
    /// <summary>
    /// Подсчёт стабильного хеша мира для проверки детерминизма.
    /// </summary>
    public static class WorldHash
    {
        /// <summary>
        /// Вычисляет хеш всего мира.
        /// </summary>
        public static uint Compute(WorldData world)
        {
            if (world == null) return 0;

            unchecked
            {
                uint hash = 2166136261u; // FNV offset basis

                // Хеш размера
                hash = Mix(hash, (long)world.Size.x);
                hash = Mix(hash, (long)world.Size.y);

                // Хеш слоёв
                hash = Mix(hash, (long)ComputeLayerHash(world.Ground));
                hash = Mix(hash, (long)ComputeLayerHash(world.Roads));
                hash = Mix(hash, (long)ComputeLayerHash(world.Walls));

                // Хеш графа
                if (world.Graph != null)
                {
                    hash = Mix(hash, (long)ComputeGraphHash(world.Graph));
                }

                // Хеш зданий
                hash = Mix(hash, (long)ComputeBuildingsHash(world.Buildings));

                // Хеш плана спавна
                hash = Mix(hash, (long)ComputeSpawnPlanHash(world.SpawnPlan));

                return hash;
            }
        }

        /// <summary>
        /// Вычисляет хеш слоя тайлов.
        /// </summary>
        private static uint ComputeLayerHash(TileLayer layer)
        {
            if (layer == null) return 0;

            unchecked
            {
                uint hash = 2166136261u;

                hash = Mix(hash, (long)layer.Width);
                hash = Mix(hash, (long)layer.Height);

                var cells = layer.GetRawCells();
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i] != TileType.Empty)
                    {
                        hash = Mix(hash, (long)i);
                        hash = Mix(hash, (long)cells[i]);
                    }
                }

                return hash;
            }
        }

        /// <summary>
        /// Вычисляет хеш графа районов.
        /// </summary>
        private static uint ComputeGraphHash(DistrictGraph graph)
        {
            if (graph == null) return 0;

            unchecked
            {
                uint hash = 2166136261u;

                // Хеш узлов
                hash = Mix(hash, (long)graph.Nodes.Count);
                for (int i = 0; i < graph.Nodes.Count; i++)
                {
                    var node = graph.Nodes[i];
                    hash = Mix(hash, (long)node.id);
                    hash = Mix(hash, (long)node.position.x);
                    hash = Mix(hash, (long)node.position.y);
                }

                // Хеш рёбер
                hash = Mix(hash, (long)graph.Edges.Count);
                for (int i = 0; i < graph.Edges.Count; i++)
                {
                    var edge = graph.Edges[i];
                    hash = Mix(hash, (long)edge.a);
                    hash = Mix(hash, (long)edge.b);
                    hash = Mix(hash, (long)edge.weightSqr);
                }

                return hash;
            }
        }

        private static uint Mix(uint acc, long value)
        {
            return StableHash.HashToUInt((int)acc, value.ToString());
        }

        /// <summary>
        /// Вычисляет хеш списка зданий.
        /// </summary>
        private static uint ComputeBuildingsHash(List<BuildingData> buildings)
        {
            if (buildings == null || buildings.Count == 0) return 0;

            unchecked
            {
                uint hash = 2166136261u;

                hash = Mix(hash, (long)buildings.Count);
                for (int i = 0; i < buildings.Count; i++)
                {
                    var b = buildings[i];
                    hash = Mix(hash, (long)b.id);
                    hash = Mix(hash, (long)b.position.x);
                    hash = Mix(hash, (long)b.position.y);
                    hash = Mix(hash, (long)b.width);
                    hash = Mix(hash, (long)b.height);
                    hash = Mix(hash, (long)b.prefabId);
                    hash = Mix(hash, (long)b.rotationIndex);
                    hash = Mix(hash, (long)b.type);
                }

                return hash;
            }
        }

        /// <summary>
        /// Вычисляет хеш плана спавна.
        /// </summary>
        private static uint ComputeSpawnPlanHash(SpawnPlan spawnPlan)
        {
            if (spawnPlan == null || spawnPlan.Count == 0) return 0;

            unchecked
            {
                uint hash = 2166136261u;

                hash = Mix(hash, (long)spawnPlan.Count);
                for (int i = 0; i < spawnPlan.Count; i++)
                {
                    var entry = spawnPlan.Entries[i];
                    hash = Mix(hash, (long)entry.prefabId);
                    hash = Mix(hash, (long)entry.cellPos.x);
                    hash = Mix(hash, (long)entry.cellPos.y);
                    hash = Mix(hash, (long)entry.rotationIndex);
                }

                return hash;
            }
        }
    }
}
