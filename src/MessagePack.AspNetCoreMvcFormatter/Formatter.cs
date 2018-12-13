using Microsoft.AspNetCore.Mvc.Formatters;
using System.Threading.Tasks;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackOutputFormatter : IOutputFormatter //, IApiResponseTypeMetadataProvider
    {
        const string ContentType = "application/x-msgpack";
        static readonly string[] SupportedContentTypes = new[] { ContentType };

        readonly MessagePackSerializer.NonGeneric serializer;

        public MessagePackOutputFormatter()
            : this((IFormatterResolver)null)
        {
        }

        public MessagePackOutputFormatter(IFormatterResolver resolver)
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer(resolver)))
        {
        }

        public MessagePackOutputFormatter(MessagePackSerializer.NonGeneric serializer)
        {
            this.serializer = serializer ?? new MessagePackSerializer.NonGeneric(new MessagePackSerializer());
        }

        //public IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        //{
        //    return SupportedContentTypes;
        //}

        public bool CanWriteResult(OutputFormatterCanWriteContext context)
        {
            return true;
        }

        public Task WriteAsync(OutputFormatterWriteContext context)
        {
            context.HttpContext.Response.ContentType = ContentType;

            // 'object' want to use anonymous type serialize, etc...
            if (context.ObjectType == typeof(object))
            {
                if (context.Object == null)
                {
                    context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
                    return Task.CompletedTask;
                }
                else
                {
                    // use concrete type.
                    serializer.Serialize(context.Object.GetType(), context.HttpContext.Response.Body, context.Object);
                    return Task.CompletedTask;
                }
            }
            else
            {
                serializer.Serialize(context.ObjectType, context.HttpContext.Response.Body, context.Object);
                return Task.CompletedTask;
            }
        }
    }

    public class MessagePackInputFormatter : IInputFormatter // , IApiRequestFormatMetadataProvider
    {
        const string ContentType = "application/x-msgpack";
        static readonly string[] SupportedContentTypes = new[] { ContentType };

        readonly MessagePackSerializer.NonGeneric serializer;

        public MessagePackInputFormatter()
            : this((IFormatterResolver)null)
        {
        }

        public MessagePackInputFormatter(IFormatterResolver resolver)
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer(resolver)))
        {
        }

        public MessagePackInputFormatter(MessagePackSerializer.NonGeneric serializer)
        {
            this.serializer = serializer ?? new MessagePackSerializer.NonGeneric(new MessagePackSerializer());
        }

        //public IReadOnlyList<string> GetSupportedContentTypes(string contentType, Type objectType)
        //{
        //    return SupportedContentTypes;
        //}

        public bool CanRead(InputFormatterContext context)
        {
            return true;
        }

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = serializer.Deserialize(context.ModelType, request.Body);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}