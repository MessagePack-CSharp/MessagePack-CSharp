namespace MessagePack.AspNetCoreMvcFormatter
{
    /// <summary>
    ///     MessagePack MVC formatter options.
    /// </summary>
    public class MessagePackFormatterOptions
    {
        public IFormatterResolver FormatterResolver { get; set; }
    }
}