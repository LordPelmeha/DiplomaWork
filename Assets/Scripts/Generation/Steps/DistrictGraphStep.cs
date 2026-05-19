using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    public sealed class DistrictGraphStep : IWorldGenStep
    {
        public string Key => "Graph";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (world == null) throw new ArgumentNullException(nameof(world));

            var rng = seed.CreateRng(Key);

            int n = Mathf.Max(1, config.districtCount);
            var size = world.Size;

            int margin = Mathf.Max(0, config.districtMargin);
            int minDist = Mathf.Max(0, config.districtMinDistance);
            int minDistSqr = minDist * minDist;

            int gridStep = Mathf.Max(1, config.districtGridStep);
            int maxDegree = Mathf.Clamp(config.maxNodeDegree, 1, 4);

            int minX = margin;
            int maxX = size.x - margin;
            int minY = margin;
            int maxY = size.y - margin;

            if (minX >= maxX || minY >= maxY)
                throw new InvalidOperationException($"Invalid district placement range. mapSize={size}, margin={margin}");

            var graph = new DistrictGraph();
            graph.Nodes.Capacity = n;

            const int MaxAttemptsPerNode = 300;

            for (int id = 0; id < n; id++)
            {
                Vector2Int chosen = default;

                for (int attempt = 0; attempt < MaxAttemptsPerNode; attempt++)
                {
                    int x = rng.Next(minX, maxX);
                    int y = rng.Next(minY, maxY);

                    x = Snap(x, gridStep, minX, maxX - 1);
                    y = Snap(y, gridStep, minY, maxY - 1);

                    var candidate = new Vector2Int(x, y);

                    if (minDistSqr <= 0)
                    {
                        chosen = candidate;
                        break;
                    }

                    bool ok = true;
                    for (int i = 0; i < graph.Nodes.Count; i++)
                    {
                        var p = graph.Nodes[i].position;
                        int dx = candidate.x - p.x;
                        int dy = candidate.y - p.y;
                        int ds = dx * dx + dy * dy;
                        if (ds < minDistSqr) { ok = false; break; }
                    }

                    if (ok)
                    {
                        chosen = candidate;
                        break;
                    }

                    chosen = candidate;
                }

                graph.Nodes.Add(new DistrictGraph.Node { id = id, position = chosen });
            }

            var candidates = new List<EdgeCandidate>(n * (n - 1) / 2);
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                {
                    var a = graph.Nodes[i].position;
                    var b = graph.Nodes[j].position;

                    int dx = a.x - b.x;
                    int dy = a.y - b.y;
                    int w = dx * dx + dy * dy;

                    candidates.Add(new EdgeCandidate(i, j, w));
                }

            candidates.Sort(EdgeCandidateComparer.Instance);

            var dsu = new DisjointSetUnion(n);
            var mstEdges = new List<EdgeCandidate>(n - 1);

            for (int k = 0; k < candidates.Count && mstEdges.Count < n - 1; k++)
            {
                var e = candidates[k];
                if (dsu.Union(e.A, e.B))
                    mstEdges.Add(e);
            }

            var degree = new int[n];

            foreach (var e in mstEdges)
            {
                graph.Edges.Add(new DistrictGraph.Edge { a = e.A, b = e.B, weightSqr = e.W });
                degree[e.A]++;
                degree[e.B]++;
            }

            int extra = Mathf.Max(0, config.extraEdges);
            if (extra > 0)
            {
                var used = new HashSet<(int, int)>();
                foreach (var e in mstEdges)
                    used.Add(NormPair(e.A, e.B));

                var remaining = new List<EdgeCandidate>(candidates.Count - mstEdges.Count);
                for (int i = 0; i < candidates.Count; i++)
                {
                    var e = candidates[i];
                    if (!used.Contains(NormPair(e.A, e.B)))
                        remaining.Add(e);
                }

                int attempts = 0;
                int maxAttempts = extra * 20 + 20;

                while (extra > 0 && remaining.Count > 0 && attempts < maxAttempts)
                {
                    attempts++;

                    int idx = rng.Next(0, remaining.Count);
                    var e = remaining[idx];

                    if (degree[e.A] < maxDegree && degree[e.B] < maxDegree)
                    {
                        graph.Edges.Add(new DistrictGraph.Edge { a = e.A, b = e.B, weightSqr = e.W });
                        degree[e.A]++;
                        degree[e.B]++;
                        extra--;
                    }

                    remaining.RemoveAt(idx);
                }
            }

            world.Graph = graph;
        }

        private static int Snap(int v, int step, int min, int max)
        {
            if (step <= 1) return Mathf.Clamp(v, min, max);

            int snapped = Mathf.RoundToInt(v / (float)step) * step;
            return Mathf.Clamp(snapped, min, max);
        }

        private static (int, int) NormPair(int a, int b) => a < b ? (a, b) : (b, a);

        private readonly struct EdgeCandidate
        {
            public int A { get; }
            public int B { get; }
            public int W { get; }

            public EdgeCandidate(int a, int b, int w) { A = a; B = b; W = w; }
        }

        private sealed class EdgeCandidateComparer : IComparer<EdgeCandidate>
        {
            public static readonly EdgeCandidateComparer Instance = new();
            public int Compare(EdgeCandidate x, EdgeCandidate y)
            {
                int c = x.W.CompareTo(y.W);
                if (c != 0) return c;
                c = x.A.CompareTo(y.A);
                if (c != 0) return c;
                return x.B.CompareTo(y.B);
            }
        }

        private sealed class DisjointSetUnion
        {
            private readonly int[] _parent;
            private readonly byte[] _rank;

            public DisjointSetUnion(int n)
            {
                _parent = new int[n];
                _rank = new byte[n];
                for (int i = 0; i < n; i++) _parent[i] = i;
            }

            private int Find(int x)
            {
                while (_parent[x] != x)
                {
                    _parent[x] = _parent[_parent[x]];
                    x = _parent[x];
                }
                return x;
            }

            public bool Union(int a, int b)
            {
                int ra = Find(a);
                int rb = Find(b);
                if (ra == rb) return false;

                if (_rank[ra] < _rank[rb]) _parent[ra] = rb;
                else if (_rank[ra] > _rank[rb]) _parent[rb] = ra;
                else { _parent[rb] = ra; _rank[ra]++; }

                return true;
            }
        }
    }
}