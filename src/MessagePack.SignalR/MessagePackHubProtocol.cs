using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.Options;
using SerializerFoundation;
using static MessagePack.MessagePackPrimitives;

namespace MessagePack.SignalR;

/// <summary>
/// The SignalR "messagepack" hub protocol (version 2) on MessagePack v4. The envelope is the one
/// Microsoft.AspNetCore.SignalR.Protocols.MessagePack writes (arrays of message type, headers map, invocation id,
/// target, arguments and stream ids, each message length-prefixed with a 7-bit varint), so it interoperates with the
/// official server, .NET, JavaScript and Java clients; only the argument and result payloads come from the v4
/// serializer, through <see cref="MessagePackHubProtocolOptions.SerializerOptions"/>.
/// </summary>
public sealed class MessagePackHubProtocol : IHubProtocol
{
    const string ProtocolName = "messagepack";
    const int ProtocolVersion = 2;

    const int ErrorResult = 1;
    const int VoidResult = 2;
    const int NonVoidResult = 3;

    readonly MessagePackSerializerOptions serializerOptions;

    public string Name => ProtocolName;

    public int Version => ProtocolVersion;

    public TransferFormat TransferFormat => TransferFormat.Binary;

    [RequiresDynamicCode(MessagePackHubProtocolOptions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackHubProtocolOptions.RequiresUnreferencedCodeMessage)]
    public MessagePackHubProtocol()
        : this(Options.Create(new MessagePackHubProtocolOptions()))
    {
    }

    [RequiresDynamicCode(MessagePackHubProtocolOptions.RequiresDynamicCodeMessage)]
    [RequiresUnreferencedCode(MessagePackHubProtocolOptions.RequiresUnreferencedCodeMessage)]
    public MessagePackHubProtocol(IOptions<MessagePackHubProtocolOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        serializerOptions = options.Value.SerializerOptions;
        RejectMessageProcessor(serializerOptions, nameof(options));
    }

    /// <summary>A protocol over explicit serializer options, the constructor for trimmed and AOT applications.</summary>
    public MessagePackHubProtocol(MessagePackSerializerOptions serializerOptions)
    {
        ArgumentNullException.ThrowIfNull(serializerOptions);
        RejectMessageProcessor(serializerOptions, nameof(serializerOptions));
        this.serializerOptions = serializerOptions;
    }

    // arguments are written into and read out of the middle of the envelope through the serializer's buffer-level
    // entries, which have no complete message for a processor to encode or decode
    static void RejectMessageProcessor(MessagePackSerializerOptions serializerOptions, string paramName)
    {
        if (serializerOptions.MessageProcessor != null)
        {
            throw new ArgumentException("The hub protocol embeds each argument inside the SignalR envelope, so serializer options with a MessageProcessor (whole-message compression or framing) cannot apply. Pass options without a MessageProcessor.", paramName);
        }
    }

    public bool IsVersionSupported(int version) => version <= Version;

    // ---- read side ----

    public bool TryParseMessage(ref ReadOnlySequence<byte> input, IInvocationBinder binder, [NotNullWhen(true)] out HubMessage? message)
    {
        if (!BinaryFraming.TryParseMessage(ref input, out var payload))
        {
            message = null;
            return false;
        }
        // the core's own shape: a stack scratch for tokens that straddle segments, and Dispose to return the rented
        // temp the buffer falls back to when a straddling token outgrows the scratch
        Span<byte> scratch = stackalloc byte[ReadScratchSize];
        var buffer = new ReadOnlySequenceReadBuffer(in payload, scratch);
        try
        {
            message = ParseMessage(ref buffer, in payload, binder);
        }
        finally
        {
            buffer.Dispose();
        }
        return message != null;
    }

    const int ReadScratchSize = 64;

