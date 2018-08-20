using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    /// <summary>
    ///     MVC Input formatter.
    /// </summary>
    public class MessagePackInputFormatter : InputFormatter
    {
        private readonly IFormatterResolver _resolver;

        /// <inheritdoc />
        public MessagePackInputFormatter()
            : this(null)
        {
        }

        /// <inheritdoc />
        public MessagePackInputFormatter(IFormatterResolver resolver)
        {
            _resolver = resolver ?? MessagePackSerializer.DefaultResolver;
            SupportedMediaTypes.Add(MessagePackOutputFormatter.ContentType);
        }

        /// <inheritdoc />
        public override Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = MessagePackSerializer.NonGeneric.Deserialize(context.ModelType, request.Body, _resolver);
            return InputFormatterResult.SuccessAsync(result);
        }
    }
}
