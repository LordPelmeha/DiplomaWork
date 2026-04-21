using System;
using System.Collections.Generic;
using UnityEngine;

namespace Diploma.Generation.Model
{
    /// <summary>
    /// План спавна объектов: что и где разместить на сцене.
    /// Содержит только данные, без Unity-объектов.
    /// </summary>
    [Serializable]
    public sealed class SpawnPlan
    {
        /// <summary>
        /// Список объектов для спавна.
        /// </summary>
        public List<SpawnEntry> Entries = new List<SpawnEntry>();

        /// <summary>
        /// Добавляет запись в план спавна.
        /// </summary>
        public void Add(int prefabId, Vector2Int cellPos, int rotationIndex = 0, object metadata = null)
        {
            Entries.Add(new SpawnEntry
            {
                prefabId = prefabId,
                cellPos = cellPos,
                rotationIndex = rotationIndex,
                metadata = metadata
            });
        }

        /// <summary>
        /// Очищает план спавна.
        /// </summary>
        public void Clear() => Entries.Clear();

        /// <summary>
        /// Количество объектов в плане.
        /// </summary>
        public int Count => Entries.Count;
    }

    /// <summary>
    /// Запись в плане спавна: один объект для размещения.
    /// </summary>
    [Serializable]
    public struct SpawnEntry
    {
        /// <summary>
        /// ID префаба для спавна.
        /// </summary>
        public int prefabId;

        /// <summary>
        /// Позиция в клетках сетки.
        /// </summary>
        public Vector2Int cellPos;

        /// <summary>
        /// Поворот (0-3, умножать на 90 градусов).
        /// </summary>
        public int rotationIndex;

        /// <summary>
        /// Дополнительные данные (опционально).
        /// </summary>
        public object metadata;
    }

    /// <summary>
    /// Типы объектов для спавна.
    /// </summary>
    public enum SpawnType : byte
    {
        Prop = 0,           // Декорация (дерево, фонарь)
        Building = 1,       // Здание
        Vehicle = 2,        // Транспорт
        Character = 3,      // Персонаж
        Interaction = 4     // Интерактивный объект
    }
}
