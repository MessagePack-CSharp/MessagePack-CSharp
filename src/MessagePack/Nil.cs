using System;
using System.Buffers;

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

        public void Serialize(ref MessagePackWriter writer, Nil value, IFormatterResolver typeResolver)
           
        {
            writer.WriteNil();
        }

        public Nil Deserialize(ref MessagePackReader reader, IFormatterResolver typeResolver)
        {
            return reader.ReadNil();
        }
    }

    // NullableNil is same as Nil.
    public class NullableNilFormatter : IMessagePackFormatter<Nil?>
    {
        public static readonly IMessagePackFormatter<Nil?> Instance = new NullableNilFormatter();

        NullableNilFormatter()
        {

        }

        public void Serialize(ref MessagePackWriter writer, Nil? value, IFormatterResolver typeResolver)
           
        {
            writer.WriteNil();
        }

        public Nil? Deserialize(ref MessagePackReader reader, IFormatterResolver typeResolver)
        {
            return reader.ReadNil();
        }
    }
}