// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        public async Task<InputFormatterResult> ReadAsync(InputFormatterContext context)
        {
            Microsoft.AspNetCore.Http.HttpRequest request = context.HttpContext.Request;
            object result = await MessagePackSerializer.DeserializeAsync(context.ModelType, request.Body, this.options, context.HttpContext.RequestAborted);
            return await InputFormatterResult.SuccessAsync(result);
        }
    }
}
