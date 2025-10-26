using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Соединяет пары комнат через L-образные (ортогональные) коридоры с возможными короткими ответвлениями (detours).
/// Возвращает список RectInt, каждый — прямоугольник пола коридора (как в текущей реализации).
/// </summary>
public static class LShapedConnector
{
    public static List<RectInt> Connect(RoomLayout layout, DungeonSettings settings)
    {
        var corridors = new List<RectInt>();
        var edges = layout.Graph.Edges;
        var rnd = new System.Random(settings.seed + 987654);

        foreach (var edge in edges)
        {
            Vector2 startF = layout.Rooms[edge.a].center;
            Vector2 targetF = layout.Rooms[edge.b].center;

            var start = new Vector2Int(Mathf.RoundToInt(startF.x), Mathf.RoundToInt(startF.y));
            var target = new Vector2Int(Mathf.RoundToInt(targetF.x), Mathf.RoundToInt(targetF.y));

            bool horizontalFirst = rnd.NextDouble() < Mathf.Clamp01(settings.preferHorizontalFirst);

            // Базовая точка сгиба
            Vector2Int bend = horizontalFirst
                ? new Vector2Int(target.x, start.y)
                : new Vector2Int(start.x, target.y);

            // случайный оффсет сгиба
            int bx = bend.x + RandomRangeInt(rnd, -settings.bendOffsetMax, settings.bendOffsetMax);
            int by = bend.y + RandomRangeInt(rnd, -settings.bendOffsetMax, settings.bendOffsetMax);
            bend = new Vector2Int(bx, by);

            // clamp внутрь карты
            bend.x = Mathf.Clamp(bend.x, 0, settings.mapWidth - 1);
            bend.y = Mathf.Clamp(bend.y, 0, settings.mapHeight - 1);

            // строим сегменты
            WalkStraightAndMaybeDetours(corridors, start, bend, settings, rnd);
            WalkStraightAndMaybeDetours(corridors, bend, target, settings, rnd);

            // гарантия входа/выхода — небольшой «круг» вокруг центров комнат
            AddCorridorBlock(corridors, start, settings.corridorRadius);
            AddCorridorBlock(corridors, target, settings.corridorRadius);
        }

        return MergeCorridorRects(corridors, settings.mapWidth, settings.mapHeight);
    }

    private static void WalkStraightAndMaybeDetours(List<RectInt> corridors, Vector2Int a, Vector2Int b, DungeonSettings settings, System.Random rnd)
    {
        // определяем, вдоль какой оси основной сегмент (предпочитаем строго горизонтальные/вертикальные)
        if (a.y == b.y) // строго по X
        {
            int sx = a.x, ex = b.x;
            int step = (ex >= sx) ? 1 : -1;
            int counter = 0;
            for (int x = sx; x != ex + step; x += step)
            {
                var cell = new Vector2Int(x, a.y);
                AddCorridorBlock(corridors, cell, settings.corridorRadius);

                counter++;
                if (settings.detourInterval > 0 && counter % settings.detourInterval == 0 && rnd.NextDouble() < settings.detourProbability)
                {
                    int detourLen = RandomRangeInt(rnd, 1, settings.detourMaxLength);
                    int dirY = (rnd.NextDouble() < 0.5) ? 1 : -1;
                    // туда
                    for (int k = 1; k <= detourLen; k++)
                        AddCorridorBlock(corridors, new Vector2Int(x, a.y + k * dirY), settings.corridorRadius);
                    // назад (чтобы не ломать связность)
                    for (int k = detourLen; k >= 1; k--)
                        AddCorridorBlock(corridors, new Vector2Int(x, a.y + k * dirY), settings.corridorRadius);
                }
            }
        }
        else if (a.x == b.x) // строго по Y
        {
            int sy = a.y, ey = b.y;
            int step = (ey >= sy) ? 1 : -1;
            int counter = 0;
            for (int y = sy; y != ey + step; y += step)
            {
                var cell = new Vector2Int(a.x, y);
                AddCorridorBlock(corridors, cell, settings.corridorRadius);

                counter++;
                if (settings.detourInterval > 0 && counter % settings.detourInterval == 0 && rnd.NextDouble() < settings.detourProbability)
                {
                    int detourLen = RandomRangeInt(rnd, 1, settings.detourMaxLength);
                    int dirX = (rnd.NextDouble() < 0.5) ? 1 : -1;
                    for (int k = 1; k <= detourLen; k++)
                        AddCorridorBlock(corridors, new Vector2Int(a.x + k * dirX, y), settings.corridorRadius);
                    for (int k = detourLen; k >= 1; k--)
                        AddCorridorBlock(corridors, new Vector2Int(a.x + k * dirX, y), settings.corridorRadius);
                }
            }
        }
        else
        {
            // Если a и b не лежат строго по одной оси (редкий случай при смещении bend), просто двигаться по X, потом по Y
            var mid = new Vector2Int(b.x, a.y);
            WalkStraightAndMaybeDetours(corridors, a, mid, settings, rnd);
            WalkStraightAndMaybeDetours(corridors, mid, b, settings, rnd);
        }
    }

