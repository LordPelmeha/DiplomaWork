using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public static class TilemapBuilder
{
    public static void Build(RoomLayout layout, List<RectInt> corridors)
    {
        var groundMap = layout.Settings.groundTilemap;
        var wallMap = layout.Settings.wallTilemap;
        var groundTile = layout.Settings.groundTile;
        var wallTile = layout.Settings.wallTile;
        groundMap.ClearAllTiles();
        wallMap.ClearAllTiles();

        int width = layout.Settings.mapWidth;
        int height = layout.Settings.mapHeight;

        int[,] map = layout.MapData;

        // Отрисовываем пол
        int painted = 0;
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                if (map[x, y] == 0)
                {
                    groundMap.SetTile(new Vector3Int(x, y, 0), groundTile);
                    painted++;

                }

        // Отрисовываем стены вокруг пола
        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (map[x, y] != 0) continue;

                foreach (var d in dirs)
                {
                    int nx = x + d.x;
                    int ny = y + d.y;
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (map[nx, ny] == 1)
                            wallMap.SetTile(new Vector3Int(nx, ny, 0), wallTile);
                    }
                }
            }
        }
    }
}
