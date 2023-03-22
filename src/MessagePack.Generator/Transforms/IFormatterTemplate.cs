// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.CodeAnalysis;

namespace MessagePack.Generator.Transforms;

public interface IFormatterTemplate
{
    string Namespace { get; }

    ObjectSerializationInfo Info { get; }

    string TransformText();
}
