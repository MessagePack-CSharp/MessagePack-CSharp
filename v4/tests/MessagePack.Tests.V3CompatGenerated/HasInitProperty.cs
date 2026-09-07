// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
public record HasInitProperty
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public int? B { get; init; }

    [Key(2)]
    public int C { get; set; }
}
