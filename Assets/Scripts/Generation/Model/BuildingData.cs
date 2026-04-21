using System;
using UnityEngine;

namespace Diploma.Generation.Model
{
    /// <summary>
    /// Данные здания: контур и информация о типах помещений.
    /// Хранит только данные, никаких Unity-объектов.
    /// </summary>
    [Serializable]
    public sealed class BuildingData
    {
        /// <summary>
        /// Уникальный ID здания в пределах мира.
        /// </summary>
        public int id;

        /// <summary>
        /// Позиция левого нижнего угла здания в клетках сетки.
        /// </summary>
        public Vector2Int position;

        /// <summary>
        /// Ширина здания в клетках.
        /// </summary>
        public int width;

        /// <summary>
        /// Высота здания в клетках.
        /// </summary>
        public int height;

        /// <summary>
        /// ID префаба здания для визуализации.
        /// </summary>
        public int prefabId;

        /// <summary>
        /// Поворот здания (0-3, умножать на 90 градусов).
        /// </summary>
        public int rotationIndex;

        /// <summary>
        /// Тип здания (жилое, коммерческое, промышленное и т.д.).
        /// </summary>
        public BuildingType type;

        /// <summary>
        /// Этажность.
        /// </summary>
        public int floors = 1;

        public BuildingData() { }

        public BuildingData(int id, Vector2Int pos, int w, int h, int prefabId, BuildingType type)
        {
            this.id = id;
            this.position = pos;
            this.width = w;
            this.height = h;
            this.prefabId = prefabId;
            this.type = type;
            this.rotationIndex = 0;
            this.floors = 1;
        }

        /// <summary>
        /// Прямоугольник здания в клетках.
        /// </summary>
        public RectInt Bounds => new RectInt(position.x, position.y, width, height);

        /// <summary>
        /// Проверяет, пересекается ли здание с другим прямоугольником.
        /// </summary>
        public bool Intersects(RectInt other) => Bounds.Overlaps(other);

        /// <summary>
        /// Проверяет, пересекается ли здание с другим зданием.
        /// </summary>
        public bool Intersects(BuildingData other) => Intersects(other.Bounds);
    }

    /// <summary>
    /// Типы зданий для категоризации.
    /// </summary>
    public enum BuildingType : byte
    {
        Residential = 0,    // Жилое
        Commercial = 1,     // Коммерческое
        Industrial = 2,     // Промышленное
        Public = 3,         // Общественное
        Special = 4         // Особое
    }
}
