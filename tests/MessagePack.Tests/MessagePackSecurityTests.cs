// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using MessagePack;
using MessagePack.Tests;
using Xunit;
using Xunit.Abstractions;

public class MessagePackSecurityTests
{
    public MessagePackSecurityTests(ITestOutputHelper logger)
    {
        Logger = logger;
    }

    public ITestOutputHelper Logger { get; }

    [Fact]
    public void Untrusted()
    {
        Assert.True(MessagePackSecurity.UntrustedData.HashCollisionResistant);
        Assert.Equal(64 * 1024 * 1024, MessagePackSecurity.UntrustedData.MaximumDecompressedSize);
    }

    [Fact]
    public void Trusted()
    {
        Assert.False(MessagePackSecurity.TrustedData.HashCollisionResistant);
        Assert.Equal(int.MaxValue, MessagePackSecurity.TrustedData.MaximumDecompressedSize);
    }

    [Fact]
    public void WithHashCollisionResistant()
    {
        Assert.Same(MessagePackSecurity.TrustedData, MessagePackSecurity.TrustedData.WithHashCollisionResistant(false));
        Assert.True(MessagePackSecurity.TrustedData.WithHashCollisionResistant(true).HashCollisionResistant);
    }

    [Fact]
    [Trait("CWE", "409")]
    public void WithMaximumDecompressedSize()
    {
        Assert.Same(MessagePackSecurity.UntrustedData, MessagePackSecurity.UntrustedData.WithMaximumDecompressedSize(64 * 1024 * 1024));
        Assert.Throws<ArgumentOutOfRangeException>(() => MessagePackSecurity.UntrustedData.WithMaximumDecompressedSize(-1));
        Assert.Equal(1024, MessagePackSecurity.UntrustedData.WithMaximumDecompressedSize(1024).MaximumDecompressedSize);
    }

    [Fact]
    public void EqualityComparer_CollisionResistance_Int64()
    {
        const long value1 = 0x100000001;
        const long value2 = 0x200000002;
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<long>();
        Assert.Equal(EqualityComparer<long>.Default.GetHashCode(value1), EqualityComparer<long>.Default.GetHashCode(value2)); // demonstrate insecurity
        Assert.NotEqual(eq.GetHashCode(value1), eq.GetHashCode(value2));
    }

