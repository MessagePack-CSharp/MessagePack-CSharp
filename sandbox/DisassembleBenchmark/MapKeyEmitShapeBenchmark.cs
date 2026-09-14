// MsgPack104: hand-copied generated-formatter shapes; the emit skeleton is the benchmarked subject.
#pragma warning disable MsgPack104

using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SerializerFoundation;
using MessagePack;
using static MessagePack.MessagePackPrimitives;

// In-run A/B of ObjectEmitter's string-key match skeletons on the REAL formatter shape (NbPocoMap: "SomeInt" + "SomeString").
//   ChainShape    - the pre-automata emitted shape: if/else if key.SequenceEqual chain
//   AutomataShape - the length-first automata emitted shape: switch (byteCount) + exact-width LE constant compares
//   Generated     - the actual source-generated formatter through the default chain (sanity row)
//   AutomataNoSwitch / ChainContinue - isolation variants (switch statement / loop shape)
//   AutomataNoInline - the automata with [MethodImpl(NoInlining)] on Deserialize
//
// VERDICT (elevated counters + BDN-child JitDisasm via --jitdisasm=): the 22.6 vs 43.0ns split between ChainShape and AutomataShape was never the key matching.
// Both formatters' standalone Tier1 bodies are equivalent (2.7KB, all callees inlined, 0 branch misses), but in the automata child the JIT chose to inline the whole Deserialize body into the PGO-devirtualized caller and the inline budget then starved the body's OWN callees: BinaryPrimitives reads, Advance, ReadInt32, ReadString all stayed real calls (+150 instructions/op, IPC 4.26 -> 3.04).
// The chain child left the formatter as a direct call into its clean standalone body.
// NoInlining pins that outcome deterministically: AutomataNoInline ties ChainShape (22.7 vs 22.6ns), and ObjectEmitter now emits it on every generated Deserialize.
public class MapKeyEmitShapeBenchmark
{
    MessagePackSerializerOptions chainOptions = null!;
    MessagePackSerializerOptions automataOptions = null!;
    MessagePackSerializerOptions noSwitchOptions = null!;
    MessagePackSerializerOptions chainContinueOptions = null!;
    MessagePackSerializerOptions noInlineOptions = null!;
    ReadOnlySequence<byte> payload;

    [GlobalSetup]
    public void Setup()
    {
        chainOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [ChainShapeFactory.Instance, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        automataOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [AutomataShapeFactory.Instance, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        noSwitchOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [NoSwitchShapeFactory.Instance, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        chainContinueOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [ChainContinueShapeFactory.Instance, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        noInlineOptions = new MessagePackSerializerOptions(new MessagePackFormatterResolver(
            [NoInlineShapeFactory.Instance, BuiltInFormatterFactory.Instance, GenericFormatterFactory.Instance]));
        payload =new ReadOnlySequence<byte>(MessagePack.MessagePackSerializer.Serialize(new NbPocoMap { SomeInt = 42, SomeString = "Hello, World!" }));

        foreach (var options in new[] { chainOptions, automataOptions, noSwitchOptions, chainContinueOptions, noInlineOptions, null })
        {
            var v = options is null
                ? MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload)!
                : MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, options)!;
            if (v.SomeInt != 42 || v.SomeString != "Hello, World!")
            {
                throw new InvalidOperationException("shape formatter roundtrip mismatch");
            }
        }
    }

    [Benchmark(Baseline = true)]
    public NbPocoMap? DeserializeChainShape() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, chainOptions);

    [Benchmark]
    public NbPocoMap? DeserializeAutomataShape() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, automataOptions);

    [Benchmark]
    public NbPocoMap? DeserializeGenerated() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload);

    [Benchmark]
    public NbPocoMap? DeserializeAutomataNoSwitch() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, noSwitchOptions);

    [Benchmark]
    public NbPocoMap? DeserializeChainContinue() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, chainContinueOptions);

    [Benchmark]
    public NbPocoMap? DeserializeAutomataNoInline() => MessagePack.MessagePackSerializer.Deserialize<NbPocoMap>(payload, noInlineOptions);
}

