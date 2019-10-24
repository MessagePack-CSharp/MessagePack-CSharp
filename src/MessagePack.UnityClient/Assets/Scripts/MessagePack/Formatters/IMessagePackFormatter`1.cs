// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;
using System.ComponentModel;
using System.Threading;

namespace MessagePack.Formatters
{
#pragma warning disable SA1649 // File name should match first type name
    /// <summary>
    /// A base interface for <see cref="IMessagePackFormatter{T}"/> so that all generic implementations
    /// can be detected by a common base type.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IMessagePackFormatter
    {
    }
#pragma warning restore SA1649 // File name should match first type name

    /// <summary>
    /// The contract for serialization of some specific type.
    /// </summary>
    /// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
    public interface IMessagePackFormatter<T> : IMessagePackFormatter
    {
        /// <summary>
        /// Serializes a value.
        /// </summary>
        /// <param name="writer">The writer to use when serializing the value.</param>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="state">State relevant to this serialization operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        void Serialize(ref MessagePackWriter writer, T value, ref SerializationState state, CancellationToken cancellationToken);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="state">State relevant to this specific serialization operation.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The deserialized value.</returns>
        T Deserialize(ref MessagePackReader reader, ref SerializationState state, CancellationToken cancellationToken);
    }
}
