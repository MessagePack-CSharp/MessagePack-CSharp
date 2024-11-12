// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !(MESSAGEPACK_FORCE_AOT || ENABLE_IL2CPP)
#define DYNAMIC_GENERATION
#endif

using System;
using System.Runtime.Serialization;
using MessagePack.Resolvers;
using Xunit;

namespace MessagePack.Tests
{
#if DYNAMIC_GENERATION

    public class EnumAsStringIgnoreCaseTests
    {
        private static readonly MessagePackSerializerOptions IgnoreCaseOptions = MessagePackSerializerOptions.Standard
            .WithResolver(DynamicEnumAsStringIgnoreCaseResolver.Instance);

        private static readonly MessagePackSerializerOptions CaseSensitiveOptions = DynamicEnumAsStringResolver.Options;

        public static object[][] EnumData = new object[][]
        {
            // simple ignore case
            new object[] { AsString.Foo, null, "Foo", null, true },
            new object[] { AsString.Bar, AsString.Baz, "BAr", "BAz", true },
            new object[] { AsString.FooBar, AsString.FooBaz, "FooBAr", "FooBAz", true },
            new object[] { AsString.BarBaz, AsString.FooBarBaz, "BarBAz", "FooBarBAz", true },
            new object[] { (AsString)10, (AsString)999, "10", "999", true },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", true },

            // flags ignore case
            new object[] { AsStringFlag.Foo, null, "Foo", null, true },
            new object[] { AsStringFlag.Bar, AsStringFlag.Baz, "BAr", "BAz", true },
            new object[] { AsStringFlag.FooBar, AsStringFlag.FooBaz, "FooBAr", "FooBAz", true },
            new object[] { AsStringFlag.BarBaz, AsStringFlag.FooBarBaz, "BarBAz", "FooBarBAz", true },
            new object[] { AsStringFlag.Bar | AsStringFlag.FooBaz, AsStringFlag.BarBaz | AsStringFlag.FooBarBaz, "Bar, FooBAz", "BarBAz, FooBarBAz", true },
            new object[] { (AsStringFlag)10, (AsStringFlag)999, "BAz, FooBAz", "999", true },

            // simple case sensetive
            new object[] { AsString.Foo, null, "Foo", null, false },
            new object[] { AsString.Bar, AsString.Baz, "Bar", "Baz", false },
            new object[] { AsString.FooBar, AsString.FooBaz, "FooBar", "FooBaz", false },
            new object[] { AsString.BarBaz, AsString.FooBarBaz, "BarBaz", "FooBarBaz", false },
            new object[] { (AsString)10, (AsString)999, "10", "999", false },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", false },

            // flags case sensetive
            new object[] { AsStringFlag.Foo, null, "Foo", null, false },
            new object[] { AsStringFlag.Bar, AsStringFlag.Baz, "Bar", "Baz", false },
            new object[] { AsStringFlag.FooBar, AsStringFlag.FooBaz, "FooBar", "FooBaz", false },
            new object[] { AsStringFlag.BarBaz, AsStringFlag.FooBarBaz, "BarBaz", "FooBarBaz", false },
            new object[] { AsStringFlag.Bar | AsStringFlag.FooBaz, AsStringFlag.BarBaz | AsStringFlag.FooBarBaz, "Bar, FooBaz", "BarBaz, FooBarBaz", false },
            new object[] { (AsStringFlag)10, (AsStringFlag)999, "Baz, FooBaz", "999", false },
        };

        public static object[][] EnumDataForEnumMember =
        {
            // ignore case
            new object[] { AsStringWithEnumMember.Foo, null, "FooVAlue", null, true },
            new object[] { AsStringWithEnumMember.Bar, AsStringWithEnumMember.Baz, "BarVAlue", "BazVAlue", true },
            new object[] { AsStringWithEnumMember.FooBar, AsStringWithEnumMember.FooBaz, "FooBArValue", "FooBAzValue", true },
            new object[] { AsStringWithEnumMember.BarBaz, AsStringWithEnumMember.FooBarBaz, "BarBAzValue", "FooBarBAzValue", true },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", true },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", true },
            new object[] { AsStringWithEnumMember.FooBarBazOther, AsStringWithEnumMember.FooBarBazOther, "FooBarBAzOther", "FooBArBazOther", true },

