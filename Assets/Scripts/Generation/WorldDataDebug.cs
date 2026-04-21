using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation.Model;

namespace Diploma.Generation
{
    /// <summary>
    /// Компонент для визуализации графа районов в редакторе.
    /// Добавьте на пустой GameObject и назначьте WorldData.
    /// </summary>
    public class WorldDataDebug : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Данные мира для визуализации")]
        public WorldData worldData;

        [Tooltip("Tilemap для преобразования координат (изометрия)")]
        public Tilemap tilemap;

        [Header("Gizmos")]
        [Tooltip("Рисовать граф районов")]
        public bool drawGraph = true;
        
        [Tooltip("Рисовать здания")]
        public bool drawBuildings = true;
        
        [Tooltip("Рисовать позиции спавна")]
        public bool drawSpawnPoints = false;

        [Header("Colors")]
        public Color graphNodeColor = Color.green;
        public Color graphEdgeColor = Color.yellow;
        public Color buildingColor = Color.blue;
        public Color spawnPointColor = Color.red;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (worldData == null)
                return;

            // Если tilemap не назначен, пробуем найти на сцене
            if (tilemap == null)
            {
                var groundTilemap = FindFirstObjectByType<Tilemap>();
                if (groundTilemap != null && groundTilemap.name.Contains("Ground"))
                {
                    tilemap = groundTilemap;
                }
            }

            // Рисуем граф районов
            if (drawGraph && worldData.Graph != null)
            {
                Gizmos.color = graphEdgeColor;
                
                // Рёбра
                foreach (var edge in worldData.Graph.Edges)
                {
                    if (edge.a < worldData.Graph.Nodes.Count && edge.b < worldData.Graph.Nodes.Count)
                    {
                        var nodeA = worldData.Graph.Nodes[edge.a];
                        var nodeB = worldData.Graph.Nodes[edge.b];

                        Vector3 posA = CellToWorld(nodeA.position);
                        Vector3 posB = CellToWorld(nodeB.position);

                        Gizmos.DrawLine(posA, posB);
                    }
                }

                // Узлы
                Gizmos.color = graphNodeColor;
                foreach (var node in worldData.Graph.Nodes)
                {
                    Vector3 pos = CellToWorld(node.position);
                    Gizmos.DrawSphere(pos, 0.3f);
                }
            }

            // Рисуем здания
            if (drawBuildings && worldData.Buildings != null)
            {
                Gizmos.color = buildingColor;
                foreach (var building in worldData.Buildings)
                {
                    Vector3 bottomLeft = CellToWorld(building.position);
                    Vector3 topRight = CellToWorld(new Vector2Int(
                        building.position.x + building.width,
                        building.position.y + building.height
                    ));
                    
                    Vector3 center = (bottomLeft + topRight) / 2f;
                    Vector3 size = new Vector3(
                        Mathf.Abs(topRight.x - bottomLeft.x),
                        Mathf.Abs(topRight.y - bottomLeft.y),
                        1
                    );
                    
                    Gizmos.DrawWireCube(center, size);
                }
            }

            // Рисуем позиции спавна
            if (drawSpawnPoints && worldData.SpawnPlan != null)
            {
                Gizmos.color = spawnPointColor;
                foreach (var entry in worldData.SpawnPlan.Entries)
                {
                    Vector3 pos = CellToWorld(entry.cellPos);
                    Gizmos.DrawSphere(pos, 0.2f);
                }
            }
        }

        /// <summary>
        /// Преобразует координаты клетки в мировые координаты.
        /// Для Isometric Z as Y.
        /// </summary>
        private Vector3 CellToWorld(Vector2Int cellPos)
        {
            if (tilemap != null)
            {
                Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cellPos.x, cellPos.y, 0));
                return worldPos;
            }
            
            // Если tilemap не назначен, используем изометрическую формулу
            // Для Isometric Z as Y: x = (cellX - cellY) * cellSize.x/2, y = (cellX + cellY) * cellSize.y/2
            float isoX = (cellPos.x - cellPos.y) * 0.5f;
            float isoY = (cellPos.x + cellPos.y) * 0.25f;
            
            return new Vector3(isoX, isoY, 0);
        }
#endif
    }
}
