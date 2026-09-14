using System.Buffers;
using MessagePack;
using MessagePack.SignalR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace MessagePack.Tests.AspNetCore;

// The protocol pinned to the official implementation's byte fixtures (dotnet/aspnetcore
// MessagePackHubProtocolTestBase.BaseTestData, base64 of the unframed message). Every fixture is written and
// compared byte for byte, then parsed back and compared field by field, so a v4 endpoint stays interoperable with
// Microsoft.AspNetCore.SignalR.Protocols.MessagePack and the JavaScript/Java clients on the wire.
public class HubProtocolFixtureTests
{
    static readonly Dictionary<string, string> TestHeaders = new(StringComparer.Ordinal)
    {
        { "Foo", "Bar" },
        { "KeyWith\nNew\r\nLines", "Still Works" },
        { "ValueWithNewLines", "Also\nWorks\r\nFine" },
    };

    static T AddHeaders<T>(T message) where T : HubInvocationMessage
    {
        message.Headers = TestHeaders;
        return message;
    }

    public static IEnumerable<TheoryDataRow<string, HubMessage, string>> Fixtures()
    {
        yield return new("InvocationWithNoHeadersAndNoArgs", new InvocationMessage("xyz", "method", []), "lgGAo3h5eqZtZXRob2SQkA==");
        yield return new("InvocationWithNoHeadersNoIdAndNoArgs", new InvocationMessage("method", []), "lgGAwKZtZXRob2SQkA==");
        yield return new("InvocationWithNoHeadersNoIdAndSingleIntArg", new InvocationMessage("method", [42]), "lgGAwKZtZXRob2SRKpA=");
        yield return new("InvocationWithNoHeadersNoIdIntAndStringArgs", new InvocationMessage("method", [42, "string"]), "lgGAwKZtZXRob2SSKqZzdHJpbmeQ");
        yield return new("InvocationWithStreamId", new InvocationMessage(null, "Target", [], ["__test_id__"]), "lgGAwKZUYXJnZXSQkatfX3Rlc3RfaWRfXw==");
        yield return new("InvocationWithStreamIdAndArg", new InvocationMessage(null, "Target", [42], ["__test_id__"]), "lgGAwKZUYXJnZXSRKpGrX190ZXN0X2lkX18=");
        yield return new("InvocationWithTwoStreamIds", new InvocationMessage(null, "Target", [], ["__test_id__", "__test_id2__"]), "lgGAwKZUYXJnZXSQkqtfX3Rlc3RfaWRfX6xfX3Rlc3RfaWQyX18=");
        yield return new("StreamItemWithNoHeadersAndIntItem", new StreamItemMessage("xyz", 42), "lAKAo3h5eio=");
        yield return new("StreamItemWithNoHeadersAndFloatItem", new StreamItemMessage("xyz", 42.0f), "lAKAo3h5espCKAAA");
        yield return new("StreamItemWithNoHeadersAndStringItem", new StreamItemMessage("xyz", "string"), "lAKAo3h5eqZzdHJpbmc=");
        yield return new("StreamItemWithNoHeadersAndBoolItem", new StreamItemMessage("xyz", true), "lAKAo3h5esM=");
        yield return new("CompletionWithNoHeadersAndError", CompletionMessage.WithError("xyz", "Error not found!"), "lQOAo3h5egGwRXJyb3Igbm90IGZvdW5kIQ==");
        yield return new("CompletionWithHeadersAndError", AddHeaders(CompletionMessage.WithError("xyz", "Error not found!")), "lQODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6AbBFcnJvciBub3QgZm91bmQh");
        yield return new("CompletionWithNoHeadersAndNoResult", CompletionMessage.Empty("xyz"), "lAOAo3h5egI=");
        yield return new("CompletionWithHeadersAndNoResult", AddHeaders(CompletionMessage.Empty("xyz")), "lAODo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6Ag==");
        yield return new("CompletionWithNoHeadersAndIntResult", CompletionMessage.WithResult("xyz", 42), "lQOAo3h5egMq");
        yield return new("CompletionWithNoHeadersAndFloatResult", CompletionMessage.WithResult("xyz", 42.0f), "lQOAo3h5egPKQigAAA==");
        yield return new("CompletionWithNoHeadersAndStringResult", CompletionMessage.WithResult("xyz", "string"), "lQOAo3h5egOmc3RyaW5n");
        yield return new("CompletionWithNoHeadersAndBooleanResult", CompletionMessage.WithResult("xyz", true), "lQOAo3h5egPD");
        yield return new("StreamInvocationWithNoHeadersAndNoArgs", new StreamInvocationMessage("xyz", "method", []), "lgSAo3h5eqZtZXRob2SQkA==");
        yield return new("StreamInvocationWithNoHeadersAndIntArg", new StreamInvocationMessage("xyz", "method", [42]), "lgSAo3h5eqZtZXRob2SRKpA=");
        yield return new("StreamInvocationWithStreamId", new StreamInvocationMessage("xyz", "method", [], ["__test_id__"]), "lgSAo3h5eqZtZXRob2SQkatfX3Rlc3RfaWRfXw==");
        yield return new("StreamInvocationWithStreamIdAndArg", new StreamInvocationMessage("xyz", "method", [42], ["__test_id__"]), "lgSAo3h5eqZtZXRob2SRKpGrX190ZXN0X2lkX18=");
        yield return new("StreamInvocationWithNoHeadersAndIntAndStringArgs", new StreamInvocationMessage("xyz", "method", [42, "string"]), "lgSAo3h5eqZtZXRob2SSKqZzdHJpbmeQ");
        yield return new("CancelInvocationWithNoHeaders", new CancelInvocationMessage("xyz"), "kwWAo3h5eg==");
        yield return new("CancelInvocationWithHeaders", AddHeaders(new CancelInvocationMessage("xyz")), "kwWDo0Zvb6NCYXKyS2V5V2l0aApOZXcNCkxpbmVzq1N0aWxsIFdvcmtzsVZhbHVlV2l0aE5ld0xpbmVzsEFsc28KV29ya3MNCkZpbmWjeHl6");
        yield return new("Ping", PingMessage.Instance, "kQY=");
        yield return new("CloseMessage", CloseMessage.Empty, "kwfAwg==");
        yield return new("CloseMessage_HasError", new CloseMessage("Error!"), "kwemRXJyb3Ihwg==");
        yield return new("CloseMessage_HasAllowReconnect", new CloseMessage(error: null, allowReconnect: true), "kwfAww==");
        yield return new("CloseMessage_HasErrorAndAllowReconnect", new CloseMessage("Error!", allowReconnect: true), "kwemRXJyb3Ihww==");
        yield return new("AckMessage", new AckMessage(42), "kggq");
        yield return new("SequenceMessage", new SequenceMessage(146), "kgnMkg==");
    }

