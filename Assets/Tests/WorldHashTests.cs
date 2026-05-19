using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Model;

public class WorldHashTests
{
    [Test]
    public void ComputeHash_SameSeed_ProducesSameHash()
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
            .Add(new StreetCarvingStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.AreEqual(w1.Meta.WorldHash, w2.Meta.WorldHash, "Hash should be identical for the same seed");
        Assert.AreNotEqual(w1.Meta.WorldHash, 0u, "Hash should not be zero");
    }

    [Test]
    public void ComputeHash_DifferentSeeds_ProducesDifferentHash()
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
            .Add(new StreetCarvingStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(54321, cfg, pipeline);

        Assert.AreNotEqual(w1.Meta.WorldHash, w2.Meta.WorldHash, "Different seeds should produce different hashes");
    }

    [Test]
    public void ComputeHash_DifferentConfig_ProducesDifferentHash()
    {
        var cfg1 = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg1.MapSize = new Vector2Int(64, 64);
        cfg1.districtCount = 8;
        cfg1.extraEdges = 3;

        var cfg2 = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg2.MapSize = new Vector2Int(64, 64);
        cfg2.districtCount = 10; // Другое количество районов
        cfg2.extraEdges = 3;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        var w1 = WorldGeneratorService.Generate(12345, cfg1, pipeline);
        var w2 = WorldGeneratorService.Generate(12345, cfg2, pipeline);

        Assert.AreNotEqual(w1.Meta.ConfigHash, w2.Meta.ConfigHash, "Different configs should produce different ConfigHash");
    }

    [Test]
    public void ComputeHash_OnlyGroundLayer_ConstantHash()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());

        var w1 = WorldGeneratorService.Generate(999, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(999, cfg, pipeline);

        Assert.AreEqual(w1.Meta.WorldHash, w2.Meta.WorldHash);

        Assert.IsNotNull(w1.Graph);
        Assert.AreEqual(0, w1.Graph.Nodes.Count);
    }

    [Test]
    public void ComputeHash_FullPipeline_DeterministicHash()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(80, 60);
        cfg.districtCount = 10;
        cfg.extraEdges = 4;
        cfg.districtMargin = 4;
        cfg.districtMinDistance = 6;
        cfg.roadRadius = 1;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        uint[] hashes = new uint[5];
        for (int i = 0; i < 5; i++)
        {
            var world = WorldGeneratorService.Generate(77777, cfg, pipeline);
            hashes[i] = world.Meta.WorldHash;
        }

        for (int i = 1; i < hashes.Length; i++)
        {
            Assert.AreEqual(hashes[0], hashes[i], $"Hash mismatch at iteration {i}");
        }
    }
}
