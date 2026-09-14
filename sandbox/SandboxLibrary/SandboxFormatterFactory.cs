using System;
using MessagePack;
using SerializerFoundation;

namespace SandboxLibrary;

// The two-tier factory shape: this generic method is the interface member on modern
// TFMs (and just a plain method downlevel, where the interface only knows the
// Type-based overload). The `partial` keyword is load-bearing — the FactoryBridgeGenerator
// emits the Type-based CreateFormatter(writeBufferType, readBufferType, valueType)
// dispatch in the other partial declaration, so this file only writes the generic one.
//
// The method-level `#if` on the constraints stays hand-written: C# has no way to attach
// constraints to a method from another partial declaration (unlike the formatter TYPE
// above, whose constraints come from BufferConstraintsGenerator).
public sealed partial class SandboxFormatterFactory : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
        => type == typeof(SandboxPoint) ? new SandboxPointFormatter<TWriteBuffer, TReadBuffer>() : null;
}
