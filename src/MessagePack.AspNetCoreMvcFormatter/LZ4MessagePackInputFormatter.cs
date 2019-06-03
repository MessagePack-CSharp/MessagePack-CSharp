namespace MessagePack.AspNetCoreMvcFormatter
{
    public class LZ4MessagePackInputFormatter : MessagePackInputFormatter
    {
        public LZ4MessagePackInputFormatter()
            : base(new LZ4MessagePackSerializer.NonGeneric())
        {
        }

        public LZ4MessagePackInputFormatter(IFormatterResolver resolver)
            : base(new LZ4MessagePackSerializer.NonGeneric(resolver))
        {
        }
    }
}