    HubMessage? ParseMessage(ref ReadOnlySequenceReadBuffer buffer, in ReadOnlySequence<byte> payload, IInvocationBinder binder)
    {
        var itemCount = ReadArrayLength(ref buffer, "elements");
        var messageType = ReadInt32(ref buffer, "messageType");
        switch (messageType)
        {
            case HubProtocolConstants.InvocationMessageType:
                return CreateInvocationMessage(ref buffer, binder, itemCount);
            case HubProtocolConstants.StreamInvocationMessageType:
                return CreateStreamInvocationMessage(ref buffer, binder, itemCount);
            case HubProtocolConstants.StreamItemMessageType:
                return CreateStreamItemMessage(ref buffer, binder);
            case HubProtocolConstants.CompletionMessageType:
                return CreateCompletionMessage(ref buffer, in payload, binder);
            case HubProtocolConstants.CancelInvocationMessageType:
                return CreateCancelInvocationMessage(ref buffer);
            case HubProtocolConstants.PingMessageType:
                return PingMessage.Instance;
            case HubProtocolConstants.CloseMessageType:
                return CreateCloseMessage(ref buffer, itemCount);
            case HubProtocolConstants.AckMessageType:
                return new AckMessage(ReadInt64(ref buffer, "sequenceId"));
            case HubProtocolConstants.SequenceMessageType:
                return new SequenceMessage(ReadInt64(ref buffer, "sequenceId"));
            default:
                return null; // unknown message types are skipped, as the official protocol does
        }
    }

    HubMessage CreateInvocationMessage(ref ReadOnlySequenceReadBuffer buffer, IInvocationBinder binder, int itemCount)
    {
        var headers = ReadHeaders(ref buffer);
        var invocationId = ReadString(ref buffer, "invocationId");
        if (string.IsNullOrEmpty(invocationId))
        {
            invocationId = null;
        }
        var target = ReadString(ref buffer, "target");
        ThrowIfNullOrEmpty(target, "target for Invocation message");

        object?[]? arguments;
        try
        {
            var parameterTypes = binder.GetParameterTypes(target);
            arguments = BindArguments(ref buffer, parameterTypes);
        }
        catch (Exception exception)
        {
            return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(exception));
        }

