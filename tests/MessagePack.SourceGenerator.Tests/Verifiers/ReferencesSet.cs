// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[Flags]
internal enum ReferencesSet
{
    None = 0x0,
    MessagePackAnnotations = 0x1,
    MessagePack = 0x2 | MessagePackAnnotations,
}
