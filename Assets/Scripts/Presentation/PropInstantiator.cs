using System;
using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation;
using Diploma.Generation.Model;

namespace Diploma.Presentation
{
    /// <summary>
    /// Создаёт GameObjects из SpawnPlan и BuildingData.
    /// Sorting via renderer.sortingOrder based on grid coordinates.
    /// Buildings use Alpha-Clipped transparent materials to respect sortingOrder.
    /// </summary>
    public static class PropInstantiator
    {
        public static void Instantiate(SpawnPlan spawnPlan, PrefabCatalog catalog, Tilemap tilemap, Transform parent = null, bool useYRotation = true, bool randomTreeRotation = false, TileLayer roads = null)
        {
            if (spawnPlan == null) return;
            if (catalog == null) return;
            if (tilemap == null) return;

            System.Random treeRng = randomTreeRotation ? new System.Random() : null;

            foreach (var entry in spawnPlan.Entries)
            {
                if (catalog.TryGetPrefab(entry.prefabId, out GameObject prefab))
                {
                    Vector3Int cellPos = new Vector3Int(entry.cellPos.x, entry.cellPos.y, -1);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

                    Quaternion finalRotation;
                    if (entry.prefabId == 1004 && roads != null)
                    {
                        finalRotation = CalculateBenchRotation(roads, entry.cellPos);
                    }
                    else
                    {
                        Quaternion prefabRotation = prefab.transform.rotation;
                        Quaternion rotationOffset;
                        if (useYRotation)
                        {
                            if (randomTreeRotation && treeRng != null && entry.prefabId >= 1000 && entry.prefabId < 1004)
                            {
                                float randomAngle = (float)(treeRng.NextDouble() * 360.0);
                                rotationOffset = Quaternion.Euler(0, randomAngle, 0);
                            }
                            else
                            {
                                rotationOffset = Quaternion.Euler(0, entry.rotationIndex * 90, 0);
                            }
                        }
                        else
                        {
                            rotationOffset = Quaternion.Euler(0, 0, entry.rotationIndex * 90);
                        }
                        finalRotation = prefabRotation * rotationOffset;
                    }

                    GameObject instance = GameObject.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Spawned_{entry.prefabId}_{entry.cellPos.x}_{entry.cellPos.y}";
                    instance.layer = 0;

                    int depth = entry.cellPos.x + entry.cellPos.y;
                    // База 30000: объекты с меньшим depth (ближе к камере) получают больший sortingOrder.
                    // У tav-tiles sortingOrder=0, поэтому 30000 гарантирует, что объекты будут выше.
                    int order = 30000 - depth * 100;
                    SetupSortingAndMaterial(instance, order);
                }
                else
                {
                    Debug.LogWarning($"Prefab ID {entry.prefabId} not found.");
                }
            }
        }

        public static void InstantiateBuildings(System.Collections.Generic.List<BuildingData> buildings, PrefabCatalog catalog, Tilemap tilemap, Transform parent = null, bool randomRotation = false)
        {
            if (buildings == null) return;
            if (catalog == null) return;
            if (tilemap == null) return;

            var rng = randomRotation ? new System.Random(42) : null;

            foreach (var building in buildings)
            {
                if (catalog.TryGetPrefab(building.prefabId, out GameObject prefab))
                {
                    Vector3Int cellPos = new Vector3Int(building.position.x, building.position.y, 0);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

                    Quaternion prefabRotation = prefab.transform.rotation;
                    int rotationAngle = randomRotation ? (rng?.Next(0, 4) ?? 0) * 90 : building.rotationIndex * 90;
                    Quaternion rotationOffset = Quaternion.Euler(0, rotationAngle, 0);
                    Quaternion finalRotation = prefabRotation * rotationOffset;

                    GameObject instance = GameObject.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Building_{building.id}_{building.type}";
                    instance.layer = 0;

                    int depth = building.position.x + building.position.y;
                    int order = 30000 - depth * 100;
                    SetupSortingAndMaterial(instance, order);
                }
                else
                {
                    Debug.LogWarning($"Building prefab ID {building.prefabId} not found.");
                }
            }
        }

        /// <summary>
        /// Настраивает sortingOrder и конвертирует материал в Cutout режим для корректной сортировки.
        /// Without SortingGroup, sets order per-renderer. Opaque -> Cutout conversion.
        /// </summary>
        private static void SetupSortingAndMaterial(GameObject root, int sortingOrder)
        {
            // Настраиваем все Renderer'ы
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                renderer.sortingLayerName = "Default";
                renderer.sortingOrder = sortingOrder;

                // Расширяем bounds для надёжности (как в оригинале)
                var localBounds = renderer.localBounds;
                renderer.localBounds = new Bounds(localBounds.center, localBounds.size * 1.5f);

                // Для MeshRenderer'ов конвертируем материал в Cutout режим,
                // чтобы они подчинялись sortingOrder (попадают в Transparent queue)
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    var mat = renderer.material; // instance, не затрагиваем asset
                    if (mat != null)
                    {
                        // Устанавливаем режим рендеринга в Cutout (Alpha Clipping)
                        // Standard shader: _Mode = 0(Opaque), 1(Cutout), 2(Fade), 3(Transparent)
                        mat.SetFloat("_Mode", 1f);      // Cutout
                        mat.SetFloat("_Cutoff", 0.5f);
                        mat.SetInt("_SrcBlend", 1);     // One (BlendMode.One)
                        mat.SetInt("_DstBlend", 0);     // Zero (BlendMode.Zero)
                        mat.SetInt("_ZWrite", 1);       // пишем в Z-buffer
                        mat.renderQueue = 3000; // Transparent queue (same as sprites)
                    }
                }
            }
        }

        private static Quaternion CalculateBenchRotation(TileLayer roads, Vector2Int cellPos)
        {
            bool hasHorizontalRoad = false;
            bool hasVerticalRoad = false;
            Vector2Int[] directions = { Vector2Int.left, Vector2Int.right, Vector2Int.up, Vector2Int.down };

            foreach (var dir in directions)
            {
                int nx = cellPos.x + dir.x;
                int ny = cellPos.y + dir.y;
                if (roads.InBounds(nx, ny) && roads.Get(nx, ny) == TileType.Road)
                {
                    if (dir.x != 0) hasHorizontalRoad = true;
                    else hasVerticalRoad = true;
                }
            }

            if (hasHorizontalRoad && !hasVerticalRoad)
            {
                return Quaternion.Euler(-20f, -45f, 20f);
            }
            else if (hasVerticalRoad && !hasHorizontalRoad)
            {
                return Quaternion.Euler(-20f, 50f, -20f);
            }
            else
            {
                return Quaternion.Euler(-20f, -45f, 20f);
            }
        }
    }
}
