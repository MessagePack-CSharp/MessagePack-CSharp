// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

public enum ContainerKind
{
    /// <summary>
    /// The type to be serialized is declared in the global namespace.
    /// </summary>
    None,

    /// <summary>
    /// The type to be serialized is declared in a non-global namespace.
    /// </summary>
    Namespace,

    /// <summary>
    /// The type to be serialized is declared as nested within another class.
    /// </summary>
    NestingClass,
}
