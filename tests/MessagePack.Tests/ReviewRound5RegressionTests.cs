using MessagePack;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Sanitize: Edge_.A and Edge._A used to share one generated formatter name (Edge___A); compiling is the assertion
public class Edge_
{
    [MessagePackObject]
    public class A
    {
        [Key(0)] public int X { get; set; }
    }
}

public class Edge
{
    [MessagePackObject]
    public class _A
    {
        [Key(0)] public int X { get; set; }
    }
}

public class ReviewRound5RegressionTests
{
    // a multi-dimensional array can start at a non-zero lower bound (COM / Excel interop): the wire carries lengths
    // only, so it serializes like its zero-based twin and populates back into the same bounds
    [Fact]
    public void MultiDimensionalArray_NonZeroLowerBounds()
    {
        var oneBased = (int[,])Array.CreateInstance(typeof(int), [2, 3], [1, 1]);
        var zeroBased = new int[2, 3];
        for (int x = 0; x < 2; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                oneBased[x + 1, y + 1] = zeroBased[x, y] = x * 10 + y;
            }
        }
        var bytes = V4.Serialize(oneBased);
        Assert.Equal(V4.Serialize(zeroBased), bytes);

        var fresh = V4.Deserialize<int[,]>(bytes)!;
        Assert.Equal(0, fresh.GetLowerBound(0));
        Assert.Equal(12, fresh[1, 2]);

        var target = (int[,])Array.CreateInstance(typeof(int), [2, 3], [1, 1]);
        V4.Deserialize(bytes, ref target);
        Assert.Equal(1, target.GetLowerBound(0));
        Assert.Equal(12, target[2, 3]);
        Assert.Equal(oneBased.Cast<int>(), target.Cast<int>());

        var cube = (int[,,])Array.CreateInstance(typeof(int), [1, 2, 2], [5, 5, 5]);
        cube[5, 6, 6] = 9;
        var cubeBack = V4.Deserialize<int[,,]>(V4.Serialize(cube))!;
        Assert.Equal(9, cubeBack[0, 1, 1]);
        var hyper = (int[,,,])Array.CreateInstance(typeof(int), [1, 1, 2, 1], [0, 0, 3, 0]);
        hyper[0, 0, 4, 0] = 4;
        Assert.Equal(4, V4.Deserialize<int[,,,]>(V4.Serialize(hyper))![0, 0, 1, 0]);
    }

    [Fact]
    public void SanitizedNames_StayDistinct()
    {
        Assert.Equal(1, V4.Deserialize<Edge_.A>(V4.Serialize(new Edge_.A { X = 1 }))!.X);
        Assert.Equal(2, V4.Deserialize<Edge._A>(V4.Serialize(new Edge._A { X = 2 }))!.X);
    }

    // the Type-based entries reach every built-in scalar statically (Native AOT has no MakeGenericType to fall back
    // to); on JIT the same entries serve, so the wire must match the generic entry's
    [Fact]
    public void NonGenericEntries_CoverTheBuiltInScalars()
    {
        Assert.Equal(V4.Serialize(new DateOnly(2026, 10, 8)), V4.Serialize(typeof(DateOnly), new DateOnly(2026, 10, 8)));
        Assert.Equal(V4.Serialize((Half)1.5), V4.Serialize(typeof(Half), (Half)1.5));
        Assert.Equal(V4.Serialize(new byte[] { 1, 2 }), V4.Serialize(typeof(byte[]), new byte[] { 1, 2 }));
        Assert.Equal(new DateOnly(2026, 10, 8), V4.Deserialize(typeof(DateOnly?), V4.Serialize((DateOnly?)new DateOnly(2026, 10, 8))));
    }
}
