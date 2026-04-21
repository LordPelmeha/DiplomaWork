using Diploma.Core;
using Diploma.Generation.Model;

namespace Diploma.Generation.Pipeline
{
    public interface IWorldGenStep
    {
        string Key { get; }

        void Execute(WorldGenConfig config, SeedContext seed, WorldData world);
    }
}