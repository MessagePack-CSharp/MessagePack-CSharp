// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MessagePack;

namespace TestData2
{
    [MessagePackObject]
    public record Record([property: Key("SomeProperty")] string SomeProperty);
}
