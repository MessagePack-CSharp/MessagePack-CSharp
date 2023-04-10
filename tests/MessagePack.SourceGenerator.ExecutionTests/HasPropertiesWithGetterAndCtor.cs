// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
internal record HasPropertiesWithGetterAndCtor
{
    [Key(0)]
    internal int A { get; }

    [Key(1)]
    internal string? B { get; }

    internal HasPropertiesWithGetterAndCtor(int a, string? b)
    {
        A = a;
        B = b;
    }
}
