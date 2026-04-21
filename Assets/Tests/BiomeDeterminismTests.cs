using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class BiomeDeterminismTests
{
    [Test]
    public void Biomes_SameSeed_ProducesSameGroundLayer()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        
        cfg.biomeScale = 0.05f;
        cfg.biomeOctaves = 4;
        cfg.biomePersistence = 0.5f;
        cfg.biomeLacunarity = 2f;
        cfg.biomeStoneThreshold = 0.20f;
        cfg.biomeDirtThreshold = 0.35f;
        cfg.biomeFlowerThreshold = 0.45f;
        cfg.biomeSandThreshold = 0.75f;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new BiomeNoiseStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(12345, cfg, pipeline);

        var ground1 = w1.Ground.GetRawCells();
        var ground2 = w2.Ground.GetRawCells();

        Assert.AreEqual(ground1.Length, ground2.Length);
        for (int i = 0; i < ground1.Length; i++)
        {
            Assert.AreEqual(ground1[i], ground2[i], $"Ground tile mismatch at index {i}");
        }
    }

    [Test]
    public void Biomes_DifferentSeeds_ProducesDifferentGroundLayer()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        
        cfg.biomeScale = 0.05f;
        cfg.biomeOctaves = 4;
        cfg.biomePersistence = 0.5f;
        cfg.biomeLacunarity = 2f;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new BiomeNoiseStep());

        var w1 = WorldGeneratorService.Generate(11111, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(22222, cfg, pipeline);

        var ground1 = w1.Ground.GetRawCells();
        var ground2 = w2.Ground.GetRawCells();

        int diffCount = 0;
        for (int i = 0; i < ground1.Length; i++)
        {
            if (ground1[i] != ground2[i])
                diffCount++;
        }

        Assert.Greater(diffCount, ground1.Length * 0.5f, 
            "Different seeds should produce significantly different biomes");
    }

    [Test]
    public void Biomes_HasEnoughGrassForTreeSpawning()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        
        cfg.biomeScale = 0.05f;
        cfg.biomeOctaves = 4;
        cfg.biomePersistence = 0.5f;
        cfg.biomeLacunarity = 2f;
        cfg.biomeStoneThreshold = 0.20f;
        cfg.biomeDirtThreshold = 0.35f;
        cfg.biomeFlowerThreshold = 0.45f;
        cfg.biomeSandThreshold = 0.75f;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new BiomeNoiseStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var ground = world.Ground.GetRawCells();

        int grassCount = 0;
        foreach (var tile in ground)
        {
            if (tile == TileType.Grass)
                grassCount++;
        }

        float grassPercentage = (float)grassCount / ground.Length * 100f;
        Assert.GreaterOrEqual(grassPercentage, 30f, 
            $"Grass should cover at least 30% of the map, but only {grassPercentage:F1}%");
    }

    [Test]
    public void Biomes_ContainsMultipleBiomeTypes()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        
        cfg.biomeScale = 0.05f;
        cfg.biomeOctaves = 4;
        cfg.biomePersistence = 0.5f;
        cfg.biomeLacunarity = 2f;
        cfg.biomeStoneThreshold = 0.20f;
        cfg.biomeDirtThreshold = 0.35f;
        cfg.biomeFlowerThreshold = 0.45f;
        cfg.biomeSandThreshold = 0.75f;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new BiomeNoiseStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var ground = world.Ground.GetRawCells();

        var biomeCounts = new System.Collections.Generic.Dictionary<TileType, int>();
        foreach (var tile in ground)
        {
            if (!biomeCounts.ContainsKey(tile))
                biomeCounts[tile] = 0;
            biomeCounts[tile]++;
        }

        Assert.GreaterOrEqual(biomeCounts.Count, 3, 
            $"Map should have at least 3 biome types, but has {biomeCounts.Count}");

        Debug.Log($"[BiomeTest] Biome distribution:");
        foreach (var kvp in biomeCounts)
        {
            float pct = (float)kvp.Value / ground.Length * 100f;
            Debug.Log($"  {kvp.Key}: {kvp.Value} ({pct:F1}%)");
        }
    }

    [Test]
    public void Biomes_FullPipeline_DeterministicHash()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        
        cfg.biomeScale = 0.05f;
        cfg.biomeOctaves = 4;
        cfg.biomePersistence = 0.5f;
        cfg.biomeLacunarity = 2f;
        cfg.biomeStoneThreshold = 0.20f;
        cfg.biomeDirtThreshold = 0.35f;
        cfg.biomeFlowerThreshold = 0.45f;
        cfg.biomeSandThreshold = 0.75f;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new BiomeNoiseStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.AreEqual(w1.Meta.WorldHash, w2.Meta.WorldHash,
            "World hash should be identical for same seed with biomes");
    }
}
