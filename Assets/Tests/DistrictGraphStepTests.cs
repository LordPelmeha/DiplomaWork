using NUnit.Framework;
using System;
using UnityEngine;
using Diploma.Core;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

public class DistrictGraphStepTests
{
    [Test]
    public void Execute_CreatesGraphWithCorrectNodeCount()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.districtMargin = 5;
        cfg.districtMinDistance = 6;

        var world = new WorldData(cfg.MapSize);
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed, world);

        Assert.IsNotNull(world.Graph);
        Assert.AreEqual(8, world.Graph.Nodes.Count);
    }

    [Test]
    public void Execute_CreatesConnectedGraph()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var world = new WorldData(cfg.MapSize);
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed, world);

        Assert.GreaterOrEqual(world.Graph.Edges.Count, world.Graph.Nodes.Count - 1);

        var visited = new bool[world.Graph.Nodes.Count];
        var queue = new System.Collections.Generic.Queue<int>();
        queue.Enqueue(0);
        visited[0] = true;
        int visitedCount = 1;

        while (queue.Count > 0)
        {
            int current = queue.Dequeue();
            foreach (var edge in world.Graph.Edges)
            {
                int neighbor = -1;
                if (edge.a == current) neighbor = edge.b;
                else if (edge.b == current) neighbor = edge.a;
                
                if (neighbor >= 0 && !visited[neighbor])
                {
                    visited[neighbor] = true;
                    visitedCount++;
                    queue.Enqueue(neighbor);
                }
            }
        }

        Assert.AreEqual(world.Graph.Nodes.Count, visitedCount);
    }

    [Test]
    public void Execute_Deterministic_SameSeedSameGraph()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var seed1 = new SeedContext(12345);
        var seed2 = new SeedContext(12345);
        
        var world1 = new WorldData(cfg.MapSize);
        var world2 = new WorldData(cfg.MapSize);
        
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed1, world1);
        step.Execute(cfg, seed2, world2);

        Assert.AreEqual(world1.Graph.Nodes.Count, world2.Graph.Nodes.Count);
        
        for (int i = 0; i < world1.Graph.Nodes.Count; i++)
        {
            Assert.AreEqual(world1.Graph.Nodes[i].position, world2.Graph.Nodes[i].position);
        }
    }

    [Test]
    public void Execute_DifferentSeed_DifferentGraph()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var seed1 = new SeedContext(11111);
        var seed2 = new SeedContext(22222);
        
        var world1 = new WorldData(cfg.MapSize);
        var world2 = new WorldData(cfg.MapSize);
        
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed1, world1);
        step.Execute(cfg, seed2, world2);

        bool different = false;
        for (int i = 0; i < world1.Graph.Nodes.Count && !different; i++)
        {
            if (world1.Graph.Nodes[i].position != world2.Graph.Nodes[i].position)
            {
                different = true;
            }
        }

        Assert.IsTrue(different);
    }

    [Test]
    public void Execute_NodesWithinMapBounds()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.districtMargin = 5;

        var world = new WorldData(cfg.MapSize);
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed, world);

        foreach (var node in world.Graph.Nodes)
        {
            Assert.GreaterOrEqual(node.position.x, cfg.districtMargin);
            Assert.Less(node.position.x, cfg.MapSize.x - cfg.districtMargin);
            Assert.GreaterOrEqual(node.position.y, cfg.districtMargin);
            Assert.Less(node.position.y, cfg.MapSize.y - cfg.districtMargin);
        }
    }

    [Test]
    public void Execute_ExtraEdges_AddedToGraph()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 5;

        var world = new WorldData(cfg.MapSize);
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed, world);

        int expectedMinEdges = cfg.districtCount - 1 + cfg.extraEdges;

        Assert.GreaterOrEqual(world.Graph.Edges.Count, cfg.districtCount - 1);
    }

    [Test]
    public void Execute_MinDistance_Respected()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.MapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.districtMinDistance = 10;
        cfg.districtMargin = 0;

        var world = new WorldData(cfg.MapSize);
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        step.Execute(cfg, seed, world);

        int minDistSqr = cfg.districtMinDistance * cfg.districtMinDistance;
        for (int i = 0; i < world.Graph.Nodes.Count; i++)
        {
            for (int j = i + 1; j < world.Graph.Nodes.Count; j++)
            {
                var pos1 = world.Graph.Nodes[i].position;
                var pos2 = world.Graph.Nodes[j].position;
                int distSqr = (pos1.x - pos2.x) * (pos1.x - pos2.x) + 
                              (pos1.y - pos2.y) * (pos1.y - pos2.y);
                
                Assert.GreaterOrEqual(distSqr, minDistSqr, 
                    $"Nodes {i} and {j} are too close: {pos1} vs {pos2}");
            }
        }
    }

    [Test]
    public void Execute_NullWorld_ThrowsException()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        Assert.Throws<ArgumentNullException>(() => {
            step.Execute(cfg, seed, null);
        });
    }

    [Test]
    public void Execute_NullConfig_ThrowsException()
    {
        var world = new WorldData(new Vector2Int(64, 64));
        var seed = new SeedContext(12345);
        var step = new DistrictGraphStep();

        Assert.Throws<ArgumentNullException>(() => {
            step.Execute(null, seed, world);
        });
    }

    [Test]
    public void Execute_NullSeed_ThrowsException()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        var world = new WorldData(new Vector2Int(64, 64));
        var step = new DistrictGraphStep();

        Assert.Throws<ArgumentNullException>(() => {
            step.Execute(cfg, null, world);
        });
    }
}
