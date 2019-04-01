using System;
using MessagePack;

namespace TestData.SubDir
{

    [MessagePackObject(true)]
    public class A
    {
        public int X { get; set; }
    }
}
