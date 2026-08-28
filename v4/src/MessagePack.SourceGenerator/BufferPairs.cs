namespace MessagePack.SourceGenerator;

/// <summary>
/// THE single source of truth for the serializer's built-in FALLBACK buffer-type pairs,
/// used to emit the Type-based CreateFormatter dispatch — the downlevel resolver's only
/// path. Update here when a first-party fallback buffer ships (the async pipe buffers
/// join when async entries land) — every generated factory picks the change up on
/// rebuild.
///
/// On `allows ref struct` TFMs the emitted member returns null ("not mine") WITHOUT the
/// dispatch: the generic member is implemented directly there, the resolver calls it,
/// and the bridge only serves types that do NOT implement the generic member — so the
/// dispatch would be dead code whose static `new Formatter&lt;ConcretePair&gt;()`
/// references force every formatter x every pair instantiation into Native AOT binaries
/// (value-type buffer args get no __Canon sharing). It must be null and not a throw:
/// the resolver's missing-formatter probe and composite passthroughs route Type-based
/// calls through modern factories, and a throw there would abort the whole chain.
/// </summary>
internal static class BufferPairs
{
    // (writeBuffer, readBuffer) fully-qualified names; the preprocessor split below is
    // evaluated per consuming TFM (generated trees share the compilation's parse options)
    static readonly (string Write, string Read)[] FallbackTier =
    [
        ("global::SerializerFoundation.CompatibleArrayPoolListWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySpanReadBuffer"),
        ("global::SerializerFoundation.CompatibleArrayPoolListWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySequenceReadBuffer"),
        ("global::SerializerFoundation.CompatibleBufferWriterWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySpanReadBuffer"),
        ("global::SerializerFoundation.CompatibleBufferWriterWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySequenceReadBuffer"),
    ];

    /// <summary>
    /// Appends the full non-generic CreateFormatter implementation: fallback-pair
    /// dispatch into the type's own generic CreateFormatter&lt;,&gt; on downlevel TFMs
    /// (unknown pairs return null — "not mine" chain semantics), unconditional null
    /// on modern TFMs (see the type comment).
    /// </summary>
    public static void AppendCreateFormatterDispatch(CodeWriter writer)
    {
        writer.Line("public override object? CreateFormatter(global::System.Type writeBufferType, global::System.Type readBufferType, global::System.Type valueType)");
        using (writer.OpenScope())
        {
            writer.Line("#if NET9_0_OR_GREATER");
            writer.Line("// the resolver takes the generic member on this TFM; only probes and composite");
            writer.Line("// passthroughs land here, and \"not mine\" must not abort their chain. the pair");
            writer.Line("// dispatch is omitted so AOT binaries do not carry formatter x pair instantiations");
            writer.Line("return null;");
            writer.Line("#else");
            AppendTier(writer, FallbackTier);
            writer.Line("return null;");
            writer.Line("#endif");
        }
    }

    static void AppendTier(CodeWriter writer, (string Write, string Read)[] tier)
    {
        string? currentWrite = null;
        CodeWriter.BlockScope? scope = null;
        foreach (var (write, read) in tier)
        {
            if (write != currentWrite)
            {
                scope?.Dispose();
                scope = writer.Block($"if (writeBufferType == typeof({write}))");
                currentWrite = write;
            }
            writer.Line($"if (readBufferType == typeof({read})) return CreateFormatter<{write}, {read}>(valueType);");
        }
        scope?.Dispose();
    }
}
