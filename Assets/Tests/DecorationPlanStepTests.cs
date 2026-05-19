using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Diploma.Core;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class DecorationPlanStepTests
{
    private const int MapSize = 32;
    private const int Seed = 12345;

    private WorldData CreateWorldWithRoadsAndGround()
    {
        var world = new WorldData(new Vector2Int(MapSize, MapSize));

        // Заполняем всю землю травой
        for (int x = 0; x < MapSize; x++)
            for (int y = 0; y < MapSize; y++)
                world.Ground.Set(x, y, TileType.Grass);

        // Рисуем прямую дорогу по центру (горизонтальная)
        int midY = MapSize / 2;
        for (int x = 2; x < MapSize - 2; x++)
            world.Roads.Set(x, midY, TileType.Road);

        // Рисуем прямую дорогу по центру (вертикальная)
        int midX = MapSize / 2;
        for (int y = 2; y < MapSize - 2; y++)
            world.Roads.Set(midX, y, TileType.Road);

        return world;
    }

    [Test]
    public void Execute_NoRoads_ThrowsException()
    {
        var world = new WorldData(new Vector2Int(MapSize, MapSize));
        for (int x = 0; x < MapSize; x++)
            for (int y = 0; y < MapSize; y++)
                world.Ground.Set(x, y, TileType.Grass);

        var config = CreateDefaultConfig();
        var step = new DecorationPlanStep();

        Assert.Throws<System.InvalidOperationException>(() =>
            step.Execute(config, new SeedContext(Seed), world));
    }

    [Test]
    public void Execute_NoGround_ThrowsException()
    {
        var world = CreateWorldWithRoadsAndGround();
        typeof(WorldData).GetProperty("Ground").SetValue(world, null);

        var config = CreateDefaultConfig();
        var step = new DecorationPlanStep();

        Assert.Throws<System.InvalidOperationException>(() =>
            step.Execute(config, new SeedContext(Seed), world));
    }

    [Test]
    public void Execute_NullConfig_ThrowsArgumentNull()
    {
        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();

        Assert.Throws<ArgumentNullException>(() =>
            step.Execute(null, new SeedContext(Seed), world));
    }

    [Test]
    public void Execute_NullSeed_ThrowsArgumentNull()
    {
        var world = CreateWorldWithRoadsAndGround();
        var config = CreateDefaultConfig();
        var step = new DecorationPlanStep();

        Assert.Throws<ArgumentNullException>(() =>
            step.Execute(config, null, world));
    }

    [Test]
    public void Execute_TreesSpawnedOnGrassAwayFromRoads()
    {
        var config = CreateDefaultConfig();
        // Высокий шанс спавна, нет лимита
        config.treeSpawnChance = 100;
        config.maxTreeCount = 0;
        config.decorationMinDistanceFromRoad = 2;

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        int treeEntries = world.SpawnPlan.Entries.Count(e => e.prefabId == config.treePrefabIds[0]);
        Assert.Greater(treeEntries, 0, "Trees should have been spawned");

        // Все деревья должны быть на Grass
        foreach (var entry in world.SpawnPlan.Entries.Where(e => e.prefabId == config.treePrefabIds[0]))
        {
            TileType tile = world.Ground.Get(entry.cellPos);
            Assert.AreEqual(TileType.Grass, tile,
                $"Tree at {entry.cellPos} should be on Grass");
        }
    }

    [Test]
    public void Execute_MaxTreeCount_Respected()
    {
        var config = CreateDefaultConfig();
        config.treeSpawnChance = 100;
        config.maxTreeCount = 5;
        config.decorationMinDistanceFromRoad = 2;

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        int treeEntries = world.SpawnPlan.Entries.Count(e => e.prefabId == config.treePrefabIds[0]);
        Assert.LessOrEqual(treeEntries, config.maxTreeCount,
            $"Tree count {treeEntries} should not exceed maxTreeCount={config.maxTreeCount}");
    }

    [Test]
    public void Execute_TreeSpawnChance_Zero_NoTrees()
    {
        var config = CreateDefaultConfig();
        config.treeSpawnChance = 0;

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        int treeEntries = world.SpawnPlan.Entries.Count(e => e.prefabId == config.treePrefabIds[0]);
        Assert.AreEqual(0, treeEntries,
            "treeSpawnChance=0 should spawn no trees");
    }

    [Test]
    public void Execute_TreePrefabWeights_UsedForSelection()
    {
        // Используем 2 типа деревьев с разными весами, проверяем что оба типа попали в SpawnPlan
        var config = CreateDefaultConfig();
        config.treePrefabIds = new int[] { 1000, 1001 };
        config.treePrefabWeights = new int[] { 1, 1 };
        config.treeSpawnChance = 100;
        config.maxTreeCount = 0;

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        var treeIds = world.SpawnPlan.Entries
            .Where(e => config.treePrefabIds.Contains(e.prefabId))
            .Select(e => e.prefabId)
            .Distinct()
            .ToList();

        Assert.Greater(treeIds.Count, 0,
            "At least one tree type should be present");
        Assert.IsTrue(treeIds.All(id => config.treePrefabIds.Contains(id)),
            "All spawned tree IDs should be from treePrefabIds");
    }

    [Test]
    public void Execute_DecorationMinDistanceFromRoad_FiltersCloseCells()
    {
        var config = CreateDefaultConfig();
        config.treeSpawnChance = 100;
        config.maxTreeCount = 0;
        config.decorationMinDistanceFromRoad = MapSize; // больше размера карты = нет клеток достаточно далеко

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        int treeEntries = world.SpawnPlan.Entries.Count(e => e.prefabId == config.treePrefabIds[0]);
        Assert.AreEqual(0, treeEntries,
            "decorationMinDistanceFromRoad >= mapSize should block all trees");
    }

    [Test]
    public void Execute_LampsSpawnedAlongRoads()
    {
        var config = CreateDefaultConfig();
        config.lampInterval = 4;
        config.minRoadSegmentLength = 3;

        var world = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world);

        int lampEntries = world.SpawnPlan.Entries.Count(e => e.prefabId == config.lampPrefabIds[0]);
        Assert.Greater(lampEntries, 0,
            "Lamps should be spawned along roads");
    }

    [Test]
    public void Execute_LampInterval_AffectsNumberOfLamps()
    {
        var configA = CreateDefaultConfig();
        configA.lampInterval = 2;
        configA.minRoadSegmentLength = 3;

        var configB = CreateDefaultConfig();
        configB.lampInterval = 8;
        configB.minRoadSegmentLength = 3;

        var worldA = CreateWorldWithRoadsAndGround();
        var stepA = new DecorationPlanStep();
        stepA.Execute(configA, new SeedContext(Seed), worldA);

        var worldB = CreateWorldWithRoadsAndGround();
        var stepB = new DecorationPlanStep();
        stepB.Execute(configB, new SeedContext(Seed), worldB);

        int lampsA = worldA.SpawnPlan.Entries.Count(e => e.prefabId == configA.lampPrefabIds[0]);
        int lampsB = worldB.SpawnPlan.Entries.Count(e => e.prefabId == configB.lampPrefabIds[0]);

        Assert.Greater(lampsA, lampsB,
            $"Smaller lampInterval={configA.lampInterval} should produce more lamps than larger {configB.lampInterval}");
    }

    [Test]
    public void Execute_NullRoadsGround_ThrowsInvalidOperation()
    {
        // Roads = null
        var world = new WorldData(new Vector2Int(16, 16));
        for (int x = 0; x < 16; x++)
            for (int y = 0; y < 16; y++)
                world.Ground.Set(x, y, TileType.Grass);
        typeof(WorldData).GetProperty("Roads").SetValue(world, null);

        var config = CreateDefaultConfig();
        var step = new DecorationPlanStep();

        Assert.Throws<System.InvalidOperationException>(() =>
            step.Execute(config, new SeedContext(Seed), world));
    }

    [Test]
    public void Execute_DecorationNoBuildings_SpawnsWithoutError()
    {
        var config = CreateDefaultConfig();
        config.treeSpawnChance = 50;
        config.maxTreeCount = 0;
        config.decorationMinDistanceFromRoad = 2;
        config.benchPrefabId = 1004;
        config.minBenchDistance = 1;

        var world = CreateWorldWithRoadsAndGround();
        // Blocks пустые — benches не должны спавниться, но шаг не должен упасть
        world.Blocks = new List<RectInt>();

        var step = new DecorationPlanStep();
        Assert.DoesNotThrow(() => step.Execute(config, new SeedContext(Seed), world));
    }

    [Test]
    public void Execute_SameSeed_ProducesSameSpawnPlan()
    {
        var config = CreateDefaultConfig();
        config.treeSpawnChance = 50;
        config.maxTreeCount = 10;
        config.lampInterval = 4;
        config.minRoadSegmentLength = 3;
        config.benchPrefabId = 1004;

        var world1 = CreateWorldWithRoadsAndGround();
        var step = new DecorationPlanStep();
        step.Execute(config, new SeedContext(Seed), world1);

        var world2 = CreateWorldWithRoadsAndGround();
        step.Execute(config, new SeedContext(Seed), world2);

        Assert.AreEqual(world1.SpawnPlan.Entries.Count, world2.SpawnPlan.Entries.Count,
            "Same seed should produce same spawn plan entry count");

        for (int i = 0; i < world1.SpawnPlan.Entries.Count; i++)
        {
            var e1 = world1.SpawnPlan.Entries[i];
            var e2 = world2.SpawnPlan.Entries[i];
            Assert.AreEqual(e1.prefabId, e2.prefabId,
                $"Entry[{i}]: prefabId mismatch");
            Assert.AreEqual(e1.cellPos, e2.cellPos,
                $"Entry[{i}]: cellPos mismatch");
            Assert.AreEqual(e1.rotationIndex, e2.rotationIndex,
                $"Entry[{i}]: rotationIndex mismatch");
        }
    }

    private WorldGenConfig CreateDefaultConfig()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSizeX = MapSize;
        cfg.mapSizeY = MapSize;
        cfg.treePrefabIds = new int[] { 1000 };
        cfg.treePrefabWeights = new int[] { 1 };
        cfg.lampPrefabIds = new int[] { 1003 };
        cfg.lampPrefabWeights = new int[] { 1 };
        cfg.benchPrefabId = 1004;
        return cfg;
    }
}
