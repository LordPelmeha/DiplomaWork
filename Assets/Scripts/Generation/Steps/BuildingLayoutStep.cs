using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using UnityEngine;

namespace Diploma.Generation.Steps
{
    /// <summary>
    /// Генерация зданий внутри кварталов.
    /// Размещает здания в данных, не создавая Unity-объекты.
    /// </summary>
    public sealed class BuildingLayoutStep : IWorldGenStep
    {
        public string Key => "Buildings";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            var rng = seed.CreateRng(Key);

            if (world.Roads == null)
                throw new InvalidOperationException("BuildingLayoutStep: world.Roads is null.");

            // Получаем кварталы (пока просто генерируем здания на свободных местах)
            // В будущем можно использовать данные из BlockLayoutStep

            var roads = world.Roads;
            int width = roads.Width;
            int height = roads.Height;

            int buildingId = 0;
            int attemptsPerBlock = 50;

            // Проходим по карте и ищем места для зданий
            // Простая эвристика: случайные прямоугольники вдали от дорог
            var placedBuildings = new List<RectInt>();

            for (int attempt = 0; attempt < attemptsPerBlock; attempt++)
            {
                // Случайный размер здания
                int buildingWidth = rng.Next(2, 5);
                int buildingHeight = rng.Next(2, 5);

                // Случайная позиция
                int startX = rng.Next(1, width - buildingWidth - 1);
                int startY = rng.Next(1, height - buildingHeight - 1);

                var buildingRect = new RectInt(startX, startY, buildingWidth, buildingHeight);

                // Проверяем, можно ли разместить здание
                if (CanPlaceBuilding(roads, buildingRect, placedBuildings))
                {
                    // Выбираем случайный тип здания
                    var buildingType = (BuildingType)rng.Next(0, Enum.GetValues(typeof(BuildingType)).Length);

                    // Создаём данные здания
                    var building = new BuildingData(
                        id: buildingId++,
                        pos: new Vector2Int(startX, startY),
                        w: buildingWidth,
                        h: buildingHeight,
                        prefabId: GetPrefabIdForType(buildingType, rng),
                        type: buildingType
                    );

                    // Добавляем в мир
                    world.AddBuilding(building);
                    placedBuildings.Add(buildingRect);

                    // Помечаем клетки как занятые (стены зданий)
                    for (int x = startX; x < startX + buildingWidth; x++)
                    {
                        for (int y = startY; y < startY + buildingHeight; y++)
                        {
                            world.Walls.Set(x, y, TileType.Wall);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, можно ли разместить здание в указанном месте.
        /// </summary>
        private bool CanPlaceBuilding(TileLayer roads, RectInt buildingRect, List<RectInt> placedBuildings)
        {
            int width = roads.Width;
            int height = roads.Height;

            // Проверка границ
            if (buildingRect.xMin < 0 || buildingRect.xMax > width ||
                buildingRect.yMin < 0 || buildingRect.yMax > height)
            {
                return false;
            }

            // Проверка на пересечение с дорогами
            for (int x = buildingRect.xMin; x < buildingRect.xMax; x++)
            {
                for (int y = buildingRect.yMin; y < buildingRect.yMax; y++)
                {
                    if (roads.Get(x, y) == TileType.Road)
                    {
                        return false;
                    }
                }
            }

            // Проверка на пересечение с другими зданиями
            foreach (var placed in placedBuildings)
            {
                if (buildingRect.Overlaps(placed))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Возвращает ID префаба для типа здания.
        /// </summary>
        private int GetPrefabIdForType(BuildingType type, System.Random rng)
        {
            // Простая мапа: тип здания -> диапазон ID префабов
            // В реальном проекте это должно быть в PrefabCatalog
            int baseId = (int)type * 100;
            return baseId + rng.Next(0, 10);
        }
    }
}
