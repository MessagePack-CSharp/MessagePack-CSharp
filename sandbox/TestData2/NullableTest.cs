// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private
#pragma warning disable SA1649 // File name should match first type name

#nullable enable

namespace TestData2;

[MessagePackObject(true)]
public class NullableTest
{
    public int[]? a;
    public List<byte[]?> b = null!;
}
