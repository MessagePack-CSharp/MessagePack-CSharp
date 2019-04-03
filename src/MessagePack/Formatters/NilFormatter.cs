namespace MessagePack.Formatters
{
    internal class NilFormatter : IMessagePackFormatter<Nil>
    {
        internal static readonly IMessagePackFormatter<Nil> Instance = new NilFormatter();

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
    internal class NullableNilFormatter : IMessagePackFormatter<Nil?>
    {
        internal static readonly IMessagePackFormatter<Nil?> Instance = new NullableNilFormatter();

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
