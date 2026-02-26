// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

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
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        void Serialize(ref MessagePackWriter writer, T value, MessagePackSerializerOptions options);

        /// <summary>
        /// Deserializes a value.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
        /// <returns>The deserialized value.</returns>
        T Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options);
    }

    /// <summary>
    /// Provides an optional by-ref API for serializing <typeparamref name="T"/> values.
    /// </summary>
    /// <typeparam name="T">The type to be serialized.</typeparam>
    public interface IMessagePackFormatterSerializeIn<T>
    {
        /// <summary>
        /// Serializes a value passed by readonly reference.
        /// </summary>
        /// <param name="writer">The writer to use when serializing the value.</param>
        /// <param name="value">The value to be serialized.</param>
        /// <param name="options">The serialization settings to use.</param>
        void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options);
    }

    /// <summary>
    /// Provides an optional by-ref API for deserializing into an existing <typeparamref name="T"/> value.
    /// </summary>
    /// <typeparam name="T">The type to be deserialized.</typeparam>
    public interface IMessagePackFormatterDeserializeRef<T>
    {
        /// <summary>
        /// Deserializes into an existing value passed by reference.
        /// </summary>
        /// <param name="reader">The reader to deserialize from.</param>
        /// <param name="value">The existing value that will receive deserialized data.</param>
        /// <param name="options">The serialization settings to use.</param>
        void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);
    }
}
