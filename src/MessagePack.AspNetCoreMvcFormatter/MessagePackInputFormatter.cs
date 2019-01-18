using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackInputFormatter : IInputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly MessagePackSerializer.NonGeneric serializer;

        public MessagePackInputFormatter()
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer()))
        {
        }

        public MessagePackInputFormatter(IFormatterResolver resolver)
            : this(new MessagePackSerializer.NonGeneric(new MessagePackSerializer(resolver)))
        {
        }

        public MessagePackInputFormatter(MessagePackSerializer.NonGeneric serializer)
        {
            this.serializer = serializer ?? new MessagePackSerializer.NonGeneric();
        }

        public bool CanRead(InputFormatterContext context) => 
            context.HttpContext.Request.ContentType == ContentType;

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = this.serializer.Deserialize(context.ModelType, request.Body);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}
