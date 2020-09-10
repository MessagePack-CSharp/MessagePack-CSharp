// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Dynamic;
using MessagePack.Formatters;

namespace MessagePack.Resolvers
{
    /// <summary>
    /// A resolver for use when deserializing MessagePack data where the schema is not known at compile-time
    /// such that strong-types can be instantiated.
    /// Instead, <see cref="ExpandoObject"/> is used wherever a MessagePack <em>map</em> is encountered.
    /// </summary>
    public static class ExpandoObjectResolver
    {
        /// <summary>
        /// The resolver to use to deserialize into C#'s <c>dynamic</c> keyword.
        /// </summary>
        /// <remarks>
        /// This resolver includes more than just the <see cref="ExpandoObjectFormatter"/>.
        /// </remarks>
        public static readonly IFormatterResolver Instance = CompositeResolver.Create(
            new IMessagePackFormatter[]
            {
                ExpandoObjectFormatter.Instance,
                new PrimitiveObjectWithExpandoMaps(),
            },
            new IFormatterResolver[] { BuiltinResolver.Instance });

        /// <summary>
        /// A set of options that includes the <see cref="Instance"/>
        /// and puts the deserializer into <see cref="MessagePackSecurity.UntrustedData"/> mode.
        /// </summary>
        public static readonly MessagePackSerializerOptions Options = MessagePackSerializerOptions.Standard
            .WithSecurity(MessagePackSecurity.UntrustedData) // when the schema isn't known beforehand, that generally suggests you don't know/trust the data.
            .WithResolver(Instance);

        private class PrimitiveObjectWithExpandoMaps : PrimitiveObjectFormatter
        {
            protected override object DeserializeMap(ref MessagePackReader reader, int length, MessagePackSerializerOptions options)
            {
                IMessagePackFormatter<string> keyFormatter = options.Resolver.GetFormatterWithVerify<string>();
                IMessagePackFormatter<object> objectFormatter = options.Resolver.GetFormatter<object>();
                IDictionary<string, object> dictionary = new ExpandoObject();
                for (int i = 0; i < length; i++)
                {
                    var key = keyFormatter.Deserialize(ref reader, options);
                    var value = objectFormatter.Deserialize(ref reader, options);
                    dictionary.Add(key, value);
                }

                return dictionary;
            }
        }
    }
}
