namespace MessagePack.AspNetCoreMvcFormatter
{
    public class LZ4MessagePackOutputFormatter : MessagePackOutputFormatter
    {
        public LZ4MessagePackOutputFormatter()
            : base(new LZ4MessagePackSerializer.NonGeneric())
        {
        }

        public LZ4MessagePackOutputFormatter(IFormatterResolver resolver)
            : base(new LZ4MessagePackSerializer.NonGeneric(resolver))
        {
        }
    }
}
