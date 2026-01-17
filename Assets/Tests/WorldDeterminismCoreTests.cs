using System;
using NUnit.Framework;
using Diploma.Core;

public class WorldDeterminismCoreTests
{
    [Test]
    public void StableHash_SameInput_SameOutput()
    {
        uint h1 = StableHash.HashToUInt(12345, "Graph");
        uint h2 = StableHash.HashToUInt(12345, "Graph");
        Assert.AreEqual(h1, h2);
    }

    [Test]
    public void SeedContext_SameSeedSameStreamKey_ProducesSameSequence()
    {
        var a = new SeedContext(777);
        var b = new SeedContext(777);

        Random ra = a.CreateRng("Graph");
        Random rb = b.CreateRng("Graph");

        for (int i = 0; i < 100; i++)
        {
            Assert.AreEqual(ra.Next(), rb.Next(), $"Mismatch at i={i}");
        }
    }

    [Test]
    public void SeedContext_DifferentStreamKeys_AreIndependent()
    {
        var ctx = new SeedContext(777);

        Random rGraph = ctx.CreateRng("Graph");
        Random rStreets = ctx.CreateRng("Streets");

        // Не строгая матем. гарантия, но на практике почти наверняка разные последовательности.
        // Главное: они стабильно "свои" и не зависят друг от друга.
        int g0 = rGraph.Next();
        int s0 = rStreets.Next();

        Assert.AreNotEqual(g0, s0);
    }

    [Test]
    public void SeedContext_AttemptIndex_ChangesSequenceButIsDeterministic()
    {
        var ctx = new SeedContext(777);

        Random r0a = ctx.CreateRng("Buildings", attemptIndex: 0);
        Random r0b = ctx.CreateRng("Buildings", attemptIndex: 0);
        Random r1 = ctx.CreateRng("Buildings", attemptIndex: 1);

        for (int i = 0; i < 50; i++)
        {
            Assert.AreEqual(r0a.Next(), r0b.Next(), $"Mismatch at i={i}");
        }

        Assert.AreNotEqual(ctx.GetSubSeed("Buildings#0"), ctx.GetSubSeed("Buildings#1"));
        Assert.AreNotEqual(r0a.Next(), r1.Next());
    }
}