    private static void AddCorridorBlock(List<RectInt> corridors, Vector2Int center, int radius)
    {
        int r = Mathf.Max(0, radius);
        corridors.Add(new RectInt(center.x - r, center.y - r, r * 2 + 1, r * 2 + 1));
    }

    // Замените метод MergeCorridorRects на этот
    private static List<RectInt> MergeCorridorRects(List<RectInt> rects, int mapWidth, int mapHeight)
    {
        // 1) Создать булеву карту заполненности
        bool[,] filled = new bool[mapWidth, mapHeight];
        foreach (var r in rects)
        {
            int x0 = Mathf.Clamp(r.xMin, 0, mapWidth - 1);
            int y0 = Mathf.Clamp(r.yMin, 0, mapHeight - 1);
            int x1 = Mathf.Clamp(r.xMax - 1, 0, mapWidth - 1);
            int y1 = Mathf.Clamp(r.yMax - 1, 0, mapHeight - 1);

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    filled[x, y] = true;
        }

        var result = new List<RectInt>();

        // 2) Жадный проход по сетке: находим левый-верхний заполненный пиксель,
        //    растём вправо, а затем вниз столько, сколько возможно, чтобы сформировать прямоугольник,
        //    после чего очищаем эти клетки и повторяем.
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                if (!filled[x, y]) continue;

                // Определяем максимально возможую ширину от (x,y)
                int w = 0;
                while (x + w < mapWidth && filled[x + w, y]) w++;

                // Теперь ищём высоту: для h от 1.. пока каждая колонка ширины w заполнена на строке y+h-1
                int h = 1;
                bool canGrow = true;
                while (canGrow && (y + h) < mapHeight)
                {
                    for (int ix = 0; ix < w; ix++)
                    {
                        if (!filled[x + ix, y + h])
                        {
                            canGrow = false;
                            break;
                        }
                    }
                    if (canGrow) h++;
                }

                // У нас есть candidate rect (x, y, w, h) — но возможна оптимизация:
                // иногда стоит попробовать уменьшить w, чтобы получить большую h (опционально).
                // Для простоты используем найденные w и h.

                // Добавляем rect и очищаем клетки
                result.Add(new RectInt(x, y, w, h));
                for (int ix = 0; ix < w; ix++)
                    for (int iy = 0; iy < h; iy++)
                        filled[x + ix, y + iy] = false;
            }
        }

        return result;
    }


    private static bool RectsOverlapStrict(RectInt a, RectInt b)
    {
        return a.Overlaps(b);
    }

    private static RectInt UnionRect(RectInt a, RectInt b)
    {
        int xMin = Math.Min(a.xMin, b.xMin);
        int yMin = Math.Min(a.yMin, b.yMin);
        int xMax = Math.Max(a.xMax, b.xMax);
        int yMax = Math.Max(a.yMax, b.yMax);
        return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static int RandomRangeInt(System.Random rnd, int aInclusive, int bInclusive)
    {
        if (bInclusive < aInclusive) return aInclusive;
        return rnd.Next(aInclusive, bInclusive + 1);
    }
}
