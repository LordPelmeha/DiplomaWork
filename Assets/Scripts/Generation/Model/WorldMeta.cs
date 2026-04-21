using System;

namespace Diploma.Generation.Model
{
    [Serializable]
    public struct WorldMeta
    {
        public int Seed;
        public string GeneratorVersion;

        public uint ConfigHash;
        public uint WorldHash;
    }
}