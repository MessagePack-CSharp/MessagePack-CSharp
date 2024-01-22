// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.Transforms;

public interface IFormatterTemplate
{
    string FileName { get; }

    /// <summary>
    /// Gets the namespace of the formatter type.
    /// </summary>
    /// <remarks>
    /// This must <em>not</em> begin with <c>global::</c>.
    /// </remarks>
    string? FormattedTypeNamespace { get; }

    string ResolverNamespace { get; }

    string ResolverName { get; }

    string TransformText();
}
