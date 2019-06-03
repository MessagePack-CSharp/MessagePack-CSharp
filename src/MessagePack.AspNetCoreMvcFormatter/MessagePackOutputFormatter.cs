using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackOutputFormatter : IOutputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly MessagePackSerializer.NonGeneric serializer;

        public MessagePackOutputFormatter()
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer()))
        {
        }

        public MessagePackOutputFormatter(IFormatterResolver resolver)
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer(resolver)))
        {
        }

        public MessagePackOutputFormatter(MessagePackSerializer.NonGeneric serializer)
        {
            this.serializer = serializer ?? new MessagePackSerializer.NonGeneric();
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
                    this.serializer.Serialize(context.Object.GetType(), context.HttpContext.Response.Body, context.Object);
                    return Task.CompletedTask;
                }
            }
            else
            {
                this.serializer.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object);
                return Task.CompletedTask;
            }
        }
    }
}
