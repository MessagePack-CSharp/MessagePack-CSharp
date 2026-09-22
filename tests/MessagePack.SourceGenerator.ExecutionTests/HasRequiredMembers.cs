// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
public record HasRequiredMembers
{
    [Key(0)]
    public int A { get; set; }

    [Key(1)]
    public required int? B { get; set; }

    [Key(2)]
    public required int? C;

    [Key(3)]
    public int D { get; set; }
}
