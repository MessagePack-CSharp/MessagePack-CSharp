using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack.Formatters;

// MsgPack104: like PrimitiveObjectFormatter this is a self-contained mini-protocol (v3 parity); the embedded type name and runtime-typed string values are part of its wire form.
#pragma warning disable MsgPack104

// TypelessFormatter exists mainly for compatibility with v3.
// The v3-compatible blacklist style is in TypelessTypeLoader.LoadAnyType, which is Obsolete.
// There is also TypelessLoader.AllowedTypes, a relatively safe whitelist style.

public sealed partial class TypelessFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?>
{
    // v3's MessagePackSerializerOptions.DisallowedTypes verbatim: the known BinaryFormatter-era deserialization gadgets.
    static readonly HashSet<string> DisallowedTypes = new(StringComparer.Ordinal)
    {
        "Microsoft.VisualStudio.Text.Formatting.TextFormattingRunProperties",
        "System.CodeDom.Compiler.CompilerResults",
        "System.CodeDom.Compiler.TempFileCollection",
        "System.Configuration.SettingsPropertyValue",
        "System.Data.DataSet",
        "System.Data.DataTable",
        "System.Diagnostics.Process",
        "System.Diagnostics.ProcessStartInfo",
        "System.Drawing.Design.ToolboxItemContainer",
        "System.IdentityModel.Tokens.SessionSecurityToken",
        "System.Management.IWbemClassObjectFreeThreaded",
        "System.Security.Claims.ClaimsIdentity",
        "System.Security.Claims.ClaimsPrincipal",
        "System.Security.Principal.WindowsIdentity",
        "System.Security.Principal.WindowsPrincipal",
        "System.Web.Security.RolePrincipal",
        "System.Windows.Data.ObjectDataProvider",
        "System.Windows.ResourceDictionary",
        "System.Workflow.ComponentModel.Serialization.ActivitySurrogateSelector",
    };

    readonly ConcurrentDictionary<Type, TypelessSerializer> serializersByType = new();
    readonly ConcurrentDictionary<string, TypelessSerializer> serializersByName = new();
    readonly TypelessTypeLoader typeLoader;
    readonly bool omitAssemblyVersion;
    readonly PrimitiveObjectFormatter<TWriteBuffer, TReadBuffer> primitiveFallback = new();
    MessagePackFormatterResolver resolver = null!;

