using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class LZ4MessagePackOutputFormatter : IOutputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly IFormatterResolver resolver;

        public LZ4MessagePackOutputFormatter() : this(null)
        {
        }

        public LZ4MessagePackOutputFormatter(IFormatterResolver resolver)
        {
            this.resolver = resolver ?? MessagePackSerializer.DefaultResolver;
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
                    LZ4MessagePackSerializer.NonGeneric.Serialize(context.Object.GetType(), context.HttpContext.Response.Body, context.Object, resolver);
                    return Task.CompletedTask;
                }
            }
            else
            {
                LZ4MessagePackSerializer.NonGeneric.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object, resolver);
                return Task.CompletedTask;
            }
        }
    }
}
