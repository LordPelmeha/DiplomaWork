using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diploma.Generation.Model
{
    [Serializable]
    public sealed class DistrictGraph
    {
        [Serializable]
        public struct Node
        {
            public int id;
            public Vector2Int position;
        }

        [Serializable]
        public struct Edge
        {
            public int a;
            public int b;
            public int weightSqr;
        }

        public List<Node> Nodes = new();
        public List<Edge> Edges = new();
    }
}