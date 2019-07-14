// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using MessagePack.Formatters;
using Nerdbank.Streams;
using SharedData;
using Xunit;

namespace MessagePack.Tests
{
    public class FormatterTest
    {
        private T Convert<T>(T value)
        {
            return MessagePackSerializer.Deserialize<T>(MessagePackSerializer.Serialize(value));
        }

        public static object[][] PrimitiveFormatterTestData = new object[][]
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
        [MemberData(nameof(PrimitiveFormatterTestData))]
        public void PrimitiveFormatterTest<T>(T x, T? y)
            where T : struct
        {
            this.Convert(x).Is(x);
            this.Convert(y).Is(y);
        }

        public static object[][] EnumFormatterTestData = new object[][]
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
        [MemberData(nameof(EnumFormatterTestData))]
        public void EnumFormatterTest<T>(T x, T? y)
            where T : struct
        {
            this.Convert(x).Is(x);
            this.Convert(y).Is(y);
        }

        [Fact]
        public void NilFormatterTest()
        {
            this.Convert(Nil.Default).Is(Nil.Default);
            this.Convert((Nil?)null).Is(Nil.Default);
        }

        public static object[][] StandardStructFormatterTestData = new object[][]
        {
            new object[] { decimal.MaxValue, decimal.MinValue, null },
            new object[] { TimeSpan.MaxValue, TimeSpan.MinValue, null },
            new object[] { DateTimeOffset.MaxValue, DateTimeOffset.MinValue, null },
            new object[] { Guid.NewGuid(), Guid.Empty, null },
            new object[] { new KeyValuePair<int, string>(10, "hoge"), default(KeyValuePair<int, string>), null },
            new object[] { System.Numerics.BigInteger.Zero, System.Numerics.BigInteger.One, null },
            new object[] { System.Numerics.Complex.Zero, System.Numerics.Complex.One, null },
        };

        [Fact]
        public void PrimitiveStringTest()
        {
            this.Convert("a").Is("a");
            this.Convert("test").Is("test");
            this.Convert("testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest")
                .Is("testtesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttesttest");
            this.Convert((string)null).IsNull();
        }

        [Theory]
        [MemberData(nameof(StandardStructFormatterTestData))]
        public void StandardClassLibraryStructFormatterTest(object x, object y, object z)
        {
            MethodInfo helper = typeof(FormatterTest).GetTypeInfo().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(m => m.Name == nameof(this.StandardClassLibraryStructFormatterTest_Helper));
            MethodInfo helperClosedGeneric = helper.MakeGenericMethod(x.GetType());

            helperClosedGeneric.Invoke(this, new object[] { x });
            helperClosedGeneric.Invoke(this, new object[] { y });
            helperClosedGeneric.Invoke(this, new object[] { z });
        }

        private void StandardClassLibraryStructFormatterTest_Helper<T>(T? value)
            where T : struct
            => this.Convert(value).Is(value);

        public static object[][] StandardClassFormatterTestData = new object[][]
        {
            new object[] { new byte[] { 1, 10, 100 }, new byte[0] { }, null },
            new object[] { "aaa", string.Empty, null },
            new object[] { new Uri("Http://hogehoge.com"), new Uri("Https://hugahuga.com"), null },
            new object[] { new Version(0, 0), new Version(1, 2, 3), new Version(255, 100, 30) },
            new object[] { new Version(1, 2), new Version(100, 200, 300, 400), null },
            new object[] { new BitArray(new[] { true, false, true }), new BitArray(1), null },
        };

        [Theory]
        [MemberData(nameof(StandardClassFormatterTestData))]
        public void StandardClassLibraryFormatterTest<T>(T x, T y, T z)
        {
            this.Convert(x).Is(x);
            this.Convert(y).Is(y);
            this.Convert(z).Is(z);
        }

        [Fact]
        public void StringBuilderTest()
        {
            var sb = new StringBuilder("aaa");
            this.Convert(sb).ToString().Is("aaa");

            StringBuilder nullSb = null;
            this.Convert(nullSb).IsNull();
        }

        [Fact]
        public void LazyTest()
        {
            var lz = new Lazy<int>(() => 100);
            this.Convert(lz).Value.Is(100);

            Lazy<int> nullLz = null;
            this.Convert(nullLz).IsNull();
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

            var sequenceA = new Sequence<byte>();
            var sequenceAWriter = new MessagePackWriter(sequenceA);
            sequenceAWriter.Write(a);
            sequenceAWriter.Flush();
            sequenceA.Length.Is(Encoding.UTF8.GetByteCount(a) + 1);

            var sequenceB = new Sequence<byte>();
            var sequenceBWriter = new MessagePackWriter(sequenceB);
            sequenceBWriter.Write(b);
            sequenceBWriter.Flush();
            sequenceB.Length.Is(Encoding.UTF8.GetByteCount(b) + 2);

            var sequenceC = new Sequence<byte>();
            var sequenceCWriter = new MessagePackWriter(sequenceC);
            sequenceCWriter.Write(c);
            sequenceCWriter.Flush();
            sequenceC.Length.Is(Encoding.UTF8.GetByteCount(c) + 3);

            var sequenceD = new Sequence<byte>();
            var sequenceDWriter = new MessagePackWriter(sequenceD);
            sequenceDWriter.Write(d);
            sequenceDWriter.Flush();
            sequenceD.Length.Is(Encoding.UTF8.GetByteCount(d) + 5);

            var readerA = new MessagePackReader(sequenceA.AsReadOnlySequence);
            var readerB = new MessagePackReader(sequenceB.AsReadOnlySequence);
            var readerC = new MessagePackReader(sequenceC.AsReadOnlySequence);
            var readerD = new MessagePackReader(sequenceD.AsReadOnlySequence);
            readerA.ReadString().Is(a);
            readerB.ReadString().Is(b);
            readerC.ReadString().Is(c);
            readerD.ReadString().Is(d);
        }

        // https://github.com/neuecc/MessagePack-CSharp/issues/22
        [Fact]
        public void DecimalLang()
        {
            var estonian = new CultureInfo("et-EE");
            CultureInfo.CurrentCulture = estonian;

            var b = MessagePackSerializer.Serialize(12345.6789M);
            var d = MessagePackSerializer.Deserialize<decimal>(b);

            d.Is(12345.6789M);
        }

        [Fact]
        public void UriTest()
        {
            var absolute = new Uri("http://google.com/");
            this.Convert(absolute).ToString().Is("http://google.com/");

            var relative = new Uri("/me/", UriKind.Relative);
            this.Convert(relative).ToString().Is("/me/");
        }
    }
}
