// Shims that map the MessagePack-CSharp v3 test surface onto the v4 API, so the ported
// v3.1.8 test files compile with as few edits as possible. Everything here is
// test-support code owned by this repo, NOT ported upstream code.
//
// Mapping notes (v3 name -> v4 composition):
//   StandardResolver                        -> MessagePackFormatterFactory.Default
//   StandardResolverAllowPrivate            -> Default chain with ReflectionFormatterFactory(annotatedOnly: true, allowPrivate: true)
//   ContractlessStandardResolver            -> Default.WithContractless()
//   ContractlessStandardResolverAllowPrivate-> allow-private chain .WithContractless(allowPrivate: true)
//   TypelessObjectResolver /
//   TypelessContractlessStandardResolver    -> Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())
//   DynamicEnumAsStringResolver             -> GenericEnumAsStringFormatterFactory composed before the chain
//   BuiltinResolver                         -> BuiltInFormatterFactory alone
//   options.WithCompression(Lz4*)           -> options.WithLz4Block()/WithLz4BlockArray() (MessagePack.LZ4)
#pragma warning disable CS0618 // ExpandoObjectFormatterFactory is deliberately Obsolete; the v3 suite is a consenting caller

using MessagePack;
using MessagePack.Formatters;

namespace Xunit.Abstractions
{
    // keeps upstream's `using Xunit.Abstractions;` lines compiling; ITestOutputHelper
    // itself lives in the Xunit namespace in xunit.v3
    internal static class NamespaceAnchor
    {
    }
}

namespace Xunit
{
    public sealed class SkippableFactAttribute : FactAttribute
    {
        public SkippableFactAttribute(
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
        }
    }

    public sealed class SkippableTheoryAttribute : TheoryAttribute
    {
        public SkippableTheoryAttribute(
            [System.Runtime.CompilerServices.CallerFilePath] string sourceFilePath = null,
            [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
            : base(sourceFilePath, sourceLineNumber)
        {
        }
    }

    public static class Skip
    {
        public static void If(bool condition, string reason = null) => Assert.SkipWhen(condition, reason ?? "skipped by Skip.If");

        public static void IfNot(bool condition, string reason = null) => Assert.SkipUnless(condition, reason ?? "skipped by Skip.IfNot");
    }
}

namespace MessagePack
{
    /// <summary>
    /// v3's UnionAttribute over v4's renamed UnionTagAttribute (deliberate deviation #1:
    /// C# 15's union feature claims the "Union" name; v4 also swapped the argument order).
    /// NOTE: only the source generator consumes UnionTag, so hierarchies annotated this
    /// way still need generated formatters to actually serialize polymorphically.
    /// </summary>
    public sealed class UnionAttribute : UnionTagAttribute
    {
        public int Key => Tag;

        public Type SubType => CaseType;

        public UnionAttribute(int key, Type subType)
            : base(subType, key)
        {
        }

        public UnionAttribute(int key, string subType)
            : base(Type.GetType(subType, throwOnError: true), key)
        {
        }
    }

    public enum MessagePackCompression
    {
        None,
        Lz4Block,
        Lz4BlockArray,
    }

    /// <summary>v3 MessagePackSecurity stand-in: only the members the ported tests touch.</summary>
    public record class MessagePackSecurity
    {
        public static readonly MessagePackSecurity TrustedData = new() { UntrustedDeserialization = false };

        // v3's UntrustedData preset caps decompressed output at 64MB
        public static readonly MessagePackSecurity UntrustedData = new() { UntrustedDeserialization = true, MaximumDecompressedSize = 64 * 1024 * 1024 };

        public bool UntrustedDeserialization { get; init; }

        public int MaximumObjectGraphDepth { get; init; } = 500;

        public int MaximumDecompressedSize { get; init; } = int.MaxValue;

        public MessagePackSecurity WithMaximumObjectGraphDepth(int maxDepth) => this with { MaximumObjectGraphDepth = maxDepth };

        // v4 has no LZ4 decompressed-size cap knob; carried for compile compatibility so the
        // v3 tests asserting the cap run (and document the divergence by failing).
        public MessagePackSecurity WithMaximumDecompressedSize(int maxSize) => this with { MaximumDecompressedSize = maxSize };
    }

