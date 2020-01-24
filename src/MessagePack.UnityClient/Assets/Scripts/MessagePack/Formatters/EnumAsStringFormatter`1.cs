// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace MessagePack.Formatters
{
    // Note:This implementation is 'not' fastest, should more improve.
    public sealed class EnumAsStringFormatter<T> : IMessagePackFormatter<T>
    {
        private readonly Dictionary<string, T> nameValueMapping;
        private readonly Dictionary<T, string> valueNameMapping;
        private readonly Dictionary<string, T> enumMemberMapping;
        private readonly Dictionary<string, string> nameToEnumMemberMapping;

        public EnumAsStringFormatter()
        {
            Type type = typeof(T);
            var names = Enum.GetNames(type);
            Array values = Enum.GetValues(type);
            this.nameValueMapping = new Dictionary<string, T>(names.Length);
            this.valueNameMapping = new Dictionary<T, string>();
            enumMemberMapping = new Dictionary<string, T>();
            nameToEnumMemberMapping = new Dictionary<string, string>();
            for (int i = 0; i < names.Length; i++)
            {
                this.nameValueMapping[names[i]] = (T)values.GetValue(i);
                this.valueNameMapping[(T)values.GetValue(i)] = names[i];
                var em = type.GetMember(names[i]).FirstOrDefault()?.GetCustomAttributes(false).OfType<EnumMemberAttribute>().FirstOrDefault();
                if (em != null)
                {
                    enumMemberMapping.Add(em.Value, (T)values.GetValue(i));
                    nameToEnumMemberMapping.Add(names[i], em.Value);
                }
            }
        }

        public void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options)
        {
            if (!this.nameToEnumMemberMapping.TryGetValue(value.ToString(), out var name))
            {
                if (!this.valueNameMapping.TryGetValue(value, out name))
                {
                    name = value.ToString(); // fallback for flags etc, But Enum.ToString is too slow.
                }
            }

            writer.Write(name);
        }

        public T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            var name = reader.ReadString();

            T value;
            if (this.enumMemberMapping.TryGetValue(name, out value))
            {
                return value;
            }

            if (!this.nameValueMapping.TryGetValue(name, out value))
            {
                value = (T)Enum.Parse(typeof(T), name); // Enum.Parse is too slow
            }

            return value;
        }
    }
}
