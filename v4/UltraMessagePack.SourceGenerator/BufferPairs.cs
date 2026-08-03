using System.Text;

namespace UltraMessagePack.SourceGenerator;

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
        ("global::SerializerFoundation.CompatibleArrayPoolListWriteBuffer", "global::SerializerFoundation.UnsafeReadOnlySpanReadBuffer"),
        ("global::SerializerFoundation.CompatibleArrayPoolListWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySequenceReadBuffer"),
        ("global::SerializerFoundation.CompatibleBufferWriterWriteBuffer", "global::SerializerFoundation.UnsafeReadOnlySpanReadBuffer"),
        ("global::SerializerFoundation.CompatibleBufferWriterWriteBuffer", "global::SerializerFoundation.CompatibleReadOnlySequenceReadBuffer"),
    ];

    /// <summary>
    /// Appends the full non-generic CreateFormatter implementation: fallback-pair
    /// dispatch into the type's own generic CreateFormatter&lt;,&gt; on downlevel TFMs
    /// (unknown pairs return null — "not mine" chain semantics), unconditional null
    /// on modern TFMs (see the type comment).
    /// </summary>
    public static void AppendCreateFormatterDispatch(StringBuilder builder, string indent)
    {
        builder.Append(indent).Append("public object? CreateFormatter(global::System.Type writeBufferType, global::System.Type readBufferType, global::System.Type valueType)\n");
        builder.Append(indent).Append("{\n");
        builder.Append("#if NET9_0_OR_GREATER\n");
        builder.Append(indent).Append("    // the resolver takes the generic member on this TFM; only probes and composite\n");
        builder.Append(indent).Append("    // passthroughs land here, and \"not mine\" must not abort their chain. the pair\n");
        builder.Append(indent).Append("    // dispatch is omitted so AOT binaries do not carry formatter x pair instantiations\n");
        builder.Append(indent).Append("    return null;\n");
        builder.Append("#else\n");
        AppendTier(builder, indent, FallbackTier);
        builder.Append(indent).Append("    return null;\n");
        builder.Append("#endif\n");
        builder.Append(indent).Append("}\n");
    }

    static void AppendTier(StringBuilder builder, string indent, (string Write, string Read)[] tier)
    {
        string? currentWrite = null;
        foreach (var (write, read) in tier)
        {
            if (write != currentWrite)
            {
                if (currentWrite != null)
                {
                    builder.Append(indent).Append("    }\n");
                }
                builder.Append(indent).Append($"    if (writeBufferType == typeof({write}))\n");
                builder.Append(indent).Append("    {\n");
                currentWrite = write;
            }
            builder.Append(indent).Append($"        if (readBufferType == typeof({read})) return CreateFormatter<{write}, {read}>(valueType);\n");
        }
        if (currentWrite != null)
        {
            builder.Append(indent).Append("    }\n");
        }
    }
}
