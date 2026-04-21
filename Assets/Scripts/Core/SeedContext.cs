using System;

namespace Diploma.Core
{
    public sealed class SeedContext
    {
        public int RootSeed { get; }

        public SeedContext(int rootSeed)
        {
            RootSeed = rootSeed;
        }

        public Random CreateRng(string streamKey)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            int subSeed = StableHash.HashToInt(RootSeed, streamKey);
            return new Random(subSeed);
        }
        public Random CreateRng(string streamKey, int attemptIndex)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            string compositeKey = streamKey + "#" + attemptIndex.ToString();
            int subSeed = StableHash.HashToInt(RootSeed, compositeKey);
            return new Random(subSeed);
        }

        public int GetSubSeed(string streamKey)
        {
            if (string.IsNullOrWhiteSpace(streamKey))
                throw new ArgumentException("Stream key must be non-empty.", nameof(streamKey));

            return StableHash.HashToInt(RootSeed, streamKey);
        }
    }
}