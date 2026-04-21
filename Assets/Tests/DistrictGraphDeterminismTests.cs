using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class DistrictGraphDeterminismTests
{
    [Test]
    public void Graph_SameSeed_ProducesSameNodesAndEdges()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 10;
        cfg.extraEdges = 4;
        cfg.districtMargin = 5;
        cfg.districtMinDistance = 6;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        var w1 = WorldGeneratorService.Generate(123, cfg, pipeline);
        var w2 = WorldGeneratorService.Generate(123, cfg, pipeline);

        Assert.NotNull(w1.Graph);
        Assert.NotNull(w2.Graph);

        Assert.AreEqual(w1.Graph.Nodes.Count, w2.Graph.Nodes.Count);
        for (int i = 0; i < w1.Graph.Nodes.Count; i++)
        {
            Assert.AreEqual(w1.Graph.Nodes[i].id, w2.Graph.Nodes[i].id);
            Assert.AreEqual(w1.Graph.Nodes[i].position, w2.Graph.Nodes[i].position);
        }

        Assert.AreEqual(w1.Graph.Edges.Count, w2.Graph.Edges.Count);
        for (int i = 0; i < w1.Graph.Edges.Count; i++)
        {
            var e1 = w1.Graph.Edges[i];
            var e2 = w2.Graph.Edges[i];
            Assert.AreEqual(e1.a, e2.a);
            Assert.AreEqual(e1.b, e2.b);
            Assert.AreEqual(e1.weightSqr, e2.weightSqr);
        }
    }
}