// (partial: FactoryBridgeGenerator supplies the Type-based CreateFormatter tier)
sealed partial class ChainShapeFactory : MessagePackFormatterFactory
{
    public static readonly ChainShapeFactory Instance = new ChainShapeFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(NbPocoMap) ? new ChainShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

sealed partial class AutomataShapeFactory : MessagePackFormatterFactory
{
    public static readonly AutomataShapeFactory Instance = new AutomataShapeFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(NbPocoMap) ? new AutomataShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

sealed partial class NoSwitchShapeFactory : MessagePackFormatterFactory
{
    public static readonly NoSwitchShapeFactory Instance = new NoSwitchShapeFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(NbPocoMap) ? new NoSwitchShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

sealed partial class ChainContinueShapeFactory : MessagePackFormatterFactory
{
    public static readonly ChainContinueShapeFactory Instance = new ChainContinueShapeFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(NbPocoMap) ? new ChainContinueShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

sealed partial class NoInlineShapeFactory : MessagePackFormatterFactory
{
    public static readonly NoInlineShapeFactory Instance = new NoInlineShapeFactory();

    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
    {
        return type == typeof(NbPocoMap) ? new NoInlineShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

// the automata shape with [MethodImpl(NoInlining)] on Deserialize: keeps the formatter a standalone compilation unit so the caller-side inline roulette (inline the big body, starve its callees) cannot happen
sealed class NoInlineShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> fSomeString = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        fSomeString = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, NbPocoMap? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        {
            ref var d = ref buffer.GetReference(1 + 8 + MaxInt32Length + 11);
            var w = 0;
            w += UnsafeWriteFixMapHeader(ref Unsafe.Add(ref d, w), 2);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xa7, 0x53, 0x6f, 0x6d, 0x65, 0x49, 0x6e, 0x74 });
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.SomeInt);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xaa, 0x53, 0x6f, 0x6d, 0x65, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67 });
            buffer.Advance(w);
        }
        fSomeString.Serialize(ref buffer, ref state, value.SomeString);
        state.Exit();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NbPocoMap? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var result = value ?? new NbPocoMap();
        state.Enter();

        var count = buffer.ReadMapHeader();
        ulong seenMembers = 0;
        for (int i = 0; i < count; i++)
        {
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);

            switch (byteCount)
            {
                case 7:
                    if (BinaryPrimitives.ReadUInt32LittleEndian(key) == 0x656d6f53U && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) == 0x746e4965U) // "SomeInt"
                    {
                        buffer.Advance(byteCount);
                        if ((seenMembers & (1UL << 0)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                        seenMembers |= 1UL << 0;
                        result.SomeInt = buffer.ReadInt32();
                        continue;
                    }
                    break;
                case 10:
                    if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x69727453656d6f53UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(2)) == 0x676e69727453656dUL) // "SomeString"
                    {
                        buffer.Advance(byteCount);
                        if ((seenMembers & (1UL << 1)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                        seenMembers |= 1UL << 1;
                        {
                            var v = result.SomeString;
                            fSomeString.Deserialize(ref buffer, ref state, ref v);
                            result.SomeString = v;
                        }
                        continue;
                    }
                    break;
            }

            buffer.Advance(byteCount);
            buffer.Skip(); // version tolerance: unknown key's value
        }

        state.Exit();
        value = result;
    }
}

