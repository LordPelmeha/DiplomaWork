using System;
using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Validation;
using UnityEngine;

namespace Diploma.Generation.Pipeline
{
    public static class WorldGeneratorService
    {
        public const string GeneratorVersion = "gen_v0.1";

        public static (WorldData world, int attempts) GenerateWithRetries(
            int seed, WorldGenConfig config, WorldGenPipeline pipeline, int maxAttempts = 3)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (pipeline == null) throw new ArgumentNullException(nameof(pipeline));

            int attempts = 0;
            int currentSeed = seed;

            do
            {
                attempts++;
                var world = GenerateInternal(currentSeed, config, pipeline, attempts - 1);

                var validationResult = WorldValidator.Validate(world);
                if (validationResult.IsValid)
                {
                    Debug.Log($"[WorldGeneratorService] World generated successfully on attempt {attempts}");
                    return (world, attempts);
                }

                Debug.LogWarning($"[WorldGeneratorService] Attempt {attempts} failed validation: {string.Join("; ", validationResult.Errors)}");

                currentSeed = seed + attempts;

            } while (maxAttempts <= 0 || attempts < maxAttempts);

            if (maxAttempts > 1)
            {
                Debug.LogError($"[WorldGeneratorService] Failed to generate valid world after {attempts} attempts");
            }

            var lastWorld = GenerateInternal(currentSeed, config, pipeline, attempts - 1);
            return (lastWorld, attempts);
        }

        public static WorldData Generate(int seed, WorldGenConfig config, WorldGenPipeline pipeline)
        {
            var result = GenerateWithRetries(seed, config, pipeline, maxAttempts: 1);
            return result.world;
        }

        private static WorldData GenerateInternal(int seed, WorldGenConfig config, WorldGenPipeline pipeline, int attemptIndex)
        {
            var seedContext = new SeedContext(seed);
            var world = new WorldData(config.mapSize, config.terrainSmoothChance);

            world.Meta = new WorldMeta
            {
                Seed = seed,
                GeneratorVersion = GeneratorVersion,
                ConfigHash = config.ComputeStableHash(),
                WorldHash = 0
            };

            pipeline.Run(config, seedContext, world);

            world.Meta.WorldHash = WorldHash.Compute(world);

            return world;
        }
    }
}