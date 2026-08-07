using System.Collections;
using System.Text;
using UltraMessagePack.Formatters;

namespace UltraMessagePack;

public sealed partial class BuiltInFormatterFactory : MessagePackFormatterFactory
{
    public static readonly BuiltInFormatterFactory Instance = new BuiltInFormatterFactory();

    BuiltInFormatterFactory()
    {
    }

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
        // PrimitiveFormatters.cs
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

        // NilFormatters.cs
        if (type == typeof(Nil)) return new NilFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Nil?)) return new NullableNilFormatter<TWriteBuffer, TReadBuffer>();

        // NumericFormatters.cs
        if (type == typeof(System.Numerics.BigInteger)) return new BigIntegerFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Complex)) return new ComplexFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Vector2)) return new Vector2Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Vector3)) return new Vector3Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Vector4)) return new Vector4Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Quaternion)) return new QuaternionFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Matrix3x2)) return new Matrix3x2Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Matrix4x4)) return new Matrix4x4Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Numerics.Plane)) return new PlaneFormatter<TWriteBuffer, TReadBuffer>();

        // BclFormatters.cs
        if (type == typeof(decimal)) return new DecimalFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(TimeSpan)) return new TimeSpanFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(DateTimeOffset)) return new DateTimeOffsetFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Guid)) return new GuidFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Uri)) return new UriFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Version)) return new VersionFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(StringBuilder)) return new StringBuilderFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(BitArray)) return new BitArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Globalization.CultureInfo)) return new CultureInfoFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(TimeZoneInfo)) return new TimeZoneInfoFormatter<TWriteBuffer, TReadBuffer>();

        // NetworkFormatters.cs
        if (type == typeof(System.Net.IPAddress)) return new IPAddressFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(System.Net.IPEndPoint)) return new IPEndPointFormatter<TWriteBuffer, TReadBuffer>();
        
        // typeof(Type) is deliberately NOT served (v4 break from v3's BuiltinResolver)
        if (type == typeof(Type))
        {
            throw new InvalidOperationException(
                "Serializing System.Type is disabled by default: deserializing executes Type.GetType over payload-provided names, which can load assemblies and permanently grow the process. " +
                "Opt in by composing the factory before the default chain: new MessagePackSerializerOptions([new TypeFormatterFactory(), MessagePackFormatterFactory.Default]).");
        }

#if NET
        // BclFormatters.cs (types that do not exist downlevel)
        if (type == typeof(Half)) return new HalfFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Rune)) return new RuneFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(DateOnly)) return new DateOnlyFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(TimeOnly)) return new TimeOnlyFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Int128)) return new Int128Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(UInt128)) return new UInt128Formatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Index)) return new IndexFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Range)) return new RangeFormatter<TWriteBuffer, TReadBuffer>();
#endif

        // PrimitiveObjectFormatter.cs / NonGenericCollectionFormatters.cs
        if (type == typeof(object)) return new PrimitiveObjectFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(IEnumerable)) return new NonGenericInterfaceEnumerableFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ICollection)) return new NonGenericInterfaceCollectionFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(IList)) return new NonGenericInterfaceListFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(IDictionary)) return new NonGenericInterfaceDictionaryFormatter<TWriteBuffer, TReadBuffer>();

        // ByteArrayFormatters.cs (bin-format)
        if (type == typeof(byte[])) return new ByteArrayFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ArraySegment<byte>)) return new ByteArraySegmentFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(Memory<byte>)) return new ByteMemoryFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ReadOnlyMemory<byte>)) return new ByteReadOnlyMemoryFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(ReadOnlySequence<byte>)) return new ByteReadOnlySequenceFormatter<TWriteBuffer, TReadBuffer>();
        if (type == typeof(List<byte>)) return new ByteListFormatter<TWriteBuffer, TReadBuffer>(); // List<byte> is not serialized as a bin-format, however, we need to inject it for compatibility of previous issues

        // PrimitiveCollectionFormatters.cs
        // TODO: needs more primitive collections
        if (type == typeof(int[])) return new Int32ArrayFormatter<TWriteBuffer, TReadBuffer>();
        return null;
    }
}