// byte-for-byte the OLD emitted Deserialize (linear SequenceEqual chain), everything else identical to the generated formatter
sealed class ChainShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> fSomeString = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        fSomeString = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, NbPocoMap? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        {
            ref var d = ref buffer.GetReference(1 + 8 + MaxInt32Length + 11);
            var w = 0;
            w += UnsafeWriteFixMapHeader(ref Unsafe.Add(ref d, w), 2);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xa7, 0x53, 0x6f, 0x6d, 0x65, 0x49, 0x6e, 0x74 });
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.SomeInt);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xaa, 0x53, 0x6f, 0x6d, 0x65, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67 });
            buffer.Advance(w);
        }
        fSomeString.Serialize(ref buffer, ref state, value.SomeString);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NbPocoMap? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var result = value ?? new NbPocoMap();
        state.Enter();

        var count = buffer.ReadMapHeader();
        ulong seenMembers = 0;
        for (int i = 0; i < count; i++)
        {
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);

            if (key.SequenceEqual("SomeInt"u8))
            {
                buffer.Advance(byteCount);
                if ((seenMembers & (1UL << 0)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                seenMembers |= 1UL << 0;
                result.SomeInt = buffer.ReadInt32();
            }
            else if (key.SequenceEqual("SomeString"u8))
            {
                buffer.Advance(byteCount);
                if ((seenMembers & (1UL << 1)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                seenMembers |= 1UL << 1;
                {
                    var v = result.SomeString;
                    fSomeString.Deserialize(ref buffer, ref state, ref v);
                    result.SomeString = v;
                }
            }
            else
            {
                buffer.Advance(byteCount);
                buffer.Skip(); // version tolerance: unknown key's value
            }
        }

        state.Exit();
        value = result;
    }
}

// byte-for-byte the NEW emitted Deserialize (length-first automata), everything else identical
sealed class AutomataShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> fSomeString = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        fSomeString = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, NbPocoMap? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        {
            ref var d = ref buffer.GetReference(1 + 8 + MaxInt32Length + 11);
            var w = 0;
            w += UnsafeWriteFixMapHeader(ref Unsafe.Add(ref d, w), 2);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xa7, 0x53, 0x6f, 0x6d, 0x65, 0x49, 0x6e, 0x74 });
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.SomeInt);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xaa, 0x53, 0x6f, 0x6d, 0x65, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67 });
            buffer.Advance(w);
        }
        fSomeString.Serialize(ref buffer, ref state, value.SomeString);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NbPocoMap? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var result = value ?? new NbPocoMap();
        state.Enter();

        var count = buffer.ReadMapHeader();
        ulong seenMembers = 0;
        for (int i = 0; i < count; i++)
        {
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);

            switch (byteCount)
            {
                case 7:
                    if (BinaryPrimitives.ReadUInt32LittleEndian(key) == 0x656d6f53U && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) == 0x746e4965U) // "SomeInt"
                    {
                        buffer.Advance(byteCount);
                        if ((seenMembers & (1UL << 0)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                        seenMembers |= 1UL << 0;
                        result.SomeInt = buffer.ReadInt32();
                        continue;
                    }
                    break;
                case 10:
                    if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x69727453656d6f53UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(2)) == 0x676e69727453656dUL) // "SomeString"
                    {
                        buffer.Advance(byteCount);
                        if ((seenMembers & (1UL << 1)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                        seenMembers |= 1UL << 1;
                        {
                            var v = result.SomeString;
                            fSomeString.Deserialize(ref buffer, ref state, ref v);
                            result.SomeString = v;
                        }
                        continue;
                    }
                    break;
            }

            buffer.Advance(byteCount);
            buffer.Skip(); // version tolerance: unknown key's value
        }

        state.Exit();
        value = result;
    }
}

