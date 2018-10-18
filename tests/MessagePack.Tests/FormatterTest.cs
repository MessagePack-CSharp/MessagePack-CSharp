using SharedData;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MessagePack.Tests
{
    public class FormatterTest
    {
        T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static IEnumerable<object[]> primitiveFormatterTestData = new List<object[]>
        {
            new object[] { Int16.MinValue, Int16.MaxValue },
            new object[] { (Int16?)100, null },
            new object[] { Int32.MinValue, Int32.MaxValue },
            new object[] { (Int32?)100, null },
            new object[] { Int64.MinValue, Int64.MaxValue },
            new object[] { (Int64?)100, null },
            new object[] { UInt16.MinValue, UInt16.MaxValue },
            new object[] { (UInt16?)100, null },
            new object[] { UInt32.MinValue, UInt32.MaxValue },
            new object[] { (UInt32?)100, null },
            new object[] { UInt64.MinValue, UInt64.MaxValue },
            new object[] { (UInt64?)100, null },
            new object[] { Single.MinValue, Single.MaxValue },
            new object[] { (Single?)100.100, null },
            new object[] { Double.MinValue, Double.MaxValue },
            new object[] { (Double?)100.100, null },
            new object[] { true, false },
            new object[] { (Boolean?)true, null },
            new object[] { Byte.MinValue, Byte.MaxValue },
            new object[] { (Byte?)100.100, null },
            new object[] { SByte.MinValue, SByte.MaxValue },
            new object[] { (SByte?)100.100, null },
            new object[] { Char.MinValue, Char.MaxValue },
            new object[] { (Char?)'a', null },
            new object[] { DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime() },
            new object[] { (DateTime?)DateTime.UtcNow, null },
        };

        [Theory]
        [MemberData(nameof(primitiveFormatterTestData))]
        public void PrimitiveFormatterTest<T>(T x, T? y)
            where T : struct
        {
            Convert(x).Is(x);
            Convert(y).Is(y);
        }

        public static IEnumerable<object[]> enumFormatterTestData = new List<object[]>
        {
            new object[] { ByteEnum.A, ByteEnum.B },
            new object[] { (ByteEnum?)ByteEnum.C, null },
            new object[] { SByteEnum.A, SByteEnum.B },
            new object[] { (SByteEnum?)SByteEnum.C, null },
            new object[] { ShortEnum.A, ShortEnum.B },
            new object[] { (ShortEnum?)ShortEnum.C, null },
            new object[] { UShortEnum.A, UShortEnum.B },
            new object[] { (UShortEnum?)UShortEnum.C, null },
            new object[] { IntEnum.A, IntEnum.B },
            new object[] { (IntEnum?)IntEnum.C, null },
            new object[] { UIntEnum.A, UIntEnum.B },
            new object[] { (UIntEnum?)UIntEnum.C, null },
            new object[] { LongEnum.A, LongEnum.B },
            new object[] { (LongEnum?)LongEnum.C, null },
            new object[] { ULongEnum.A, ULongEnum.B },
            new object[] { (ULongEnum?)ULongEnum.C, null },
        };

        [Theory]
        [MemberData(nameof(enumFormatterTestData))]
        public void EnumFormatterTest<T>(T x, T? y)
            where T : struct
        {
            Convert(x).Is(x);
            Convert(y).Is(y);
        }

        [Fact]
        public void NilFormatterTest()
        {
            Convert(Nil.Default).Is(Nil.Default);
            Convert((Nil?)null).Is(Nil.Default);
        }

        public static IEnumerable<object[]> standardStructFormatterTestData = new List<object[]>
        {
            new object[] { decimal.MaxValue, decimal.MinValue, null },
            new object[] { TimeSpan.MaxValue, TimeSpan.MinValue, null },
            new object[] { DateTimeOffset.MaxValue, DateTimeOffset.MinValue, null },
            new object[] { Guid.NewGuid(), Guid.Empty, null },
            new object[] { new KeyValuePair<int,string>(10, "hoge"), default(KeyValuePair<int, string>), null },
            new object[] { System.Numerics.BigInteger.Zero, System.Numerics.BigInteger.One, null },
            new object[] { System.Numerics.Complex.Zero, System.Numerics.Complex.One, null },
        };

        [Fact]
        public void PrimitiveStringTest()
        {
            Convert("a").Is("a");
            Convert("test").Is("test");
            Convert("testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest")
                .Is("testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest");
            Convert((string)null).IsNull();
        }

        [Theory]
        [MemberData(nameof(standardStructFormatterTestData))]
        public void StandardClassLibraryStructFormatterTest<T>(T x, T? y, T? z)
            where T : struct
        {
            Convert(x).Is(x);
            Convert(y).Is(y);
            Convert(z).Is(z);
        }

        public static IEnumerable<object[]> standardClassFormatterTestData = new List<object[]>
        {
            new object[] { new byte[] { 1, 10, 100 }, new byte[0] { }, null },
            new object[] { "aaa", "", null },
            new object[] { new Uri("Http://hogehoge.com"), new Uri("Https://hugahuga.com"), null },
            new object[] { new Version(), new Version(1,2,3), new Version(255,100,30) },
            new object[] { new Version(1,2), new Version(100, 200,300,400), null },
            new object[] { new BitArray(new[] { true, false, true }), new BitArray(1), null },
        };

        [Theory]
        [MemberData(nameof(standardClassFormatterTestData))]
        public void StandardClassLibraryFormatterTest<T>(T x, T y, T z)
        {
            Convert(x).Is(x);
            Convert(y).Is(y);
            Convert(z).Is(z);
        }

        [Fact]
        public void StringBuilderTest()
        {
            var sb = new StringBuilder("aaa");
            Convert(sb).ToString().Is("aaa");

            StringBuilder nullSb = null;
            Convert(nullSb).IsNull();
        }

        [Fact]
        public void LazyTest()
        {
            var lz = new Lazy<int>(() => 100);
            Convert(lz).Value.Is(100);

            Lazy<int> nullLz = null;
            Convert(nullLz).IsNull();
        }

        [Fact]
        public void TaskTest()
        {
            var intTask = Task.Run(() => 100);
            Convert(intTask).Result.Is(100);

            Task<int> nullTask = null;
            Convert(nullTask).IsNull();

            Task unitTask = Task.Run(() => 100);
            Convert(unitTask).Status.Is(TaskStatus.RanToCompletion);

            Task nullUnitTask = null;
            Convert(nullUnitTask).Status.Is(TaskStatus.RanToCompletion); // write to nil

            ValueTask<int> valueTask = new ValueTask<int>(100);
            Convert(valueTask).Result.Is(100);

            ValueTask<int>? nullValueTask = new ValueTask<int>(100);
            Convert(nullValueTask).Value.Result.Is(100);

            ValueTask<int>? nullValueTask2 = null;
            Convert(nullValueTask2).IsNull();
        }

        [Fact]
        public void DateTimeOffsetTest()
        {
            DateTimeOffset now = new DateTime(DateTime.UtcNow.Ticks + TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time").BaseUtcOffset.Ticks, DateTimeKind.Local);
            var binary = MessagePackSerializer.Serialize(now);
            MessagePackSerializer.Deserialize<DateTimeOffset>(binary).Is(now);
        }

        [Fact]
        public void StringTest_Part2()
        {
            var a = "あいうえお";
            var b = new String('あ', 20);
            var c = new String('あ', 130);
            var d = new String('あ', 40000);

            byte[] bytesA = null;
            MessagePackBinary.WriteString(ref bytesA, 0, a).Is(Encoding.UTF8.GetByteCount(a) + 1);

            byte[] bytesB = null;
            MessagePackBinary.WriteString(ref bytesB, 0, b).Is(Encoding.UTF8.GetByteCount(b) + 2);

            byte[] bytesC = null;
            MessagePackBinary.WriteString(ref bytesC, 0, c).Is(Encoding.UTF8.GetByteCount(c) + 3);

            byte[] bytesD = null;
            MessagePackBinary.WriteString(ref bytesD, 0, d).Is(Encoding.UTF8.GetByteCount(d) + 5);

            int readSize = 0;
            MessagePackBinary.ReadString(bytesA, 0, out readSize).Is(a);
            MessagePackBinary.ReadString(bytesB, 0, out readSize).Is(b);
            MessagePackBinary.ReadString(bytesC, 0, out readSize).Is(c);
            MessagePackBinary.ReadString(bytesD, 0, out readSize).Is(d);
        }

        // https://github.com/neuecc/MessagePack-CSharp/issues/22
        [Fact]
        public void DecimalLang()
        {
            var estonian = CultureInfo.GetCultureInfo("et-EE");
            CultureInfo.CurrentCulture = estonian;

            var b = MessagePackSerializer.Serialize(12345.6789M);
            var d = MessagePackSerializer.Deserialize<decimal>(b);

            d.Is(12345.6789M);
        }

        [Fact]
        public void UriTest()
        {
            var absolute = new Uri("http://google.com/");
            Convert(absolute).ToString().Is("http://google.com/");

            var relative = new Uri("/me/", UriKind.Relative);
            Convert(relative).ToString().Is("/me/");
        }
    }
}
