using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackOutputFormatter : IOutputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly MessagePackSerializerOptions options;

        public MessagePackOutputFormatter()
            : this(null)
        {
        }

        public MessagePackOutputFormatter(MessagePackSerializerOptions options)
        {
            this.options = options;
        }

        public bool CanWriteResult(OutputFormatterCanWriteContext context) =>
            context.HttpContext.Request.ContentType == ContentType;

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            context.HttpContext.Response.ContentType = ContentType;

            if (context.ObjectType == typeof(object))
            {
                if (context.Object == null)
                {
                    context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
                    return Task.CompletedTask;
                }
                else
                {
                    // TODO: switch to async typeless method when available.
                    MessagePackSerializer.Serialize(context.Object.GetType(), context.HttpContext.Response.Body, context.Object, this.options);
                    return Task.CompletedTask;
                }
            }
            else
            {
                // TODO: switch to async typeless method when available.
                MessagePackSerializer.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object, this.options);
                return Task.CompletedTask;
            }
        }
    }
}