    static readonly MessagePackHubProtocol Protocol = new();

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Write_MatchesOfficialBytes(string name, HubMessage message, string base64)
    {
        var expectedPayload = Convert.FromBase64String(base64);
        var framed = Protocol.GetMessageBytes(message).ToArray();
        Assert.Equal(Frame(expectedPayload), framed);

        // WriteMessage takes the same path into a caller's writer
        var writer = new ArrayBufferWriter<byte>();
        Protocol.WriteMessage(message, writer);
        Assert.Equal(Frame(expectedPayload), writer.WrittenSpan.ToArray());
        Assert.NotNull(name);
    }

    [Theory]
    [MemberData(nameof(Fixtures))]
    public void Parse_MatchesExpectedMessage(string name, HubMessage expected, string base64)
    {
        var input = new ReadOnlySequence<byte>(Frame(Convert.FromBase64String(base64)));
        Assert.True(Protocol.TryParseMessage(ref input, new FixtureBinder(expected), out var parsed), name);
        Assert.True(input.IsEmpty);
        AssertMessageEqual(expected, parsed);
    }

    [Fact]
    public void Parse_TwoFramedMessages_ConsumesOneAtATime()
    {
        var ping = Frame(Convert.FromBase64String("kQY="));
        var ack = Frame(Convert.FromBase64String("kggq"));
        var input = new ReadOnlySequence<byte>(ping.Concat(ack).ToArray());
        Assert.True(Protocol.TryParseMessage(ref input, new FixtureBinder(PingMessage.Instance), out var first));
        Assert.IsType<PingMessage>(first);
        Assert.Equal(ack.Length, input.Length);
        Assert.True(Protocol.TryParseMessage(ref input, new FixtureBinder(new AckMessage(42)), out var second));
        Assert.Equal(42, Assert.IsType<AckMessage>(second).SequenceId);
        Assert.False(Protocol.TryParseMessage(ref input, new FixtureBinder(PingMessage.Instance), out _));
    }

    [Fact]
    public void Parse_PartialFrame_WaitsForMore()
    {
        var framed = Frame(Convert.FromBase64String("lgGAo3h5eqZtZXRob2SQkA=="));
        for (var cut = 1; cut < framed.Length; cut++)
        {
            var input = new ReadOnlySequence<byte>(framed, 0, cut);
            Assert.False(Protocol.TryParseMessage(ref input, new FixtureBinder(new InvocationMessage("xyz", "method", [])), out _));
        }
    }

    [Fact]
    public void Parse_ArgumentCountMismatch_IsBindingFailure()
    {
        // "method" with one int argument, bound against a target expecting two parameters
        var input = new ReadOnlySequence<byte>(Frame(Convert.FromBase64String("lgGAwKZtZXRob2SRKpA=")));
        var binder = new FixtureBinder(new InvocationMessage("method", [42, "extra"]));
        Assert.True(Protocol.TryParseMessage(ref input, binder, out var message));
        var failure = Assert.IsType<InvocationBindingFailureMessage>(message);
        Assert.Equal("method", failure.Target);
        Assert.IsType<InvalidDataException>(failure.BindingFailure.SourceException);
    }

