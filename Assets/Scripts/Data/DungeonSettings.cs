using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "DungeonSettings", menuName = "Generation Settings")]
public class DungeonSettings : ScriptableObject
{
    [Header("Map Dimensions")]
    public int mapWidth = 100;
    public int mapHeight = 70;

    [Header("Graph Generation")]
    public int roomCount = 20;
    public int neighborCount = 2;
    public int seed = 12345;
    public bool isSeedConstant = false;

    [Header("Room Placement")]
    public int roomMinSize = 5;
    public int roomMaxSize = 15;
    public int maxRoomPlacementAttempts = 50;

    [Header("Corridor Generation (L-shaped)")]
    [Range(0f, 1f)]
    public float preferHorizontalFirst = 0.5f; 
    public int bendOffsetMax = 2;               
    [Tooltip("Вероятность в каждой detourInterval клетке вставить короткое ответвление")]
    [Range(0f, 1f)]
    public float detourProbability = 0.12f;
    public int detourInterval = 6;              
    public int detourMaxLength = 3;             
    public int corridorRadius = 1;


    [Header("Post-Processing")]
    public int iterations = 3;
    public int birthLimit = 4;
    public int deathLimit = 3;

    [Header("Tilemaps & Tiles")]
    public Tilemap groundTilemap;
    public Tilemap wallTilemap;
    public TileBase groundTile;
    public TileBase wallTile;

    [Header("Gameplay Prefabs")]
    public GameObject playerPrefab;
    public GameObject exitPrefab;
}
