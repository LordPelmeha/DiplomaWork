using System.Collections.Generic;
using UnityEngine;

public static class DungeonValidator
{
    public static bool Validate(RoomLayout layout)
    {
        int width = layout.Settings.mapWidth;
        int height = layout.Settings.mapHeight;

        bool[,] walkable = new bool[width, height];

        (int start, int end) ClampRange(int min, int max, int limit)
        {
            int s = Mathf.Max(0, min);
            int e = Mathf.Min(limit, max);
            return (s, e);
        }

        // 1) Помечаем комнаты как проходимые
        foreach (var room in layout.Rooms)
        {
            var (x0, x1) = ClampRange(room.xMin, room.xMax, width);
            var (y0, y1) = ClampRange(room.yMin, room.yMax, height);
            for (int x = x0; x < x1; x++)
                for (int y = y0; y < y1; y++)
                    walkable[x, y] = true;
        }

        // 2) Помечаем коридоры как проходимые
        foreach (var c in layout.Corridors)
        {
            var (x0, x1) = ClampRange(c.xMin, c.xMax, width);
            var (y0, y1) = ClampRange(c.yMin, c.yMax, height);
            for (int x = x0; x < x1; x++)
                for (int y = y0; y < y1; y++)
                    walkable[x, y] = true;
        }

        // 3) BFS от центра стартовой комнаты
        Vector2 startF = layout.Rooms[0].center;
        Vector2Int start = new Vector2Int(
            Mathf.RoundToInt(startF.x),
            Mathf.RoundToInt(startF.y)
        );

        var visited = new bool[width, height];
        var queue = new Queue<Vector2Int>();

        // Проверим, что start внутри карты
        if (start.x < 0 || start.x >= width || start.y < 0 || start.y >= height)
            return false;

        visited[start.x, start.y] = true;
        queue.Enqueue(start);

        Vector2Int[] dirs = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        while (queue.Count > 0)
        {
            var cur = queue.Dequeue();
            foreach (var d in dirs)
            {
                int nx = cur.x + d.x;
                int ny = cur.y + d.y;
                if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                if (!visited[nx, ny] && walkable[nx, ny])
                {
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }

        // 4) Проверяем достижимость каждой комнаты
        foreach (var room in layout.Rooms)
        {
            var (x0, x1) = ClampRange(room.xMin, room.xMax, width);
            var (y0, y1) = ClampRange(room.yMin, room.yMax, height);

            bool reached = false;
            for (int x = x0; x < x1 && !reached; x++)
                for (int y = y0; y < y1; y++)
                    if (visited[x, y])
                    {
                        reached = true;
                        break;
                    }

            if (!reached)
                return false;
        }
        return true;
    }
}
