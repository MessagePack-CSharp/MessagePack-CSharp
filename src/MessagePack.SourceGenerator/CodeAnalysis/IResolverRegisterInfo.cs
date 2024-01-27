// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.CodeAnalysis;

public interface IResolverRegisterInfo
{
    /// <summary>
    /// Gets the full name of the type to be formatted.
    /// </summary>
    string FullName { get; }

    /// <summary>
    /// Gets the name of the formatter.
    /// </summary>
    string FormatterName { get; }

    /// <summary>
    /// Gets the namespace of the formatter.
    /// </summary>
    string? Namespace { get; }
}
