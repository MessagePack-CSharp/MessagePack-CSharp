using System.Diagnostics.CodeAnalysis;
using MessagePack;

namespace MessagePack.Tests.Trimmed;

// The chains under test are the ones annotated RequiresUnreferencedCode: this project exists to run them trimmed.
[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "the run-time tiers under trimming are the subject of this project")]
static class Chains
{
    public static readonly MessagePackSerializerOptions Default = MessagePackSerializerOptions.Default;

    public static readonly MessagePackSerializerOptions Contractless = new(MessagePackFormatterFactory.Default.WithContractless());

    public static readonly MessagePackSerializerOptions EnumAsStringIgnoreCase = new(MessagePackFormatterFactory.Combine(
        new GenericEnumAsStringFormatterFactory(ignoreCase: true),
        MessagePackFormatterFactory.Default));

    public static readonly MessagePackSerializerOptions Typeless = new(MessagePackFormatterFactory.Default.WithTypeless(
        TypelessTypeLoader.AllowedTypes(typeof(System.Collections.ArrayList), typeof(int), typeof(string))));
}

static class Trimmed
{
    // The type parameter's annotation keeps every member of the serialized type, the way an application that heeded
    // the Default chain's IL2026 keeps its own types. What still fails after that is the library's: something of its
    // own that only reflection reaches and the trimmer therefore removed.
    public static byte[] Serialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(T value, MessagePackSerializerOptions options)
    {
        return MessagePackSerializer.Serialize(value, options);
    }

    public static T Deserialize<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>(byte[] bytes, MessagePackSerializerOptions options)
    {
        return MessagePackSerializer.Deserialize<T>(bytes, options);
    }
}
