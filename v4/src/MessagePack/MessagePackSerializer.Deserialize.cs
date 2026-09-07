using System.Diagnostics.CodeAnalysis;

namespace MessagePack;

public static partial class MessagePackSerializer
{
    const int DeserializeScratchSize = 64;

	/// <summary>Deserializes a value from the MessagePack bytes in <paramref name="source"/>.</summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static T Deserialize<T>(ReadOnlySpan<byte> source)
	{
		return Deserialize<T>(source, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(ReadOnlySpan{byte})"/>
	public static T Deserialize<T>(ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
	{
		T result = default!;
		Deserialize(source, ref result, options);
		return result;
	}

	/// <summary>
	/// Deserializes from <paramref name="source"/> into an existing instance.
	/// A non-null <paramref name="value"/> is reused, so pooled objects avoid the result allocation.
	/// </summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static void Deserialize<T>(ReadOnlySpan<byte> source, ref T value)
	{
		Deserialize(source, ref value, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(ReadOnlySpan{byte}, ref T)"/>
	public static void Deserialize<T>(ReadOnlySpan<byte> source, ref T value, MessagePackSerializerOptions options)
	{
		var processor = options.MessageProcessor;
		if (processor != null && processor.TryDecode(source, out var decoded))
		{
			try
			{
				DeserializeDecoded(ref value, in decoded, options);
			}
			finally
			{
				decoded.Dispose();
			}
			return;
		}

		DeserializeSpanCore(ref value, source, options);
	}

#if NET9_0_OR_GREATER

	/// <summary>
	/// Deserializes one value straight from a read buffer and leaves the following bytes unconsumed, for reading MessagePack embedded inside another protocol.
	/// Options with a MessageProcessor are rejected, since the processor needs the complete message and this entry never sees it.
	/// </summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static T Deserialize<TReadBuffer, T>(ref TReadBuffer buffer)
		where TReadBuffer : struct, IReadBuffer, allows ref struct
	{
		return Deserialize<TReadBuffer, T>(ref buffer, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{TReadBuffer, T}(ref TReadBuffer)"/>
	public static T Deserialize<TReadBuffer, T>(ref TReadBuffer buffer, MessagePackSerializerOptions options)
		where TReadBuffer : struct, IReadBuffer, allows ref struct
	{
		T value = default!;
		Deserialize(ref buffer, ref value, options);
		return value;
	}

	/// <inheritdoc cref="Deserialize{TReadBuffer, T}(ref TReadBuffer)"/>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static void Deserialize<TReadBuffer, T>(ref TReadBuffer buffer, ref T value)
		where TReadBuffer : struct, IReadBuffer, allows ref struct
	{
		Deserialize(ref buffer, ref value, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{TReadBuffer, T}(ref TReadBuffer)"/>
	public static void Deserialize<TReadBuffer, T>(ref TReadBuffer buffer, ref T value, MessagePackSerializerOptions options)
		where TReadBuffer : struct, IReadBuffer, allows ref struct
	{
		if (options.MessageProcessor != null)
		{
			ThrowMessageProcessorNotApplicable();
		}
		var state = new DeserializeState(options.MaxDepth);
		options.Resolver.GetFormatter<ArrayPoolListWriteBuffer, TReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
	}

#endif

	/// <summary>Deserializes a value from the MessagePack bytes in <paramref name="source"/>.</summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static T Deserialize<T>(in ReadOnlySequence<byte> source)
	{
		return Deserialize<T>(source, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte})"/>
	public static T Deserialize<T>(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
	{
		T result = default!;
		Deserialize(source, ref result, options);
		return result;
	}

	/// <summary>
	/// Deserializes from <paramref name="source"/> into an existing instance.
	/// A non-null <paramref name="value"/> is reused, so pooled objects avoid the result allocation.
	/// </summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(MessagePackFormatterFactory.RequiresUnreferencedCodeMessage)]
	public static void Deserialize<T>(in ReadOnlySequence<byte> source, ref T value)
	{
		Deserialize(source, ref value, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(in ReadOnlySequence{byte}, ref T)"/>
	public static void Deserialize<T>(in ReadOnlySequence<byte> source, ref T value, MessagePackSerializerOptions options)
	{
		var processor = options.MessageProcessor;
		if (processor != null && processor.TryDecode(in source, out var decoded))
		{
			try
			{
				DeserializeDecoded(ref value, in decoded, options);
			}
			finally
			{
				decoded.Dispose();
			}
			return;
		}

		if (source.IsSingleSegment)
		{
			DeserializeSpanCore(ref value, source.FirstSpan, options);
		}
		else
		{
			DeserializeSequenceCore(ref value, source, options);
		}
	}
	
	// returns the bytes consumed by the value; the sync entry points ignore it, the async completed-reader fast path advances the PipeReader by it
	static unsafe long DeserializeSpanCore<T>(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
	{
#if NET9_0_OR_GREATER
		if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySpanReadBuffer, T>(out var formatter))
		{
			return DeserializeSpanCompatible(ref value, source, options);
		}

		var buffer = new ReadOnlySpanReadBuffer(source);
		try
		{
			var state = new DeserializeState(options.MaxDepth);
			formatter.Deserialize(ref buffer, ref state, ref value);
			return buffer.BytesConsumed;
		}
		finally
		{
			buffer.Dispose();
		}

		static long DeserializeSpanCompatible(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
#endif
		{
			// The Compatible read buffer reads through a fixed view of the caller's span, so the whole deserialization
			// runs inside the fixed scope (an empty source pins to null, which PointerSpan represents as an empty window).
			fixed (byte* pointer = source)
			{
				var buffer = new CompatibleReadOnlySpanReadBuffer(pointer, source.Length);
				try
				{
					var state = new DeserializeState(options.MaxDepth);
					options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySpanReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
					return buffer.BytesConsumed;
				}
				finally
				{
					buffer.Dispose();
				}
			}
		}
	}

	/// <inheritdoc cref="DeserializeSpanCore{T}(ref T, ReadOnlySpan{byte}, MessagePackSerializerOptions)"/>
	[SkipLocalsInit]
	static long DeserializeSequenceCore<T>(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
	{
#if NET9_0_OR_GREATER
		if (!options.Resolver.TryGetFormatter<ArrayPoolListWriteBuffer, ReadOnlySequenceReadBuffer, T>(out var formatter))
		{
			return DeserializeSequenceCompatible(ref value, in source, options);
		}

		Span<byte> scratch = stackalloc byte[DeserializeScratchSize];
		var buffer = new ReadOnlySequenceReadBuffer(source, scratch);
		try
		{
			var state = new DeserializeState(options.MaxDepth);
			formatter.Deserialize(ref buffer, ref state, ref value);
			return buffer.BytesConsumed;
		}
		finally
		{
			buffer.Dispose();
		}

		static long DeserializeSequenceCompatible(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
#endif
		{
			// the Compatible tier windows each segment as ReadOnlyMemory (pin-free) and stitches through its rented temp,
			// with no caller scratch involved
			var buffer = new CompatibleReadOnlySequenceReadBuffer(in source);
			try
			{
				var state = new DeserializeState(options.MaxDepth);
				options.Resolver.GetFormatter<CompatibleArrayPoolListWriteBuffer, CompatibleReadOnlySequenceReadBuffer, T>().Deserialize(ref buffer, ref state, ref value);
				return buffer.BytesConsumed;
			}
			finally
			{
				buffer.Dispose();
			}
		}
	}

	static void DeserializeDecoded<T>(ref T value, in DecodedMessage decoded, MessagePackSerializerOptions options)
	{
		var sequence = decoded.Sequence;
		if (sequence.IsSingleSegment)
		{
			DeserializeSpanCore(ref value, sequence.FirstSpan, options);
		}
		else
		{
			DeserializeSequenceCore(ref value, sequence, options);
		}
	}
}
