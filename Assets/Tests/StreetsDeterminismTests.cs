using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class StreetsDeterminismTests
{
    [Test]
    public void Streets_SameSeed_ProducesSameRoadLayer()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(80, 60);
        cfg.districtCount = 12;
        cfg.extraEdges = 5;
        cfg.districtMargin = 4;
        cfg.districtMinDistance = 6;
        cfg.roadRadius = 1;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        var w1 = WorldGeneratorService.Generate(999, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(999, cfg, pipeline);

        var a1 = w1.Roads.GetRawCells();
        var a2 = w2.Roads.GetRawCells();

        Assert.AreEqual(a1.Length, a2.Length);
        for (int i = 0; i < a1.Length; i++)
            Assert.AreEqual(a1[i], a2[i], $"Mismatch at index {i}");
    }
}