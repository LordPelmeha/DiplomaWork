using NUnit.Framework;
using System;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Model;

public class BuildingLayoutTests
{
    [Test]
    public void BuildingLayout_SameSeed_ProducesSameBuildings()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 3;
        cfg.districtMargin = 5;
        cfg.districtMinDistance = 6;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.AreEqual(w1.Buildings.Count, w2.Buildings.Count, "Building count should be the same");
        
        for (int i = 0; i < w1.Buildings.Count; i++)
        {
            var b1 = w1.Buildings[i];
            var b2 = w2.Buildings[i];
            
            Assert.AreEqual(b1.id, b2.id, $"Building {i} id mismatch");
            Assert.AreEqual(b1.position, b2.position, $"Building {i} position mismatch");
            Assert.AreEqual(b1.width, b2.width, $"Building {i} width mismatch");
            Assert.AreEqual(b1.height, b2.height, $"Building {i} height mismatch");
            Assert.AreEqual(b1.type, b2.type, $"Building {i} type mismatch");
        }
    }

    [Test]
    public void BuildingLayout_DifferentSeeds_ProducesDifferentBuildings()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var w1 = WorldGeneratorService.Generate(11111, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(22222, cfg, pipeline);

        bool different = w1.Buildings.Count != w2.Buildings.Count;
        
        if (!different && w1.Buildings.Count > 0)
        {
            for (int i = 0; i < Mathf.Min(w1.Buildings.Count, w2.Buildings.Count); i++)
            {
                if (w1.Buildings[i].position != w2.Buildings[i].position)
                {
                    different = true;
                    break;
                }
            }
        }

        Assert.IsTrue(different, "Different seeds should produce different buildings");
    }

    [Test]
    public void BuildingLayout_BuildingsDoNotOverlap()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        for (int i = 0; i < world.Buildings.Count; i++)
        {
            for (int j = i + 1; j < world.Buildings.Count; j++)
            {
                var b1 = world.Buildings[i];
                var b2 = world.Buildings[j];
                
                Assert.IsFalse(b1.Intersects(b2), 
                    $"Buildings {i} and {j} overlap: {b1.Bounds} vs {b2.Bounds}");
            }
        }
    }

    [Test]
    public void BuildingLayout_BuildingsAreOnGround()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        foreach (var building in world.Buildings)
        {
            for (int x = building.position.x; x < building.position.x + building.width; x++)
            {
                for (int y = building.position.y; y < building.position.y + building.height; y++)
                {
                    Assert.IsTrue(world.Ground.InBounds(x, y), 
                        $"Building {building.id} is out of bounds");

                    Assert.AreNotEqual(world.Roads.Get(x, y), Diploma.Generation.Model.TileType.Road,
                        $"Building {building.id} overlaps with road at ({x}, {y})");
                }
            }
        }
    }

    [Test]
    public void BuildingLayout_MinBuildingGap_Respected()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.minBuildingGap = 3;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        foreach (var building in world.Buildings)
        {
            // Проверяем, что здание не перекрывает дороги
            for (int x = building.position.x; x < building.position.x + building.width; x++)
            {
                for (int y = building.position.y; y < building.position.y + building.height; y++)
                {
                    Assert.AreNotEqual(world.Roads.Get(x, y), TileType.Road,
                        $"Building {building.id} at ({x},{y}) should be at least minBuildingGap={cfg.minBuildingGap} cells away from roads");
                }
            }
        }
    }

    [Test]
    public void BuildingLayout_MinBuildingDistance_BuildingsDoNotOverlap()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.minBuildingDistance = 2;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        for (int i = 0; i < world.Buildings.Count; i++)
        {
            for (int j = i + 1; j < world.Buildings.Count; j++)
            {
                var b1 = world.Buildings[i];
                var b2 = world.Buildings[j];

                var rect1 = new RectInt(b1.position.x, b1.position.y, b1.width, b1.height);
                var rect2 = new RectInt(b2.position.x, b2.position.y, b2.width, b2.height);

                var expanded1 = new RectInt(
                    rect1.xMin - cfg.minBuildingDistance,
                    rect1.yMin - cfg.minBuildingDistance,
                    rect1.width + 2 * cfg.minBuildingDistance,
                    rect1.height + 2 * cfg.minBuildingDistance);

                Assert.IsFalse(expanded1.Overlaps(rect2),
                    $"Buildings {i} and {j} should be at least minBuildingDistance={cfg.minBuildingDistance} cells apart. " +
                    $"Expanded rect of building {i}: {expanded1}, building {j}: {rect2}");
            }
        }
    }
}
