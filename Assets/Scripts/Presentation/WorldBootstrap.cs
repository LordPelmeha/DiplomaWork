using UnityEngine;
using UnityEngine.Tilemaps;
using Diploma.Generation;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;
using Diploma.Generation.Steps;

namespace Diploma.Presentation
{
    public sealed class WorldBootstrap : MonoBehaviour
    {
        [Header("Config")]
        public WorldGenConfig config;

        [Header("Seed")]
        public bool useRandomSeed = false;
        public int seed = 12345;
        
        [Tooltip("Использовать seed из PlayerPrefs (устанавливается в главном меню)")]
        public bool usePlayerPrefsSeed = true;

        [Header("Tiles")]
        public TileAssetSet tileAssetSet;

        [Header("Tilemaps (layers)")]
        public Tilemap groundTilemap;
        public Tilemap roadsTilemap;
        public Tilemap wallsTilemap;

        [Header("Spawn")]
        public PrefabCatalog prefabCatalog;
        public Transform spawnedObjectsParent;
        
        [Header("Spawn Options")]
        [Tooltip("Вращать объекты вокруг Y оси (для 3D в изометрии)")]
        public bool useYRotation = true;
        [Tooltip("Случайный поворот зданий для разнообразия")]
        public bool randomBuildingRotation = false;
        [Tooltip("Случайный поворот деревьев для разнообразия (не детерминировано)")]
        public bool randomTreeRotation = true;
        
        [Header("Debug")]
        [Tooltip("Сохранить WorldData для отладки")]
        public bool saveWorldDataForDebug = false;
        [Tooltip("GameObject для отладки (назначить WorldDataDebug)")]
        public WorldDataDebug worldDataDebug;
        
        [Header("Generation Options")]
        [Tooltip("Максимальное количество попыток генерации (0 = без ограничений)")]
        [Min(0)] public int maxGenerationAttempts = 3;

        private WorldGenPipeline _pipeline;
        private WorldData _lastGeneratedWorld;

        private void Awake()
        {
            _pipeline = new WorldGenPipeline()
                .Add(new BaseFillStep())
                .Add(new BiomeNoiseStep())
                .Add(new DistrictGraphStep())
                .Add(new StreetCarvingStep())
                .Add(new TerrainPostProcessStep())
                .Add(new BlockLayoutStep())
                .Add(new BuildingLayoutStep())
                .Add(new DecorationPlanStep())
                .Add(new ValidationStep())
                .Add(new RepairStep());
        }

        private void Start()
        {
            Generate();     
        }

        private void ConfigureMainCamera(WorldData world)
        {
            var cam = Camera.main;
            if (cam == null) 
            {
                Debug.LogWarning("[WorldBootstrap] Main Camera not found!");
                return;
            }

            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;

            if (cam.orthographic)
            {
                float mapWidth = world.Size.x;
                float mapHeight = world.Size.y;
                float sizeByHeight = mapHeight / 2f + 2f;
                float sizeByWidth = (mapWidth / 2f) / cam.aspect + 2f;
                float requiredSize = Mathf.Max(sizeByHeight, sizeByWidth);
                cam.orthographicSize = requiredSize;
            }

            Debug.Log($"[WorldBootstrap] Camera configured: pos={cam.transform.position}, rot={cam.transform.rotation.eulerAngles}, orthoSize={cam.orthographicSize}, near={cam.nearClipPlane}, far={cam.farClipPlane}, aspect={cam.aspect}");
        }

        [ContextMenu("Generate")]
        public void Generate()
        {
            if (config == null)
            {
                Debug.LogError("WorldBootstrap: config is null.");
                return;
            }

            if (tileAssetSet == null || groundTilemap == null || roadsTilemap == null || wallsTilemap == null)
            {
                Debug.LogError("WorldBootstrap: tile assets or tilemaps not assigned.");
                return;
            }

            int finalSeed;
            if (useRandomSeed)
            {
                finalSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }
            else if (usePlayerPrefsSeed && PlayerPrefs.HasKey("GameSeed"))
            {
                finalSeed = PlayerPrefs.GetInt("GameSeed");
                Debug.Log($"[WorldBootstrap] Using seed from PlayerPrefs: {finalSeed}");
            }
            else
            {
                finalSeed = seed;
                Debug.Log($"[WorldBootstrap] Using default seed: {finalSeed}");
            }

            var (world, attempts) = WorldGeneratorService.GenerateWithRetries(
                finalSeed, config, _pipeline, maxGenerationAttempts);

            _lastGeneratedWorld = world;
            
            ConfigureMainCamera(world);

            if (saveWorldDataForDebug && worldDataDebug != null)
            {
                worldDataDebug.worldData = world;
            }

            Debug.Log($"[WorldBootstrap] Blocks: {world.Blocks?.Count ?? 0}, Buildings: {world.Buildings?.Count ?? 0}, SpawnPlan: {world.SpawnPlan?.Entries?.Count ?? 0}");

            TilemapBuilder.Build(world, tileAssetSet, groundTilemap, roadsTilemap, wallsTilemap);

            if (prefabCatalog != null)
            {
                PropInstantiator.Instantiate(world.SpawnPlan, prefabCatalog, groundTilemap, spawnedObjectsParent, useYRotation, randomTreeRotation, world.Roads);
                PropInstantiator.InstantiateBuildings(world.Buildings, prefabCatalog, groundTilemap, spawnedObjectsParent, randomBuildingRotation);
            }

            Debug.Log($"World generated. Seed={world.Meta.Seed}, ConfigHash={world.Meta.ConfigHash}, WorldHash={world.Meta.WorldHash}, GenVer={world.Meta.GeneratorVersion}, Attempts={attempts}");
        }

        public WorldData GetLastGeneratedWorld() => _lastGeneratedWorld;
    }
}
