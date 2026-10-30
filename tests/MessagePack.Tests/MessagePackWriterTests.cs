using System.Buffers;
using System.Reflection;

namespace MessagePack.Tests;

[MessagePackObject]
public class ManualEnvelopeBody
{
    [Key(0)] public int Id { get; set; }
    [Key(1)] public string? Name { get; set; }
}

// MessagePackWriter: the hand-built message API over the buffer extensions. This file is also compiled into
// MessagePack.Tests.Net8 (the netstandard2.1 asset) and MessagePack.Tests.NetFx (the netstandard2.0 asset), where the
// writer sits on the Compatible buffers, so it uses nothing a .NET Framework test cannot reference.
// MessagePackReaderTests reads the writer's output back.
public class MessagePackWriterTests
{
    [Fact]
    public void Serialize_MatchesWholeMessageEntry()
    {
        var value = new ManualEnvelopeBody { Id = 99, Name = "wire" };
        using var writer = new MessagePackWriter();
        writer.Serialize(value, MessagePackSerializerOptions.Default);
        Assert.Equal(MessagePackSerializer.Serialize(value, MessagePackSerializerOptions.Default), writer.ToArray());
    }

    [Fact]
    public void TypeBasedSerialize_MatchesWholeMessageEntry()
    {
        var body = new ManualEnvelopeBody { Id = 3, Name = "t" };
        using var writer = new MessagePackWriter();
        writer.Serialize(typeof(ManualEnvelopeBody), body, MessagePackSerializerOptions.Default);
        writer.Serialize(typeof(int), 42, MessagePackSerializerOptions.Default);

        var expected = MessagePackSerializer.Serialize(typeof(ManualEnvelopeBody), body, MessagePackSerializerOptions.Default)
            .Concat(MessagePackSerializer.Serialize(typeof(int), 42, MessagePackSerializerOptions.Default));
        Assert.Equal(expected, writer.ToArray());

        var rejected = false;
        try { writer.Serialize(typeof(int), "not an int", MessagePackSerializerOptions.Default); }
        catch (ArgumentException) { rejected = true; }
        Assert.True(rejected);
    }

    [Fact]
    public void Tokens_MatchBufferExtensionWire()
    {
        // the writer delegates to the extensions; the forced forms keep their width
        using var writer = new MessagePackWriter();
        writer.WriteForcedInt32(1);
        writer.WriteInt32(1);
        writer.WriteFixArrayHeader(2);
        writer.WriteMapHeader(0);
        Assert.Equal(new byte[] { 0xd2, 0, 0, 0, 1, 0x01, 0x92, 0x80 }, writer.ToArray());
        Assert.Equal(8, writer.BytesWritten);
    }

    [Fact]
    public void Output_ToArrayAndWriteToAgree()
    {
        // large enough to span several pooled segments
        using var writer = new MessagePackWriter();
        writer.WriteArrayHeader(300);
        var text = new string('x', 1000);
        for (var i = 0; i < 300; i++)
        {
            writer.WriteString(text);
        }
        var array = writer.ToArray();
        Assert.Equal(writer.BytesWritten, array.Length);

        var bufferWriter = new GrowingBufferWriter();
        writer.WriteTo(bufferWriter);
        Assert.Equal(array, bufferWriter.ToArray());

        using var stream = new MemoryStream();
        writer.WriteTo(stream);
        Assert.Equal(array, stream.ToArray());

        var span = new byte[array.Length + 1];
        writer.WriteTo(span);
        Assert.Equal(array, span.AsSpan(0, array.Length).ToArray());

        using var small = new MessagePackWriter();
        small.WriteInt32(1000);
        Span<byte> stack = stackalloc byte[8];
        small.WriteTo(stack);
        Assert.Equal(new byte[] { 0xcd, 0x03, 0xe8 }, stack.Slice(0, 3).ToArray());
        var tooShort = false;
        try { writer.WriteTo(new byte[array.Length - 1]); }
        catch (ArgumentException) { tooShort = true; }
        Assert.True(tooShort);

        Assert.Equal(300, MessagePackSerializer.Deserialize<string[]>(array).Length);
    }

    [Fact]
    public void Dispose_TwiceIsHarmlessAndTheWriterCanBeReused()
    {
        var writer = new MessagePackWriter();
        writer.WriteString(new string('y', 10_000));
        writer.Dispose();
        writer.Dispose();

        writer.WriteInt32(5);
        Assert.Equal(new byte[] { 0x05 }, writer.ToArray());
        writer.Dispose();
    }

    [Fact]
    public void ProcessorOptions_Rejected()
    {
        var options = MessagePackSerializerOptions.Default with { MessageProcessor = new FramingProcessor() };
        using var writer = new MessagePackWriter();
        var rejected = false;
        try { writer.Serialize(1, options); }
        catch (ArgumentException) { rejected = true; }
        Assert.True(rejected);
    }

    // the marker puts it under SerializerFoundation's SF002 copy checks, which reach consumers with the package;
    // IDisposable, as on the buffer it wraps, lets dispose analyzers see it
    [Fact]
    public void Writer_IsMarkedNonCopyableAndDisposable()
    {
        Assert.True(typeof(MessagePackWriter).IsDefined(typeof(SerializerFoundation.CodeAnalysis.NonCopyableAttribute), inherit: false));
        Assert.True(typeof(IDisposable).IsAssignableFrom(typeof(MessagePackWriter)));
    }

    // a method added to the extensions or the writer without the other fails here
    [Fact]
    public void TokenMethods_MatchTheBufferExtensions()
    {
        Assert.Equal(BufferExtensionParity.ExtensionSignatures(typeof(WriteBufferExtensions)), BufferExtensionParity.InstanceSignatures(typeof(MessagePackWriter), "Write"));
    }

    // ArrayBufferWriter<T> is not in .NET Framework's System.Memory; this hands out small spans so WriteTo crosses them
    sealed class GrowingBufferWriter : IBufferWriter<byte>
    {
        byte[] buffer = new byte[16];
        int written;

        public void Advance(int count) => written += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return buffer.AsMemory(written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            Ensure(sizeHint);
            return buffer.AsSpan(written);
        }

        void Ensure(int sizeHint)
        {
            var needed = written + Math.Max(sizeHint, 1);
            if (needed > buffer.Length)
            {
                Array.Resize(ref buffer, Math.Max(needed, buffer.Length * 2));
            }
        }

        public byte[] ToArray() => buffer.AsSpan(0, written).ToArray();
    }
}

// the token methods of MessagePackWriter / MessagePackReader against the buffer extensions they delegate to
static class BufferExtensionParity
{
    public static string[] ExtensionSignatures(Type extensions)
    {
        return extensions.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Where(m => m.GetParameters().FirstOrDefault()?.ParameterType is { IsByRef: true } receiver && receiver.GetElementType()!.IsGenericParameter)
            .Select(m => Signature(m.Name, m.GetParameters().Skip(1)))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }

    public static string[] InstanceSignatures(Type type, params string[] prefixes)
    {
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(m => !m.IsSpecialName && prefixes.Any(p => m.Name.StartsWith(p, StringComparison.Ordinal)) && m.Name != nameof(MessagePackWriter.WriteTo))
            .Select(m => Signature(m.Name, m.GetParameters()))
            .OrderBy(s => s, StringComparer.Ordinal)
            .ToArray();
    }

    static string Signature(string name, IEnumerable<ParameterInfo> parameters)
    {
        return name + "(" + string.Join(", ", parameters.Select(p => p.ParameterType.Name)) + ")";
    }
}
