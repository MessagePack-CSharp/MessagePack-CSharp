// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias newmsgpack;
extern alias oldmsgpack;

#pragma warning disable SA1306 // Field names should begin with lower-case letter
#pragma warning disable SA1312 // Variable names should begin with lower-case letter

namespace PerfBenchmarkDotNet
{
    public class Resolver : newmsgpack::MessagePack.IFormatterResolver
    {
        private object formatter;

        public Resolver(object formatter)
        {
            this.formatter = formatter;
        }

        public newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return (newmsgpack::MessagePack.Formatters.IMessagePackFormatter<T>)this.formatter;
        }
    }
}

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace

