using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    /// <summary>
    ///     MVC output formatter.
    /// </summary>
    public class MessagePackOutputFormatter : OutputFormatter
    {
        /// <summary>
        ///     Supported media type.
        /// </summary>
        public const string ContentType = "application/x-msgpack";

        private readonly IFormatterResolver _resolver;

        /// <inheritdoc />
        public MessagePackOutputFormatter()
            : this(null)
        {
        }

        public MessagePackOutputFormatter(IFormatterResolver resolver)
        {
            _resolver = resolver ?? MessagePackSerializer.DefaultResolver;
            SupportedMediaTypes.Add(ContentType);
        }

        /// <inheritdoc />
        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context.ObjectType == typeof(object))
            {
                if (context.Object == null)
                {
                    context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
                    return Task.CompletedTask;
                }

                // use concrete type.
                MessagePackSerializer.NonGeneric.Serialize(context.Object.GetType(),
                    context.HttpContext.Response.Body, context.Object, _resolver);
                return Task.CompletedTask;
            }

            MessagePackSerializer.NonGeneric.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object, _resolver);
            return Task.CompletedTask;
        }
    }
}
