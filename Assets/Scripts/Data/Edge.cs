using System;

[Serializable]
public struct Edge : IEquatable<Edge>
{
    public int a;
    public int b;

    public Edge(int a, int b)
    {
        this.a = a;
        this.b = b;
    }

    public bool Equals(Edge other)
    {
        return (a == other.a && b == other.b) || (a == other.b && b == other.a);
    }

    public override bool Equals(object obj)
    {
        return obj is Edge other && Equals(other);
    }

    public override int GetHashCode()
    {
        int min = Math.Min(a, b);
        int max = Math.Max(a, b);
        unchecked
        {
            return (min * 397) ^ max;
        }
    }
}
