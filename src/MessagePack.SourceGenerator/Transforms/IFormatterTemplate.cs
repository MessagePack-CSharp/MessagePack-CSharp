// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.SourceGenerator.CodeAnalysis;

namespace MessagePack.SourceGenerator.Transforms;

public interface IFormatterTemplate
{
    string FileName { get; }

    string Namespace { get; }

    ObjectSerializationInfo Info { get; }

    string TransformText();
}
