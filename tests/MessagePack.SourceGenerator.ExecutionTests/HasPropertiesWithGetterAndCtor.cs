// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

[MessagePackObject(false)]
public record HasPropertiesWithGetterAndCtor
{
    [Key(0)]
    public int A { get; }

    [Key(1)]
    public string? B { get; }

    public HasPropertiesWithGetterAndCtor(int a, string? b)
    {
        A = a;
        B = b;
    }

    [Key(2)]
    public int C { get; set; }
}
