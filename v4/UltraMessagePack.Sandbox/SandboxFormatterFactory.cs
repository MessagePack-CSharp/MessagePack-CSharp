using System;
using SerializerFoundation;

namespace UltraMessagePack.Sandbox;

// The two-tier factory shape: this generic method is the interface member on modern
// TFMs (and just a plain method downlevel, where the interface only knows the
// Type-based overload). The `partial` keyword is load-bearing — the FactoryBridgeGenerator
// emits the Type-based CreateFormatter(writeBufferType, readBufferType, valueType)
// dispatch in the other partial declaration, so this file only writes the generic one.
//
// The method-level `#if` on the constraints stays hand-written: C# has no way to attach
// constraints to a method from another partial declaration (unlike the formatter TYPE
// above, whose constraints come from BufferConstraintsGenerator). UMP101 still flags
// this symbol on downlevel-only TFM shapes — method-level detection.
public sealed partial class SandboxFormatterFactory : IMessagePackFormatterFactory
{
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        where TReadBuffer : struct, IReadBuffer
#if NET9_0_OR_GREATER
        , allows ref struct
#endif
        => type == typeof(SandboxPoint) ? new SandboxPointFormatter<TWriteBuffer, TReadBuffer>() : null;
}
