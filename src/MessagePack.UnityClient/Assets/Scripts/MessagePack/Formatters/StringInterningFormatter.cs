// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Formatters
{
    /// <summary>
    /// A <see cref="string" /> formatter that interns strings on deserialization.
    /// </summary>
    public sealed class StringInterningFormatter : IMessagePackFormatter<string>
    {
        /// <inheritdoc/>
        public string Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (options.StringInterning is null)
            {
                return reader.ReadString();
            }

            return options.StringInterning.GetString(ref reader);
        }

        /// <inheritdoc/>
        public void Serialize(ref MessagePackWriter writer, string value, MessagePackSerializerOptions options) => writer.Write(value);
    }
}
