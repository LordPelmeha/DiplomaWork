using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class WorldGeneratorMvpTests
{
    [Test]
    public void Generate_SameSeed_ProducesSameGround()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(16, 16);

        var pipeline = new WorldGenPipeline().Add(new BaseFillStep());

        var w1 = WorldGeneratorService.Generate(42, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(42, cfg, pipeline);

        var a1 = w1.Ground.GetRawCells();
        var a2 = w2.Ground.GetRawCells();

        Assert.AreEqual(a1.Length, a2.Length);
        for (int i = 0; i < a1.Length; i++)
            Assert.AreEqual(a1[i], a2[i], $"Mismatch at {i}");
    }
}