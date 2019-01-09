using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class LZ4MessagePackInputFormatter : IInputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private readonly IFormatterResolver resolver;

        public LZ4MessagePackInputFormatter() : this(null)
        {
        }

        public LZ4MessagePackInputFormatter(IFormatterResolver resolver)
        {
            this.resolver = resolver ?? MessagePackSerializer.DefaultResolver;
        }

        public bool CanRead(InputFormatterContext context) => 
            context.HttpContext.Request.ContentType == ContentType;

        public Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = LZ4MessagePackSerializer.NonGeneric.Deserialize(context.ModelType, request.Body, resolver);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}
