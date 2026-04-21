using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Validation;
using Diploma.Generation.Model;

public class ConnectivityValidatorTests
{
    [Test]
    public void ValidateConnectivity_ConnectedRoads_ReturnsValid()
    {
        var world = new WorldData(new Vector2Int(10, 10));
        
        for (int x = 0; x < 10; x++)
        {
            world.Roads.Set(x, 5, TileType.Road);
        }

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);

        Assert.IsTrue(result.IsValid, $"Validation failed: {string.Join("; ", result.Errors)}");
    }

    [Test]
    public void ValidateConnectivity_DisconnectedRoads_ReturnsInvalid()
    {
        var world = new WorldData(new Vector2Int(10, 10));

        world.Roads.Set(0, 0, TileType.Road);
        world.Roads.Set(9, 9, TileType.Road);

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("disconnected")));
    }

    [Test]
    public void ValidateConnectivity_NoRoads_ReturnsValid()
    {
        var world = new WorldData(new Vector2Int(10, 10));

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);

        Assert.IsTrue(result.IsValid || result.Warnings.Any(w => w.Contains("No road")));
    }

    [Test]
    public void ValidateConnectivity_LShape_Roads_ReturnsValid()
    {
        var world = new WorldData(new Vector2Int(10, 10));

        for (int x = 0; x < 5; x++)
        {
            world.Roads.Set(x, 5, TileType.Road);
        }
        for (int y = 5; y < 10; y++)
        {
            world.Roads.Set(4, y, TileType.Road);
        }

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);

        Assert.IsTrue(result.IsValid);
    }

    [Test]
    public void ValidateGraphConnectivity_ConnectedGraph_ReturnsValid()
    {
        var graph = new DistrictGraph();
        graph.Nodes.Add(new DistrictGraph.Node { id = 0, position = new Vector2Int(0, 0) });
        graph.Nodes.Add(new DistrictGraph.Node { id = 1, position = new Vector2Int(5, 5) });
        graph.Nodes.Add(new DistrictGraph.Node { id = 2, position = new Vector2Int(10, 10) });
        
        graph.Edges.Add(new DistrictGraph.Edge { a = 0, b = 1, weightSqr = 50 });
        graph.Edges.Add(new DistrictGraph.Edge { a = 1, b = 2, weightSqr = 50 });

        var result = new ValidationResult();
        ConnectivityValidator.ValidateGraphConnectivity(graph, result);

        Assert.IsTrue(result.IsValid);
    }

    [Test]
    public void ValidateGraphConnectivity_DisconnectedGraph_ReturnsInvalid()
    {
        var graph = new DistrictGraph();
        graph.Nodes.Add(new DistrictGraph.Node { id = 0, position = new Vector2Int(0, 0) });
        graph.Nodes.Add(new DistrictGraph.Node { id = 1, position = new Vector2Int(5, 5) });
        graph.Nodes.Add(new DistrictGraph.Node { id = 2, position = new Vector2Int(10, 10) });

        graph.Edges.Add(new DistrictGraph.Edge { a = 0, b = 1, weightSqr = 50 });

        var result = new ValidationResult();
        ConnectivityValidator.ValidateGraphConnectivity(graph, result);

        Assert.IsFalse(result.IsValid);
        Assert.IsTrue(result.Errors.Any(e => e.Contains("disconnected")));
    }
}
