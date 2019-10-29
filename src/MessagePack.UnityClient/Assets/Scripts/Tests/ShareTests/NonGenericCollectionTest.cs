// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class NonGenericCollectionTest
    {
#if !ENABLE_IL2CPP
        [Fact]
        public void List()
        {
            var xs = new System.Collections.ArrayList { 1, 100, "hoge", 999.888 };
            {
                var bin = MessagePackSerializer.Serialize<IList>(xs);
                IList v = MessagePackSerializer.Deserialize<IList>(bin);

                ((byte)v[0]).Is((byte)1);
                ((byte)v[1]).Is((byte)100);
                ((string)v[2]).Is("hoge");
                ((double)v[3]).Is(999.888);
            }

            {
                var bin = MessagePackSerializer.Serialize(xs);
                ArrayList v = MessagePackSerializer.Deserialize<ArrayList>(bin);

                ((byte)v[0]).Is((byte)1);
                ((byte)v[1]).Is((byte)100);
                ((string)v[2]).Is("hoge");
                ((double)v[3]).Is(999.888);
            }
        }

        [Fact]
        public void Dictionary()
        {
            {
                var xs = new System.Collections.Hashtable { { "a", 1 }, { 100, "hoge" }, { "foo", 999.888 } };
                var bin = MessagePackSerializer.Serialize<IDictionary>(xs);
                IDictionary v = MessagePackSerializer.Deserialize<IDictionary>(bin);

                v["a"].Is((object)(byte)1);
                v[(byte)100].Is((object)(string)"hoge");
                v["foo"].Is((object)(double)999.888);
            }

            {
                var xs = new System.Collections.Hashtable { { "a", 1 }, { 100, "hoge" }, { "foo", 999.888 } };
                var bin = MessagePackSerializer.Serialize<Hashtable>(xs);
                Hashtable v = MessagePackSerializer.Deserialize<Hashtable>(bin);

                v["a"].Is((object)(byte)1);
                v[(byte)100].Is((object)(string)"hoge");
                v["foo"].Is((object)(double)999.888);
            }
        }

#endif
    }
}
