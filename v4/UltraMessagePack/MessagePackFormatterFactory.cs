using SerializerFoundation;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using UltraMessagePack.Formatters;

namespace UltraMessagePack;

// The default factory chain. Order matters: Primitive first (also claims byte[] as bin),
// then the generic collection shapes, then user registrations.
public static class DefaultFormatterFactory
{
    internal const string RequiresDynamicCodeMessage =
        "The default chain contains GenericFormatterFactory, which closes collection/Nullable/enum formatters " +
        "over runtime element types via MakeGenericType. For Native AOT, compose an explicit chain instead " +
        "(e.g. the source-generated factory + PrimitiveFormatterFactory).";

    static IMessagePackFormatterFactory? instance;

    /// <summary>
    /// The default chain (Primitive → Generic → Registry). Acquisition requires dynamic
    /// code because of the Generic link; the property is lazy so that merely loading this
    /// type stays AOT-clean (a static readonly field would run the annotated construction
    /// in the type initializer, where no annotation can gate it). The ??= race is benign:
    /// both composites are equivalent and stateless.
    /// </summary>
    public static IMessagePackFormatterFactory Instance
    {
        [RequiresDynamicCode(RequiresDynamicCodeMessage)]
        get => instance ??= new CompositeFormatterFactory(
            PrimitiveFormatterFactory.Instance,
            GenericFormatterFactory.Instance,
            FormatterRegistry.Instance);
    }
}

/// <summary>A factory over factories: asks each in order, first non-null wins.</summary>
public sealed class CompositeFormatterFactory : IMessagePackFormatterFactory
{
    readonly IMessagePackFormatterFactory[] factories;

    public CompositeFormatterFactory(params IMessagePackFormatterFactory[] factories)
    {
        this.factories = factories;
    }

#if NET9_0_OR_GREATER
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        foreach (var factory in factories)
        {
            var created = factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
            if (created != null)
            {
                return created;
            }
        }
        return null;
    }
#endif

    public object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        foreach (var factory in factories)
        {
            var created = factory.CreateFormatter(writeBufferType, readBufferType, valueType);
            if (created != null)
            {
                return created;
            }
        }
        return null;
    }
}

public sealed partial class PrimitiveFormatterFactory : IMessagePackFormatterFactory
{
    public static readonly PrimitiveFormatterFactory Instance = new PrimitiveFormatterFactory();

    PrimitiveFormatterFactory()
    {
    }

    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
    {
        if (type == typeof(int)) return new Int32Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(long)) return new Int64Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(short)) return new Int16Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(byte)) return new ByteFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(sbyte)) return new SByteFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(uint)) return new UInt32Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ulong)) return new UInt64Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ushort)) return new UInt16Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(char)) return new CharFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(bool)) return new BooleanFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(float)) return new SingleFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(double)) return new DoubleFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(string)) return new StringFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(DateTime)) return new DateTimeFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(byte[])) return new ByteArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(int[])) return new Int32ArrayFormatter<TWriteBuffer, TReadBuffer>(); // SIMD fixint-run formatter
        return null;
    }
}

// AOT annotation pattern (same as System.Text.Json's DefaultJsonTypeInfoResolver): the
// interface stays UNANNOTATED — annotating IMessagePackFormatterFactory.CreateFormatter
// would poison every implementation including the AOT-safe generated one, and annotating
// only the implementation would be an IL3051 interface/implementation mismatch. Instead
// the requirement sits on the ACQUISITION points (constructor / Instance): you can only
// hold a GenericFormatterFactory after opting into dynamic code, so the interface calls
// on it need no annotation and the internal warnings are suppressed against that gate.
public sealed class GenericFormatterFactory : IMessagePackFormatterFactory
{
    static GenericFormatterFactory? instance;

