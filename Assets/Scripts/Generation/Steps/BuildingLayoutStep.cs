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

            var roads = world.Roads;
            int buildingId = 0;

            // Три случая:
            // 1) Blocks не вычислены (null) — fallback для совместимости со старыми тестами
            // 2) Blocks вычислены и есть блоки — генерируем внутри кварталов
            // 3) Blocks вычислены, но пуст — районов нет, зданий не генерируем
            if (world.Blocks == null)
            {
                Debug.LogWarning("[BuildingLayoutStep] world.Blocks is null — using legacy whole-map generation (BlockLayoutStep missing?)");
                GenerateOnWholeMap(config, world, roads, ref buildingId, rng);
            }
            else if (world.Blocks.Count > 0)
            {
                GenerateInBlocks(config, world, roads, ref buildingId, rng);
            }
            else
            {
                Debug.LogWarning("[BuildingLayoutStep] No valid blocks found — no buildings will be generated.");
            }
        }

        /// <summary>
        /// Генерация зданий внутри кварталов.
        /// Здания прижимаются к границам квартала (дорогам) с минимальным зазором.
        /// </summary>
        private void GenerateInBlocks(WorldGenConfig config, WorldData world, TileLayer roads, ref int buildingId, System.Random rng)
        {
            int attemptsPerBlock = 30;
            int roadClearance = config.minBuildingGap;        // расстояние от дорог
            int buildingClearance = config.minBuildingDistance; // расстояние между зданиями

            foreach (var block in world.Blocks)
            {
                if (block.width < 3 || block.height < 3)
                    continue;

                var placedInBlock = new List<RectInt>();

                // Доступная ширина/высота interior с учётом зазоров от дорог
                int interiorWidth = block.width - 2 * roadClearance;
                int interiorHeight = block.height - 2 * roadClearance;

                if (interiorWidth < 2 || interiorHeight < 2)
                    continue; // блок слишком мал для любых зданий с учётом зазоров

                for (int attempt = 0; attempt < attemptsPerBlock; attempt++)
                {
                    // Случайный размер здания (2-5, но не больше interior)
                    int maxW = Mathf.Min(5, interiorWidth);
                    int maxH = Mathf.Min(5, interiorHeight);
                    int buildingWidth = rng.Next(2, maxW + 1);
                    int buildingHeight = rng.Next(2, maxH + 1);

                    // Случайная сторона примыкания (0=left, 1=right, 2=bottom, 3=top)
                    int side = rng.Next(0, 4);

                    int startX = 0, startY = 0;
                    bool fits = true;

                    switch (side)
                    {
                        case 0: // left flush
                            startX = block.xMin + roadClearance;
                            // Y может варьироваться
                            int maxOffsetY = interiorHeight - buildingHeight;
                            if (maxOffsetY < 0) fits = false;
                            else startY = block.yMin + roadClearance + rng.Next(0, maxOffsetY + 1);
                            break;
                        case 1: // right flush
                            startX = block.xMax - roadClearance - buildingWidth;
                            int maxOffsetY2 = interiorHeight - buildingHeight;
                            if (maxOffsetY2 < 0) fits = false;
                            else startY = block.yMin + roadClearance + rng.Next(0, maxOffsetY2 + 1);
                            break;
                        case 2: // bottom flush
                            startY = block.yMin + roadClearance;
                            int maxOffsetX = interiorWidth - buildingWidth;
                            if (maxOffsetX < 0) fits = false;
                            else startX = block.xMin + roadClearance + rng.Next(0, maxOffsetX + 1);
                            break;
                        case 3: // top flush
                            startY = block.yMax - roadClearance - buildingHeight;
                            int maxOffsetX2 = interiorWidth - buildingWidth;
                            if (maxOffsetX2 < 0) fits = false;
                            else startX = block.xMin + roadClearance + rng.Next(0, maxOffsetX2 + 1);
                            break;
                    }

                    if (!fits)
                        continue;

                    var buildingRect = new RectInt(startX, startY, buildingWidth, buildingHeight);

                    if (CanPlaceBuilding(roads, buildingRect, placedInBlock, buildingClearance, roadClearance, true))
                    {
                        int prefabId = SelectPrefabById(config.buildingPrefabIds, config.buildingPrefabWeights, rng);

                        var building = new BuildingData(
                            id: buildingId++,
                            pos: new Vector2Int(startX, startY),
                            w: buildingWidth,
                            h: buildingHeight,
                            prefabId: prefabId,
                            type: BuildingType.Residential
                        );

                        world.AddBuilding(building);
                        placedInBlock.Add(buildingRect);

                        MarkAsWall(world, startX, startY, buildingWidth, buildingHeight);
                    }
                }
            }
        }

        /// <summary>
        /// Старый алгоритм генерации по всей карте (fallback).
        /// </summary>
        private void GenerateOnWholeMap(WorldGenConfig config, WorldData world,
            TileLayer roads, ref int buildingId, System.Random rng)
        {
            int width = roads.Width;
            int height = roads.Height;
            int attemptsPerBlock = 50;
            var placedBuildings = new List<RectInt>();
            int buildingClearance = config.minBuildingDistance;

            for (int attempt = 0; attempt < attemptsPerBlock; attempt++)
            {
                int buildingWidth = rng.Next(2, 5);
                int buildingHeight = rng.Next(2, 5);

                int startX = rng.Next(1, width - buildingWidth - 1);
                int startY = rng.Next(1, height - buildingHeight - 1);

                var buildingRect = new RectInt(startX, startY, buildingWidth, buildingHeight);

                if (CanPlaceBuilding(roads, buildingRect, placedBuildings, buildingClearance, roadClearance: 0, enforceRoadClearance: false))
                {
                    int prefabId = SelectPrefabById(config.buildingPrefabIds, config.buildingPrefabWeights, rng);

                    var building = new BuildingData(
                        id: buildingId++,
                        pos: new Vector2Int(startX, startY),
                        w: buildingWidth,
                        h: buildingHeight,
                        prefabId: prefabId,
                        type: BuildingType.Residential // все здания одного типа
                    );

                    world.AddBuilding(building);
                    placedBuildings.Add(buildingRect);

                    MarkAsWall(world, startX, startY, buildingWidth, buildingHeight);
                }
            }
        }

        private void MarkAsWall(WorldData world, int startX, int startY, int w, int h)
        {
            for (int x = startX; x < startX + w; x++)
            {
                for (int y = startY; y < startY + h; y++)
                {
                    world.Walls.Set(x, y, TileType.Wall);
                }
            }
        }

        /// <summary>
        /// Проверяет, можно ли разместить здание в указанном месте.
        /// </summary>
        /// <param name="roads">Слой дорог</param>
        /// <param name="buildingRect">Прямоугольник здания</param>
        /// <param name="placedBuildings">Уже размещённые здания</param>
        /// <param name="buildingClearance">Минимальное расстояние до других зданий</param>
        /// <param name="roadClearance">Минимальное расстояние до дорог</param>
        /// <param name="enforceRoadClearance">Требовать ли проверку расстояния до дорог</param>
        private bool CanPlaceBuilding(TileLayer roads, RectInt buildingRect, List<RectInt> placedBuildings, int buildingClearance, int roadClearance, bool enforceRoadClearance)
        {
            int width = roads.Width;
            int height = roads.Height;

            // Проверка границ
            if (buildingRect.xMin < 0 || buildingRect.xMax > width ||
                buildingRect.yMin < 0 || buildingRect.yMax > height)
            {
                return false;
            }

            // Проверка на пересечение с дорогами внутри самого здания
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

            // Проверка на слишком близкое расположение к другим зданиям (с учётом зазора между зданиями)
            foreach (var placed in placedBuildings)
            {
                var expanded = new RectInt(
                    placed.xMin - buildingClearance,
                    placed.yMin - buildingClearance,
                    placed.width + 2 * buildingClearance,
                    placed.height + 2 * buildingClearance
                );

                if (buildingRect.Overlaps(expanded))
                {
                    return false;
                }
            }

            // Проверка минимального расстояния до дорог (требуется только для блоков)
            if (enforceRoadClearance)
            {
                var expandedForRoads = new RectInt(
                    buildingRect.xMin - roadClearance,
                    buildingRect.yMin - roadClearance,
                    buildingRect.width + 2 * roadClearance,
                    buildingRect.height + 2 * roadClearance
                );

                for (int x = expandedForRoads.xMin; x < expandedForRoads.xMax; x++)
                {
                    for (int y = expandedForRoads.yMin; y < expandedForRoads.yMax; y++)
                    {
                        if (!roads.InBounds(x, y))
                            continue;

                        if (roads.Get(x, y) == TileType.Road)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Выбирает ID префаба из списка с учётом весов.
        /// </summary>
        private int SelectPrefabById(int[] prefabIds, int[] weights, System.Random rng)
        {
            if (prefabIds == null || prefabIds.Length == 0)
                return 2000; // fallback

            if (prefabIds.Length == 1)
                return prefabIds[0];

            if (weights == null || weights.Length != prefabIds.Length)
            {
                return prefabIds[rng.Next(0, prefabIds.Length)];
            }

            int totalWeight = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                totalWeight += weights[i];
            }

            int randomValue = rng.Next(0, totalWeight);
            int cumulative = 0;

            for (int i = 0; i < prefabIds.Length; i++)
            {
                cumulative += weights[i];
                if (randomValue < cumulative)
                {
                    return prefabIds[i];
                }
            }

            return prefabIds[prefabIds.Length - 1];
        }
    }
}
