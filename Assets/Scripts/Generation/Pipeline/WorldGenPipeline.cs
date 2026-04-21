using System;
using System.Collections.Generic;
using Diploma.Core;
using Diploma.Generation.Model;

namespace Diploma.Generation.Pipeline
{
    public sealed class WorldGenPipeline
    {
        private readonly List<IWorldGenStep> _steps = new();

        public IReadOnlyList<IWorldGenStep> Steps => _steps;

        public WorldGenPipeline Add(IWorldGenStep step)
        {
            if (step == null) throw new ArgumentNullException(nameof(step));
            _steps.Add(step);
            return this;
        }

        public void Run(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (seed == null) throw new ArgumentNullException(nameof(seed));
            if (world == null) throw new ArgumentNullException(nameof(world));

            for (int i = 0; i < _steps.Count; i++)
            {
                _steps[i].Execute(config, seed, world);
            }
        }
    }
}