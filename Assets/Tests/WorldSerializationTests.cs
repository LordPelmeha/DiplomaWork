using NUnit.Framework;
using UnityEngine;
using Diploma.Generation;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;
using Diploma.Generation.Model;

public class WorldSerializationTests
{
    [Test]
    public void Serialize_Deserialize_ProducesSameWorldHash()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;
        cfg.extraEdges = 3;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);
        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserializedWorld = WorldDataSerializer.Deserialize(json);

        Assert.AreEqual(originalWorld.Meta.WorldHash, deserializedWorld.Meta.WorldHash, 
            "World hash should be the same after serialization/deserialization");
    }

    [Test]
    public void Serialize_Deserialize_ProducesSameGraph()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);
        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserializedWorld = WorldDataSerializer.Deserialize(json);

        Assert.AreEqual(originalWorld.Graph.Nodes.Count, deserializedWorld.Graph.Nodes.Count);
        Assert.AreEqual(originalWorld.Graph.Edges.Count, deserializedWorld.Graph.Edges.Count);

        for (int i = 0; i < originalWorld.Graph.Nodes.Count; i++)
        {
            Assert.AreEqual(originalWorld.Graph.Nodes[i].position, deserializedWorld.Graph.Nodes[i].position);
        }
    }

    [Test]
    public void Serialize_Deserialize_ProducesSameBuildings()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(64, 64);
        cfg.districtCount = 8;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep())
            .Add(new BuildingLayoutStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);
        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserializedWorld = WorldDataSerializer.Deserialize(json);

        Assert.AreEqual(originalWorld.Buildings.Count, deserializedWorld.Buildings.Count);

        for (int i = 0; i < originalWorld.Buildings.Count; i++)
        {
            var b1 = originalWorld.Buildings[i];
            var b2 = deserializedWorld.Buildings[i];

            Assert.AreEqual(b1.id, b2.id);
            Assert.AreEqual(b1.position, b2.position);
            Assert.AreEqual(b1.width, b2.width);
            Assert.AreEqual(b1.height, b2.height);
            Assert.AreEqual(b1.type, b2.type);
        }
    }

    [Test]
    public void Serialize_Deserialize_ProducesSameTileLayers()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep())
            .Add(new StreetCarvingStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);
        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserializedWorld = WorldDataSerializer.Deserialize(json);

        var origGround = originalWorld.Ground.GetRawCells();
        var deserGround = deserializedWorld.Ground.GetRawCells();

        Assert.AreEqual(origGround.Length, deserGround.Length);
        for (int i = 0; i < origGround.Length; i++)
        {
            Assert.AreEqual(origGround[i], deserGround[i], $"Ground tile mismatch at index {i}");
        }
    }

    [Test]
    public void SaveToFile_LoadFromFile_ProducesSameWorld()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);
        cfg.districtCount = 4;

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);

        string filePath = Application.temporaryCachePath + "/test_world.json";
        WorldDataSerializer.SaveToFile(originalWorld, filePath);

        var loadedWorld = WorldDataSerializer.LoadFromFile(filePath);

        Assert.IsNotNull(loadedWorld);
        Assert.AreEqual(originalWorld.Meta.WorldHash, loadedWorld.Meta.WorldHash);

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }

    [Test]
    public void Serialize_Deserialize_NullGraph_HandlesCorrectly()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);

        Assert.IsNotNull(originalWorld.Graph);
        Assert.AreEqual(0, originalWorld.Graph.Nodes.Count);

        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserializedWorld = WorldDataSerializer.Deserialize(json);

        Assert.IsNotNull(deserializedWorld.Graph);
        Assert.AreEqual(0, deserializedWorld.Graph.Nodes.Count);
    }

    [Test]
    public void Serialize_Deserialize_SpawnPlan_ProducesSamePlan()
    {
        var world = new WorldData(new Vector2Int(32, 32));

        world.SpawnPlan.Add(1000, new Vector2Int(5, 5), 0);
        world.SpawnPlan.Add(1001, new Vector2Int(10, 10), 1);
        world.SpawnPlan.Add(1002, new Vector2Int(15, 15), 2);

        string json = WorldDataSerializer.Serialize(world);
        var deserialized = WorldDataSerializer.Deserialize(json);

        Assert.AreEqual(world.SpawnPlan.Count, deserialized.SpawnPlan.Count);
        
        for (int i = 0; i < world.SpawnPlan.Count; i++)
        {
            var orig = world.SpawnPlan.Entries[i];
            var deser = deserialized.SpawnPlan.Entries[i];
            
            Assert.AreEqual(orig.prefabId, deser.prefabId);
            Assert.AreEqual(orig.cellPos, deser.cellPos);
            Assert.AreEqual(orig.rotationIndex, deser.rotationIndex);
        }
    }

    [Test]
    public void Serialize_Deserialize_WorldMeta_ProducesSameMeta()
    {
        var cfg = ScriptableObject.CreateInstance<WorldGenConfig>();
        cfg.mapSize = new Vector2Int(32, 32);

        var pipeline = new WorldGenPipeline()
            .Add(new BaseFillStep())
            .Add(new DistrictGraphStep());

        var originalWorld = WorldGeneratorService.Generate(12345, cfg, pipeline);

        string json = WorldDataSerializer.Serialize(originalWorld);
        var deserialized = WorldDataSerializer.Deserialize(json);

        Assert.AreEqual(originalWorld.Meta.Seed, deserialized.Meta.Seed);
        Assert.AreEqual(originalWorld.Meta.GeneratorVersion, deserialized.Meta.GeneratorVersion);
        Assert.AreEqual(originalWorld.Meta.ConfigHash, deserialized.Meta.ConfigHash);
        Assert.AreEqual(originalWorld.Meta.WorldHash, deserialized.Meta.WorldHash);
    }

    [Test]
    public void Serialize_EmptyString_ReturnsNull()
    {
        var result = WorldDataSerializer.Deserialize("");
        Assert.IsNull(result);
        
        result = WorldDataSerializer.Deserialize(null);
        Assert.IsNull(result);
    }

    [Test]
    public void Serialize_InvalidJson_ThrowsOrReturnsNull()
    {
        Assert.Throws<System.ArgumentException>(() => {
            WorldDataSerializer.Deserialize("{ invalid json }");
        });
    }

    [Test]
    public void SaveToFile_FileContainsValidJson()
    {
        var world = new WorldData(new Vector2Int(16, 16));
        world.Meta.Seed = 12345;
        
        string filePath = Application.temporaryCachePath + "/test_world_meta.json";
        WorldDataSerializer.SaveToFile(world, filePath);

        string content = System.IO.File.ReadAllText(filePath);
        
        Assert.IsNotNull(content);
        Assert.IsNotEmpty(content);
        Assert.IsTrue(content.Contains("12345")); // Seed должен быть в JSON

        if (System.IO.File.Exists(filePath))
            System.IO.File.Delete(filePath);
    }
}
