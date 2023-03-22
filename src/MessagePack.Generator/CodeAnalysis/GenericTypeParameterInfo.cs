// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.Generator.CodeAnalysis;

public class GenericTypeParameterInfo
{
    public string Name { get; }

    public string Constraints { get; }

    public bool HasConstraints { get; }

    public GenericTypeParameterInfo(string name, string constraints)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Constraints = constraints ?? throw new ArgumentNullException(nameof(name));
        HasConstraints = constraints != string.Empty;
    }
}
