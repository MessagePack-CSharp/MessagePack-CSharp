// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace MessagePack.Formatters;

public sealed class OldListFormatter<T>
    : IMessagePackFormatter<List<T>?>
{
    public void Serialize(ref MessagePackWriter writer, List<T>? value, MessagePackSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNil();
        }
        else
        {
            IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

            var c = value.Count;
            writer.WriteArrayHeader(c);
            for (int i = 0; i < c; i++)
            {
                writer.CancellationToken.ThrowIfCancellationRequested();
                formatter.Serialize(ref writer, value[i], options);
            }
        }
    }

    public List<T>? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        if (reader.TryReadNil())
        {
            return default;
        }
        else
        {
            IMessagePackFormatter<T> formatter = options.Resolver.GetFormatterWithVerify<T>();

            var len = reader.ReadArrayHeader();
            var list = new List<T>((int)len);
            options.Security.DepthStep(ref reader);
            try
            {
                for (int i = 0; i < len; i++)
                {
                    reader.CancellationToken.ThrowIfCancellationRequested();
                    list.Add(formatter.Deserialize(ref reader, options));
                }
            }
            finally
            {
                reader.Depth--;
            }

            return list;
        }
    }
}
