using System;

namespace Diploma.Core
{
    /// <summary>
    /// Стабильный хеш для получения sub-seed'ов из (rootSeed + streamKey).
    /// </summary>
    public static class StableHash
    {
        // FNV-1a 32-bit constants
        private const uint OffsetBasis = 2166136261u;
        private const uint Prime = 16777619u;

        /// <summary>
        /// Стабильно хеширует (rootSeed + key) в uint.
        /// </summary>
        public static uint HashToUInt(int rootSeed, string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));

            unchecked
            {
                uint hash = OffsetBasis;

                // Mix seed bytes
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

        /// <summary>
        /// Стабильно хеширует (rootSeed + key) в int для System.Random(int seed).
        /// </summary>
        public static int HashToInt(int rootSeed, string key)
        {
            unchecked
            {
                return (int)HashToUInt(rootSeed, key);
            }
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