    [Fact]
    public void EqualityComparer_CollisionResistance_Guid()
    {
        Guid value1 = new Guid(new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1 });
        Guid value2 = new Guid(new byte[] { 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 2 });
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<Guid>();
#if NETCOREAPP // We contrived the two GUIDs to force a collision on .NET Core. NETFx's algorithm is slightly different.
        Assert.Equal(EqualityComparer<Guid>.Default.GetHashCode(value1), EqualityComparer<Guid>.Default.GetHashCode(value2)); // demonstrate insecurity
#endif
        Assert.NotEqual(eq.GetHashCode(value1), eq.GetHashCode(value2));
    }

    [Fact]
    public unsafe void EqualityComparer_Single()
    {
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<float>();
        Assert.Equal(eq.GetHashCode(0.0f), eq.GetHashCode(-0.0f));

        // Try multiple forms of NaN
        float nan1, nan2;
        *(uint*)&nan1 = 0xFFC00001;
        *(uint*)&nan2 = 0xFF800001;

        Assert.True(float.IsNaN(nan1));
        Assert.True(float.IsNaN(nan2));
#if NETCOREAPP // .NET Framework had a bug where these would not be equal
        Assert.Equal(nan1.GetHashCode(), nan2.GetHashCode());
#endif
        Assert.Equal(eq.GetHashCode(nan1), eq.GetHashCode(nan2));
        Assert.Equal(eq.GetHashCode(float.NaN), eq.GetHashCode(-float.NaN));

        // Try various other clearly different numbers
        Assert.NotEqual(eq.GetHashCode(1.0f), eq.GetHashCode(-1.0f));
        Assert.NotEqual(eq.GetHashCode(1.0f), eq.GetHashCode(2.0f));
    }

    [Fact]
    public unsafe void EqualityComparer_Double()
    {
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<double>();
        Assert.Equal(eq.GetHashCode(0.0), eq.GetHashCode(-0.0));

        // Try multiple forms of NaN
        double nan1, nan2;
        *(ulong*)&nan1 = 0xFFF8000000000001;
        *(ulong*)&nan2 = 0xFFF8000000000002;

        Assert.True(double.IsNaN(nan1));
        Assert.True(double.IsNaN(nan2));
#if NETCOREAPP // .NET Framework had a bug where these would not be equal
        Assert.Equal(nan1.GetHashCode(), nan2.GetHashCode());
#endif
        Assert.Equal(eq.GetHashCode(nan1), eq.GetHashCode(nan2));
        Assert.Equal(eq.GetHashCode(double.NaN), eq.GetHashCode(-double.NaN));

        // Try various other clearly different numbers
        Assert.NotEqual(eq.GetHashCode(1.0), eq.GetHashCode(-1.0));
        Assert.NotEqual(eq.GetHashCode(1.0), eq.GetHashCode(2.0));
    }

    [Fact]
    public void EqualityComparer_Enums()
    {
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeInt8Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeUInt8Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeInt16Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeUInt16Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeInt32Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeUInt32Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeInt64Enum>());
        Assert.NotNull(MessagePackSecurity.UntrustedData.GetEqualityComparer<SomeUInt64Enum>());
    }

    [Fact]
    public void EqualityComparer_ObjectFallback()
    {
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<object>();

        Assert.Equal(eq.GetHashCode(null), eq.GetHashCode(null));
        Assert.NotEqual(eq.GetHashCode(null), eq.GetHashCode(new object()));

        Assert.Equal(eq.GetHashCode("hi"), eq.GetHashCode("hi"));
        Assert.NotEqual(eq.GetHashCode("hi"), eq.GetHashCode("bye"));

        Assert.Equal(eq.GetHashCode(true), eq.GetHashCode(true));
        Assert.NotEqual(eq.GetHashCode(true), eq.GetHashCode(false));

        var o = new object();
        Assert.Equal(eq.GetHashCode(o), eq.GetHashCode(o));
        Assert.NotEqual(eq.GetHashCode(o), eq.GetHashCode(new object()));
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_AfterCopyCtor()
    {
        var security = MessagePackSecurity.UntrustedData.WithMaximumObjectGraphDepth(15);
        Assert.NotNull(security.GetEqualityComparer<object>());
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_MatchesTypedHash()
    {
        var security = MessagePackSecurity.UntrustedData;
        AssertObjectHash(security, true);
        AssertObjectHash(security, 'x');
        AssertObjectHash(security, sbyte.MinValue);
        AssertObjectHash(security, byte.MaxValue);
        AssertObjectHash(security, short.MinValue);
        AssertObjectHash(security, ushort.MaxValue);
        AssertObjectHash(security, int.MinValue);
        AssertObjectHash(security, uint.MaxValue);
        AssertObjectHash(security, long.MinValue);
        AssertObjectHash(security, ulong.MaxValue);
        AssertObjectHash(security, Guid.Parse("b1575f42-57b7-4ea7-9971-122850196ffc"));
        AssertObjectHash(security, "a string key");
        AssertObjectHash(security, 1.25f);
        AssertObjectHash(security, float.NaN);
        AssertObjectHash(security, -0.0f);
        AssertObjectHash(security, 1.25d);
        AssertObjectHash(security, double.NaN);
        AssertObjectHash(security, -0.0d);
        AssertObjectHash(security, new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        AssertObjectHash(security, new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(2)));
        AssertObjectHash(security, (SomeInt8Enum)sbyte.MinValue);
        AssertObjectHash(security, (SomeUInt8Enum)byte.MaxValue);
        AssertObjectHash(security, (SomeInt16Enum)short.MinValue);
        AssertObjectHash(security, (SomeUInt16Enum)ushort.MaxValue);
        AssertObjectHash(security, (SomeInt32Enum)int.MinValue);
        AssertObjectHash(security, (SomeUInt32Enum)uint.MaxValue);
        AssertObjectHash(security, (SomeInt64Enum)long.MinValue);
        AssertObjectHash(security, (SomeUInt64Enum)ulong.MaxValue);
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_EquivalentRepresentations()
    {
        var comparer = MessagePackSecurity.UntrustedData.GetEqualityComparer<object>();
        Assert.Equal(comparer.GetHashCode(0.0f), comparer.GetHashCode(-0.0f));
        Assert.Equal(comparer.GetHashCode(0.0d), comparer.GetHashCode(-0.0d));
        var utc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Assert.Equal(comparer.GetHashCode(utc), comparer.GetHashCode(DateTime.SpecifyKind(utc, DateTimeKind.Local)));
        var instant = new DateTimeOffset(utc);
        Assert.Equal(comparer.GetHashCode(instant), comparer.GetHashCode(instant.ToOffset(TimeSpan.FromHours(2))));
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_UsesGenericOverrides()
    {
        var security = new CustomSecurity();
        AssertObjectHash(security, "a string key");
        AssertObjectHash(security, 17);
        AssertObjectHash(security, (SomeInt64Enum)0x100000001);
        AssertObjectHash(security, new CustomKey("a custom key"));

        var comparer = security.GetEqualityComparer<object>();
        int calls = security.FactoryCalls[typeof(CustomKey)];
        comparer.GetHashCode(new CustomKey("another custom key"));
        Assert.Equal(calls, security.FactoryCalls[typeof(CustomKey)]);
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_CloneUsesItsOwnOverrides()
    {
        var security = new CustomSecurity();
        var key = new CustomKey("a custom key");
        int originalHash = security.GetEqualityComparer<object>().GetHashCode(key);
        var clone = Assert.IsType<CustomSecurity>(security.WithMaximumObjectGraphDepth(37));
        Assert.True(clone.HashCollisionResistant);
        Assert.Equal(security.MaximumDecompressedSize, clone.MaximumDecompressedSize);
        AssertObjectHash(clone, key);
        Assert.NotEqual(originalHash, clone.GetEqualityComparer<object>().GetHashCode(key));
        Assert.Equal(originalHash, security.GetEqualityComparer<object>().GetHashCode(key));
    }

    [Fact]
    public void EqualityComparer_NonGenericOverride_IsIndependent()
    {
        var security = new NonGenericCustomSecurity();
        Assert.Same(MessagePackSecurity.UntrustedData.GetEqualityComparer<string>(), security.GetEqualityComparer());
        Assert.NotSame(security.GetEqualityComparer(), security.GetEqualityComparer<object>());
        Assert.Equal(security.GetEqualityComparer<string>().GetHashCode("a string key"), security.GetEqualityComparer().GetHashCode("a string key"));
    }

    [Fact]
    public void EqualityComparer_ObjectFallback_NonGenericPreservesRejection()
    {
        var security = MessagePackSecurity.UntrustedData;
        Assert.Equal(0, security.GetEqualityComparer().GetHashCode(null));
        foreach (object key in new object[] { 1m, new Version(1, 2), new byte[] { 1, 2 } })
        {
            Assert.Throws<TypeAccessException>(() => security.GetEqualityComparer().GetHashCode(key));
            Assert.Throws<TypeAccessException>(() => security.GetEqualityComparer<object>().GetHashCode(key));
        }
    }

    private static void AssertObjectHash<T>(MessagePackSecurity security, T value)
    {
        int expected = security.GetEqualityComparer<T>().GetHashCode(value);
        Assert.Equal(expected, security.GetEqualityComparer<object>().GetHashCode(value));
        Assert.Equal(expected, security.GetEqualityComparer().GetHashCode(value));
    }

    /// <summary>
    /// Verifies that arbitrary other types not known to be hash safe will be rejected.
    /// </summary>
    [Fact]
    public void EqualityComparer_ObjectFallback_UnsupportedType()
    {
        var eq = MessagePackSecurity.UntrustedData.GetEqualityComparer<object>();
        var ex = Assert.Throws<TypeAccessException>(() => eq.GetHashCode(new AggregateException()));
        this.Logger.WriteLine(ex.ToString());
    }

    [Fact]
    public void TypelessFormatterWithUntrustedData_SafeKeys()
    {
        var data = new
        {
            A = (byte)3,
            B = new Dictionary<object, byte>
            {
                { "C", 15 },
            },
            D = new string[] { "E", "F" },
        };
        byte[] msgpack = MessagePackSerializer.Serialize(data);
        var deserialized = (IDictionary<object, object>)MessagePackSerializer.Deserialize<object>(msgpack, MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData));
        Assert.Equal(data.A, deserialized["A"]);
        Assert.Equal(data.B["C"], ((Dictionary<object, object>)deserialized["B"])["C"]);
        Assert.Equal(data.D, deserialized["D"]);
    }

    [Fact]
    public void TypelessFormatterWithUntrustedData_UnsafeKeys()
    {
        var data = new
        {
            B = new Dictionary<object, byte>
            {
                { new ArbitraryType(), 15 },
            },
        };
        byte[] msgpack = MessagePackSerializer.Serialize(data);
        var ex = Assert.Throws<MessagePackSerializationException>(() => MessagePackSerializer.Deserialize<object>(msgpack, MessagePackSerializerOptions.Standard.WithSecurity(MessagePackSecurity.UntrustedData)));
        Assert.IsType<TypeAccessException>(ex.InnerException);
        this.Logger.WriteLine(ex.ToString());
    }

    [DataContract]
    public class ArbitraryType
    {
    }

    private sealed class CustomKey
    {
        internal CustomKey(string value) => this.Value = value;

        internal string Value { get; }
    }

    private sealed class CustomSecurity : MessagePackSecurity
    {
        internal CustomSecurity()
            : base(UntrustedData)
        {
        }

        private CustomSecurity(CustomSecurity template)
            : base(template)
        {
        }

        internal Dictionary<Type, int> FactoryCalls { get; } = new Dictionary<Type, int>();

        protected override IEqualityComparer<T> GetHashCollisionResistantEqualityComparer<T>()
        {
            this.FactoryCalls.TryGetValue(typeof(T), out int calls);
            this.FactoryCalls[typeof(T)] = calls + 1;
            if (typeof(T) == typeof(object))
            {
                return base.GetHashCollisionResistantEqualityComparer<T>();
            }

            if (typeof(T) == typeof(CustomKey))
            {
                var strings = base.GetHashCollisionResistantEqualityComparer<string>();
                return new CustomComparer<T>(value => strings.GetHashCode(((CustomKey)(object)value).Value) ^ this.MaximumObjectGraphDepth);
            }

            var comparer = base.GetHashCollisionResistantEqualityComparer<T>();
            return new CustomComparer<T>(value => comparer.GetHashCode(value) ^ this.MaximumObjectGraphDepth);
        }

        protected override MessagePackSecurity Clone() => new CustomSecurity(this);
    }

    private sealed class CustomComparer<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private readonly Func<T, int> hash;

        internal CustomComparer(Func<T, int> hash) => this.hash = hash;

        public bool Equals(T x, T y) => EqualityComparer<T>.Default.Equals(x, y);

        public int GetHashCode(T value) => this.hash(value);

        bool IEqualityComparer.Equals(object x, object y) => ((IEqualityComparer)EqualityComparer<T>.Default).Equals(x, y);

        int IEqualityComparer.GetHashCode(object value) => this.GetHashCode((T)value);
    }

    private sealed class NonGenericCustomSecurity : MessagePackSecurity
    {
        internal NonGenericCustomSecurity()
            : base(UntrustedData)
        {
        }

        protected override IEqualityComparer GetHashCollisionResistantEqualityComparer() => (IEqualityComparer)this.GetHashCollisionResistantEqualityComparer<string>();
    }

    public enum SomeInt8Enum : sbyte
    {
    }

    public enum SomeUInt8Enum : byte
    {
    }

    public enum SomeInt16Enum : short
    {
    }

    public enum SomeUInt16Enum : ushort
    {
    }

    public enum SomeInt32Enum : int
    {
    }

    public enum SomeUInt32Enum : uint
    {
    }

    public enum SomeInt64Enum : long
    {
    }

    public enum SomeUInt64Enum : ulong
    {
    }
}
