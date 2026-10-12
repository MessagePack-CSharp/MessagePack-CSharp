// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using MessagePack;
using MessagePack.Formatters;

internal static class Program
{
    private static int assertions;

    private static int Main(string[] args)
    {
        try
        {
            bool optIn = AppContext.TryGetSwitch("MessagePack.Security.UseNonReflectiveObjectComparer", out bool enabled) && enabled;
            Check(args.Length == 1 && args[0] == (optIn ? "opt-in" : "legacy"), "Expected mode must match the application configuration.");
            VerifyBuiltInKeys();
            VerifyCustomKeys(optIn);
            VerifySerialization();
            Console.WriteLine($"{(optIn ? "Opt-in" : "Legacy")} security: {assertions} assertions passed.");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static void VerifyBuiltInKeys()
    {
        MessagePackSecurity security = MessagePackSecurity.UntrustedData;
        Check(security.HashCollisionResistant, "Collision protection must remain enabled.");
        Check(security.MaximumDecompressedSize == 64 * 1024 * 1024, "Decompression limit changed.");
        Check(ReferenceEquals(EqualityComparer<object>.Default, MessagePackSecurity.TrustedData.GetEqualityComparer<object>()), "TrustedData changed.");
        CheckHash(security, true);
        CheckHash(security, 'x');
        CheckHash(security, sbyte.MinValue);
        CheckHash(security, byte.MaxValue);
        CheckHash(security, short.MinValue);
        CheckHash(security, ushort.MaxValue);
        CheckHash(security, int.MinValue);
        CheckHash(security, uint.MaxValue);
        CheckHash(security, long.MinValue);
        CheckHash(security, ulong.MaxValue);
        CheckHash(security, "a string key");
        CheckHash(security, Guid.Parse("b1575f42-57b7-4ea7-9971-122850196ffc"));
        CheckHash(security, 1.25f);
        CheckHash(security, 1.25d);
        CheckHash(security, (Int8Key)sbyte.MinValue);
        CheckHash(security, (UInt8Key)byte.MaxValue);
        CheckHash(security, (Int16Key)short.MinValue);
        CheckHash(security, (UInt16Key)ushort.MaxValue);
        CheckHash(security, (Int32Key)int.MinValue);
        CheckHash(security, (UInt32Key)uint.MaxValue);
        CheckHash(security, (Int64Key)long.MinValue);
        CheckHash(security, (UInt64Key)ulong.MaxValue);

        IEqualityComparer<object> objects = security.GetEqualityComparer<object>();
        Check(objects.GetHashCode(null!) == 0 && security.GetEqualityComparer().GetHashCode(null!) == 0, "Null hashing changed.");
        object identity = new object();
        Check(objects.GetHashCode(identity) == identity.GetHashCode(), "Exact object identity hashing changed.");
        Check(objects.Equals("same", "same") && !objects.Equals(1, 1L), "Object equality changed.");
        Check(objects.GetHashCode(0.0f) == objects.GetHashCode(-0.0f), "Single signed zero mismatch.");
        Check(objects.GetHashCode(0.0d) == objects.GetHashCode(-0.0d), "Double signed zero mismatch.");
        float nan1 = BitConverter.Int32BitsToSingle(unchecked((int)0xffc00001));
        float nan2 = BitConverter.Int32BitsToSingle(unchecked((int)0xff800001));
        Check(objects.GetHashCode(nan1) == objects.GetHashCode(nan2), "Single NaN normalization changed.");
        double nan3 = BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000001));
        double nan4 = BitConverter.Int64BitsToDouble(unchecked((long)0xfff8000000000002));
        Check(objects.GetHashCode(nan3) == objects.GetHashCode(nan4), "Double NaN normalization changed.");
        DateTime utc = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        CheckHash(security, utc);
        Check(objects.GetHashCode(utc) == objects.GetHashCode(DateTime.SpecifyKind(utc, DateTimeKind.Local)), "DateTime normalization changed.");
        DateTimeOffset instant = new DateTimeOffset(utc);
        CheckHash(security, instant);
        Check(objects.GetHashCode(instant) == objects.GetHashCode(instant.ToOffset(TimeSpan.FromHours(2))), "DateTimeOffset normalization changed.");

        const long collision1 = 0x100000001;
        const long collision2 = 0x200000002;
        Check(collision1.GetHashCode() == collision2.GetHashCode(), "The ordinary-hash collision fixture is invalid.");
        Check(objects.GetHashCode(collision1) != objects.GetHashCode(collision2), "Int64 keyed hashing regressed.");
        Check(objects.GetHashCode((Int64Key)collision1) != objects.GetHashCode((Int64Key)collision2), "Enum keyed hashing regressed.");
        Guid guid1 = new Guid(new byte[] { 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1 });
        Guid guid2 = new Guid(new byte[] { 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 2, 0, 0, 0, 2 });
        Check(guid1.GetHashCode() == guid2.GetHashCode(), "The ordinary Guid collision fixture is invalid.");
        Check(objects.GetHashCode(guid1) != objects.GetHashCode(guid2), "Guid keyed hashing regressed.");

        Reject(() => security.GetEqualityComparer<decimal>());
        Reject(() => security.GetEqualityComparer<Version>());
        Reject(() => security.GetEqualityComparer<byte[]>());
        foreach (object unsupported in new object[] { 1m, new Version(1, 2), new byte[] { 1, 2 } })
        {
            Reject(() => objects.GetHashCode(unsupported));
            Reject(() => security.GetEqualityComparer().GetHashCode(unsupported));
        }

        MessagePackSecurity clone = security.WithMaximumObjectGraphDepth(37).WithMaximumDecompressedSize(4096);
        Check(clone.HashCollisionResistant && clone.MaximumObjectGraphDepth == 37 && clone.MaximumDecompressedSize == 4096, "Security clone changed.");
        CheckHash(clone, (UInt64Key)ulong.MaxValue);
    }

