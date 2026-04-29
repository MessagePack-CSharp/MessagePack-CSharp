// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackInputFormatter : InputFormatter
    {
        private const string ContentType = "application/x-msgpack";
        private static readonly MessagePackSerializerOptions DefaultOptions = MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData);
        private readonly MessagePackSerializerOptions options;

        public MessagePackInputFormatter()
            : this(DefaultOptions)
        {
        }

        public MessagePackInputFormatter(MessagePackSerializerOptions? options)
        {
            this.options = options ?? DefaultOptions;

            SupportedMediaTypes.Add(ContentType);
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context)
        {
            var request = context.HttpContext.Request;
            var result = await MessagePackSerializer.DeserializeAsync(context.ModelType, request.Body, this.options, context.HttpContext.RequestAborted).ConfigureAwait(false);
            return await InputFormatterResult.SuccessAsync(result).ConfigureAwait(false);
        }
    }
}