    public static class V3CompatOptionsExtensions
    {
        public static MessagePackSerializerOptions WithCompression(this MessagePackSerializerOptions options, MessagePackCompression compression) => compression switch
        {
            MessagePackCompression.None => options with { MessageProcessor = null },
            MessagePackCompression.Lz4Block => options.WithLz4Block(),
            MessagePackCompression.Lz4BlockArray => options.WithLz4BlockArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(compression)),
        };

        public static MessagePackSerializerOptions WithResolver(this MessagePackSerializerOptions options, MessagePackFormatterResolver resolver) =>
            new MessagePackSerializerOptions(resolver)
            {
                MessageProcessor = options.MessageProcessor,
                MaxDepth = options.MaxDepth,
                MaxBufferedMessageSize = options.MaxBufferedMessageSize,
            };

        // v4 folds the v3 security knobs into the resolver (hash-flooding-resistant
        // comparers, always-on validated reads), MaxDepth, and the LZ4 processor's
        // decompression cap; the last needs rebuilding the processor with the v3 value.
        public static MessagePackSerializerOptions WithSecurity(this MessagePackSerializerOptions options, MessagePackSecurity security)
        {
            var processor = options.MessageProcessor switch
            {
                Lz4BlockProcessor => new Lz4BlockProcessor(security.MaximumDecompressedSize),
                Lz4BlockArrayProcessor => (MessagePackMessageProcessor)new Lz4BlockArrayProcessor(security.MaximumDecompressedSize),
                var other => other,
            };
            return options with { MaxDepth = security.MaximumObjectGraphDepth, MessageProcessor = processor };
        }

        public static MessagePackSecurity Security(this MessagePackSerializerOptions options) => MessagePackSecurity.UntrustedData;
    }
}

namespace MessagePack.Resolvers
{
    public static class V3CompatChains
    {
        // v3 CompositeResolver combines IFormatterResolver instances; v4 combines factories.
        // Every resolver this shim layer hands out remembers its factory here so
        // CompositeResolver.Create can recover it.
        static readonly System.Runtime.CompilerServices.ConditionalWeakTable<MessagePackFormatterResolver, MessagePackFormatterFactory> factoryOf = new();

        public static MessagePackFormatterFactory AllowPrivate { get; } = MessagePackFormatterFactory.Combine(
            SourceGeneratedFormatterFactory.Instance,
            BuiltInFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            new ReflectionFormatterFactory(annotatedOnly: true, allowPrivate: true));

        static V3CompatChains()
        {
            factoryOf.Add(MessagePackSerializerOptions.Default.Resolver, MessagePackFormatterFactory.Default);
        }

        public static MessagePackSerializerOptions ToOptions(MessagePackFormatterFactory factory)
        {
            var resolver = new MessagePackFormatterResolver(factory);
            factoryOf.Add(resolver, factory);
            return new(resolver);
        }

        public static MessagePackFormatterFactory FactoryOf(MessagePackFormatterResolver resolver) =>
            factoryOf.TryGetValue(resolver, out var factory)
                ? factory
                : throw new NotSupportedException("This resolver did not come from the V3Compat shim layer, so its factory chain cannot be recovered for composition.");
    }

    /// <summary>v3 CompositeResolver, resolver-combining form only (v4 composes factories).</summary>
    public static class CompositeResolver
    {
        public static MessagePackFormatterResolver Create(params MessagePackFormatterResolver[] resolvers)
        {
            var options = V3CompatChains.ToOptions(MessagePackFormatterFactory.Combine(
                Array.ConvertAll(resolvers, V3CompatChains.FactoryOf)));
            return options.Resolver;
        }
    }

    public static class DynamicEnumAsStringIgnoreCaseResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(MessagePackFormatterFactory.Combine(
            new GenericEnumAsStringFormatterFactory(ignoreCase: true), MessagePackFormatterFactory.Default));

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class StandardResolver
    {
        public static MessagePackFormatterResolver Instance => MessagePackSerializerOptions.Default.Resolver;

        public static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Default;
    }

