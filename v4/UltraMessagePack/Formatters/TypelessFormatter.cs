using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using static UltraMessagePack.MessagePackPrimitives;

namespace UltraMessagePack.Formatters;

// TypelessFormatter exists mainly for compatibility with v3.
// The v3-compatible blacklist style is in TypelessTypeLoader.LoadAnyType, which is Obsolete.
// There is also TypelessLoader.AllowedTypes, a relatively safe whitelist style.

// v3 Typeless wire compatibility (probed against MessagePack 3.1.8):
// - Non-primitive values wrap in ext (type code 100) whose body is the concatenation of
//   the type name (str token, full assembly-qualified name) and the value serialized by
//   its concrete-type formatter. Nested object slots wrap recursively.
// - Primitives are NOT wrapped; the wire format width IS the type tag: integers use the
//   FORCED-width writers (int always int32, byte always uint8, ...), bool/float/double/
//   string/byte[]/nil use their single natural forms. The reader maps the format family
//   back to the exact CLR type.
// - char/decimal/Guid/enum/TimeSpan/DateTimeOffset wrap with their STANDARD v3 payloads
//   (identical to this library's builtin formatters), so the resolver serves them; only
//   DateTime is special inside typeless: ToBinary as smallest int64 (Kind-preserving),
//   not the timestamp ext.
public sealed partial class TypelessFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?>
{
    internal const sbyte ExtTypeCode = 100; // v3 TypelessFormatter's ext code

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
        if (typeLoader is null)
        {
            throw new ArgumentNullException(nameof(typeLoader));
        }
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

            buffer.Advance(UnsafeWriteExtHeader(ref buffer.GetReference(MaxExtHeaderLength), ExtTypeCode, (int)bodyLength));
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
        if (buffer.TryReadExtHeader(ExtTypeCode, out _))
        {
            var typeName = buffer.ReadString();
            if (typeName is null)
            {
                throw new MessagePackSerializationException("Typeless payload carries a nil type name.");
            }
            var serializer = GetSerializerByName(typeName);
            state.Enter();
            value = serializer.Deserialize(ref buffer, ref state);
            state.Exit();
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
        serializer = serializersByType.GetOrAdd(type, serializer);
        serializersByName.TryAdd(typeName, serializer);
        return serializer;
    }

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
        if (type.FullName is { } fullName && DisallowedTypes.Contains(fullName))
        {
            throw new MessagePackSerializationException($"Deserialization of type '{fullName}' is disallowed: it is a known deserialization gadget (v3's default deny list).");
        }

        var serializer = GetSerializerByType(type);
        serializersByName.TryAdd(typeName, serializer); // the payload spelling may differ from our own
        return serializer;
    }

    TypelessSerializer CreateSerializer<T>(string typeName)
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
        if (typeLoader is null)
        {
            throw new ArgumentNullException(nameof(typeLoader));
        }
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
