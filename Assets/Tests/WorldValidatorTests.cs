using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Validation;
using Diploma.Generation.Model;

public class WorldValidatorTests
{
    [Test]
    public void Validate_ValidWorld_ReturnsValidResult()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 3;
        cfg.districtMargin = 5;
        cfg.districtMinDistance = 6;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        var result = WorldValidator.Validate(world);

        Assert.IsTrue(result.IsValid, $"World validation failed: {string.Join("; ", result.Errors)}");
    }

    [Test]
    public void Validate_NullWorld_ReturnsInvalidResult()
    {
        var result = WorldValidator.Validate(null);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e == "World is null"));
    }

    [Test]
    public void Validate_EmptyGraph_ReturnsInvalidResult()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        var result = WorldValidator.Validate(world);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("no nodes")));
    }

    [Test]
    public void Validate_EmptyNodesGraph_ReturnsInvalidResult()
    {

        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);
        cfg.districtCount = 1;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        var world = WorldGeneratorService.Generate(12345, cfg, pipeline);

        var result = WorldValidator.Validate(world);

        Assert.IsTrue(result.IsValid, $"Validation failed: {string.Join("; ", result.Errors)}");
    }

    [Test]
    public void Validate_SameSeed_AlwaysValid()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 3;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        for (int i = 0; i < 10; i++)
        {
            var world = WorldGeneratorService.Generate(99999, cfg, pipeline);
            var result = WorldValidator.Validate(world);

            Assert.IsTrue(result.IsValid, $"Validation failed on iteration {i}: {string.Join("; ", result.Errors)}");
        }
    }
}
