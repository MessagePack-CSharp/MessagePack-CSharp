﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace MessagePack.AspNetCoreMvcFormatter
{
    public class MessagePackOutputFormatter : OutputFormatter
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

            SupportedMediaTypes.Add(ContentType);
        }

        public override Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
        {
            if (context.ObjectType == typeof(object))
            {
                if (context.Object == null)
                {
#if NETSTANDARD2_0
                    context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
                    return Task.CompletedTask;
#else
                    var writer = context.HttpContext.Response.BodyWriter;
                    if (writer == null)
                    {
                        context.HttpContext.Response.Body.WriteByte(MessagePackCode.Nil);
                        return Task.CompletedTask;
                    }

                    var span = writer.GetSpan(1);
                    span[0] = MessagePackCode.Nil;
                    writer.Advance(1);
                    return writer.FlushAsync().AsTask();
#endif
                }
                else
                {
#if NETSTANDARD2_0
                    return MessagePackSerializer.SerializeAsync(context.Object.GetType(), context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
#else
                    var writer = context.HttpContext.Response.BodyWriter;
                    if (writer == null)
                    {
                        return MessagePackSerializer.SerializeAsync(context.Object.GetType(), context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
                    }

                    MessagePackSerializer.Serialize(context.Object.GetType(), writer, context.Object, this.options, context.HttpContext.RequestAborted);
                    return writer.FlushAsync().AsTask();
#endif
                }
            }
            else
            {
#if NETSTANDARD2_0
                return MessagePackSerializer.SerializeAsync(context.ObjectType, context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
#else
                var writer = context.HttpContext.Response.BodyWriter;
                if (writer == null)
                {
                    return MessagePackSerializer.SerializeAsync(context.ObjectType, context.HttpContext.Response.Body, context.Object, this.options, context.HttpContext.RequestAborted);
                }

                MessagePackSerializer.Serialize(context.ObjectType, writer, context.Object, this.options, context.HttpContext.RequestAborted);
                return writer.FlushAsync().AsTask();
#endif
            }
        }
    }
}
