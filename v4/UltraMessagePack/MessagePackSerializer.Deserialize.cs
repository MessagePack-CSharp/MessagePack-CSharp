using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace UltraMessagePack;

public static partial class MessagePackSerializer
{
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	public static T Deserialize<T>(ReadOnlySpan<byte> source)
	{
		return Deserialize<T>(source, MessagePackSerializerOptions.Default);
	}

	public static T Deserialize<T>(ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
	{
		T result = default!;
		Deserialize(ref result, source, options);
		return result;
	}

	/// <summary>
	/// Populate overload, deserializes into an existing instance (formatters treat a non-null ref as reuse).
	/// </summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source)
	{
		Deserialize(ref value, source, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(ref T, ReadOnlySpan{byte})"/>
	public static void Deserialize<T>(ref T value, ReadOnlySpan<byte> source, MessagePackSerializerOptions options)
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
			// the Compatible read buffer reads through a fixed view of the caller's span;
			// the whole deserialization runs inside the fixed scope (empty source pins to
			// null, which PointerSpan represents as an empty window)
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

	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	public static T Deserialize<T>(in ReadOnlySequence<byte> source)
	{
		return Deserialize<T>(source, MessagePackSerializerOptions.Default);
	}

	public static T Deserialize<T>(in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
	{
		T result = default!;
		Deserialize(ref result, source, options);
		return result;
	}

	/// <summary>
	/// Populate overload: deserializes into an existing instance (formatters treat a
	/// non-null ref as reuse), eliminating the result allocation for pooled objects.
	/// </summary>
	[RequiresDynamicCode(MessagePackFormatterFactory.RequiresDynamicCodeMessage)]
	public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source)
	{
		Deserialize(ref value, source, MessagePackSerializerOptions.Default);
	}

	/// <inheritdoc cref="Deserialize{T}(ref T, in ReadOnlySequence{byte})"/>
	public static void Deserialize<T>(ref T value, in ReadOnlySequence<byte> source, MessagePackSerializerOptions options)
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
			// the Compatible tier windows each segment as ReadOnlyMemory (pin-free) and
			// stitches through its rented temp; no caller scratch involved
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
