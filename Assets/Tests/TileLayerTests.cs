using NUnit.Framework;
using Diploma.Generation.Model;

public class TileLayerTests
{
    [Test]
    public void TileLayer_FillAndGetSet_Works()
    {
        var layer = new TileLayer(4, 3, TileType.Empty);
        layer.Fill(TileType.Grass);

        Assert.AreEqual(TileType.Grass, layer.Get(0, 0));
        Assert.AreEqual(TileType.Grass, layer.Get(3, 2));

        layer.Set(2, 1, TileType.Road);
        Assert.AreEqual(TileType.Road, layer.Get(2, 1));
    }
}