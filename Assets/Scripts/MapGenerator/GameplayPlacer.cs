using System.Collections.Generic;
using UnityEngine;

public static class GameplayPlacer
{
    public static void Place(RoomLayout layout, DungeonSettings settings)
    {
        // 1) Спавн игрока внутри стартовой комнаты
        RectInt startRoom = layout.Rooms[0];

        // 2) Ищем spawnCell
        Vector3Int spawnCell = FindValidGroundCell(startRoom, layout);
        Vector3 worldStart = settings.groundTilemap.GetCellCenterWorld(spawnCell);
        worldStart.z = -1f;

        var player = GameObject.Instantiate(settings.playerPrefab, worldStart, Quaternion.identity);

        // Привязываем камеру
        var cameraFollow = Camera.main.GetComponent<CameraFollow>();
        if (cameraFollow != null)
            cameraFollow.target = player.transform;

        // 2) Спавн выхода — в самой удалённой комнате от spawnCell
        Vector2Int startCell2D = new Vector2Int(spawnCell.x, spawnCell.y);
        int farIdx = 0;
        float maxDist2 = -1f;

        for (int i = 1; i < layout.Rooms.Count; i++)
        {
            RectInt room = layout.Rooms[i];
            Vector2Int center = new Vector2Int(
                (room.xMin + room.xMax) / 2,
                (room.yMin + room.yMax) / 2
            );
            Vector2Int delta = center - startCell2D;
            float dist2 = delta.x * delta.x + delta.y * delta.y;
            if (dist2 > maxDist2)
            {
                maxDist2 = dist2;
                farIdx = i;
            }
        }

        RectInt endRoom = layout.Rooms[farIdx];

        Vector3Int exitCell = FindValidExitCell(endRoom, layout, settings);

        Vector3 worldEnd = settings.groundTilemap.GetCellCenterWorld(exitCell);

        worldEnd.z = -0.1f;

        if (settings.exitPrefab != null)
            GameObject.Instantiate(settings.exitPrefab, worldEnd, Quaternion.identity);
    }

    private static Vector3Int FindValidGroundCell(RectInt room, RoomLayout layout)
    {
        var map = layout.MapData;
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        List<Vector3Int> candidates = new List<Vector3Int>();


        for (int x = room.xMin + 1; x < room.xMax - 1; x++)
        {
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                if (x >= 0 && x < w && y >= 0 && y < h && map[x, y] == 0)
                    candidates.Add(new Vector3Int(x, y, 0));
            }
        }

        if (candidates.Count > 0)
        {
            var chosen = candidates[Random.Range(0, candidates.Count)];
            return chosen;
        }

        int fx = Mathf.Clamp(room.xMin, 0, w - 1);
        int fy = Mathf.Clamp(room.yMin, 0, h - 1);
        return new Vector3Int(fx, fy, 0);
    }

    private static Vector3Int FindValidExitCell(RectInt room, RoomLayout layout, DungeonSettings settings)
    {
        var map = layout.MapData;
        var groundMap = settings.groundTilemap;
        var wallMap = settings.wallTilemap;
        int w = map.GetLength(0), h = map.GetLength(1);
        var candidates = new List<Vector3Int>();

        // проходим по всем «внутренним» клеткам комнаты
        for (int x = room.xMin + 1; x < room.xMax - 1; x++)
            for (int y = room.yMin + 1; y < room.yMax - 1; y++)
            {
                if (x < 0 || x >= w || y < 0 || y >= h) continue;

                var cell = new Vector3Int(x, y, 0);
                // условие 1: это действительно пол
                bool isFloor = map[x, y] == 0 && groundMap.HasTile(cell);
                // условие 2: в этой клетке нет стены
                bool noWall = !wallMap.HasTile(cell);

                if (isFloor && noWall)
                    candidates.Add(cell);
            }

        if (candidates.Count > 0)
            return candidates[Random.Range(0, candidates.Count)];

        int fx = Mathf.Clamp((room.xMin + room.xMax) / 2, 0, w - 1);
        int fy = Mathf.Clamp((room.yMin + room.yMax) / 2, 0, h - 1);
        return new Vector3Int(fx, fy, 0);
    }
}
