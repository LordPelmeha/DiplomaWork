using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diploma.Generation.Model
{
    [Serializable]
    public sealed class WorldData
    {
        public Vector2Int Size { get; private set; }

        public TileLayer Ground { get; internal set; }
        public TileLayer Roads { get; internal set; }
        public TileLayer Walls { get; internal set; }

        public DistrictGraph Graph = new DistrictGraph();

        public List<BuildingData> Buildings = new List<BuildingData>();

        public SpawnPlan SpawnPlan = new SpawnPlan();

        public WorldMeta Meta;

        public int SmoothChance = 30;

        public WorldData(Vector2Int size, int smoothChance = 30)
        {
            if (size.x <= 0) throw new ArgumentOutOfRangeException(nameof(size.x));
            if (size.y <= 0) throw new ArgumentOutOfRangeException(nameof(size.y));

            Size = size;
            SmoothChance = smoothChance;

            Ground = new TileLayer(size.x, size.y, TileType.Empty);
            Roads = new TileLayer(size.x, size.y, TileType.Empty);
            Walls = new TileLayer(size.x, size.y, TileType.Empty);

            Graph = new DistrictGraph();
            Buildings = new List<BuildingData>();
            SpawnPlan = new SpawnPlan();
        }

        public void AddBuilding(BuildingData building)
        {
            Buildings.Add(building);
        }
    }
}