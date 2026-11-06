using System.Diagnostics.CodeAnalysis;
using MessagePack;
using MessagePack.Formatters;

namespace MessagePack.Tests.NetFx
{
    // Nullable annotation validation (on by default) on the netstandard2.0 asset: the reflection tier's field slot is the
    // boxed FieldInfo one there, and the flow attributes are this assembly's own internal copies (net48 has none), which
    // both tiers must recognize by name.
    public class DownlevelNullableValidationTests
    {
        static readonly MessagePackSerializerOptions Reflection = new(new MessagePackFormatterResolver(
        [
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true),
        ]));

        static readonly MessagePackSerializerOptions Lenient = new(
            new MessagePackFormatterResolver(MessagePackFormatterFactory.Default) { ValidateNullableAnnotations = false });

        static readonly byte[] NilArray = [0x91, 0xc0];

        [Fact]
        public void NonNullableField_RejectsNilAndNull()
        {
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DownlevelNullField>(NilArray));
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<DownlevelNullField>(NilArray, Reflection));
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Serialize(new DownlevelNullField { Name = null! }));
            Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Serialize(new DownlevelNullField { Name = null! }, Reflection));

            Assert.Null(MessagePackSerializer.Deserialize<DownlevelNullField>(NilArray, Lenient)!.Name);
            Assert.Equal(NilArray, MessagePackSerializer.Serialize(new DownlevelNullField { Name = null! }, Lenient));
        }

        [Fact]
        public void PolyfilledAllowNull_IsHonored()
        {
            Assert.Equal("", MessagePackSerializer.Deserialize<DownlevelAllowNull>(NilArray)!.Name);
            Assert.Equal("", MessagePackSerializer.Deserialize<DownlevelAllowNull>(NilArray, Reflection)!.Name);
        }

        [Fact]
        public void ConstructorParameterAnnotation_IsTheDeserializationContract()
        {
            Assert.Equal("", MessagePackSerializer.Deserialize<DownlevelCtorAllowsNull>(NilArray)!.Name);
            Assert.Equal("", MessagePackSerializer.Deserialize<DownlevelCtorAllowsNull>(NilArray, Reflection)!.Name);
        }
    }

    [MessagePackObject]
    public class DownlevelNullField
    {
        [Key(0)] public string Name = "";
    }

    [MessagePackObject]
    public class DownlevelAllowNull
    {
        string name = "";

        [Key(0)]
        [AllowNull]
        public string Name { get => name; set => name = value ?? ""; }
    }

    [MessagePackObject]
    public class DownlevelCtorAllowsNull
    {
        [Key(0)] public string Name { get; }

        public DownlevelCtorAllowsNull(string? name)
        {
            Name = name ?? "";
        }
    }
}

namespace System.Diagnostics.CodeAnalysis
{
    // net48 declares none of the flow attributes; the compiler (and both MessagePack tiers) match them by name
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property)]
    internal sealed class AllowNullAttribute : Attribute
    {
    }
}
