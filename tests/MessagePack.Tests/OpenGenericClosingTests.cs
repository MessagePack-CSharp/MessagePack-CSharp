using MessagePack.Formatters;
using Xunit;
using V4 = MessagePack.MessagePackSerializer;

namespace MessagePack.Tests;

// Instantiations of a generic [MessagePackObject] type that the generator never saw (used only at a call site). The
// generated factory used to close them itself behind RuntimeFeature.IsDynamicCodeSupported, so a JIT run of DefaultAot
// passed and the Native AOT build failed with formatter-not-found. Now the registry closes them, and only in its
// Instance view: Default (RequiresDynamicCode) still gets the generated formatter, never the reflection tier, while
// DefaultAot (StaticInstance) declines on the JIT exactly as the published app does.
public class OpenGenericClosingTests
{
    [Fact]
    public void Default_ClosesTheGeneratedFormatter_NotTheReflectionTier()
    {
        var value = new OpenOnly<Guid> { Item = Guid.Parse("01020304-0506-0708-090a-0b0c0d0e0f10") };
        var bytes = V4.Serialize(value);
        Assert.Equal(value.Item, V4.Deserialize<OpenOnly<Guid>>(bytes)!.Item);

        var formatter = MessagePackSerializerOptions.Default.Resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, OpenOnly<Guid>>();
        Assert.StartsWith("MessagePack.Generated.", formatter.GetType().FullName, StringComparison.Ordinal); // the generated formatter, not ReflectionObjectFormatter
        Assert.Contains("OpenOnly", formatter.GetType().FullName, StringComparison.Ordinal);
    }

    [Fact]
    public void DefaultAot_OnTheJit_FailsLikeNativeAot()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => V4.Serialize(new OpenOnly<Guid>(), MessagePackSerializerOptions.DefaultAot));
        Assert.Contains("is not found", ex.Message, StringComparison.Ordinal);
        Assert.Contains("MessagePackSerializable", ex.Message, StringComparison.Ordinal); // the hint names the declaration that fixes it

        // a harvested instantiation (a member of an annotated type) is constructed statically and stays available
        var harvested = new OpenOnlyHolder { Boxed = new OpenOnly<int> { Item = 7 } };
        Assert.Equal(7, V4.Deserialize<OpenOnlyHolder>(V4.Serialize(harvested, MessagePackSerializerOptions.DefaultAot), MessagePackSerializerOptions.DefaultAot)!.Boxed!.Item);
        Assert.Equal(7, V4.Deserialize<OpenOnly<int>>(V4.Serialize(new OpenOnly<int> { Item = 7 }, MessagePackSerializerOptions.DefaultAot), MessagePackSerializerOptions.DefaultAot)!.Item);
    }

    [Fact]
    public void TheTwoRegistryViews_ShareRegistrationsAndDifferOnlyInClosing()
    {
        Assert.NotNull(SourceGeneratedFormatterFactory.Instance.CreateFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer>(typeof(OpenOnly<Guid>)));
        Assert.Null(SourceGeneratedFormatterFactory.StaticInstance.CreateFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer>(typeof(OpenOnly<Guid>)));
        Assert.NotNull(SourceGeneratedFormatterFactory.StaticInstance.CreateFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer>(typeof(OpenOnly<int>)));

        // the generated factory itself is static: an explicit AOT composition around it gets no dynamic closing either
        Assert.Null(MessagePack.Generated.GeneratedMessagePackFormatterFactory.Instance.CreateFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer>(typeof(OpenOnly<Guid>)));
    }
}

[MessagePackObject]
public class OpenOnly<T>
{
    [Key(0)] public T? Item { get; set; }
}

[MessagePackObject]
public class OpenOnlyHolder
{
    [Key(0)] public OpenOnly<int>? Boxed { get; set; }
}
