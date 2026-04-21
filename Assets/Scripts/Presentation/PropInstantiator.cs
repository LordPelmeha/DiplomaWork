using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation;
using Diploma.Generation.Model;

namespace Diploma.Presentation
{
    /// <summary>
    /// Инстанс префабов на сцене из плана спавна.
    /// Презентационный слой: работает с Unity-объектами.
    /// </summary>
    public static class PropInstantiator
    {
        /// <summary>
        /// Размещает объекты на сцене согласно плану спавна.
        /// </summary>
        /// <param name="spawnPlan">План спавна</param>
        /// <param name="catalog">Каталог префабов</param>       
        /// <param name="tilemap">Tilemap для правильного преобразования координат</param>
        /// <param name="parent">Родительский объект (опционально)</param>
        /// <param name="useYRotation">Вращать вокруг Y оси (для 3D объектов в изометрии)</param>
        /// <param name="randomTreeRotation">Случайный поворот для деревьев (не детерминировано)</param>
        /// <param name="roads">Слой дорог (для определения поворота скамеек)</param>
        public static void Instantiate(SpawnPlan spawnPlan, PrefabCatalog catalog, Tilemap tilemap, Transform parent = null, bool useYRotation = true, bool randomTreeRotation = false, TileLayer roads = null)
        {
            if (spawnPlan == null) return;
            if (catalog == null) return;
            if (tilemap == null) return;

            // RNG для случайного поворота деревьев (не детерминированный)
            System.Random treeRng = randomTreeRotation ? new System.Random() : null;

            foreach (var entry in spawnPlan.Entries)
            {
                if (catalog.TryGetPrefab(entry.prefabId, out GameObject prefab))
                {
                    Vector3Int cellPos = new Vector3Int(entry.cellPos.x, entry.cellPos.y, 0);
                    Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);

                    // Комбинируем ротацию префаба с нашей коррекцией
                    Quaternion finalRotation;
                    
                    if (entry.prefabId == 1004 && roads != null)
                    {
                        // Скамейки (ID 1004) - используем АБСОЛЮТНЫЙ поворот, не комбинируя с префабом
                        finalRotation = CalculateBenchRotation(roads, entry.cellPos);
                    }
                    else
                    {
                        // Берём ротацию из префаба и добавляем нашу коррекцию
                        Quaternion prefabRotation = prefab.transform.rotation;

                        // Для 3D объектов в изометрии вращаем вокруг Y оси
                        // rotationIndex: 0=0°, 1=90°, 2=180°, 3=270°
                        Quaternion rotationOffset;
                        if (useYRotation)
                        {
                            // Если случайный поворот для деревьев - добавляем случайность
                            if (randomTreeRotation && treeRng != null && entry.prefabId >= 1000 && entry.prefabId < 1004)
                            {
                                // Деревья (ID 1000-1003) получают полностью случайный поворот
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

                        // Комбинируем ротацию префаба с нашей коррекцией
                        finalRotation = prefabRotation * rotationOffset;
                    } 

                    GameObject instance = Object.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Spawned_{entry.prefabId}_{entry.cellPos.x}_{entry.cellPos.y}";
                    
                    // Для скамеек (3D объекты) настраиваем правильную сортировку с тайлмапом
                    if (entry.prefabId == 1004)
                    {
                        Setup3DObjectSorting(instance, 3000); // Order in Layer выше тайлмапа
                    }
                }
                else
                {
                    Debug.LogWarning($"PropInstantiator: Prefab with ID {entry.prefabId} not found in catalog.");
                }
            }
        }

        /// <summary>
        /// Вычисляет поворот скамейки на основе направления ближайшей дороги.
        /// </summary>
        private static Quaternion CalculateBenchRotation(TileLayer roads, Vector2Int cellPos)
        {
            // Определяем направление дороги рядом со скамейкой
            bool hasHorizontalRoad = false;
            bool hasVerticalRoad = false;

            // Проверяем соседние клетки
            Vector2Int[] directions = {
                Vector2Int.left, Vector2Int.right,
                Vector2Int.up, Vector2Int.down
            };

            foreach (var dir in directions)
            {
                int nx = cellPos.x + dir.x;
                int ny = cellPos.y + dir.y;

                if (roads.InBounds(nx, ny) && roads.Get(nx, ny) == TileType.Road)
                {
                    if (dir.x != 0) // Горизонтальная дорога
                        hasHorizontalRoad = true;
                    else // Вертикальная дорога
                        hasVerticalRoad = true;
                }
            }

            // Определяем поворот на основе направления дороги
            if (hasHorizontalRoad && !hasVerticalRoad)
            {
                // Горизонтальная дорога: поворот x=-25, y=85, z=40
                return Quaternion.Euler(-25f, -85f, 40f);
            }
            else if (hasVerticalRoad && !hasHorizontalRoad)
            {
                // Вертикальная дорога: поворот x=25, y=-85, z=50
                return Quaternion.Euler(25f, -85f, 50f);
            }
            else
            {
                // Перекрёсток или неясно - используем горизонтальный по умолчанию
                return Quaternion.Euler(-25f, -85f, 40f);
            }
        }

        /// <summary>
        /// Настраивает правильную сортировку для 3D объектов в изометрической сцене.
        /// </summary>
        private static void Setup3DObjectSorting(GameObject obj, int orderInLayer)
        {
            // Находим все MeshRenderer на объекте
            var renderers = obj.GetComponentsInChildren<MeshRenderer>();
            
            foreach (var renderer in renderers)
            {
                // Устанавливаем порядок сортировки выше тайлмапов
                renderer.sortingOrder = orderInLayer;
                
                // Важно: расширяем bounds для предотвращения frustum culling
                var bounds = renderer.bounds;
                renderer.localBounds = new Bounds(
                    renderer.transform.InverseTransformPoint(bounds.center),
                    bounds.size * 2f // Увеличиваем bounds в 2 раза
                );
            }
        }

        /// <summary>
        /// Размещает здания на сцене.
        /// </summary>
        /// <param name="buildings">Список зданий</param>
        /// <param name="catalog">Каталог префабов</param>
        /// <param name="tilemap">Tilemap для правильного преобразования координат</param>
        /// <param name="parent">Родительский объект (опционально)</param>
        /// <param name="randomRotation">Случайный поворот для разнообразия</param>
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
                    
                    // Берём ротацию из префаба
                    Quaternion prefabRotation = prefab.transform.rotation;
                    
                    // Вращаем вокруг Y оси для 3D объектов
                    int rotationAngle = randomRotation 
                        ? (rng?.Next(0, 4) ?? 0) * 90 
                        : building.rotationIndex * 90;
                    
                    Quaternion rotationOffset = Quaternion.Euler(0, rotationAngle, 0);
                    
                    // Комбинируем ротацию префаба с нашей коррекцией
                    Quaternion finalRotation = prefabRotation * rotationOffset;

                    GameObject instance = Object.Instantiate(prefab, worldPos, finalRotation, parent);
                    instance.name = $"Building_{building.id}_{building.type}";
                }
                else
                {
                    Debug.LogWarning($"PropInstantiator: Building prefab with ID {building.prefabId} not found.");
                }
            }
        }
    }
}
