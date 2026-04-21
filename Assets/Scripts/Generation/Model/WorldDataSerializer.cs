using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Diploma.Generation.Model
{
    /// <summary>
    /// Сериализация WorldData в JSON и обратно.
    /// Использует Unity JsonUtility.
    /// </summary>
    public static class WorldDataSerializer
    {
        /// <summary>
        /// Сериализует WorldData в JSON строку.
        /// </summary>
        public static string Serialize(WorldData world)
        {
            if (world == null) return null;

            var dto = new WorldDataDto
            {
                Size = world.Size,
                SmoothChance = world.SmoothChance,
                Ground = TileLayerDto.FromTileLayer(world.Ground),
                Roads = TileLayerDto.FromTileLayer(world.Roads),
                Walls = TileLayerDto.FromTileLayer(world.Walls),
                Graph = world.Graph != null ? DistrictGraphDto.FromGraph(world.Graph) : null,
                Buildings = world.Buildings?.ConvertAll(b => BuildingDataDto.FromBuilding(b)),
                SpawnPlan = world.SpawnPlan != null ? SpawnPlanDto.FromSpawnPlan(world.SpawnPlan) : null,
                Meta = world.Meta
            };

            return JsonUtility.ToJson(dto, true);
        }

        /// <summary>
        /// Десериализует JSON строку в WorldData.
        /// </summary>
        public static WorldData Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;

            var dto = JsonUtility.FromJson<WorldDataDto>(json);
            if (dto == null) return null;

            var world = new WorldData(dto.Size, dto.SmoothChance)
            {
                Ground = TileLayerDto.ToTileLayer(dto.Ground),
                Roads = TileLayerDto.ToTileLayer(dto.Roads),
                Walls = TileLayerDto.ToTileLayer(dto.Walls),
                Graph = dto.Graph != null ? DistrictGraphDto.ToGraph(dto.Graph) : null,
                Buildings = dto.Buildings?.ConvertAll(b => BuildingDataDto.ToBuilding(b)) ?? new List<BuildingData>(),
                SpawnPlan = dto.SpawnPlan != null ? SpawnPlanDto.ToSpawnPlan(dto.SpawnPlan) : new SpawnPlan(),
                Meta = dto.Meta
            };

            return world;
        }

        /// <summary>
        /// Сохраняет WorldData в файл.
        /// </summary>
        public static void SaveToFile(WorldData world, string filePath)
        {
            string json = Serialize(world);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Загружает WorldData из файла.
        /// </summary>
        public static WorldData LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;

            string json = File.ReadAllText(filePath);
            return Deserialize(json);
        }
    }

    #region DTO Classes

    [Serializable]
    internal class WorldDataDto
    {
        public Vector2Int Size;
        public int SmoothChance;
        public TileLayerDto Ground;
        public TileLayerDto Roads;
        public TileLayerDto Walls;
        public DistrictGraphDto Graph;
        public List<BuildingDataDto> Buildings;
        public SpawnPlanDto SpawnPlan;
        public WorldMeta Meta;
    }

    [Serializable]
    internal class TileLayerDto
    {
        public int Width;
        public int Height;
        public TileType[] Cells;

        public static TileLayerDto FromTileLayer(TileLayer layer)
        {
            return new TileLayerDto
            {
                Width = layer.Width,
                Height = layer.Height,
                Cells = layer.GetRawCells()
            };
        }

        public static TileLayer ToTileLayer(TileLayerDto dto)
        {
            var layer = new TileLayer(dto.Width, dto.Height, TileType.Empty);
            var cells = layer.GetRawCells();
            Array.Copy(dto.Cells, cells, dto.Cells.Length);
            return layer;
        }
    }

    [Serializable]
    internal class DistrictGraphDto
    {
        public List<NodeDto> Nodes;
        public List<EdgeDto> Edges;

        public static DistrictGraphDto FromGraph(DistrictGraph graph)
        {
            return new DistrictGraphDto
            {
                Nodes = graph.Nodes?.ConvertAll(n => new NodeDto { Id = n.id, Position = n.position }),
                Edges = graph.Edges?.ConvertAll(e => new EdgeDto { A = e.a, B = e.b, WeightSqr = e.weightSqr })
            };
        }

        public static DistrictGraph ToGraph(DistrictGraphDto dto)
        {
            var graph = new DistrictGraph();
            if (dto.Nodes != null)
            {
                graph.Nodes.AddRange(dto.Nodes.ConvertAll(n => new DistrictGraph.Node { id = n.Id, position = n.Position }));
            }
            if (dto.Edges != null)
            {
                graph.Edges.AddRange(dto.Edges.ConvertAll(e => new DistrictGraph.Edge { a = e.A, b = e.B, weightSqr = e.WeightSqr }));
            }
            return graph;
        }
    }

    [Serializable]
    internal class NodeDto
    {
        public int Id;
        public Vector2Int Position;
    }

    [Serializable]
    internal class EdgeDto
    {
        public int A;
        public int B;
        public int WeightSqr;
    }

    [Serializable]
    internal class BuildingDataDto
    {
        public int Id;
        public Vector2Int Position;
        public int Width;
        public int Height;
        public int PrefabId;
        public int RotationIndex;
        public BuildingType Type;
        public int Floors;

        public static BuildingDataDto FromBuilding(BuildingData building)
        {
            return new BuildingDataDto
            {
                Id = building.id,
                Position = building.position,
                Width = building.width,
                Height = building.height,
                PrefabId = building.prefabId,
                RotationIndex = building.rotationIndex,
                Type = building.type,
                Floors = building.floors
            };
        }

        public static BuildingData ToBuilding(BuildingDataDto dto)
        {
            return new BuildingData(dto.Id, dto.Position, dto.Width, dto.Height, dto.PrefabId, dto.Type)
            {
                rotationIndex = dto.RotationIndex,
                floors = dto.Floors
            };
        }
    }

    [Serializable]
    internal class SpawnPlanDto
    {
        public List<SpawnEntryDto> Entries;

        public static SpawnPlanDto FromSpawnPlan(SpawnPlan plan)
        {
            return new SpawnPlanDto
            {
                Entries = plan.Entries?.ConvertAll(e => new SpawnEntryDto
                {
                    PrefabId = e.prefabId,
                    CellPos = e.cellPos,
                    RotationIndex = e.rotationIndex
                })
            };
        }

        public static SpawnPlan ToSpawnPlan(SpawnPlanDto dto)
        {
            var plan = new SpawnPlan();
            if (dto.Entries != null)
            {
                plan.Entries = dto.Entries.ConvertAll(e => new SpawnEntry
                {
                    prefabId = e.PrefabId,
                    cellPos = e.CellPos,
                    rotationIndex = e.RotationIndex
                });
            }
            return plan;
        }
    }

    [Serializable]
    internal class SpawnEntryDto
    {
        public int PrefabId;
        public Vector2Int CellPos;
        public int RotationIndex;
    }

    #endregion
}
