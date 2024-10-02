// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
internal record HasInitProperty
{
    [Key(0)]
    internal int A { get; set; }

    [Key(1)]
    internal int? B { get; init; }

    [Key(2)]
    internal int C { get; set; }
}
