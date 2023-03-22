// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack.Generator.Transforms;

namespace MessagePack.Generator.CodeAnalysis;

public interface INamespaceInfo
{
    string? Namespace { get; }
}
