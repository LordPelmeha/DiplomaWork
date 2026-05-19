using System;
using UnityEngine;

namespace Diploma.Generation.Model
{
    [Serializable]
    public sealed class TileLayer
    {
        [SerializeField] private int _width;
        [SerializeField] private int _height;

        [SerializeField] private TileType[] _cells;

        public int Width => _width;
        public int Height => _height;

        public TileLayer(int width, int height, TileType initial = TileType.Empty)
        {
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

            _width = width;
            _height = height;
            _cells = new TileType[width * height];

            Fill(initial);
        }

        public bool InBounds(int x, int y)
            => (uint)x < (uint)_width && (uint)y < (uint)_height;

        public int ToIndex(int x, int y) => x + y * _width;

        public TileType Get(int x, int y)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException($"Out of bounds: {x},{y}");
            return _cells[ToIndex(x, y)];
        }

        public void Set(int x, int y, TileType value)
        {
            if (!InBounds(x, y)) throw new ArgumentOutOfRangeException($"Out of bounds: {x},{y}");
            _cells[ToIndex(x, y)] = value;
        }

        public TileType Get(Vector2Int p) => Get(p.x, p.y);
        public void Set(Vector2Int p, TileType value) => Set(p.x, p.y, value);

        public void Fill(TileType value)
        {
            Array.Fill(_cells, value);
        }

        public TileType[] GetRawCells() => _cells;

        public bool HasRoads()
        {
            foreach (var cell in _cells)
                if (cell == TileType.Road) return true;
            return false;
        }
    }
}