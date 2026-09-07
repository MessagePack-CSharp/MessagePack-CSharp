// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
public record UnionContainer
{
    [Key(0)]
    public IMyType? Value { get; set; }
}
