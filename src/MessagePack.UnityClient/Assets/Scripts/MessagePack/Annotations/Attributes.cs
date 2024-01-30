﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = true)]
    public class MessagePackObjectAttribute : Attribute
    {
        public bool KeyAsPropertyName { get; private set; }

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
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class KeyAttribute : Attribute
    {
        public int? IntKey { get; private set; }

        public string? StringKey { get; private set; }

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
        public int Key { get; private set; }

        /// <summary>
        /// Gets the derived or implementing type.
        /// </summary>
        public Type SubType { get; private set; }

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
        public Type FormatterType { get; private set; }

        public object?[]? Arguments { get; private set; }

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
