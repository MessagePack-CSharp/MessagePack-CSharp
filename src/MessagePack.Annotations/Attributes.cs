// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        /// <summary>
        /// Gets a value indicating whether to automatically serialize all internal and public fields and properties using their property name as the key in a map.
        /// </summary>
        public bool KeyAsPropertyName { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessagePackObjectAttribute"/> class.
        /// </summary>
        /// <param name="keyAsPropertyName">
        /// <see langword="true" /> to automatically serialize all internal and public fields and properties using their property name as the key in a map;
        /// or <see langword="false" /> to use the <see cref="KeyAttribute"/> attribute to specify the key for each field or property.
        /// </param>
        public MessagePackObjectAttribute(bool keyAsPropertyName = false)
        {
            this.KeyAsPropertyName = keyAsPropertyName;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the source generator should <em>not</em>
        /// generate a formatter for this type at compile-time.
        /// </summary>
        /// <remarks>
        /// By default, source generators will generate a formatter for every type that is annotated with
        /// this attribute to improve startup performance.
        /// However if this leads to malfunctions during code generation or at runtime,
        /// it can be disabled by setting this property to <see langword="true"/>.
        /// When no precompiled formatter is found at runtime, the <c>DynamicObjectResolver</c>
        /// will generate a formatter at runtime instead.
        /// </remarks>
        public bool SuppressSourceGeneration { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether generated formatters should allow non-public members to be serialized and deserialized.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This is particularly important to set when <see cref="KeyAsPropertyName"/> is set to <see langword="true" /> if non-public members should be included.
        /// </para>
        /// <para>
        /// This property can also ensure non-public members are serialized by the <c>DynamicObjectResolver</c> so that the application doesn't have to use
        /// <c>DynamicObjectResolverAllowPrivate</c> explicitly for an object to be properly serialized.
        /// </para>
        /// </remarks>
        public bool AllowPrivate { get; set; }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; }

        public string? StringKey { get; }

        public KeyAttribute(int x)
        {
            this.IntKey = x;
        }

        public KeyAttribute(string x)
        {
            this.StringKey = x ?? throw new ArgumentNullException(nameof(x));
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class IgnoreMemberAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class UnionAttribute : Attribute
    {
        /// <summary>
        /// Gets the distinguishing value that identifies a particular subtype.
        /// </summary>
        public int Key { get; }

        /// <summary>
        /// Gets the derived or implementing type.
        /// </summary>
        public Type SubType { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnionAttribute"/> class.
        /// </summary>
        /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
        /// <param name="subType">The derived or implementing type.</param>
        public UnionAttribute(int key, Type subType)
        {
            this.Key = key;
            this.SubType = subType ?? throw new ArgumentNullException(nameof(subType));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnionAttribute"/> class.
        /// </summary>
        /// <param name="key">The distinguishing value that identifies a particular subtype.</param>
        /// <param name="subType">The full name (should be assembly qualified) of the derived or implementing type.</param>
        public UnionAttribute(int key, string subType)
        {
            this.Key = key;
            this.SubType = Type.GetType(subType, throwOnError: true);
        }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false, Inherited = true)]
    public class SerializationConstructorAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.ReturnValue, AllowMultiple = false, Inherited = true)]
    public class MessagePackFormatterAttribute : Attribute
    {
        public Type FormatterType { get; }

        public object?[]? Arguments { get; }

        public MessagePackFormatterAttribute(Type formatterType)
        {
            this.FormatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
        }

        public MessagePackFormatterAttribute(Type formatterType, params object?[]? arguments)
        {
            this.FormatterType = formatterType ?? throw new ArgumentNullException(nameof(formatterType));
            this.Arguments = arguments;
        }
    }
}