            // flags (for flags enumMember is partially supported) ignore case
            new object[] { AsStringFlagWithEnumMember.Foo, null, "FooValue", null, true },
            new object[] { AsStringFlagWithEnumMember.Bar, AsStringFlagWithEnumMember.Baz, "BArValue", "BAzValue", true },
            new object[] { AsStringFlagWithEnumMember.FooBar, AsStringFlagWithEnumMember.FooBaz, "FooBArValue", "FooBAzValue", true },
            new object[] { AsStringFlagWithEnumMember.BarBaz, AsStringFlagWithEnumMember.FooBarBaz, "BarBAzValue", "FooBArBazValue", true },
            new object[] { AsStringFlagWithEnumMember.Bar | AsStringFlagWithEnumMember.FooBaz, AsStringFlagWithEnumMember.BarBaz | AsStringFlagWithEnumMember.FooBarBaz, "BArValue, FooBAzValue", "BarBAzValue, FooBarBAzValue", true },
            new object[] { (AsStringFlagWithEnumMember)10, (AsStringFlagWithEnumMember)999, "BAzValue, FooBAzValue", "999", true },

            // case sensitive
            new object[] { AsStringWithEnumMember.Foo, null, "FooValue", null, false },
            new object[] { AsStringWithEnumMember.Bar, AsStringWithEnumMember.Baz, "BarValue", "BazValue", false },
            new object[] { AsStringWithEnumMember.FooBar, AsStringWithEnumMember.FooBaz, "FooBarValue", "FooBazValue", false },
            new object[] { AsStringWithEnumMember.BarBaz, AsStringWithEnumMember.FooBarBaz, "BarBazValue", "FooBarBazValue", false },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", false },
            new object[] { (AsStringWithEnumMember)10, (AsStringWithEnumMember)999, "10", "999", false },
            new object[] { AsStringWithEnumMember.FooBarBazOther, AsStringWithEnumMember.FooBarBazOther, "FooBarBazOther", "FooBarBazOther", false },

            // flags (for flags enumMember is partially supported) case sensitive
            new object[] { AsStringFlagWithEnumMember.Foo, null, "FooValue", null, false },
            new object[] { AsStringFlagWithEnumMember.Bar, AsStringFlagWithEnumMember.Baz, "BarValue", "BazValue", false },
            new object[] { AsStringFlagWithEnumMember.FooBar, AsStringFlagWithEnumMember.FooBaz, "FooBarValue", "FooBazValue", false },
            new object[] { AsStringFlagWithEnumMember.BarBaz, AsStringFlagWithEnumMember.FooBarBaz, "BarBazValue", "FooBarBazValue", false },
            new object[] { AsStringFlagWithEnumMember.Bar | AsStringFlagWithEnumMember.FooBaz, AsStringFlagWithEnumMember.BarBaz | AsStringFlagWithEnumMember.FooBarBaz, "BarValue, FooBazValue", "BarBazValue, FooBarBazValue", false },
            new object[] { (AsStringFlagWithEnumMember)10, (AsStringFlagWithEnumMember)999, "BazValue, FooBazValue", "999", false },
        };

        [Theory]
        [MemberData(nameof(EnumData))]
        public void EnumTest<T>(T x, T? y, string xName, string yName, bool ignoreCase)
            where T : struct
        {
            var bin = MessagePackSerializer.Serialize(xName, StandardResolver.Options);
            MessagePackSerializer.Deserialize<T>(bin, ignoreCase ? IgnoreCaseOptions : CaseSensitiveOptions).Is(x);

            var bin2 = MessagePackSerializer.Serialize(yName, StandardResolver.Options);
            MessagePackSerializer.Deserialize<T?>(bin2, ignoreCase ? IgnoreCaseOptions : CaseSensitiveOptions).Is(y);
        }

        [Theory]
        [MemberData(nameof(EnumDataForEnumMember))]
        public void EnumTestEnumMember<T>(T x, T? y, string xName, string yName, bool ignoreCase)
            where T : struct
        {
            var bin = MessagePackSerializer.Serialize(xName, StandardResolver.Options);
            MessagePackSerializer.Deserialize<T>(bin, ignoreCase ? IgnoreCaseOptions : CaseSensitiveOptions).Is(x);

            var bin2 = MessagePackSerializer.Serialize(yName, StandardResolver.Options);
            MessagePackSerializer.Deserialize<T?>(bin2, ignoreCase ? IgnoreCaseOptions : CaseSensitiveOptions).Is(y);
        }
    }

#endif
}
