using System;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    public sealed class StreetCarvingStep : IWorldGenStep
    {
        public string Key => "Streets";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var rng = seed.CreateRng(Key);

            if (world.Graph == null)
                throw new InvalidOperationException("StreetCarvingStep: world.Graph is null. Add DistrictGraphStep before StreetCarvingStep.");

            int radius = Mathf.Max(0, config.roadRadius);

            var nodes = world.Graph.Nodes;
            var edges = world.Graph.Edges;

            for (int i = 0; i < edges.Count; i++)
            {
                var e = edges[i];

                Vector2Int a = nodes[e.a].position;
                Vector2Int b = nodes[e.b].position;

                Vector2Int i1 = new Vector2Int(a.x, b.y);
                Vector2Int i2 = new Vector2Int(b.x, a.y);

                int score1 = EstimateOverlapScore(world, a, i1) + EstimateOverlapScore(world, i1, b);
                int score2 = EstimateOverlapScore(world, a, i2) + EstimateOverlapScore(world, i2, b);

                Vector2Int pivot;
                if (score1 > score2) pivot = i1;
                else if (score2 > score1) pivot = i2;
                else pivot = (rng.Next(0, 2) == 0) ? i1 : i2;

                CarveAxisAligned(world, a, pivot, radius);
                CarveAxisAligned(world, pivot, b, radius);
            }
        }

        private static int EstimateOverlapScore(WorldData world, Vector2Int from, Vector2Int to)
        {
            int score = 0;

            if (from.x == to.x)
            {
                int x = from.x;
                int y0 = from.y;
                int y1 = to.y;
                int step = y0 <= y1 ? 1 : -1;

                for (int y = y0; y != y1 + step; y += step)
                    if (world.Roads.InBounds(x, y) && world.Roads.Get(x, y) == TileType.Road) score++;
            }
            else if (from.y == to.y)
            {
                int y = from.y;
                int x0 = from.x;
                int x1 = to.x;
                int step = x0 <= x1 ? 1 : -1;

                for (int x = x0; x != x1 + step; x += step)
                    if (world.Roads.InBounds(x, y) && world.Roads.Get(x, y) == TileType.Road) score++;
            }
            else
            {

            }

            return score;
        }

        private static void CarveAxisAligned(WorldData world, Vector2Int from, Vector2Int to, int radius)
        {
            if (from == to)
            {
                CarveDiamond(world, from.x, from.y, radius);
                return;
            }

            if (from.x != to.x && from.y != to.y)
                throw new InvalidOperationException($"CarveAxisAligned requires axis-aligned points. from={from}, to={to}");

            if (from.x == to.x)
            {
                int x = from.x;
                int y0 = from.y;
                int y1 = to.y;
                int step = y0 <= y1 ? 1 : -1;

                for (int y = y0; y != y1 + step; y += step)
                    CarveDiamond(world, x, y, radius);
            }
            else
            {
                int y = from.y;
                int x0 = from.x;
                int x1 = to.x;
                int step = x0 <= x1 ? 1 : -1;

                for (int x = x0; x != x1 + step; x += step)
                    CarveDiamond(world, x, y, radius);
            }
        }

        private static void CarveDiamond(WorldData world, int cx, int cy, int radius)
        {
            if (radius <= 0)
            {
                if (world.Roads.InBounds(cx, cy))
                    world.Roads.Set(cx, cy, TileType.Road);
                return;
            }

            for (int dy = -radius; dy <= radius; dy++)
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (Mathf.Abs(dx) + Mathf.Abs(dy) > radius) continue;

                    int x = cx + dx;
                    int y = cy + dy;

                    if (!world.Roads.InBounds(x, y)) continue;
                    world.Roads.Set(x, y, TileType.Road);
                }
        }
    }
}