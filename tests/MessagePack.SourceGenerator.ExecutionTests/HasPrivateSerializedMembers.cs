// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject]
internal partial record HasPrivateSerializedMembers
{
    [Key(0)]
    private int value;

    [IgnoreMember]
    internal int ValueAccessor
    {
        get => this.value;
        set => this.value = value;
    }
}
