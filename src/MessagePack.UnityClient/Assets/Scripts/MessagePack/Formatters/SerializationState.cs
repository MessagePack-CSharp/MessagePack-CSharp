// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MessagePack.Formatters
{
    /// <summary>
    /// Carries state specific to a particular serialization operation.
    /// </summary>
    /// <remarks>
    /// This struct is propagated from the top-level serialization or deserialization request
    /// down through all formatters that participate in it.
    /// </remarks>
    public ref struct SerializationState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializationState"/> struct.
        /// </summary>
        /// <param name="options">The serialization settings to use.</param>
        public SerializationState(MessagePackSerializerOptions options)
        {
            this.Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>
        /// Gets the serialization settings to use, including the resolver to use to obtain <see cref="IMessagePackFormatter{T}" /> instances.
        /// </summary>
        public MessagePackSerializerOptions Options { get; }
    }
}
