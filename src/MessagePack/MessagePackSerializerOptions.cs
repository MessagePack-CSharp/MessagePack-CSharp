// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

using System;

namespace MessagePack
{
#if !DYNAMICCODEDUMPER
    /// <summary>
    /// An immutable description of options for running the <see cref="MessagePackSerializer"/>.
    /// </summary>
#endif
    public class MessagePackSerializerOptions
    {
#if !DYNAMICCODEDUMPER
        /// <summary>
        /// A good default set of options that uses the <see cref="Resolvers.StandardResolver"/> and no compression.
        /// </summary>
        public static readonly MessagePackSerializerOptions Default = new MessagePackSerializerOptions();

        /// <summary>
        /// A good default set of options that includes LZ4 compression and uses the <see cref="Resolvers.StandardResolver"/>.
        /// </summary>
        public static readonly MessagePackSerializerOptions LZ4Default = Default.WithLZ4Compression(true);

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializerOptions"/> class
        /// with default options.
        /// </summary>
        private MessagePackSerializerOptions()
            : this(Resolvers.StandardResolver.Instance, useLZ4Compression: false)
        {
        }
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializerOptions"/> class
        /// </summary>
        internal MessagePackSerializerOptions(IFormatterResolver resolver, bool useLZ4Compression = false)
        {
            this.Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.UseLZ4Compression = useLZ4Compression;
        }

        /// <summary>
        /// Gets the resolver to use for complex types.
        /// </summary>
        /// <value>An instance of <see cref="IFormatterResolver"/>. Never <c>null</c>.</value>
        /// <exception cref="ArgumentNullException">Thrown if an attempt is made to set this property to <c>null</c>.</exception>
        public IFormatterResolver Resolver { get; }

        /// <summary>
        /// Gets a value indicating whether to apply LZ4 to the MessagePack stream.
        /// </summary>
        /// <remarks>
        /// When set to <c>true</c>, uncompressed MessagePack can still be deserialized and a small MessagePack stream may not be compressed.
        /// </remarks>
        public bool UseLZ4Compression { get; }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Resolver"/> property set to a new value.
        /// </summary>
        /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithResolver(IFormatterResolver resolver) => this.Resolver != resolver ? new MessagePackSerializerOptions(resolver, this.UseLZ4Compression) : this;

        /// <summary>
        /// Gets a copy of these options with the <see cref="UseLZ4Compression"/> property set to a new value.
        /// </summary>
        /// <param name="useLZ4Compression">The new value for the <see cref="UseLZ4Compression"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithLZ4Compression(bool useLZ4Compression = true) => this.UseLZ4Compression != useLZ4Compression ? new MessagePackSerializerOptions(this.Resolver, useLZ4Compression) : this;
    }
}