    private static void VerifyCustomKeys(bool optIn)
    {
        var key = new CustomKey("custom");
        var legacyOnly = new LegacySecurity();
        CheckHash(legacyOnly, "primitive override");

        // Typed calls provide the generic instantiations needed by the legacy NativeAOT path.
        int typedCustom = legacyOnly.GetEqualityComparer<CustomKey>().GetHashCode(key);
        int typedEnum = legacyOnly.GetEqualityComparer<Int64Key>().GetHashCode((Int64Key)17);
        if (optIn)
        {
            int boxedOnlyHash = MessagePackSecurity.UntrustedData.GetEqualityComparer<object>().GetHashCode((BoxedOnlyKey)ulong.MaxValue);
            int underlyingHash = MessagePackSecurity.UntrustedData.GetEqualityComparer<ulong>().GetHashCode(ulong.MaxValue);
            Check(boxedOnlyHash == underlyingHash, "Boxed enum without a typed enum factory call failed.");
            Reject(() => legacyOnly.GetEqualityComparer<object>().GetHashCode(key));
            int defaultEnum = MessagePackSecurity.UntrustedData.GetEqualityComparer<Int64Key>().GetHashCode((Int64Key)17);
            Check(legacyOnly.GetEqualityComparer<object>().GetHashCode((Int64Key)17) == defaultEnum, "Opt-in default enum hashing changed.");
            Check(defaultEnum != typedEnum, "The enum-specific override must be observably different.");
        }
        else
        {
            Check(legacyOnly.GetEqualityComparer<object>().GetHashCode(key) == typedCustom, "Legacy custom generic dispatch changed.");
            Check(legacyOnly.GetEqualityComparer<object>().GetHashCode((Int64Key)17) == typedEnum, "Legacy enum generic dispatch changed.");
        }

        var extended = new ExtendedSecurity();
        CheckHash(extended, key);
        CheckHash(extended, (Int64Key)17);
        IEqualityComparer<object> objects = extended.GetEqualityComparer<object>();
        int factoryCalls = extended.FactoryCalls;
        objects.GetHashCode(new CustomKey("another"));
        Check(extended.FactoryCalls == factoryCalls, "Per-type comparer caching changed.");
        Check(optIn ? extended.HookCalls > 0 : extended.HookCalls == 0, "New hook must be used only with explicit opt-in.");

        MessagePackSecurity clone = extended.WithMaximumObjectGraphDepth(37);
        Check(clone is ExtendedSecurity && clone.HashCollisionResistant, "Derived clone changed.");
        CheckHash(clone, key);
        Check(clone.GetEqualityComparer<object>().GetHashCode(key) != extended.GetEqualityComparer<object>().GetHashCode(key), "Clone reused the original instance's comparer.");
        var map = new Dictionary<object, int>(extended.GetEqualityComparer<object>()) { [key] = 1, [(Int64Key)17] = 2 };
        Check(map[new CustomKey("custom")] == 1 && map[(Int64Key)17] == 2, "Custom-key dictionary lookup failed.");
    }

