using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    //public class ThreadsafeHashtableTest
    //{
    //    // internal, internal visible to...

    //    [Fact]
    //    public void HashtableTryAddGet()
    //    {
    //        var hash = new MessagePack.Internal.ThreadsafeHashTable<string, int>();

    //        hash.TryAdd("hoge", x => 1).IsTrue();
    //        hash.TryAdd("hoge", x => 2).IsFalse();
    //        int y;
    //        hash.TryGetValue("hoge", out y).IsTrue();
    //        y.Is(1);

    //        hash.TryAdd("hoge3", x => 3).IsTrue();
    //        hash.TryAdd("hoge4", x => 4).IsTrue();
    //        hash.TryAdd("hoge5", x => 5).IsTrue();
    //        hash.TryAdd("hoge6", x => 6).IsTrue();
    //        hash.TryAdd("hoge7", x => 7).IsTrue();
    //        hash.TryAdd("hoge8", x => 8).IsTrue();
    //        hash.TryAdd("hoge9", x => 9).IsTrue();
    //        hash.TryAdd("hog10", x => 0).IsTrue();
    //        hash.TryAdd("hog11", x => 1).IsTrue();
    //        hash.TryAdd("hog12", x => 2).IsTrue();
    //        hash.TryAdd("hog13", x => 3).IsTrue();
    //        hash.TryAdd("hog14", x => 4).IsTrue();
    //        hash.TryAdd("hog15", x => 5).IsTrue();

    //        hash.TryGetValue("hoge3", out y).IsTrue(); y.Is(3);
    //        hash.TryGetValue("hoge4", out y).IsTrue(); y.Is(4);
    //        hash.TryGetValue("hoge5", out y).IsTrue(); y.Is(5);
    //        hash.TryGetValue("hoge6", out y).IsTrue(); y.Is(6);
    //        hash.TryGetValue("hoge7", out y).IsTrue(); y.Is(7);
    //        hash.TryGetValue("hoge8", out y).IsTrue(); y.Is(8);
    //        hash.TryGetValue("hoge9", out y).IsTrue(); y.Is(9);
    //        hash.TryGetValue("hog10", out y).IsTrue(); y.Is(0);
    //        hash.TryGetValue("hog11", out y).IsTrue(); y.Is(1);
    //        hash.TryGetValue("hog12", out y).IsTrue(); y.Is(2);
    //        hash.TryGetValue("hog13", out y).IsTrue(); y.Is(3);
    //        hash.TryGetValue("hog14", out y).IsTrue(); y.Is(4);
    //        hash.TryGetValue("hog15", out y).IsTrue(); y.Is(5);
    //    }
    //}

    public class ByteArrayStringHashTableTest
    {
        static ArraySegment<byte> ToArraySegment(string s)
        {
            var bin = Encoding.UTF8.GetBytes(s);
            return new ArraySegment<byte>(bin, 0, bin.Length);
        }

        [Fact]
        public void ByteArrayStringHashTableAddGet()
        {
            var hash = new MessagePack.Internal.ByteArrayStringHashTable(15);

            hash.Add("hoge", 1);
            Assert.Throws<ArgumentException>(() => hash.Add("hoge", 2));
            int y;
            hash.TryGetValue(ToArraySegment("hoge"), out y).IsTrue();
            y.Is(1);

            hash.Add("hoge3", 3);
            hash.Add("hoge4", 4);
            hash.Add("hoge5", 5);
            hash.Add("hoge6", 6);
            hash.Add("hoge7", 7);
            hash.Add("hoge8", 8);
            hash.Add("hoge9", 9);
            hash.Add("hog10", 0);
            hash.Add("hog11", 1);
            hash.Add("hog12", 2);
            hash.Add("hog13", 3);
            hash.Add("hog14", 4);
            hash.Add("hog15", 5);

            hash.TryGetValue(ToArraySegment("hoge3"), out y).IsTrue(); y.Is(3);
            hash.TryGetValue(ToArraySegment("hoge4"), out y).IsTrue(); y.Is(4);
            hash.TryGetValue(ToArraySegment("hoge5"), out y).IsTrue(); y.Is(5);
            hash.TryGetValue(ToArraySegment("hoge6"), out y).IsTrue(); y.Is(6);
            hash.TryGetValue(ToArraySegment("hoge7"), out y).IsTrue(); y.Is(7);
            hash.TryGetValue(ToArraySegment("hoge8"), out y).IsTrue(); y.Is(8);
            hash.TryGetValue(ToArraySegment("hoge9"), out y).IsTrue(); y.Is(9);
            hash.TryGetValue(ToArraySegment("hog10"), out y).IsTrue(); y.Is(0);
            hash.TryGetValue(ToArraySegment("hog11"), out y).IsTrue(); y.Is(1);
            hash.TryGetValue(ToArraySegment("hog12"), out y).IsTrue(); y.Is(2);
            hash.TryGetValue(ToArraySegment("hog13"), out y).IsTrue(); y.Is(3);
            hash.TryGetValue(ToArraySegment("hog14"), out y).IsTrue(); y.Is(4);
            hash.TryGetValue(ToArraySegment("hog15"), out y).IsTrue(); y.Is(5);
        }
    }
}
