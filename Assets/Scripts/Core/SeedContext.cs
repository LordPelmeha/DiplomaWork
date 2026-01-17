using System;

namespace Diploma.Core
{
    /// <summary>
    /// даёт независимые RNG-потоки по ключу шага.
    /// </summary>
    public sealed class SeedContext
    {
        public int RootSeed { get; }

        public SeedContext(int rootSeed)
        {
            RootSeed = rootSeed;
        }

        /// <summary>
        /// Создаёт System.Random для конкретного потока
        /// </summary>
        public Random CreateRng(string streamKey)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            int subSeed = StableHash.HashToInt(RootSeed, streamKey);
            return new Random(subSeed);
        }

        /// <summary>
        /// Создаёт RNG для потока + номера попытки 
        /// </summary>
        public Random CreateRng(string streamKey, int attemptIndex)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            string compositeKey = streamKey + "#" + attemptIndex.ToString();
            int subSeed = StableHash.HashToInt(RootSeed, compositeKey);
            return new Random(subSeed);
        }

        /// <summary>
        /// Получить sub-seed
        /// </summary>
        public int GetSubSeed(string streamKey)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            return StableHash.HashToInt(RootSeed, streamKey);
        }
    }
}