// the automata reads and continue-based loop, with the switch dispatch replaced by an if chain: isolates the switch statement
sealed class NoSwitchShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> fSomeString = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        fSomeString = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, NbPocoMap? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        {
            ref var d = ref buffer.GetReference(1 + 8 + MaxInt32Length + 11);
            var w = 0;
            w += UnsafeWriteFixMapHeader(ref Unsafe.Add(ref d, w), 2);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xa7, 0x53, 0x6f, 0x6d, 0x65, 0x49, 0x6e, 0x74 });
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.SomeInt);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xaa, 0x53, 0x6f, 0x6d, 0x65, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67 });
            buffer.Advance(w);
        }
        fSomeString.Serialize(ref buffer, ref state, value.SomeString);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NbPocoMap? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var result = value ?? new NbPocoMap();
        state.Enter();

        var count = buffer.ReadMapHeader();
        ulong seenMembers = 0;
        for (int i = 0; i < count; i++)
        {
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);

            if (byteCount == 7)
            {
                if (BinaryPrimitives.ReadUInt32LittleEndian(key) == 0x656d6f53U && BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(3)) == 0x746e4965U) // "SomeInt"
                {
                    buffer.Advance(byteCount);
                    if ((seenMembers & (1UL << 0)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                    seenMembers |= 1UL << 0;
                    result.SomeInt = buffer.ReadInt32();
                    continue;
                }
            }
            else if (byteCount == 10)
            {
                if (BinaryPrimitives.ReadUInt64LittleEndian(key) == 0x69727453656d6f53UL && BinaryPrimitives.ReadUInt64LittleEndian(key.Slice(2)) == 0x676e69727453656dUL) // "SomeString"
                {
                    buffer.Advance(byteCount);
                    if ((seenMembers & (1UL << 1)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                    seenMembers |= 1UL << 1;
                    {
                        var v = result.SomeString;
                        fSomeString.Deserialize(ref buffer, ref state, ref v);
                        result.SomeString = v;
                    }
                    continue;
                }
            }

            buffer.Advance(byteCount);
            buffer.Skip(); // version tolerance: unknown key's value
        }

        state.Exit();
        value = result;
    }
}

// the SequenceEqual chain, but each matched branch ends with continue and the unknown path falls through: isolates the loop shape
sealed class ChainContinueShapeNbPocoMapFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, NbPocoMap?>
    where TWriteBuffer : struct, IWriteBuffer, allows ref struct
    where TReadBuffer : struct, IReadBuffer, allows ref struct
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, string?> fSomeString = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        fSomeString = resolver.GetFormatter<TWriteBuffer, TReadBuffer, string?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, NbPocoMap? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }
        state.Enter();
        {
            ref var d = ref buffer.GetReference(1 + 8 + MaxInt32Length + 11);
            var w = 0;
            w += UnsafeWriteFixMapHeader(ref Unsafe.Add(ref d, w), 2);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xa7, 0x53, 0x6f, 0x6d, 0x65, 0x49, 0x6e, 0x74 });
            w += UnsafeWriteInt32(ref Unsafe.Add(ref d, w), value.SomeInt);
            w += UnsafeWriteRaw(ref Unsafe.Add(ref d, w), new byte[] { 0xaa, 0x53, 0x6f, 0x6d, 0x65, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67 });
            buffer.Advance(w);
        }
        fSomeString.Serialize(ref buffer, ref state, value.SomeString);
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref NbPocoMap? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var result = value ?? new NbPocoMap();
        state.Enter();

        var count = buffer.ReadMapHeader();
        ulong seenMembers = 0;
        for (int i = 0; i < count; i++)
        {
            var byteCount = buffer.ReadStringHeader();
            if (!buffer.TryGetSpan(byteCount, out var key))
            {
                throw new MessagePackSerializationException("Unexpected end of data while reading a map key.");
            }
            key = key.Slice(0, byteCount);

            if (key.SequenceEqual("SomeInt"u8))
            {
                buffer.Advance(byteCount);
                if ((seenMembers & (1UL << 0)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                seenMembers |= 1UL << 0;
                result.SomeInt = buffer.ReadInt32();
                continue;
            }
            if (key.SequenceEqual("SomeString"u8))
            {
                buffer.Advance(byteCount);
                if ((seenMembers & (1UL << 1)) != 0) { throw new MessagePackSerializationException("The map in the payload defines the same key more than once"); }
                seenMembers |= 1UL << 1;
                {
                    var v = result.SomeString;
                    fSomeString.Deserialize(ref buffer, ref state, ref v);
                    result.SomeString = v;
                }
                continue;
            }

            buffer.Advance(byteCount);
            buffer.Skip(); // version tolerance: unknown key's value
        }

        state.Exit();
        value = result;
    }
}
