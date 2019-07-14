// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                    return MessagePackSerializer.SerializeAsync(context.Object.GetType(), context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
                }
            }
            else
            {
                return MessagePackSerializer.SerializeAsync(context.ObjectType, context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
            }
        }
    }
}
