// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Formatters;

/// <summary>
/// The contract for serialization of some specific struct.
/// </summary>
/// <typeparam name="T">The type to be serialized or deserialized.</typeparam>
/// <remarks>
/// This interface is optimized for structs larger than pointer size,
/// where passing by reference is more efficient than passing by value.
/// </remarks>
public interface IMessagePackStructFormatter<T> : IMessagePackFormatter
    where T : struct
{
    /// <summary>
    /// Serializes a value.
    /// </summary>
    /// <param name="writer">The writer to use when serializing the value.</param>
    /// <param name="value">The value to be serialized.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    void Serialize(ref MessagePackWriter writer, in T value, MessagePackSerializerOptions options);

    /// <summary>
    /// Deserializes a value.
    /// </summary>
    /// <param name="reader">The reader to deserialize from.</param>
    /// <param name="value">A reference to the struct to receive the deserialized data.</param>
    /// <param name="options">The serialization settings to use, including the resolver to use to obtain formatters for types that make up the composite type <typeparamref name="T"/>.</param>
    void Deserialize(ref MessagePackReader reader, ref T value, MessagePackSerializerOptions options);
}
