// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        /// Gets a good default set of options that uses the <see cref="Resolvers.StandardResolver"/> and no compression.
        /// </summary>
        public static MessagePackSerializerOptions Standard => MessagePackSerializerOptionsDefaultSettingsLazyInitializationHelper.Standard;

        /// <summary>
        /// Gets a good default set of options that includes LZ4 compression and uses the <see cref="Resolvers.StandardResolver"/>.
        /// </summary>
        public static MessagePackSerializerOptions LZ4Standard => MessagePackSerializerOptionsDefaultSettingsLazyInitializationHelper.LZ4Standard;
#endif

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackSerializerOptions"/> class.
        /// </summary>
        internal MessagePackSerializerOptions(IFormatterResolver resolver, bool useLZ4Compression = false, bool? oldSpec = null)
        {
            this.Resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            this.UseLZ4Compression = useLZ4Compression;
            this.OldSpec = oldSpec;
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
        /// Gets a value indicating whether to serialize with <see cref="MessagePackWriter.OldSpec"/> set to some value
        /// causing messagepack spec compliance to be explicitly set to the old or new format.
        /// </summary>
        /// <value>
        /// A null value means the <see cref="MessagePackWriter"/>'s default or previous setting will be used.
        /// A non-null value will be applied to the <see cref="MessagePackWriter.OldSpec"/> property for the duration of a
        /// serialization and then reverted to its prior setting.
        /// </value>
        /// <remarks>
        /// Reading always supports both new and old spec.
        /// </remarks>
        public bool? OldSpec { get; }

        /// <summary>
        /// Gets a copy of these options with the <see cref="Resolver"/> property set to a new value.
        /// </summary>
        /// <param name="resolver">The new value for the <see cref="Resolver"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithResolver(IFormatterResolver resolver) => this.Resolver != resolver ? new MessagePackSerializerOptions(resolver, this.UseLZ4Compression, this.OldSpec) : this;

        /// <summary>
        /// Gets a copy of these options with the <see cref="UseLZ4Compression"/> property set to a new value.
        /// </summary>
        /// <param name="useLZ4Compression">The new value for the <see cref="UseLZ4Compression"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithLZ4Compression(bool useLZ4Compression = true) => this.UseLZ4Compression != useLZ4Compression ? new MessagePackSerializerOptions(this.Resolver, useLZ4Compression, this.OldSpec) : this;

        /// <summary>
        /// Gets a copy of these options with the <see cref="OldSpec"/> property set to a new value.
        /// </summary>
        /// <param name="oldSpec">The new value for the <see cref="OldSpec"/>.</param>
        /// <returns>The new instance; or the original if the value is unchanged.</returns>
        public MessagePackSerializerOptions WithOldSpec(bool? oldSpec = true) => this.OldSpec != oldSpec ? new MessagePackSerializerOptions(this.Resolver, this.UseLZ4Compression, oldSpec) : this;

#if !DYNAMICCODEDUMPER
        private static class MessagePackSerializerOptionsDefaultSettingsLazyInitializationHelper
        {
            public static readonly MessagePackSerializerOptions Standard = new MessagePackSerializerOptions(Resolvers.StandardResolver.Instance, useLZ4Compression: false);
            public static readonly MessagePackSerializerOptions LZ4Standard = Standard.WithLZ4Compression(true);
        }
#endif
    }
}