        string[]? streams = null;
        if (itemCount > 5)
        {
            streams = ReadStreamIds(ref buffer);
        }
        return ApplyHeaders(headers, new InvocationMessage(invocationId, target, arguments, streams));
    }

    HubMessage CreateStreamInvocationMessage(ref ReadOnlySequenceReadBuffer buffer, IInvocationBinder binder, int itemCount)
    {
        var headers = ReadHeaders(ref buffer);
        var invocationId = ReadString(ref buffer, "invocationId");
        ThrowIfNullOrEmpty(invocationId, "invocation ID for StreamInvocation message");
        var target = ReadString(ref buffer, "target");
        ThrowIfNullOrEmpty(target, "target for StreamInvocation message");

        object?[] arguments;
        try
        {
            var parameterTypes = binder.GetParameterTypes(target);
            arguments = BindArguments(ref buffer, parameterTypes);
        }
        catch (Exception exception)
        {
            return new InvocationBindingFailureMessage(invocationId, target, ExceptionDispatchInfo.Capture(exception));
        }

        string[]? streams = null;
        if (itemCount > 5)
        {
            streams = ReadStreamIds(ref buffer);
        }
        return ApplyHeaders(headers, new StreamInvocationMessage(invocationId, target, arguments, streams));
    }

    HubMessage CreateStreamItemMessage(ref ReadOnlySequenceReadBuffer buffer, IInvocationBinder binder)
    {
        var headers = ReadHeaders(ref buffer);
        var invocationId = ReadString(ref buffer, "invocationId");
        ThrowIfNullOrEmpty(invocationId, "invocation ID for StreamItem message");

        object? value;
        try
        {
            var itemType = binder.GetStreamItemType(invocationId);
            value = DeserializeObject(ref buffer, itemType, "item");
        }
        catch (Exception exception)
        {
            return new StreamBindingFailureMessage(invocationId, ExceptionDispatchInfo.Capture(exception));
        }
        return ApplyHeaders(headers, new StreamItemMessage(invocationId, value));
    }

    CompletionMessage CreateCompletionMessage(ref ReadOnlySequenceReadBuffer buffer, in ReadOnlySequence<byte> payload, IInvocationBinder binder)
    {
        var headers = ReadHeaders(ref buffer);
        var invocationId = ReadString(ref buffer, "invocationId");
        ThrowIfNullOrEmpty(invocationId, "invocation ID for Completion message");
        var resultKind = ReadInt32(ref buffer, "resultKind");

        string? error = null;
        object? result = null;
        var hasResult = false;
        switch (resultKind)
        {
            case ErrorResult:
                error = ReadString(ref buffer, "error");
                break;
            case NonVoidResult:
                hasResult = true;
                var itemType = TryGetReturnType(binder, invocationId);
                if (itemType is null)
                {
                    buffer.Skip();
                }
                else if (itemType == typeof(RawResult))
                {
                    result = new RawResult(SkipValue(ref buffer, in payload));
                }
                else
                {
                    try
                    {
                        result = DeserializeObject(ref buffer, itemType, "argument");
                    }
                    catch (Exception exception)
                    {
                        error = $"Error trying to deserialize result to {itemType.Name}. {exception.Message}";
                        hasResult = false;
                    }
                }
                break;
            case VoidResult:
                break;
            default:
                throw new InvalidDataException("Invalid invocation result kind.");
        }
        return ApplyHeaders(headers, new CompletionMessage(invocationId, error, result, hasResult));
    }

    static CancelInvocationMessage CreateCancelInvocationMessage(ref ReadOnlySequenceReadBuffer buffer)
    {
        var headers = ReadHeaders(ref buffer);
        var invocationId = ReadString(ref buffer, "invocationId");
        ThrowIfNullOrEmpty(invocationId, "invocation ID for CancelInvocation message");
        return ApplyHeaders(headers, new CancelInvocationMessage(invocationId));
    }

    static CloseMessage CreateCloseMessage(ref ReadOnlySequenceReadBuffer buffer, int itemCount)
    {
        var error = ReadString(ref buffer, "error");
        var allowReconnect = false;
        if (itemCount > 2)
        {
            allowReconnect = ReadBoolean(ref buffer, "allowReconnect");
        }
        return error == null && !allowReconnect ? CloseMessage.Empty : new CloseMessage(error, allowReconnect);
    }

    static Dictionary<string, string>? ReadHeaders(ref ReadOnlySequenceReadBuffer buffer)
    {
        var headerCount = ReadMapLength(ref buffer, "headers");
        if (headerCount <= 0)
        {
            return null;
        }
        var headers = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var i = 0; i < headerCount; i++)
        {
            var key = ReadString(ref buffer, $"headers[{i}].Key");
            ThrowIfNullOrEmpty(key, "key in header");
            var value = ReadString(ref buffer, $"headers[{i}].Value");
            ThrowIfNullOrEmpty(value, "value in header");
            headers.Add(key, value);
        }
        return headers;
    }

    static string[]? ReadStreamIds(ref ReadOnlySequenceReadBuffer buffer)
    {
        var count = ReadArrayLength(ref buffer, "streamIds");
        if (count <= 0)
        {
            return null;
        }
        var streams = new string[count];
        for (var i = 0; i < count; i++)
        {
            var id = ReadString(ref buffer, "streamIds");
            ThrowIfNullOrEmpty(id, "value in streamIds received");
            streams[i] = id;
        }
        return streams;
    }

    object?[] BindArguments(ref ReadOnlySequenceReadBuffer buffer, IReadOnlyList<Type> parameterTypes)
    {
        var argumentCount = ReadArrayLength(ref buffer, "arguments");
        if (parameterTypes.Count != argumentCount)
        {
            throw new InvalidDataException($"Invocation provides {argumentCount} argument(s) but target expects {parameterTypes.Count}.");
        }
        try
        {
            var arguments = new object?[argumentCount];
            for (var i = 0; i < argumentCount; i++)
            {
                arguments[i] = DeserializeObject(ref buffer, parameterTypes[i], "argument");
            }
            return arguments;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException("Error binding arguments. Make sure that the types of the provided values match the types of the hub method being invoked.", exception);
        }
    }

    // One value out of the middle of the payload, read by the serializer's buffer-level Type entry, which consumes
    // exactly that value and leaves the rest of the envelope in place.
    // The Type-based entries are AOT-safe for the types generated code registers (generated types, harvested closures,
    // [MessagePackSerializable] roots) and the built-in scalars; any other hub argument type throws NotSupportedException
    // on Native AOT. The reflection tier is only reached through the default options, annotated where they are chosen.
    object? DeserializeObject(ref ReadOnlySequenceReadBuffer buffer, Type type, string field)
    {
        try
        {
            return MessagePackSerializer.Deserialize(type, ref buffer, serializerOptions);
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Deserializing object of the `{type.Name}` type for '{field}' failed.", exception);
        }
    }

    static ReadOnlySequence<byte> SkipValue(ref ReadOnlySequenceReadBuffer buffer, in ReadOnlySequence<byte> payload)
    {
        var start = buffer.BytesConsumed;
        buffer.Skip();
        return payload.Slice(start, buffer.BytesConsumed - start);
    }

    static Type? TryGetReturnType(IInvocationBinder binder, string invocationId)
    {
        try
        {
            return binder.GetReturnType(invocationId);
        }
        catch
        {
            return null; // an unknown invocation id: the result is skipped and the message still surfaces
        }
    }

    static T ApplyHeaders<T>(IDictionary<string, string>? source, T destination) where T : HubInvocationMessage
    {
        if (source != null && source.Count > 0)
        {
            destination.Headers = source;
        }
        return destination;
    }

    // ---- write side ----

    public void WriteMessage(HubMessage message, IBufferWriter<byte> output)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(output);
        var staging = RentStaging();
        try
        {
            WriteMessageCore(message, staging);
            BinaryFraming.WriteLengthPrefix(staging.WrittenCount, output);
            output.Write(staging.WrittenSpan);
        }
        finally
        {
            ReturnStaging(staging);
        }
    }

    public ReadOnlyMemory<byte> GetMessageBytes(HubMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        var staging = RentStaging();
        try
        {
            WriteMessageCore(message, staging);
            var dataLength = staging.WrittenCount;
            var prefixLength = BinaryFraming.LengthPrefixLength(dataLength);
            var array = new byte[prefixLength + dataLength];
            BinaryFraming.WriteLengthPrefix(dataLength, array);
            staging.WrittenSpan.CopyTo(array.AsSpan(prefixLength));
            return array;
        }
        finally
        {
            ReturnStaging(staging);
        }
    }

    // the message is composed in full before the length prefix can be written; one staging writer per thread
    [ThreadStatic]
    static ArrayBufferWriter<byte>? stagingCache;

    static ArrayBufferWriter<byte> RentStaging()
    {
        var staging = stagingCache ?? new ArrayBufferWriter<byte>(1024);
        stagingCache = null;
        return staging;
    }

    static void ReturnStaging(ArrayBufferWriter<byte> staging)
    {
        // a message that grew the buffer past 64KB is returned to the GC instead of pinning the memory per thread
        if (staging.Capacity <= 64 * 1024)
        {
            staging.ResetWrittenCount();
            stagingCache = staging;
        }
    }

    // The whole message, envelope tokens and arguments alike, goes through one write buffer over the staging writer:
    // the tokens through the library's writers, the arguments through the serializer's buffer-level Type entry.
    void WriteMessageCore(HubMessage message, IBufferWriter<byte> writer)
    {
        var buffer = new BufferWriterWriteBuffer(writer);
        try
        {
            switch (message)
            {
                case InvocationMessage invocation:
                    WriteInvocationMessage(invocation, ref buffer);
                    break;
                case StreamInvocationMessage streamInvocation:
                    WriteStreamInvocationMessage(streamInvocation, ref buffer);
                    break;
                case StreamItemMessage streamItem:
                    WriteStreamItemMessage(streamItem, ref buffer);
                    break;
                case CompletionMessage completion:
                    WriteCompletionMessage(completion, ref buffer);
                    break;
                case CancelInvocationMessage cancelInvocation:
                    buffer.WriteArrayHeader(3);
                    buffer.WriteInt32(HubProtocolConstants.CancelInvocationMessageType);
                    WriteHeaders(cancelInvocation.Headers, ref buffer);
                    buffer.WriteString(cancelInvocation.InvocationId);
                    break;
                case PingMessage:
                    buffer.WriteArrayHeader(1);
                    buffer.WriteInt32(HubProtocolConstants.PingMessageType);
                    break;
                case CloseMessage close:
                    buffer.WriteArrayHeader(3);
                    buffer.WriteInt32(HubProtocolConstants.CloseMessageType);
                    buffer.WriteString(string.IsNullOrEmpty(close.Error) ? null : close.Error);
                    buffer.WriteBoolean(close.AllowReconnect);
                    break;
                case AckMessage ack:
                    buffer.WriteArrayHeader(2);
                    buffer.WriteInt32(HubProtocolConstants.AckMessageType);
                    buffer.WriteInt64(ack.SequenceId);
                    break;
                case SequenceMessage sequence:
                    buffer.WriteArrayHeader(2);
                    buffer.WriteInt32(HubProtocolConstants.SequenceMessageType);
                    buffer.WriteInt64(sequence.SequenceId);
                    break;
                default:
                    throw new InvalidDataException($"Unexpected message type: {message.GetType().Name}");
            }
        }
        finally
        {
            buffer.Dispose();
        }
    }

    void WriteInvocationMessage(InvocationMessage message, ref BufferWriterWriteBuffer buffer)
    {
        buffer.WriteArrayHeader(6);
        buffer.WriteInt32(HubProtocolConstants.InvocationMessageType);
        WriteHeaders(message.Headers, ref buffer);
        buffer.WriteString(string.IsNullOrEmpty(message.InvocationId) ? null : message.InvocationId);
        buffer.WriteString(message.Target);
        WriteArguments(message.Arguments, ref buffer);
        WriteStreamIds(message.StreamIds, ref buffer);
    }

    void WriteStreamInvocationMessage(StreamInvocationMessage message, ref BufferWriterWriteBuffer buffer)
    {
        buffer.WriteArrayHeader(6);
        buffer.WriteInt32(HubProtocolConstants.StreamInvocationMessageType);
        WriteHeaders(message.Headers, ref buffer);
        buffer.WriteString(message.InvocationId);
        buffer.WriteString(message.Target);
        WriteArguments(message.Arguments, ref buffer);
        WriteStreamIds(message.StreamIds, ref buffer);
    }

    void WriteStreamItemMessage(StreamItemMessage message, ref BufferWriterWriteBuffer buffer)
    {
        buffer.WriteArrayHeader(4);
        buffer.WriteInt32(HubProtocolConstants.StreamItemMessageType);
        WriteHeaders(message.Headers, ref buffer);
        buffer.WriteString(message.InvocationId);
        WriteArgument(message.Item, ref buffer);
    }

    void WriteCompletionMessage(CompletionMessage message, ref BufferWriterWriteBuffer buffer)
    {
        var resultKind =
            message.Error != null ? ErrorResult :
            message.HasResult ? NonVoidResult :
            VoidResult;
        buffer.WriteArrayHeader(4 + (resultKind != VoidResult ? 1 : 0));
        buffer.WriteInt32(HubProtocolConstants.CompletionMessageType);
        WriteHeaders(message.Headers, ref buffer);
        buffer.WriteString(message.InvocationId);
        buffer.WriteInt32(resultKind);
        switch (resultKind)
        {
            case ErrorResult:
                buffer.WriteString(message.Error);
                break;
            case NonVoidResult:
                WriteArgument(message.Result, ref buffer);
                break;
        }
    }

    void WriteArguments(object?[]? arguments, ref BufferWriterWriteBuffer buffer)
    {
        if (arguments is null)
        {
            buffer.WriteArrayHeader(0);
            return;
        }
        buffer.WriteArrayHeader(arguments.Length);
        foreach (var argument in arguments)
        {
            WriteArgument(argument, ref buffer);
        }
    }

    void WriteArgument(object? argument, ref BufferWriterWriteBuffer buffer)
    {
        if (argument == null)
        {
            buffer.WriteNil();
        }
        else if (argument is RawResult raw)
        {
            buffer.WriteRaw(raw.RawSerializedData);
        }
        else
        {
            MessagePackSerializer.Serialize(argument.GetType(), ref buffer, argument, serializerOptions);
        }
    }

    static void WriteStreamIds(string[]? streamIds, ref BufferWriterWriteBuffer buffer)
    {
        if (streamIds == null)
        {
            buffer.WriteArrayHeader(0);
            return;
        }
        buffer.WriteArrayHeader(streamIds.Length);
        foreach (var id in streamIds)
        {
            buffer.WriteString(id);
        }
    }

    static void WriteHeaders(IDictionary<string, string>? headers, ref BufferWriterWriteBuffer buffer)
    {
        if (headers == null)
        {
            buffer.WriteMapHeader(0);
            return;
        }
        buffer.WriteMapHeader(headers.Count);
        foreach (var header in headers)
        {
            buffer.WriteString(header.Key);
            buffer.WriteString(header.Value);
        }
    }

    // ---- envelope readers. InvalidDataException per field with the official worker's message strings: not an IHubProtocol
    // contract (the framework catches any exception from TryParseMessage and drops the connection), but the shape the
    // official test suite and its consumers know ----

    static string? ReadString(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.TryReadNil() ? null : buffer.ReadString();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading '{field}' as String failed.", exception);
        }
    }

    static int ReadInt32(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.ReadInt32();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading '{field}' as Int32 failed.", exception);
        }
    }

    static long ReadInt64(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.ReadInt64();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading '{field}' as Int64 failed.", exception);
        }
    }

    static bool ReadBoolean(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.ReadBoolean();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading '{field}' as Boolean failed.", exception);
        }
    }

    static int ReadMapLength(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.ReadMapHeader();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading map length for '{field}' failed.", exception);
        }
    }

    static int ReadArrayLength(ref ReadOnlySequenceReadBuffer buffer, string field)
    {
        try
        {
            return buffer.ReadArrayHeader();
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Reading array length for '{field}' failed.", exception);
        }
    }

    static void ThrowIfNullOrEmpty([NotNull] string? value, string what)
    {
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidDataException($"Null or empty {what}.");
        }
    }


    // The SignalR binary framing: each message is preceded by its length as a 7-bit varint (the official
    // BinaryMessageFormatter/BinaryMessageParser, which are internal to the framework).
    static class BinaryFraming
    {
        const int MaxLengthPrefixSize = 5;

        public static bool TryParseMessage(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> payload)
        {
            if (buffer.IsEmpty)
            {
                payload = default;
                return false;
            }
            var prefix = buffer.Slice(0, Math.Min(MaxLengthPrefixSize, buffer.Length));
            Span<byte> prefixBytes = stackalloc byte[MaxLengthPrefixSize];
            prefix.CopyTo(prefixBytes);
            var prefixLength = (int)prefix.Length;

            var length = 0U;
            var numBytes = 0;
            byte byteRead;
            do
            {
                byteRead = prefixBytes[numBytes];
                length |= (uint)(byteRead & 0x7f) << (numBytes * 7);
                numBytes++;
            }
            while (numBytes < prefixLength && (byteRead & 0x80) != 0);

            if ((byteRead & 0x80) != 0 && numBytes < MaxLengthPrefixSize)
            {
                payload = default;
                return false; // the prefix itself is not complete yet
            }
            if ((byteRead & 0x80) != 0 || (numBytes == MaxLengthPrefixSize && byteRead > 7))
            {
                throw new FormatException("Messages over 2GB in size are not supported.");
            }
            if (buffer.Length < length + numBytes)
            {
                payload = default;
                return false;
            }
            payload = buffer.Slice(numBytes, (int)length);
            buffer = buffer.Slice(numBytes + (int)length);
            return true;
        }

        public static void WriteLengthPrefix(long length, IBufferWriter<byte> output)
        {
            var span = output.GetSpan(MaxLengthPrefixSize);
            output.Advance(WriteLengthPrefix(length, span));
        }

        public static int WriteLengthPrefix(long length, Span<byte> output)
        {
            var numBytes = 0;
            do
            {
                ref var current = ref output[numBytes];
                current = (byte)(length & 0x7f);
                length >>= 7;
                if (length > 0)
                {
                    current |= 0x80;
                }
                numBytes++;
            }
            while (length > 0);
            return numBytes;
        }

        public static int LengthPrefixLength(long length)
        {
            var numBytes = 0;
            do
            {
                length >>= 7;
                numBytes++;
            }
            while (length > 0);
            return numBytes;
        }
    }
}
