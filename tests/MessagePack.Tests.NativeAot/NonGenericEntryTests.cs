using System.Runtime.CompilerServices;
using MessagePack;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests.NativeAot;

// The Type-based entries (the surface hub protocols and MVC formatters are forced onto) close NonGenericEntry<T> over
// the runtime type. On Native AOT that must come from a registration or the built-in scalar table, never from
// MakeGenericType: generated types, harvested closures and [MessagePackSerializable] roots are registered by the
// generated module initializers, the scalars are closed statically in the core, and anything else is a
// NotSupportedException with the fix in its message.
public class NonGenericEntryTests
{
    static readonly MessagePackSerializerOptions Options = new(new MessagePackFormatterResolver(
    [
        MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance,
        AotRootFactory.Instance,
        BuiltInFormatterFactory.Instance,
    ]));

    static object? RoundTrip(Type type, object? value)
    {
        var bytes = V4.Serialize(type, value, Options);
        Assert.Equal(V4.Serialize(type, value, Options), bytes);
        return V4.Deserialize(type, bytes, Options);
    }

    [Fact]
    public void GeneratedClass_ThroughTypeEntry()
    {
        var back = Assert.IsType<AotIntKeyPoco>(RoundTrip(typeof(AotIntKeyPoco), new AotIntKeyPoco { Id = 7, Count = 300, Name = "abc" }));
        Assert.Equal(7, back.Id);
        Assert.Equal(300, back.Count);
        Assert.Equal("abc", back.Name);
        // byte-identical to the generic entry
        Assert.Equal(V4.Serialize(new AotIntKeyPoco { Id = 7, Count = 300, Name = "abc" }, Options), V4.Serialize(typeof(AotIntKeyPoco), new AotIntKeyPoco { Id = 7, Count = 300, Name = "abc" }, Options));
    }

    [Fact]
    public void GeneratedStruct_ThroughTypeEntry()
    {
        // a value type: MakeGenericType would need a pre-generated instantiation, the registration supplies it
        var value = new AotStructPoco { X = 1, Y = -2 };
        var back = Assert.IsType<AotStructPoco>(RoundTrip(typeof(AotStructPoco), value));
        Assert.Equal(value, back);
    }

    [Fact]
    public void HarvestedEnum_ThroughTypeEntry()
    {
        Assert.Equal(AotColor.Green, RoundTrip(typeof(AotColor), AotColor.Green));
    }

    [Fact]
    public void SerializableRoots_ThroughTypeEntry()
    {
        var array = Assert.IsType<AotIntKeyPoco[]>(RoundTrip(typeof(AotIntKeyPoco[]), new[] { new AotIntKeyPoco { Id = 1 }, new AotIntKeyPoco { Id = 2 } }));
        Assert.Equal([1, 2], array.Select(p => p.Id));
        var list = Assert.IsType<List<AotStructPoco>>(RoundTrip(typeof(List<AotStructPoco>), new List<AotStructPoco> { new() { X = 1 }, new() { Y = 2 } }));
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public void BuiltInScalars_ThroughTypeEntry()
    {
        Assert.Equal(42, RoundTrip(typeof(int), 42));
        Assert.Equal("hub", RoundTrip(typeof(string), "hub"));
        Assert.Equal(5_000_000_000L, RoundTrip(typeof(long), 5_000_000_000L));
        Assert.Equal(true, RoundTrip(typeof(bool), true));
        Assert.Equal(1.5, RoundTrip(typeof(double), 1.5));
        var guid = Guid.NewGuid();
        Assert.Equal(guid, RoundTrip(typeof(Guid), guid));
        Assert.Equal(new DateTime(2026, 9, 8, 1, 2, 3, DateTimeKind.Utc), RoundTrip(typeof(DateTime), new DateTime(2026, 9, 8, 1, 2, 3, DateTimeKind.Utc)));
        Assert.Equal(7, RoundTrip(typeof(int?), 7));
        Assert.Null(RoundTrip(typeof(int?), null));
        Assert.Null(RoundTrip(typeof(string), null));
    }

    [Fact]
    public void UnregisteredType_IsNotSupportedWithoutDynamicCode()
    {
        // no attribute, no root declaration: nothing registered a bridge. The JIT closes one over the type and then
        // fails to find a formatter in the explicit chain; Native AOT stops earlier with the actionable message.
        var exception = Record.Exception(() => V4.Serialize(typeof(AotUnregisteredPoco), new AotUnregisteredPoco(), Options));
        Assert.NotNull(exception);
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            var notSupported = Assert.IsType<NotSupportedException>(exception);
            Assert.Contains("[MessagePackSerializable<T>]", notSupported.Message);
        }
    }
}

public class AotUnregisteredPoco
{
    public int Value { get; set; }
}