    // omitAssemblyVersion writes type names without Version/Culture/PublicKeyToken,
    // falling back to the full name when the short one does not resolve.
    // the v3 option of the same name.
    public TypelessFormatter(TypelessTypeLoader typeLoader, bool omitAssemblyVersion = false)
    {
        ArgumentNullException.ThrowIfNull(typeLoader);
        this.typeLoader = typeLoader;
        this.omitAssemblyVersion = omitAssemblyVersion;
    }

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        this.resolver = resolver;
        primitiveFallback.Initialize(resolver);
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, object? value)
    {
        if (value is null)
        {
            buffer.WriteNil();
            return;
        }

        var type = value.GetType();
        if (!type.IsEnum) // enums wrap; GetTypeCode would report the underlying integer
        {
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean: buffer.WriteBoolean((bool)value); return;
                case TypeCode.SByte: buffer.WriteForcedInt8((sbyte)value); return;
                case TypeCode.Byte: buffer.WriteForcedUInt8((byte)value); return;
                case TypeCode.Int16: buffer.WriteForcedInt16((short)value); return;
                case TypeCode.UInt16: buffer.WriteForcedUInt16((ushort)value); return;
                case TypeCode.Int32: buffer.WriteForcedInt32((int)value); return;
                case TypeCode.UInt32: buffer.WriteForcedUInt32((uint)value); return;
                case TypeCode.Int64: buffer.WriteForcedInt64((long)value); return;
                case TypeCode.UInt64: buffer.WriteForcedUInt64((ulong)value); return;
                case TypeCode.Single: buffer.WriteSingle((float)value); return;
                case TypeCode.Double: buffer.WriteDouble((double)value); return;
                case TypeCode.String: buffer.WriteString((string)value); return;
                    // Char, DateTime, Decimal fall through: v3 wraps them
            }
            if (type == typeof(byte[]))
            {
                buffer.WriteBinary((byte[])value);
                return;
            }
        }

        SerializeWrapped(ref buffer, ref state, value, type);
    }

    void SerializeWrapped(ref TWriteBuffer buffer, ref SerializeState state, object value, Type type)
    {
        var serializer = GetSerializerByType(type);

        // the ext header carries the body length, so the value stages first
        var staging = new CompatibleArrayPoolListWriteBuffer();
        try
        {
            state.Enter();
            serializer.Serialize(ref staging, ref state, value);
            state.Exit();

            var segments = staging.GetWrittenSegments();
            var name = serializer.EncodedTypeName;
            var bodyLength = name.Length + segments.Length;
            if (bodyLength > int.MaxValue)
            {
                throw new MessagePackSerializationException($"Typeless body for '{type.FullName}' exceeds the ext32 length limit.");
            }

            buffer.Advance(UnsafeWriteExtHeader(ref buffer.GetReference(MaxExtHeaderLength), ThisLibraryExtensionTypeCodes.TypelessFormatter, (int)bodyLength));
            buffer.Advance(UnsafeWriteRaw(ref buffer.GetReference(name.Length), name));
            while (segments.TryGetNext(out var segment))
            {
                buffer.Advance(UnsafeWriteRaw(ref buffer.GetReference(segment.Length), segment));
            }
        }
        finally
        {
            staging.Dispose();
        }
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref object? value)
    {
        if (buffer.TryReadExtHeader(ThisLibraryExtensionTypeCodes.TypelessFormatter, out var declaredBodyLength))
        {
            var bodyStart = buffer.BytesConsumed;
            var typeName = buffer.ReadString();
            if (typeName is null)
            {
                throw new MessagePackSerializationException("Typeless payload carries a nil type name.");
            }
            var serializer = GetSerializerByName(typeName);
            state.Enter();
            value = serializer.Deserialize(ref buffer, ref state);
            state.Exit();

            // the declared body length is the boundary a header-based skip (TryReadToken)
            // would use; a parse consuming a different amount means the two disagree on
            // where this ext ends (parser-differential message smuggling), so enforce equality
            var consumed = buffer.BytesConsumed - bodyStart;
            if (consumed != declaredBodyLength)
            {
                throw new MessagePackSerializationException($"Typeless ext declares a {declaredBodyLength} byte body but its content spans {consumed} bytes.");
            }
            return;
        }

        primitiveFallback.Deserialize(ref buffer, ref state, ref value);
    }

    TypelessSerializer GetSerializerByType(Type type)
    {
        if (serializersByType.TryGetValue(type, out var slot))
        {
            return slot;
        }
        return CreateSerializerForType(type);
    }

    TypelessSerializer GetSerializerByName(string typeName)
    {
        if (serializersByName.TryGetValue(typeName, out var slot))
        {
            return slot;
        }
        return CreateSerializerForName(typeName);
    }

    static readonly MethodInfo createSerializerMethod = typeof(TypelessFormatter<TWriteBuffer, TReadBuffer>).GetMethod(nameof(CreateSerializer), BindingFlags.NonPublic | BindingFlags.Instance)!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "instances only exist behind TypelessFormatterFactory's RequiresDynamicCode gate; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2060", Justification = "CreateSerializer is a private member of this formatter, rooted by the typeof/nameof reference above; its type argument is the value's runtime type, which the caller roots")]
    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "the omitAssemblyVersion probe only verifies that the shortened spelling of an already-loaded type still resolves; gated behind TypelessFormatterFactory's RequiresUnreferencedCode annotation")]
    TypelessSerializer CreateSerializerForType(Type type)
    {
        var typeName = type.AssemblyQualifiedName ?? type.FullName!;
        if (omitAssemblyVersion)
        {
            // v3 rule: use the shortened spelling only when it still resolves
            var shortened = TypelessTypeNames.SubtractFullNameRegex.Replace(typeName, string.Empty);
            if (Type.GetType(shortened, throwOnError: false) != null)
            {
                typeName = shortened;
            }
        }
        var serializer = type == typeof(DateTime)
            ? new DateTimeSerializer(typeName)
            : (TypelessSerializer)createSerializerMethod.MakeGenericMethod(type).Invoke(this, [typeName])!;
        // the write side never seeds serializersByName: that cache is the read side's, and every entry in it
        // has passed the type loader and the deny list. Seeding it here would let a type this process serialized
        // deserialize without those checks (a shared resolver makes the read-side policy depend on cache state)
        return serializersByType.GetOrAdd(type, serializer);
    }

    // Bound on payload-spelling cache entries. Type.GetType is lenient about whitespace and
    // version/culture spelling, so distinct spellings of ONE type are effectively unbounded
    // and attacker-minted; without a cap each novel spelling would pin a permanent dictionary
    // entry (memory-growth DoS, even through the AllowedTypes loader). Legit writers (this
    // process included) use one stable spelling per type, so the cap only ever bites a
    // spelling flood - which falls back to re-resolving per message: the attacker pays the
    // parse, nothing accumulates.
    const int MaxCachedTypeNameCount = 1024;

    [MethodImpl(MethodImplOptions.NoInlining)]
    TypelessSerializer CreateSerializerForName(string typeName)
    {
        if (typeName.Length > 1024)
        {
            throw new MessagePackSerializationException($"Typeless type name is implausibly long ({typeName.Length} chars).");
        }

        var type = typeLoader.LoadType(typeName);
        if (type is null)
        {
            throw new MessagePackSerializationException($"Can't load type '{typeName}'.");
        }
        ThrowIfDisallowed(type);

        var serializer = GetSerializerByType(type);
        if (serializersByName.Count < MaxCachedTypeNameCount)
        {
            serializersByName.TryAdd(typeName, serializer); // the payload spelling may differ from our own
        }
        return serializer;
    }

    // The deny list must see through wrappers. A gadget nested as Process[], Process[][],
    // List<Process>, or Nullable<Process> resolves to a type whose OWN FullName is not on
    // the list, and the array/generic formatter reconstructs the element WITHOUT re-entering
    // typeless, so an outer-type-only check would let the gadget ride in. Walk element types
    // and generic arguments so every component is vetted (v3 fixed the same gap for CVE-2026-48517).
    static void ThrowIfDisallowed(Type type)
    {
        // depth-bounded, iterative: the 1024-char type-name cap already bounds the graph;
        // the budget just makes the bound explicit rather than trusting the stack.
        var pending = new Stack<Type>();
        pending.Push(type);
        var budget = 256;
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (budget-- <= 0)
            {
                throw new MessagePackSerializationException($"Typeless type '{type.FullName}' nests too deeply to vet against the deny list.");
            }
            if (current.FullName is { } fullName && DisallowedTypes.Contains(fullName))
            {
                throw new MessagePackSerializationException($"Deserialization of type '{fullName}' is disallowed: it is a known deserialization gadget (v3's default deny list).");
            }
            if (current.HasElementType && current.GetElementType() is { } element) // array, pointer, byref
            {
                pending.Push(element);
            }
            if (current.IsConstructedGenericType)
            {
                foreach (var argument in current.GetGenericArguments())
                {
                    pending.Push(argument);
                }
            }
        }
    }

    TypelessSerializer<T> CreateSerializer<T>(string typeName)
    {
        return new TypelessSerializer<T>(typeName, resolver);
    }

    abstract class TypelessSerializer
    {
        // the full str token (header + UTF-8 name), ready for raw emission
        public readonly byte[] EncodedTypeName;

        protected TypelessSerializer(string typeName)
        {
            var utf8 = Encoding.UTF8.GetBytes(typeName);
            var token = new byte[MaxStrHeaderLength + utf8.Length];
            TryWriteString(token, utf8, out var written);
            EncodedTypeName = token.AsSpan(0, written).ToArray();
        }

        public abstract void Serialize(ref CompatibleArrayPoolListWriteBuffer staging, ref SerializeState state, object value);
        public abstract object? Deserialize(ref TReadBuffer buffer, ref DeserializeState state);
    }

    sealed class TypelessSerializer<T> : TypelessSerializer
    {
        readonly IMessagePackFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T> writeFormatter;
        readonly IMessagePackFormatter<TWriteBuffer, TReadBuffer, T> readFormatter;

        public TypelessSerializer(string typeName, MessagePackFormatterResolver resolver)
            : base(typeName)
        {
            writeFormatter = resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>();
            readFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, T>();
        }

        public override void Serialize(ref CompatibleArrayPoolListWriteBuffer staging, ref SerializeState state, object value)
        {
            writeFormatter.Serialize(ref staging, ref state, (T)value);
        }

        public override object? Deserialize(ref TReadBuffer buffer, ref DeserializeState state)
        {
            T concrete = default!;
            readFormatter.Deserialize(ref buffer, ref state, ref concrete);
            return concrete;
        }
    }

    // v3's typeless DateTime is ToBinary as smallest int64 (Kind-preserving), NOT the imestamp ext the standard formatter writes.
    sealed class DateTimeSerializer : TypelessSerializer
    {
        public DateTimeSerializer(string typeName)
            : base(typeName)
        {
        }

        public override void Serialize(ref CompatibleArrayPoolListWriteBuffer staging, ref SerializeState state, object value)
        {
            staging.WriteInt64(((DateTime)value).ToBinary());
        }

        public override object? Deserialize(ref TReadBuffer buffer, ref DeserializeState state)
        {
            return DateTime.FromBinary(buffer.ReadInt64());
        }
    }
}

