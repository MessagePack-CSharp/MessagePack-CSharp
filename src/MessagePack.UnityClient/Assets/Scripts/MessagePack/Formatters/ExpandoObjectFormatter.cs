// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Dynamic;

namespace MessagePack.Formatters
{
    public class ExpandoObjectFormatter : IMessagePackFormatter<ExpandoObject>
    {
        public static readonly IMessagePackFormatter<ExpandoObject> Instance = new ExpandoObjectFormatter();

        private ExpandoObjectFormatter()
        {
        }

        public ExpandoObject Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            var result = new ExpandoObject();
            int count = reader.ReadMapHeader();
            if (count > 0)
            {
                IFormatterResolver resolver = options.Resolver;
                IMessagePackFormatter<string> keyFormatter = resolver.GetFormatterWithVerify<string>();
                IMessagePackFormatter<object> valueFormatter = resolver.GetFormatterWithVerify<object>();
                IDictionary<string, object> dictionary = result;

                options.Security.DepthStep(ref reader);
                try
                {
                    for (int i = 0; i < count; i++)
                    {
                        string key = keyFormatter.Deserialize(ref reader, options);
                        object value = valueFormatter.Deserialize(ref reader, options);
                        dictionary.Add(key, value);
                    }
                }
                finally
                {
                    reader.Depth--;
                }
            }

            return result;
        }

        public void Serialize(ref MessagePackWriter writer, ExpandoObject value, MessagePackSerializerOptions options)
        {
            var dictionaryFormatter = options.Resolver.GetFormatterWithVerify<IDictionary<string, object>>();
            dictionaryFormatter.Serialize(ref writer, value, options);
        }
    }
}
