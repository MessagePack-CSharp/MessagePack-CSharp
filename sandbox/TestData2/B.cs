// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using MessagePack;

#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1401 // Fields should be private

namespace TestData2
{
    [MessagePackObject(true)]
    public class B
    {
        public List<A> ass; public C c; public int a;
    }
}
