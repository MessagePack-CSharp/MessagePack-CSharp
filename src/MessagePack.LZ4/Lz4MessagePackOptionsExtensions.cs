namespace MessagePack;

/// <summary>
/// Adds LZ4 compression to <see cref="MessagePackSerializerOptions"/>. <see cref="WithLz4Frame(MessagePackSerializerOptions)"/>
/// is the format for new data (standard LZ4 frames, see <see cref="Lz4FrameProcessor"/>); the obsolete
/// <c>WithLz4Block</c> / <c>WithLz4BlockArray</c> write the MessagePack-CSharp v3 envelopes for data shared with v3
/// (see <see cref="Lz4BlockProcessor"/> and <see cref="Lz4BlockArrayProcessor"/>).
/// </summary>
public static class Lz4MessagePackOptionsExtensions
{
    extension(MessagePackSerializerOptions options)
    {
        /// <summary>Options writing every message as one standard LZ4 frame at the default decompression cap, see <see cref="Lz4FrameProcessor"/>.</summary>
        public MessagePackSerializerOptions WithLz4Frame()
            => options with { MessageProcessor = new Lz4FrameProcessor() };

        /// <summary>Options writing LZ4 frames with a custom decompression-bomb cap (see <see cref="Lz4FrameProcessor.MaxDecompressedSize"/>).</summary>
        public MessagePackSerializerOptions WithLz4Frame(long maxDecompressedSize)
            => options with { MessageProcessor = new Lz4FrameProcessor(maxDecompressedSize) };

        /// <summary>
        /// LZ4 frames compressed with a raw-content <paramref name="dictionary"/> (bytes typical of the messages) that
        /// the reader must hold too; the frames carry <paramref name="dictionaryId"/> so a mismatch is refused.
        /// See <see cref="Lz4FrameProcessor(long, ReadOnlyMemory{byte}, uint)"/>.
        /// </summary>
        public MessagePackSerializerOptions WithLz4Frame(ReadOnlyMemory<byte> dictionary, uint dictionaryId, long maxDecompressedSize = Lz4FrameProcessor.DefaultMaxDecompressedSize)
            => options with { MessageProcessor = new Lz4FrameProcessor(maxDecompressedSize, dictionary, dictionaryId) };

        /// <summary>Options writing the v3 ext 99 envelope (whole-message block); shares this instance's resolver.</summary>
        [Obsolete("v3 wire format (ext 99). Use WithLz4Frame for new data; Block stays for data shared with MessagePack-CSharp v3 readers and writers.")]
        public MessagePackSerializerOptions WithLz4Block()
            => options with { MessageProcessor = new Lz4BlockProcessor() };

        /// <summary>Options writing the v3 ext 99 envelope with a custom decompression-bomb cap (see <see cref="Lz4BlockProcessor.MaxDecompressedSize"/>).</summary>
        [Obsolete("v3 wire format (ext 99). Use WithLz4Frame for new data; Block stays for data shared with MessagePack-CSharp v3 readers and writers.")]
        public MessagePackSerializerOptions WithLz4Block(long maxDecompressedSize)
            => options with { MessageProcessor = new Lz4BlockProcessor(maxDecompressedSize) };

        /// <summary>Options writing the v3 ext 98 envelope (block per segment); shares this instance's resolver.</summary>
        [Obsolete("v3 wire format (ext 98). Use WithLz4Frame for new data; BlockArray stays for data shared with MessagePack-CSharp v3 readers and writers.")]
        public MessagePackSerializerOptions WithLz4BlockArray()
            => options with { MessageProcessor = new Lz4BlockArrayProcessor() };

        /// <summary>Options writing the v3 ext 98 envelope with a custom decompression-bomb cap (see <see cref="Lz4BlockArrayProcessor.MaxDecompressedSize"/>).</summary>
        [Obsolete("v3 wire format (ext 98). Use WithLz4Frame for new data; BlockArray stays for data shared with MessagePack-CSharp v3 readers and writers.")]
        public MessagePackSerializerOptions WithLz4BlockArray(long maxDecompressedSize)
            => options with { MessageProcessor = new Lz4BlockArrayProcessor(maxDecompressedSize) };
    }
}
