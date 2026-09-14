using MessagePack;
using MessagePack.Resolvers;

namespace V3GeneratedModels;

/// <summary>
/// Ready-made v3 options whose resolver chain puts the SOURCE-GENERATED resolver first
/// (v3's real generator output for the twins in this assembly), falling back to
/// StandardResolver for primitives and collections - the composition v3's own docs
/// prescribe for AOT. The benchmarks consume this through the V3 extern alias.
/// </summary>
public static class V3SourceGen
{
    public static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithResolver(
            CompositeResolver.Create(GeneratedMessagePackResolver.Instance, StandardResolver.Instance));
}
