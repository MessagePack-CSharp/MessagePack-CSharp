using System;

namespace MessagePack
{
    public struct Nil : IEquatable<Nil>
    {
        public static readonly Nil Default = new Nil();

        public override bool Equals(object obj)
        {
            return obj is Nil;
        }

        public bool Equals(Nil other)
        {
            return true;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return "()";
        }
    }
}

namespace MessagePack.Formatters
{
    public class NilFormatter : IMessagePackFormatter<Nil>
    {
        public static readonly IMessagePackFormatter<Nil> Instance = new NilFormatter();

        NilFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Nil value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public Nil Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadNil(bytes, offset, out readSize);
        }
    }

    // NullableNil is same as Nil.
    public class NullableNilFormatter : IMessagePackFormatter<Nil?>
    {
        public static readonly IMessagePackFormatter<Nil?> Instance = new NullableNilFormatter();

        NullableNilFormatter()
        {

        }

        public int Serialize(ref byte[] bytes, int offset, Nil? value, IFormatterResolver typeResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }

        public Nil? Deserialize(byte[] bytes, int offset, IFormatterResolver typeResolver, out int readSize)
        {
            return MessagePackBinary.ReadNil(bytes, offset, out readSize);
        }
    }
}