using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomLayout
{
    public RoomGraph Graph { get; }
    public List<RectInt> Rooms { get; }
    public List<RectInt> Corridors { get; private set; }
    public DungeonSettings Settings { get; }
    public int[,] MapData { get; private set; }

    public RoomLayout(RoomGraph graph, List<RectInt> rooms, DungeonSettings settings)
    {
        Graph = graph;
        Rooms = rooms;
        Corridors = new List<RectInt>();
        Settings = settings;
        MapData = new int[settings.mapWidth, settings.mapHeight];
    }

    public void SetCorridors(List<RectInt> corridors)
    {
        Corridors = corridors;
        BuildMapData();
    }
    private void BuildMapData()
    {
        int w = Settings.mapWidth;
        int h = Settings.mapHeight;
        // 1 = стена, 0 = пол
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                MapData[x, y] = 1;

        // «вырезаем» комнаты
        foreach (var r in Rooms)
            for (int x = r.xMin; x < r.xMax; x++)
                for (int y = r.yMin; y < r.yMax; y++)
                    if (x >= 0 && x < w && y >= 0 && y < h) MapData[x, y] = 0;

        // «вырезаем» коридоры
        foreach (var c in Corridors)
            for (int x = c.xMin; x < c.xMax; x++)
                for (int y = c.yMin; y < c.yMax; y++)
                    if (x >= 0 && x < w && y >= 0 && y < h) MapData[x, y] = 0;
    }
}
