// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using MessagePack.Resolvers;
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
        FooBarBaz = 6,
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
        FooBarBaz = 32,
    }

    public class EnumAsStringTest
    {
        public static object[][] EnumData = new object[][]
        {
            // simple
            new object[] { AsString.Foo, null, "Foo", "null" },
            new object[] { AsString.Bar, AsString.Baz, "Bar", "Baz" },
            new object[] { AsString.FooBar, AsString.FooBaz, "FooBar", "FooBaz" },
            new object[] { AsString.BarBaz, AsString.FooBarBaz, "BarBaz", "FooBarBaz" },
            new object[] { (AsString)10, (AsString)999, "10", "999" },

            // flags
            new object[] { AsStringFlag.Foo, null, "Foo", "null" },
            new object[] { AsStringFlag.Bar, AsStringFlag.Baz, "Bar", "Baz" },
            new object[] { AsStringFlag.FooBar, AsStringFlag.FooBaz, "FooBar", "FooBaz" },
            new object[] { AsStringFlag.BarBaz, AsStringFlag.FooBarBaz, "BarBaz", "FooBarBaz" },
            new object[] { AsStringFlag.Bar | AsStringFlag.FooBaz, AsStringFlag.BarBaz | AsStringFlag.FooBarBaz, "Bar, FooBaz", "BarBaz, FooBarBaz" },
            new object[] { (AsStringFlag)10, (AsStringFlag)999, "Baz, FooBaz", "999" },
        };

        [Theory]
        [MemberData(nameof(EnumData))]
        public void EnumTest<T>(T x, T? y, string xName, string yName)
            where T : struct
        {
            var bin = MessagePackSerializer.Serialize(x, DynamicEnumAsStringResolver.Options);
            MessagePackSerializer.ConvertToJson(bin).Trim('\"').Is(xName);
            MessagePackSerializer.Deserialize<T>(bin, DynamicEnumAsStringResolver.Options).Is(x);

            var bin2 = MessagePackSerializer.Serialize(y, DynamicEnumAsStringResolver.Options);
            MessagePackSerializer.ConvertToJson(bin2).Trim('\"').Is(yName);
            MessagePackSerializer.Deserialize<T?>(bin2, DynamicEnumAsStringResolver.Options).Is(y);
        }
    }
}
