using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessagePack.Tests
{
    public enum ByteEnum : byte { A, B, C, D, E }
    public enum SByteEnum : sbyte { A, B, C, D, E }
    public enum ShortEnum : short { A, B, C, D, E }
    public enum UShortEnum : ushort { A, B, C, D, E }
    public enum IntEnum : int { A, B, C, D, E }
    public enum UIntEnum : uint { A, B, C, D, E }
    public enum LongEnum : long { A, B, C, D, E }
    public enum ULongEnum : ulong { A, B, C, D, E }

    [MessagePackObject]
    public class SimpleIntKeyData
    {
        [Key(0)]
        public int Prop1 { get; set; }
        [Key(1)]
        public ByteEnum Prop2 { get; set; }
        [Key(2)]
        public string Prop3 { get; set; }
        [Key(3)]
        public SimlpeStringKeyData Prop4 { get; set; }
        [Key(4)]
        public SimpleStructIntKeyData Prop5 { get; set; }
        [Key(5)]
        public SimpleStructStringKeyData Prop6 { get; set; }
        [Key(6)]
        public byte[] BytesSpecial { get; set; }
    }

    [MessagePackObject(true)]
    public class SimlpeStringKeyData
    {
        public int Prop1 { get; set; }
        public ByteEnum Prop2 { get; set; }
        public int Prop3 { get; set; }
    }

    [MessagePackObject]
    public struct SimpleStructIntKeyData
    {
        [Key(0)]
        public int X { get; set; }
        [Key(1)]
        public int Y { get; set; }
        [Key(2)]
        public byte[] BytesSpecial { get; set; }
    }


    [MessagePackObject]
    public struct SimpleStructStringKeyData
    {
        [Key("key-X")]
        public int X { get; set; }
        [Key("key-Y")]
        public int[] Y { get; set; }
    }

}
