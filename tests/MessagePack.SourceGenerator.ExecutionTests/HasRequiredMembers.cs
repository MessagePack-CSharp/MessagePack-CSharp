// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
internal record HasRequiredMembers
{
    [Key(0)]
    internal int A { get; set; }

    [Key(1)]
    internal required int? B { get; set; }

    [Key(2)]
    internal required int? C;

    [Key(3)]
    internal int D { get; set; }
}
