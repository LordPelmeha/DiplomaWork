using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RoomGraph
{
    public List<Vector2Int> Nodes { get; }

    public List<Edge> Edges { get; }

    public RoomGraph(List<Vector2Int> nodes, List<Edge> edges)
    {
        Nodes = nodes;
        Edges = edges;
    }
}
