using System;

namespace Diploma.Core
{
    public static class StableHash
    {
        private const uint OffsetBasis = 2166136261u;
        private const uint Prime = 16777619u;

        public static uint HashToUInt(int rootSeed, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            unchecked
            {
                uint hash = OffsetBasis;

                hash = FnvaStep(hash, (byte)rootSeed);
                hash = FnvaStep(hash, (byte)(rootSeed >> 8));
                hash = FnvaStep(hash, (byte)(rootSeed >> 16));
                hash = FnvaStep(hash, (byte)(rootSeed >> 24));

                for (int i = 0; i < key.Length; i++)
                {
                    char c = key[i];
                    hash = FnvaStep(hash, (byte)c);
                    hash = FnvaStep(hash, (byte)(c >> 8));
                }

                return hash;
            }
        }

        public static int HashToInt(int rootSeed, string key)
        {
            uint hash = HashToUInt(rootSeed, key);
            return (int)(hash & 0x7FFFFFFF);
        }

        private static uint FnvaStep(uint hash, byte data)
        {
            unchecked
            {
                hash ^= data;
                hash *= Prime;
                return hash;
            }
        }
    }
}