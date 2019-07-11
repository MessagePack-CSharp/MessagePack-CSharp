using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackInputFormatter : IInputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly MessagePackSerializerOptions options;

        public MessagePackInputFormatter()
            : this(null)
        {
        }

        public MessagePackInputFormatter(MessagePackSerializerOptions options)
        {
            this.options = options;
        }

        public bool CanRead(InputFormatterContext context) => 
            context.HttpContext.Request.ContentType == ContentType;

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            // TODO: switch to async typeless method when available.
            var request = context.HttpContext.Request;
            var result = MessagePackSerializer.Deserialize(context.ModelType, request.Body, this.options);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}
