using MessagePack.Resolvers;
using System;
using Xunit;

namespace MessagePack.Tests
{
    public enum AsString
    {
        Foo = 0,
        Bar = 1,
        Baz = 2,
        FooBar = 3,
        FooBaz = 4,
        BarBaz = 5,
        FooBarBaz = 6
    }

    [Flags]
    public enum AsStringFlag
    {
        Foo = 0,
        Bar = 1,
        Baz = 2,
        FooBar = 4,
        FooBaz = 8,
        BarBaz = 16,
        FooBarBaz = 32
    }

    public class EnumAsStringTest
    {
        private MessagePackSerializer serializer = new MessagePackSerializer();

        public static object[][] enumData = new object[][]
        {
            // simple
            new object[] { AsString.Foo, null, "Foo", "null" },
            new object[] { AsString.Bar, AsString.Baz , "Bar", "Baz"},
            new object[] { AsString.FooBar, AsString.FooBaz, "FooBar", "FooBaz" },
            new object[] { AsString.BarBaz, AsString.FooBarBaz, "BarBaz", "FooBarBaz" },
            new object[] { (AsString)10, (AsString)999, "10", "999" },
            // flags
            new object[] { AsStringFlag.Foo, null, "Foo", "null" },
            new object[] { AsStringFlag.Bar, AsStringFlag.Baz , "Bar", "Baz"},
            new object[] { AsStringFlag.FooBar, AsStringFlag.FooBaz, "FooBar", "FooBaz" },
            new object[] { AsStringFlag.BarBaz, AsStringFlag.FooBarBaz, "BarBaz", "FooBarBaz" },
            new object[] { AsStringFlag.Bar | AsStringFlag.FooBaz, AsStringFlag.BarBaz | AsStringFlag.FooBarBaz, "Bar, FooBaz", "BarBaz, FooBarBaz" },
            new object[] { (AsStringFlag)10, (AsStringFlag)999, "Baz, FooBaz", "999" },
        };

        [Theory]
        [MemberData(nameof(enumData))]
        public void EnumTest<T>(T x, T? y, string xName, string yName)
            where T : struct
        {
            var bin = serializer.Serialize(x, DynamicEnumAsStringResolver.Instance);
            serializer.ToJson(bin).Trim('\"').Is(xName);
            serializer.Deserialize<T>(bin, DynamicEnumAsStringResolver.Instance).Is(x);

            var bin2 = serializer.Serialize(y, DynamicEnumAsStringResolver.Instance);
            serializer.ToJson(bin2).Trim('\"').Is(yName);
            serializer.Deserialize<T?>(bin2, DynamicEnumAsStringResolver.Instance).Is(y);
        }
    }
}
