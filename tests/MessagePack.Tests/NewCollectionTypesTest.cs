// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if NET9_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MessagePack.Tests.TestUtilities;

namespace MessagePack.Tests;

public class NewCollectionTypesTest
{
    // NET 9

    [Fact]
    public void OrderedDictionaryTest()
    {
        var v = new OrderedDictionary<int, int>
        {
            { 1, 100 },
            { 2, 200 },
            { 5, 500 },
            { 3, 300 },
            { 9, 900 },
            { 7, 700 },
        };

        var v2 = Convert(v);

        v.AsEnumerable().ShouldBe(v2.AsEnumerable());
    }

    [Fact]
    public void ReadOnlySetTest()
    {
        var v = new ReadOnlySet<int>(new HashSet<int>() { 1, 2, 5, 3, 9, 7 });
        var v2 = Convert(v);
        v2.ShouldBe(v);
    }
}

#endif