    public static GenericFormatterFactory Instance
    {
        [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
        get => instance ??= new GenericFormatterFactory(); // benign race: stateless singleton
    }

    [RequiresDynamicCode(DefaultFormatterFactory.RequiresDynamicCodeMessage)]
    GenericFormatterFactory()
    {
    }

    // Closes an element-generic factory over the runtime ELEMENT types — reflection only
    // ever touches the element types; the buffer types travel through the CreateFormatter
    // call on the returned factory (as generic parameters on modern TFMs — they may be
    // ref structs, which MakeGenericType must not see — and as Types downlevel).
    // byte[]/int[] never reach here: PrimitiveFormatterFactory claims them first (bin
    // format, not an element array).
    [UnconditionalSuppressMessage("AOT", "IL3050",
        Justification = "instances only exist behind the RequiresDynamicCode constructor/Instance; the caller has already opted into dynamic code")]
    [UnconditionalSuppressMessage("Trimming", "IL2055",
        Justification = "the closed factory types are ArrayFormatterFactory<>/ListFormatterFactory<>/DictionaryFormatterFactory<,>/NullableFormatterFactory<>/EnumFormatterFactory<> only; all are rooted by the typeof references below")]
    [UnconditionalSuppressMessage("Trimming", "IL2071",
        Justification = "EnumFormatterFactory<T>'s T is always an enum (guarded by type.IsEnum); value types always satisfy PublicParameterlessConstructor")]
    static IMessagePackFormatterFactory? CreateElementFactory(Type type)
    {
        Type? factoryType = null;
        if (type.IsArray)
        {
            factoryType = typeof(ArrayFormatterFactory<>).MakeGenericType(type.GetElementType()!);
        }
        else if (type.IsGenericType)
        {
            var definition = type.GetGenericTypeDefinition();
            if (definition == typeof(List<>))
            {
                factoryType = typeof(ListFormatterFactory<>).MakeGenericType(type.GetGenericArguments());
            }
            else if (definition == typeof(Dictionary<,>))
            {
                factoryType = typeof(DictionaryFormatterFactory<,>).MakeGenericType(type.GetGenericArguments());
            }
            else if (definition == typeof(Nullable<>))
            {
                factoryType = typeof(NullableFormatterFactory<>).MakeGenericType(type.GetGenericArguments());
            }
        }
        else if (type.IsEnum)
        {
            factoryType = typeof(EnumFormatterFactory<>).MakeGenericType(type);
        }
        if (factoryType == null)
        {
            return null;
        }
        return (IMessagePackFormatterFactory)Activator.CreateInstance(factoryType)!;
    }

#if NET9_0_OR_GREATER
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return CreateElementFactory(type)?.CreateFormatter<TWriteBuffer, TReadBuffer>(type);
    }
#endif

    public object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return CreateElementFactory(valueType)?.CreateFormatter(writeBufferType, readBufferType, valueType);
    }
}

public sealed class FormatterRegistry : IMessagePackFormatterFactory
{
    public static readonly FormatterRegistry Instance = new FormatterRegistry();

    ConcurrentDictionary<Type, IMessagePackFormatterFactory> factories = new ConcurrentDictionary<Type, IMessagePackFormatterFactory>();

    FormatterRegistry()
    {
    }

    public void Register(Type type, IMessagePackFormatterFactory factory)
    {
        factories[type] = factory;
    }

    /// <summary>Typed sugar for <see cref="Register"/>. A factory registered under a
    /// type key is only ever asked for that type and may ignore the parameter.</summary>
    public void RegisterFactory<T>(IMessagePackFormatterFactory factory)
    {
        factories[typeof(T)] = factory;
    }

#if NET9_0_OR_GREATER
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer, allows ref struct
        where TReadBuffer : struct, IReadBuffer, allows ref struct
    {
        return factories.TryGetValue(type, out var factory)
            ? factory.CreateFormatter<TWriteBuffer, TReadBuffer>(type)
            : null;
    }
#endif

    public object? CreateFormatter(Type writeBufferType, Type readBufferType, Type valueType)
    {
        return factories.TryGetValue(valueType, out var factory)
            ? factory.CreateFormatter(writeBufferType, readBufferType, valueType)
            : null;
    }
}
