// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

using System;
using System.Diagnostics;

namespace MessagePack;

// TODO: allow these attributes to also appear on the partial resolver class too.
// This isn't an acceptable exclusive option because we want the analyzer to be able to run even when the resolver partial class isn't generated.

/// <summary>
/// Identifies a custom formatter (one that implements one or more <c>IMessagePackFormatter&lt;T&gt;</c> interfaces)
/// that should be considered when checking that types are serializable and/or included in a source-generated resolver.
/// </summary>
/// <remarks>
/// Formatters identified with this attribute will be included in the source-generated resolver.
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = true)]
[Conditional("NEVERDEFINED")] // We only need this attribute for analysis, so we don't want it to be included in the user's built assembly.
public class MessagePackKnownFormatterAttribute : Attribute
{
    public MessagePackKnownFormatterAttribute(Type formatterType)
    {
        this.FormatterType = formatterType;
    }

    /// <summary>
    /// Gets a type that implements one or more <c>IMessagePackFormatter&lt;T&gt;</c> interfaces.
    /// </summary>
    public Type FormatterType { get; }
}

/// <summary>
/// Identifies a type for which a <c>IMessagePackFormatter&lt;T&gt;</c> exists and will be added manually to the <c>IFormatterResolver</c> by the program.
/// </summary>
/// <remarks>
/// <para>This attribute suppresses warnings by the MessagePack analyzer when it encounters references to the specified type within another serializable type.</para>
/// <para>When possible, using the <see cref="MessagePackKnownFormatterAttribute"/> is preferred.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, AllowMultiple = true)]
[Conditional("NEVERDEFINED")] // We only need this attribute for analysis, so we don't want it to be included in the user's built assembly.
public class MessagePackAssumedFormattableAttribute : Attribute
{
    public MessagePackAssumedFormattableAttribute(Type formattableType)
    {
        this.FormattableType = formattableType;
    }

    /// <summary>
    /// Gets a type for which an <c>IMessagePackFormatter&lt;T&gt;</c> is known to exist and will be added via a custom resolver by the program at runtime.
    /// </summary>
    public Type FormattableType { get; }
}