// The gate/annotation texts live OUTSIDE the [Obsolete] member so that referencing them
// (annotations on non-obsolete members) does not itself trip CS0618.
internal static class TypelessMessages
{
    internal const string UntrustedData =
        "LoadAnyType executes Type.GetType over payload-provided names, which can load assemblies and instantiate unexpected types. " +
        "This exists for v3 compatibility with trusted data only; use TypelessTypeLoader.AllowedTypes (or Create) for anything less " +
        "than fully trusted input, and suppress this warning to accept the risk.";

    internal const string RequiresUnreferencedCode =
        "Typeless serialization loads types by payload-provided names and closes formatters over them via reflection; " +
        "trimming can remove those types silently. Typeless is incompatible with trimming and Native AOT.";
}

/// <summary>
/// v3's ForceTypelessFormatter: routes an interface- or abstract-typed slot through the
/// typeless formatter, so the concrete runtime type rides the payload and comes back on
/// read. Boxes on write, casts on read. Initialize resolves the chain's object formatter —
/// the typeless object factory, composed first — so the dispatcher caches, type loader and
/// deny-list vetting are shared rather than duplicated.
/// </summary>
public sealed partial class ForceTypelessFormatter<TWriteBuffer, TReadBuffer, T> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, T?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> typeless = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        typeless = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, T? value)
    {
        typeless.Serialize(ref buffer, ref state, value);
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref T? value)
    {
        object? boxed = null;
        typeless.Deserialize(ref buffer, ref state, ref boxed);
        value = (T?)boxed;
    }
}