    private static void VerifySerialization()
    {
        var options = new MessagePackSerializerOptions(PrimitiveResolver.Instance).WithSecurity(MessagePackSecurity.UntrustedData);
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        PrimitiveObjectFormatter.Instance.Serialize(ref writer, new Dictionary<object, object> { ["answer"] = 42 }, options);
        writer.Flush();
        byte[] expected = { 0x81, 0xa6, 0x61, 0x6e, 0x73, 0x77, 0x65, 0x72, 0xd2, 0x00, 0x00, 0x00, 0x2a };
        Check(output.WrittenSpan.SequenceEqual(expected), "Serializer wire format changed.");
        var reader = new MessagePackReader(output.WrittenMemory);
        var value = (Dictionary<object, object>)PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options)!;
        Check(Convert.ToInt32(value["answer"]) == 42, "Untrusted map deserialization failed.");
        Check(reader.End, "Deserializer did not consume the full payload.");
    }

    private static void CheckHash<T>(MessagePackSecurity security, T value)
    {
        int expected = security.GetEqualityComparer<T>().GetHashCode(value!);
        Check(security.GetEqualityComparer<object>().GetHashCode(value!) == expected, $"Object hash mismatch for {typeof(T)}.");
        Check(security.GetEqualityComparer().GetHashCode(value!) == expected, $"Non-generic hash mismatch for {typeof(T)}.");
    }

    private static void Reject(Action action)
    {
        try
        {
            action();
        }
        catch (TypeAccessException)
        {
            assertions++;
            return;
        }

        throw new InvalidOperationException("An unsupported type was accepted.");
    }

    private static void Check(bool condition, string message)
    {
        assertions++;
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }

    private enum Int8Key : sbyte
    {
    }

    private enum UInt8Key : byte
    {
    }

    private enum Int16Key : short
    {
    }

    private enum UInt16Key : ushort
    {
    }

    private enum Int32Key : int
    {
    }

    private enum UInt32Key : uint
    {
    }

    private enum Int64Key : long
    {
    }

    private enum UInt64Key : ulong
    {
    }

    private enum BoxedOnlyKey : ulong
    {
    }

    private sealed record CustomKey(string Value);

    private class LegacySecurity : MessagePackSecurity
    {
        internal LegacySecurity()
            : base(UntrustedData)
        {
        }

        protected LegacySecurity(LegacySecurity template)
            : base(template)
        {
        }

        internal int FactoryCalls { get; private set; }

        protected override IEqualityComparer<T> GetHashCollisionResistantEqualityComparer<T>()
        {
            this.FactoryCalls++;
            if (typeof(T) == typeof(CustomKey))
            {
                var strings = base.GetHashCollisionResistantEqualityComparer<string>();
                return new KeyComparer<T>(value => strings.GetHashCode(((CustomKey)(object)value!).Value) ^ this.MaximumObjectGraphDepth);
            }

            IEqualityComparer<T> comparer = base.GetHashCollisionResistantEqualityComparer<T>();
            return typeof(T) == typeof(Int64Key) || typeof(T) == typeof(string)
                ? new KeyComparer<T>(value => comparer.GetHashCode(value!) ^ this.MaximumObjectGraphDepth)
                : comparer;
        }

        protected override MessagePackSecurity Clone() => new LegacySecurity(this);
    }

    private sealed class ExtendedSecurity : LegacySecurity
    {
        internal ExtendedSecurity()
        {
        }

        private ExtendedSecurity(ExtendedSecurity template)
            : base(template)
        {
        }

        internal int HookCalls { get; private set; }

        protected override IEqualityComparer GetHashCollisionResistantEqualityComparer(Type type)
        {
            this.HookCalls++;
            return type == typeof(CustomKey) ? (IEqualityComparer)this.GetHashCollisionResistantEqualityComparer<CustomKey>() :
                type == typeof(Int64Key) ? (IEqualityComparer)this.GetHashCollisionResistantEqualityComparer<Int64Key>() :
                base.GetHashCollisionResistantEqualityComparer(type);
        }

        protected override MessagePackSecurity Clone() => new ExtendedSecurity(this);
    }

    private sealed class KeyComparer<T> : IEqualityComparer<T>, IEqualityComparer
    {
        private readonly Func<T, int> hash;

        internal KeyComparer(Func<T, int> hash) => this.hash = hash;

        public bool Equals(T? x, T? y) => EqualityComparer<T>.Default.Equals(x, y);

        public int GetHashCode(T value) => this.hash(value);

        bool IEqualityComparer.Equals(object? x, object? y) => ((IEqualityComparer)EqualityComparer<T>.Default).Equals(x, y);

        int IEqualityComparer.GetHashCode(object value) => this.GetHashCode((T)value);
    }

    private sealed class PrimitiveResolver : IFormatterResolver
    {
        internal static readonly PrimitiveResolver Instance = new();

        public IMessagePackFormatter<T>? GetFormatter<T>() => typeof(T) == typeof(object) ? (IMessagePackFormatter<T>)PrimitiveObjectFormatter.Instance : null;
    }
}