    public static class StandardResolverAllowPrivate
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(V3CompatChains.AllowPrivate);

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class ContractlessStandardResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(MessagePackFormatterFactory.Default.WithContractless());

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class ContractlessStandardResolverAllowPrivate
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(V3CompatChains.AllowPrivate.WithContractless(allowPrivate: true));

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class TypelessObjectResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(
            MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType()));

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class TypelessContractlessStandardResolver
    {
        public static MessagePackSerializerOptions Options => TypelessObjectResolver.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class DynamicObjectResolver
    {
        public static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Default;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class DynamicObjectResolverAllowPrivate
    {
        public static MessagePackSerializerOptions Options => StandardResolverAllowPrivate.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class DynamicContractlessObjectResolver
    {
        public static MessagePackSerializerOptions Options => ContractlessStandardResolver.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class DynamicContractlessObjectResolverAllowPrivate
    {
        public static MessagePackSerializerOptions Options => ContractlessStandardResolverAllowPrivate.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class DynamicEnumAsStringResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(MessagePackFormatterFactory.Combine(
            new GenericEnumAsStringFormatterFactory(), MessagePackFormatterFactory.Default));

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class BuiltinResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(BuiltInFormatterFactory.Instance);

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class PrimitiveObjectResolver
    {
        // v4's default chain already routes `object` through PrimitiveObjectFormatter
        public static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Default;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class ExpandoObjectResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(MessagePackFormatterFactory.Combine(
            new ExpandoObjectFormatterFactory(),
            MessagePackFormatterFactory.Default.WithContractless().WithTypeless(TypelessTypeLoader.LoadAnyType())));

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class NativeDateTimeResolver
    {
        public static MessagePackSerializerOptions Options { get; } = V3CompatChains.ToOptions(MessagePackFormatterFactory.DotNetOptimized);

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class NativeGuidResolver
    {
        public static MessagePackSerializerOptions Options => NativeDateTimeResolver.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }

    public static class NativeDecimalResolver
    {
        public static MessagePackSerializerOptions Options => NativeDateTimeResolver.Options;

        public static MessagePackFormatterResolver Instance => Options.Resolver;
    }
}

// global namespace: several ported files live outside any namespace, so the substitution
// targets must be visible from everywhere

/// <summary>
/// Replaces v3's <c>MessagePackSerializer.Typeless</c> entry point (v4 has no static
/// facade; typeless is chain composition). Ported files were text-substituted from
/// <c>MessagePackSerializer.Typeless.*</c> to <c>TypelessCompat.*</c>.
/// </summary>
public static class TypelessCompat
{
    public static MessagePack.MessagePackSerializerOptions DefaultOptions { get; } = MessagePack.Resolvers.TypelessObjectResolver.Options;

    public static byte[] Serialize(object value, MessagePack.MessagePackSerializerOptions options = null) =>
        MessagePack.MessagePackSerializer.Serialize<object>(value, options ?? DefaultOptions);

    public static object Deserialize(byte[] bytes, MessagePack.MessagePackSerializerOptions options = null) =>
        MessagePack.MessagePackSerializer.Deserialize<object>(bytes, options ?? DefaultOptions);

    public static object Deserialize(ReadOnlyMemory<byte> bytes, MessagePack.MessagePackSerializerOptions options = null) =>
        MessagePack.MessagePackSerializer.Deserialize<object>(bytes.Span, options ?? DefaultOptions);
}

/// <summary>
/// Replaces v3's <c>V3JsonCompat.SerializeToJson</c> (v4 keeps only ConvertToJson).
/// Ported files were text-substituted to call this.
/// </summary>
public static class V3JsonCompat
{
    public static string SerializeToJson<T>(T value, MessagePack.MessagePackSerializerOptions options = null)
    {
        options ??= MessagePack.MessagePackSerializerOptions.Default;
        return MessagePack.MessagePackSerializer.ConvertToJson(MessagePack.MessagePackSerializer.Serialize(value, options));
    }
}

namespace MessagePack
{
    public static class V3CompatResolverExtensions
    {
        /// <summary>
        /// v3 IFormatterResolver.GetFormatterWithVerify&lt;T&gt; over the v4 resolver, bound to the
        /// byte[]-path buffer pair. Only the ported tests' uses are served: forcing formatter
        /// construction (and observing its exceptions).
        /// </summary>
        public static object GetFormatterWithVerify<T>(this MessagePackFormatterResolver resolver) =>
            resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, T>();

        /// <inheritdoc cref="GetFormatterWithVerify{T}(MessagePackFormatterResolver)"/>
        public static object GetFormatter<T>(this MessagePackFormatterResolver resolver) =>
            resolver.GetFormatter<SerializerFoundation.ArrayPoolListWriteBuffer, SerializerFoundation.ReadOnlySpanReadBuffer, T>();
    }
}