    [Fact]
    public void Write_GeneratedPocoArgument_UsesGeneratedFormatter()
    {
        var person = new Person { Name = "Alice", Age = 30, Tags = ["a"] };
        var bytes = Protocol.GetMessageBytes(new InvocationMessage("Echo", [person])).ToArray();
        var payload = MessagePackSerializer.Serialize(person);
        // the argument bytes are exactly the standalone serialization (array form from [Key]s)
        Assert.True(bytes.AsSpan().IndexOf(payload) > 0);
    }

    [Fact]
    public void DefaultOptions_ContractlessAndEnumNames()
    {
        // attribute-less types go on the wire as maps and enums as their names, matching the official defaults
        var options = MessagePackHubProtocolOptions.CreateDefaultSerializerOptions();
        var bytes = MessagePackSerializer.Serialize(new PlainDto { Id = 7, Level = Level.High }, options);
        Assert.Equal(0x82, bytes[0]); // fixmap(2)
        var text = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("Id", text);
        Assert.Contains("Level", text);
        Assert.Contains("High", text);
        var back = MessagePackSerializer.Deserialize<PlainDto>(bytes, options)!;
        Assert.Equal((7, Level.High), (back.Id, back.Level));
    }

    static byte[] Frame(byte[] payload)
    {
        // 7-bit varint length prefix, the SignalR binary framing
        var prefix = new List<byte>();
        var length = payload.Length;
        do
        {
            var current = (byte)(length & 0x7f);
            length >>= 7;
            if (length > 0) current |= 0x80;
            prefix.Add(current);
        }
        while (length > 0);
        return [.. prefix, .. payload];
    }

    static void AssertMessageEqual(HubMessage expected, HubMessage actual)
    {
        Assert.IsType(expected.GetType(), actual);
        if (expected is HubInvocationMessage expectedInvocation)
        {
            var actualInvocation = (HubInvocationMessage)actual;
            Assert.Equal(expectedInvocation.InvocationId, actualInvocation.InvocationId);
            if (expectedInvocation.Headers is { Count: > 0 })
            {
                Assert.Equal(expectedInvocation.Headers, actualInvocation.Headers);
            }
            else
            {
                Assert.Null(actualInvocation.Headers);
            }
        }
        switch (expected)
        {
            case InvocationMessage e:
                var a = (InvocationMessage)actual;
                Assert.Equal(e.Target, a.Target);
                Assert.Equal(e.Arguments, a.Arguments);
                Assert.Equal(e.StreamIds, a.StreamIds);
                break;
            case StreamInvocationMessage e:
                var s = (StreamInvocationMessage)actual;
                Assert.Equal(e.Target, s.Target);
                Assert.Equal(e.Arguments, s.Arguments);
                Assert.Equal(e.StreamIds, s.StreamIds);
                break;
            case StreamItemMessage e:
                Assert.Equal(e.Item, ((StreamItemMessage)actual).Item);
                break;
            case CompletionMessage e:
                var c = (CompletionMessage)actual;
                Assert.Equal(e.Error, c.Error);
                Assert.Equal(e.HasResult, c.HasResult);
                Assert.Equal(e.Result, c.Result);
                break;
            case CloseMessage e:
                var close = (CloseMessage)actual;
                Assert.Equal(e.Error, close.Error);
                Assert.Equal(e.AllowReconnect, close.AllowReconnect);
                break;
            case AckMessage e:
                Assert.Equal(e.SequenceId, ((AckMessage)actual).SequenceId);
                break;
            case SequenceMessage e:
                Assert.Equal(e.SequenceId, ((SequenceMessage)actual).SequenceId);
                break;
        }
    }

    // answers the binder's questions from the message the fixture expects
    sealed class FixtureBinder(HubMessage expected) : IInvocationBinder
    {
        public IReadOnlyList<Type> GetParameterTypes(string methodName)
        {
            var arguments = expected switch
            {
                InvocationMessage m => m.Arguments,
                StreamInvocationMessage m => m.Arguments,
                _ => null,
            };
            return arguments?.Select(a => a?.GetType() ?? typeof(object)).ToArray() ?? [];
        }

        public Type GetReturnType(string invocationId)
            => expected is CompletionMessage { Result: { } result } ? result.GetType() : typeof(object);

        public Type GetStreamItemType(string streamId)
            => expected is StreamItemMessage { Item: { } item } ? item.GetType() : typeof(object);

        public string? GetTarget(ReadOnlySpan<byte> utf8Bytes) => null;
    }
}

public class PlainDto
{
    public int Id { get; set; }
    public Level Level { get; set; }
}

public enum Level
{
    Low,
    High,
}
