// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Internal;

namespace MessagePack.Formatters
{
    /// <summary>
    /// Extension methods for formatter instances.
    /// </summary>
    public static class MessagePackFormatterExtensions
    {
        /// <summary>
        /// Deserializes into an existing instance when the formatter supports it.
        /// </summary>
        /// <typeparam name="T">The reference type to deserialize.</typeparam>
        /// <param name="formatter">The formatter instance to use.</param>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="value">The existing instance to populate.</param>
        /// <param name="options">The serialization settings to use.</param>
        /// <returns>The populated instance, a newly created instance, or <see langword="null"/> when the payload is nil.</returns>
        public static T? DeserializeInto<T>(this IMessagePackFormatter<T?> formatter, ref MessagePackReader reader, T? value, MessagePackSerializerOptions options)
            where T : class
        {
            if (formatter is null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            return FormatterDispatchInto<T>.Deserialize(formatter, ref reader, value, options);
        }
    }
}
