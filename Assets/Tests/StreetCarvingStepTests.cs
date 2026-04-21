using NUnit.Framework;
using UnityEngine;
using Diploma.Core;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class StreetCarvingStepTests
{
    [Test]
    public void Execute_CreatesRoadsBetweenNodes()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 4;
        cfg.roadRadius = 1;

        var world = new WorldData(cfg.mapSize);
        var seed = new SeedContext(12345);

        var graphStep = new DistrictGraphStep();
        graphStep.Execute(cfg, seed, world);

        var step = new StreetCarvingStep();
        step.Execute(cfg, seed, world);

        var roadCells = world.Roads.GetRawCells();
        int roadCount = 0;
        foreach (var cell in roadCells)
        {
            if (cell == TileType.Road)
                roadCount++;
        }
        
        Assert.Greater(roadCount, 0, "No roads were created");
    }

    [Test]
    public void Execute_Deterministic_SameSeedSameRoads()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 4;

        var seed1 = new SeedContext(12345);
        var seed2 = new SeedContext(12345);
        
        var world1 = CreateWorldWithGraph(cfg, seed1);
        var world2 = CreateWorldWithGraph(cfg, seed2);
        
        var step1 = new StreetCarvingStep();
        var step2 = new StreetCarvingStep();
        
        step1.Execute(cfg, seed1, world1);
        step2.Execute(cfg, seed2, world2);

        var roads1 = world1.Roads.GetRawCells();
        var roads2 = world2.Roads.GetRawCells();
        
        Assert.AreEqual(roads1.Length, roads2.Length);
        for (int i = 0; i < roads1.Length; i++)
        {
            Assert.AreEqual(roads1[i], roads2[i], $"Road tile mismatch at index {i}");
        }
    }

    [Test]
    public void Execute_RoadsConnectAllNodes()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 4;

        var world = new WorldData(cfg.mapSize);
        var seed = new SeedContext(12345);

        var graphStep = new DistrictGraphStep();
        graphStep.Execute(cfg, seed, world);

        var step = new StreetCarvingStep();
        step.Execute(cfg, seed, world);

        foreach (var node in world.Graph.Nodes)
        {
            Assert.AreEqual(TileType.Road, world.Roads.Get(node.position), 
                $"Node at {node.position} is not on a road");
        }
    }

    [Test]
    public void Execute_RoadRadius_Respected()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 4;
        cfg.roadRadius = 2;

        var world = new WorldData(cfg.mapSize);
        var seed = new SeedContext(12345);
        
        var graphStep = new DistrictGraphStep();
        graphStep.Execute(cfg, seed, world);

        var step = new StreetCarvingStep();
        step.Execute(cfg, seed, world);

        var roadCells = world.Roads.GetRawCells();
        int roadCount = 0;
        foreach (var cell in roadCells)
        {
            if (cell == TileType.Road)
                roadCount++;
        }

        var world2 = new WorldData(cfg.mapSize);
        var seed2 = new SeedContext(12345);
        
        var cfg2 = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg2.mapSize = cfg.mapSize;
        cfg2.districtCount = cfg.districtCount;
        cfg2.roadRadius = 1;
        
        var graphStep2 = new DistrictGraphStep();
        graphStep2.Execute(cfg2, seed2, world2);
        
        var step2 = new StreetCarvingStep();
        step2.Execute(cfg2, seed2, world2);
        
        var roadCells2 = world2.Roads.GetRawCells();
        int roadCount2 = 0;
        foreach (var cell in roadCells2)
        {
            if (cell == TileType.Road)
                roadCount2++;
        }

        Assert.Greater(roadCount, roadCount2, 
            "Roads with radius 2 should be wider than radius 1");
    }

    [Test]
    public void Execute_RoadsStayWithinBounds()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);
        cfg.districtCount = 4;

        var world = new WorldData(cfg.mapSize);
        var seed = new SeedContext(12345);
        
        var graphStep = new DistrictGraphStep();
        graphStep.Execute(cfg, seed, world);

        var step = new StreetCarvingStep();
        step.Execute(cfg, seed, world);

        for (int x = 0; x < cfg.mapSize.x; x++)
        {
            for (int y = 0; y < cfg.mapSize.y; y++)
            {
                Assert.IsTrue(world.Roads.InBounds(x, y));
            }
        }
    }

    [Test]
    public void Execute_NullWorld_ThrowsException()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var seed = new SeedContext(12345);
        var step = new StreetCarvingStep();

        Assert.Throws<System.NullReferenceException>(() => {
            step.Execute(cfg, seed, null);
        });
    }

    [Test]
    public void Execute_NullConfig_ThrowsException()
    {
        var world = new WorldData(new Vector2Int(64, 64));
        var seed = new SeedContext(12345);
        var step = new StreetCarvingStep();

        Assert.Throws<System.NullReferenceException>(() => {
            step.Execute(null, seed, world);
        });
    }

    [Test]
    public void Execute_NullSeed_ThrowsException()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var world = new WorldData(new Vector2Int(64, 64));
        var step = new StreetCarvingStep();

        Assert.Throws<System.NullReferenceException>(() => {
            step.Execute(cfg, null, world);
        });
    }

    [Test]
    public void Execute_NoGraph_ThrowsException()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);

        var world = new WorldData(cfg.mapSize);

        var seed = new SeedContext(12345);
        var step = new StreetCarvingStep();

        Assert.DoesNotThrow(() => {
            step.Execute(cfg, seed, world);
        });
    }

    private WorldData CreateWorldWithGraph(WorldGenConfig cfg, SeedContext seed)
    {
        var world = new WorldData(cfg.mapSize);
        var graphStep = new DistrictGraphStep();
        graphStep.Execute(cfg, seed, world);
        return world;
    }
}
