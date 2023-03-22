// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public record GenericTypeParameterInfo(string Name, string Constraints)
{
    public bool HasConstraints => this.Constraints.Length > 0;
}
