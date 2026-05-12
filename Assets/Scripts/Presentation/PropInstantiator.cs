using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation;
using Diploma.Generation.Model;

namespace Diploma.Presentation
{
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
                    Vector3Int cellPos = new Vector3Int(entry.cellPos.x, entry.cellPos.y, 0);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                    // Z stays 0; use sortingOrder/renderQueue for depth ordering.

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

                    GameObject instance = Object.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Spawned_{entry.prefabId}_{entry.cellPos.x}_{entry.cellPos.y}";
                    instance.layer = 0;

                    // Y-based ordering: lower Y (smaller) -> higher sortingOrder and renderQueue
                    // Factor 10 gives sufficient separation across map (Y~150 -> diff ~1500)
                    int yOrder = -Mathf.FloorToInt(worldPos.y * 10);
                    Setup3DObjectSorting(instance, 3000, yOrder);
                }
                else
                {
                    Debug.LogWarning($"Prefab ID {entry.prefabId} not found.");
                }
            }
        }

        private static void Setup3DObjectSorting(GameObject obj, int baseSortingOrder, int renderQueueOffset)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (var renderer in renderers)
            {
                // sortingOrder for 2D renderers
                renderer.sortingOrder = baseSortingOrder + renderQueueOffset;

                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                {
                    var materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        var mat = materials[i];
                        if (mat != null)
                        {
                            // Higher renderQueue for lower Y (smaller Y) -> draws later (on top)
                            mat.renderQueue = 5000 + renderQueueOffset;
                        }
                    }
                    renderer.materials = materials;
                }

                // Expand bounds slightly
                var localBounds = renderer.localBounds;
                renderer.localBounds = new Bounds(localBounds.center, localBounds.size * 1.5f);
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
                    // Z = 0

                    Quaternion prefabRotation = prefab.transform.rotation;
                    int rotationAngle = randomRotation ? (rng?.Next(0, 4) ?? 0) * 90 : building.rotationIndex * 90;
                    Quaternion rotationOffset = Quaternion.Euler(0, rotationAngle, 0);
                    Quaternion finalRotation = prefabRotation * rotationOffset;

                    GameObject instance = Object.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Building_{building.id}_{building.type}";
                    instance.layer = 0;

                    int yOrder = -Mathf.FloorToInt(worldPos.y * 10);
                    Setup3DObjectSorting(instance, 3000, yOrder);
                }
                else
                {
                    Debug.LogWarning($"Building prefab ID {building.prefabId} not found.");
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
                return Quaternion.Euler(-25f, -85f, 40f);
            }
            else if (hasVerticalRoad && !hasHorizontalRoad)
            {
                return Quaternion.Euler(25f, -85f, 50f);
            }
            else
            {
                return Quaternion.Euler(-25f, -85f, 40f);
            }
        }
    }
}
