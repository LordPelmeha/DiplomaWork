using UnityEngine;

public static class PostProcessor
{
    public static void Process(RoomLayout layout, DungeonSettings settings)
    {
        int w = settings.mapWidth, h = settings.mapHeight;
        int[,] map = layout.MapData;
        int[,] tmp = new int[w, h];

        for (int iter = 0; iter < settings.iterations; iter++)
        {
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                {
                    int walls = CountWallNeighbors(map, x, y, w, h);
                    if (map[x, y] == 1)
                        tmp[x, y] = (walls < settings.deathLimit) ? 0 : 1;
                    else
                        tmp[x, y] = (walls > settings.birthLimit) ? 1 : 0;
                }

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    map[x, y] = tmp[x, y];
        }
    }

    private static int CountWallNeighbors(int[,] map, int x, int y, int w, int h)
    {
        int cnt = 0;
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue;
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || nx >= w || ny < 0 || ny >= h) { cnt++; continue; }
                if (map[nx, ny] == 1) cnt++;
            }
        return cnt;
    }
}
