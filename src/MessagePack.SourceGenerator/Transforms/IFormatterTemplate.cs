// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Analyzers.CodeAnalysis;

namespace MessagePack.SourceGenerator.Transforms;

public interface IFormatterTemplate
{
    string FileName { get; }

    string ResolverNamespace { get; }

    string ResolverName { get; }

    ObjectSerializationInfo Info { get; }

    string TransformText();
}
