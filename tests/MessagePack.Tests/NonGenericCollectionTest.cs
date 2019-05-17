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
        private MessagePackSerializer serializer = new MessagePackSerializer();

        [Fact]
        public void List()
        {
            var xs = new System.Collections.ArrayList { 1, 100, "hoge", 999.888 };
            {
                var bin = serializer.Serialize<IList>(xs);
                var v = serializer.Deserialize<IList>(bin);

                ((byte)v[0]).Is((byte)1);
                ((byte)v[1]).Is((byte)100);
                ((string)v[2]).Is("hoge");
                ((double)v[3]).Is(999.888);
            }
            {
                var bin = serializer.Serialize(xs);
                var v = serializer.Deserialize<ArrayList>(bin);

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
                var bin = serializer.Serialize<IDictionary>(xs);
                var v = serializer.Deserialize<IDictionary>(bin);

                v["a"].Is((object)(byte)1);
                v[(byte)100].Is((object)(string)"hoge");
                v["foo"].Is((object)(double)999.888);
            }
            {
                var xs = new System.Collections.Hashtable { { "a", 1 }, { 100, "hoge" }, { "foo", 999.888 } };
                var bin = serializer.Serialize<Hashtable>(xs);
                var v = serializer.Deserialize<Hashtable>(bin);

                v["a"].Is((object)(byte)1);
                v[(byte)100].Is((object)(string)"hoge");
                v["foo"].Is((object)(double)999.888);
            }
        }
    }
}
