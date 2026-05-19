using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using Diploma.Core;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Validation;

public class RepairStepTests
{
    [Test]
    public void RepairStep_ConnectedRoads_NoChangesNeeded()
    {
        var world = new WorldData(new Vector2Int(10, 10));

        for (int x = 0; x < 10; x++)
        {
            world.Roads.Set(x, 5, TileType.Road);
        }

        var seed = new SeedContext(12345);
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var repairStep = new RepairStep();
        
        repairStep.Execute(config, seed, world);

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);
        
        Assert.IsTrue(result.IsValid);
    }

    [Test]
    public void RepairStep_DisconnectedRoads_ConnectsThem()
    {
        var world = new WorldData(new Vector2Int(20, 20));

        for (int x = 0; x < 5; x++)
        {
            world.Roads.Set(x, 5, TileType.Road); 
        }
        for (int x = 15; x < 20; x++)
        {
            world.Roads.Set(x, 15, TileType.Road); 
        }

        var seed = new SeedContext(12345);
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var repairStep = new RepairStep();
        
        repairStep.Execute(config, seed, world);

        var result = new ValidationResult();
        ConnectivityValidator.ValidateConnectivity(world, result);
        
        Assert.IsTrue(result.IsValid, $"Repair failed: {string.Join("; ", result.Errors)}");
    }

    [Test]
    public void RepairStep_Deterministic_SameResultForSameSeed()
    {

        var world1 = CreateDisconnectedWorld();
        var world2 = CreateDisconnectedWorld();

        var seed = new SeedContext(12345);
        var seed2 = new SeedContext(12345);
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var repairStep = new RepairStep();
        
        repairStep.Execute(config, seed, world1);
        repairStep.Execute(config, seed2, world2);

        var cells1 = world1.Roads.GetRawCells();
        var cells2 = world2.Roads.GetRawCells();
        
        Assert.AreEqual(cells1.Length, cells2.Length);
        for (int i = 0; i < cells1.Length; i++)
        {
            Assert.AreEqual(cells1[i], cells2[i], $"Road tile mismatch at index {i}");
        }
    }

    [Test]
    public void RepairStep_DifferentSeed_DifferentResult()
    {
        var seed1 = new SeedContext(11111);
        var seed2 = new SeedContext(22222);
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var repairStep = new RepairStep();

        // Создаём миры с рандомно размещёнными дорогами (разные сиды → разные карты)
        var world1 = CreateDisconnectedWorld(seed1);
        var world2 = CreateDisconnectedWorld(seed2);
        
        repairStep.Execute(config, seed1, world1);
        repairStep.Execute(config, seed2, world2);

        var cells1 = world1.Roads.GetRawCells();
        var cells2 = world2.Roads.GetRawCells();

        int diffCount = 0;
        for (int i = 0; i < cells1.Length; i++)
        {
            if (cells1[i] != cells2[i]) diffCount++;
        }
        Assert.Greater(diffCount, 0,
            "Different seeds should produce different road layouts after repair");
    }

    [Test]
    public void RepairStep_EmptyWorld_NoCrash()
    {
        var world = new WorldData(new Vector2Int(10, 10));
        
        var seed = new SeedContext(12345);
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var repairStep = new RepairStep();

        Assert.DoesNotThrow(() => repairStep.Execute(config, seed, world));
    }

    [Test]
    public void Execute_NullConfig_ThrowsArgumentNull()
    {
        var world = CreateDisconnectedWorld();
        var step = new RepairStep();

        Assert.Throws<ArgumentNullException>(() =>
            step.Execute(null, new SeedContext(12345), world));
    }

    [Test]
    public void Execute_NullSeed_ThrowsArgumentNull()
    {
        var world = CreateDisconnectedWorld();
        var config = ScriptableObject.CreateInstance<WorldGenConfig>();
        var step = new RepairStep();

        Assert.Throws<ArgumentNullException>(() =>
            step.Execute(config, null, world));
    }

    [Test]
    public void Execute_NullWorld_ThrowsArgumentNull()
    {
        var step = new RepairStep();

        Assert.Throws<ArgumentNullException>(() =>
            step.Execute(null, null, null));
    }

    private WorldData CreateDisconnectedWorld()
    {
        var rng = new System.Random(12345);
        return BuildDisconnectedWorld(rng);
    }

    private WorldData CreateDisconnectedWorld(SeedContext seed)
    {
        var rng = seed.CreateRng("RepairTest");
        return BuildDisconnectedWorld(rng);
    }

    private WorldData BuildDisconnectedWorld(System.Random rng)
    {
        var world = new WorldData(new Vector2Int(20, 20));

        // Генератор случайных отрезков дорог — разные сиды дадут разные карты
        int segments = rng.Next(3, 6);
        for (int s = 0; s < segments; s++)
        {
            bool horizontal = rng.Next(0, 2) == 0;
            int length = rng.Next(2, 6);
            int x = rng.Next(1, 18 - length);
            int y = rng.Next(1, 18 - length);

            if (horizontal)
            {
                for (int dx = 0; dx < length; dx++)
                    world.Roads.Set(x + dx, y, TileType.Road);
            }
            else
            {
                for (int dy = 0; dy < length; dy++)
                    world.Roads.Set(x, y + dy, TileType.Road);
            }
        }

        return world;
    }
}
