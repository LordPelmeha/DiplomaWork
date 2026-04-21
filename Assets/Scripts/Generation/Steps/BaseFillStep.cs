using Diploma.Core;
using Diploma.Generation.Model;
using Diploma.Generation.Pipeline;

namespace Diploma.Generation.Steps
{

    public sealed class BaseFillStep : IWorldGenStep
    {
        public string Key => "BaseFill";

        public void Execute(WorldGenConfig config, SeedContext seed, WorldData world)
        {
            world.Ground.Fill(TileType.Grass);
            world.Roads.Fill(TileType.Empty);
            world.Walls.Fill(TileType.Empty);
        }
    }
}