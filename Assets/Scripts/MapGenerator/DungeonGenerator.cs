using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private DungeonSettings settings;
    private RoomLayout layout;
    private DungeonSettings runtimeSettings;

    private void Awake()
    {
        runtimeSettings = Instantiate(settings);
        if (!runtimeSettings.isSeedConstant)
            runtimeSettings.seed = DateTime.Now.GetHashCode();
        runtimeSettings.groundTilemap = GameObject.Find("Ground").GetComponent<Tilemap>();
        runtimeSettings.wallTilemap = GameObject.Find("Wall").GetComponent<Tilemap>();

        GenerateDungeon(runtimeSettings);
        Debug.Log("Awake");
    }

    void Start()
    {
        var rng = new System.Random(runtimeSettings.seed);

        float r = (float)rng.NextDouble();
        float g = (float)rng.NextDouble();
        float b = (float)rng.NextDouble();

        Camera.main.backgroundColor = new Color(r, g, b);
        Debug.Log("cameraColor");
    }

    public void GenerateDungeon(DungeonSettings runtimeSettings)
    {
        bool isValid = false;
        while (!isValid)
        {
            // Генерируем граф и комнаты
            var graph = GraphGenerator.Generate(runtimeSettings);
            layout = RoomPlacer.Place(graph, runtimeSettings);
            Debug.Log(1);
            // 2) Генерируем коридоры
            var corridors = DrunkardWalkConnector.Connect(layout, runtimeSettings);
            layout.SetCorridors(corridors);
            Debug.Log(2);
            // 3) Пост-обработка карты
            PostProcessor.Process(layout, runtimeSettings);
            Debug.Log(3);
            // 4) Проверяем уровень
            isValid = DungeonValidator.Validate(layout);
            if (!isValid)
            {
                runtimeSettings.seed++;
                continue;
            }
            Debug.Log(4);
            // 5) Строим Tilemap
            TilemapBuilder.Build(layout, corridors);
            Debug.Log(5);
            // 6) Спавним игрока и выход
            GameplayPlacer.Place(layout, runtimeSettings);
            Debug.Log(6);
        }
        Debug.Log(7);
    }
}