/// <summary>
/// The tail half of typeless composition: claims interface and abstract static types with
/// <see cref="ForceTypelessFormatter{TWriteBuffer, TReadBuffer, T}"/>. Compose it LAST —
/// v3's TypelessObjectResolver sat at the end of its chain for the same reason: the
/// collection interfaces (IList&lt;T&gt;, IDictionary&lt;K,V&gt;, ...) belong to BuiltIn's
/// interface formatters and union roots to their generated formatters; only interfaces and
/// abstract bases nothing else serves fall through to the typeless envelope.
/// </summary>
public sealed class ForceTypelessFormatterFactory : GenericFormatterFactoryBase
{
    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public ForceTypelessFormatterFactory()
    {
    }

    protected override Type? GetOpenFactoryType(Type type, out Type[] typeArguments, out object?[]? constructorArguments)
    {
        typeArguments = [type];
        constructorArguments = null;
        return type.IsInterface || type.IsAbstract ? typeof(ForceTypelessFormatterFactory<>) : null;
    }
}

public sealed partial class ForceTypelessFormatterFactory<T> : MessagePackFormatterFactory
{
    // one method, two signatures: net9+ overrides the base virtual (constraints
    // inherited); downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(T) ? new ForceTypelessFormatter<TWriteBuffer, TReadBuffer, T>() : null;
    }
}

/// <summary>
/// Serves <see cref="object"/> with Typeless semantics: concrete type names embedded in the payload.
/// Compose it FIRST (before the tiers that would claim object).
/// </summary>
public sealed partial class TypelessFormatterFactory : MessagePackFormatterFactory
{
    readonly TypelessTypeLoader typeLoader;
    readonly bool omitAssemblyVersion;

    [RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(TypelessMessages.RequiresUnreferencedCode)]
    public TypelessFormatterFactory(TypelessTypeLoader typeLoader, bool omitAssemblyVersion = false)
    {
        ArgumentNullException.ThrowIfNull(typeLoader);
        this.typeLoader = typeLoader;
        this.omitAssemblyVersion = omitAssemblyVersion;
    }

#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        if (type == typeof(object))
        {
            return new TypelessFormatter<TWriteBuffer, TReadBuffer>(typeLoader, omitAssemblyVersion);
        }
        return null;
    }
}
