// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;

namespace MessagePack.Formatters
{
    // Note:This implementation is 'not' fastest, should more improve.
    public sealed class EnumAsStringFormatter<T> : IMessagePackFormatter<T>
        where T : struct, Enum
    {
        private readonly IReadOnlyDictionary<string, T> nameValueMapping;
        private readonly IReadOnlyDictionary<T, string> valueNameMapping;
        private readonly IReadOnlyDictionary<string, string>? clrToSerializationName;
        private readonly IReadOnlyDictionary<string, string>? serializationToClrName;
        private readonly bool isFlags;

        public EnumAsStringFormatter()
        {
            this.isFlags = typeof(T).GetCustomAttribute<FlagsAttribute>() is object;

            var fields = typeof(T).GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
            var nameValueMapping = new Dictionary<string, T>(fields.Length);
            var valueNameMapping = new Dictionary<T, string>();
            Dictionary<string, string>? clrToSerializationName = null;
            Dictionary<string, string>? serializationToClrName = null;

            foreach (FieldInfo enumValueMember in fields)
            {
                string name = enumValueMember.Name;
                T value = (T)enumValueMember.GetValue(null)!;

                // Consider the case where the serialized form of the enum value is overridden via an attribute.
                var attribute = enumValueMember.GetCustomAttribute<EnumMemberAttribute>();
                if (attribute is { IsValueSetExplicitly: true, Value: not null })
                {
                    clrToSerializationName ??= new();
                    serializationToClrName ??= new();

                    clrToSerializationName.Add(name, attribute.Value);
                    serializationToClrName.Add(attribute.Value, name);

                    name = attribute.Value;
                }

                nameValueMapping[name] = value;
                valueNameMapping[value] = name;
            }

            this.nameValueMapping = nameValueMapping;
            this.valueNameMapping = valueNameMapping;
            this.clrToSerializationName = clrToSerializationName;
            this.serializationToClrName = serializationToClrName;
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            // Enum.ToString() is slow, so avoid it when we can.
            if (!this.valueNameMapping.TryGetValue(value, out string? valueString))
            {
                // fallback for flags, values with no name, etc
                valueString = this.GetSerializedNames(value.ToString());
            }

            writer.Write(valueString);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            string? name = reader.ReadString();
            if (name is null)
            {
                MessagePackSerializationException.ThrowUnexpectedNilWhileDeserializing<T>();
            }

            // Avoid Enum.Parse when we can because it is too slow.
            if (!this.nameValueMapping.TryGetValue(name, out T value))
            {
                value = (T)Enum.Parse(typeof(T), this.GetClrNames(name));
            }

            return value;
        }

        private string GetClrNames(string serializedNames)
        {
            if (this.serializationToClrName is not null && this.isFlags && serializedNames.IndexOf(", ", StringComparison.Ordinal) >= 0)
            {
                return Translate(serializedNames, this.serializationToClrName);
            }

            // We don't need to consider the trivial case of no commas because our caller would have found that in the lookup table and not called us.
            return serializedNames;
        }

        private string GetSerializedNames(string clrNames)
        {
            if (this.clrToSerializationName is not null && this.isFlags && clrNames.IndexOf(", ", StringComparison.Ordinal) >= 0)
            {
                return Translate(clrNames, this.clrToSerializationName);
            }

            // We don't need to consider the trivial case of no commas because our caller would have found that in the lookup table and not called us.
            return clrNames;
        }

        private static string Translate(string items, IReadOnlyDictionary<string, string> mapping)
        {
            string[] elements = items.Split(',');

            for (int i = 0; i < elements.Length; i++)
            {
                // Trim the leading space if there is one (due to the delimiter being ", ").
                if (i > 0 && elements[i].Length > 0 && elements[i][0] == ' ')
                {
                    elements[i] = elements[i].Substring(1);
                }

                if (mapping.TryGetValue(elements[i], out string? substituteValue))
                {
                    elements[i] = substituteValue;
                }
            }

            return string.Join(", ", elements);
        }
    }
}
