// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(AllowPrivate = true)]
internal record MyMessagePackObject
{
    [Key(0)]
    internal MyEnum EnumValue { get; set; }
}
