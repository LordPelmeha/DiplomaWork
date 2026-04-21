using NUnit.Framework;
using UnityEngine;
using Diploma.Core;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class WorldGenPipelineTests
{
    [Test]
    public void Pipeline_EmptyPipeline_CreatesEmptyWorld()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline();
        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.IsNotNull(world);
        Assert.AreEqual(32, world.Size.x);
        Assert.AreEqual(32, world.Size.y);
        Assert.IsNotNull(world.Ground);
        Assert.IsNotNull(world.Roads);
        Assert.IsNotNull(world.Walls);
    }

    [Test]
    public void Pipeline_SingleStep_ExecutesStep()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());
        
        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        var groundCells = world.Ground.GetRawCells();
        foreach (var cell in groundCells)
        {
            Assert.AreEqual(TileType.Grass, cell);
        }
    }

    [Test]
    public void Pipeline_MultipleSteps_ExecuteInOrder()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);
        cfg.districtCount = 4;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());
        
        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.IsNotNull(world.Ground);
        Assert.IsNotNull(world.Graph);
        Assert.Greater(world.Graph.Nodes.Count, 0);

        var roadCells = world.Roads.GetRawCells();
        int roadCount = 0;
        foreach (var cell in roadCells)
        {
            if (cell == TileType.Road)
                roadCount++;
        }
        Assert.Greater(roadCount, 0);
    }

    [Test]
    public void Pipeline_AddNullStep_ThrowsException()
    {
        var pipeline = new WorldGenPipeline();
        
        Assert.Throws<System.ArgumentNullException>(() => {
            pipeline.Add(null);
        });
    }

    [Test]
    public void Pipeline_RunWithNullConfig_ThrowsException()
    {
        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());
        
        var seed = new SeedContext(12345);
        var world = new WorldData(new Vector2Int(32, 32));

        Assert.Throws<System.ArgumentNullException>(() => {
            pipeline.Run(null, seed, world);
        });
    }

    [Test]
    public void Pipeline_RunWithNullSeed_ThrowsException()
    {
        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());
        
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var world = new WorldData(new Vector2Int(32, 32));

        Assert.Throws<System.ArgumentNullException>(() => {
            pipeline.Run(cfg, null, world);
        });
    }

    [Test]
    public void Pipeline_RunWithNullWorld_ThrowsException()
    {
        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());
        
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var seed = new SeedContext(12345);

        Assert.Throws<System.ArgumentNullException>(() => {
            pipeline.Run(cfg, seed, null);
        });
    }

    [Test]
    public void Pipeline_StepsProperty_ReturnsAddedSteps()
    {
        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        Assert.AreEqual(2, pipeline.Steps.Count);
        Assert.IsInstanceOf<BaseFillStep>(pipeline.Steps[0]);
        Assert.IsInstanceOf<DistrictGraphStep>(pipeline.Steps[1]);
    }

    [Test]
    public void Pipeline_FullPipeline_ProducesValidWorld()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 3;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new TerrainPostProcessStep())
            .Add(new BlockLayoutStep())
            .Add(new BuildingLayoutStep())
            .Add(new DecorationPlanStep());
        
        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.IsNotNull(world);
        Assert.Greater(world.Graph.Nodes.Count, 0);
        Assert.Greater(world.Buildings.Count, 0);
        Assert.Greater(world.SpawnPlan.Count, 0);
    }
}
