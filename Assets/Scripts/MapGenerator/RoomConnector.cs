using System;
using System.Collections.Generic;
using UnityEngine;

public static class DrunkardWalkConnector
{
    public static List<RectInt> Connect(RoomLayout layout, DungeonSettings settings)
    {
        var corridors = new List<RectInt>();
        var edges = layout.Graph.Edges;
        var rnd = new System.Random(settings.seed + 123456);

        foreach (var edge in edges)
        {
            Vector2 startF = layout.Rooms[edge.a].center;
            Vector2 targetF = layout.Rooms[edge.b].center;
            Vector2 posF = startF;

            AddCorridorBlock(corridors, startF, settings.corridorRadius);

            for (int step = 0; step < settings.drunkardWalkLength; step++)
            {
                Vector2 toTarget = (targetF - posF).normalized;
                float angle = ((float)rnd.NextDouble() * 2f - 1f) * settings.drunkardTurnAngle;
                toTarget = Quaternion.Euler(0, 0, angle) * toTarget;

                posF.x = Mathf.Clamp(posF.x + Mathf.Sign(toTarget.x), 0, settings.mapWidth - 1);
                posF.y = Mathf.Clamp(posF.y + Mathf.Sign(toTarget.y), 0, settings.mapHeight - 1);

                AddCorridorBlock(corridors, posF, settings.corridorRadius);
            }
            AddCorridorBlock(corridors, targetF, settings.corridorRadius);
        }

        return corridors;
    }

    private static void AddCorridorBlock(List<RectInt> list, Vector2 posF, int r)
    {
        int cx = Mathf.RoundToInt(posF.x);
        int cy = Mathf.RoundToInt(posF.y);
        list.Add(new RectInt(cx - r, cy - r, 2 * r + 1, 2 * r + 1));
    }
}
