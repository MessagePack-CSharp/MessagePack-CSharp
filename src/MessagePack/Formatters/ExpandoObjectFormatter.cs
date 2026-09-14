using System.Dynamic;

namespace MessagePack.Formatters;

// MsgPack104: the member-name map keys are protocol structure, not string values; the values themselves go through the resolver's object formatter.
#pragma warning disable MsgPack104

// The deprecation text lives OUTSIDE the [Obsolete] members so that referencing it
// (annotations on non-obsolete members) does not itself trip CS0618.
internal static class ExpandoObjectMessages
{
    internal const string Deprecated =
        "ExpandoObject support exists for v3 compatibility only and is not part of any default chain: " +
        "every deserialized member pays ExpandoObject's class-transition Add cost, so total cost grows " +
        "quadratically with member count, which turns untrusted input into a CPU-amplification DoS vector. " +
        "Prefer Dictionary<string, object?>, which reads the identical map wire form. To keep ExpandoObject, " +
        "compose ExpandoObjectFormatterFactory into the chain and suppress this warning to accept the cost.";
}

// v3-compatible ExpandoObject support: a map of member name to value, values delegated
// to the resolver's object formatter (so a typeless chain embeds concrete types and a
// default chain writes primitive-object forms). Only the statically-requested root comes
// back as ExpandoObject; nested maps deserialize per the object formatter's rules
// (Dictionary<object, object?>), matching v3.
//
// Deliberately NOT in the default chain (and refused by the Generic/Reflection
// catch-alls): the opt-in surface is ExpandoObjectFormatterFactory below.
[Obsolete(ExpandoObjectMessages.Deprecated)]
public sealed partial class ExpandoObjectFormatter<TWriteBuffer, TReadBuffer> : IMessagePackFormatter<TWriteBuffer, TReadBuffer, ExpandoObject?>
{
    IMessagePackFormatter<TWriteBuffer, TReadBuffer, object?> valueFormatter = null!;

    public void Initialize(MessagePackFormatterResolver resolver)
    {
        valueFormatter = resolver.GetFormatter<TWriteBuffer, TReadBuffer, object?>();
    }

    public void Serialize(ref TWriteBuffer buffer, ref SerializeState state, ExpandoObject? value)
    {
        if (value == null)
        {
            buffer.WriteNil();
            return;
        }

        var members = (ICollection<KeyValuePair<string, object?>>)value;
        state.Enter();
        buffer.WriteMapHeader(members.Count);
        foreach (var member in members)
        {
            buffer.WriteString(member.Key);
            valueFormatter.Serialize(ref buffer, ref state, member.Value);
        }
        state.Exit();
    }

    public void Deserialize(ref TReadBuffer buffer, ref DeserializeState state, ref ExpandoObject? value)
    {
        if (buffer.TryReadNil())
        {
            value = null;
            return;
        }

        var count = buffer.ReadMapHeader();
        var result = new ExpandoObject();
        var members = (IDictionary<string, object?>)result;
        state.Enter();
        for (int i = 0; i < count; i++)
        {
            var key = buffer.ReadString();
            if (key is null)
            {
                throw new MessagePackSerializationException("ExpandoObject member name must be a string, not nil.");
            }
            object? memberValue = null;
            valueFormatter.Deserialize(ref buffer, ref state, ref memberValue);
            members[key] = memberValue;
        }
        // a duplicate member name collapsed into one slot: reject, same as the dictionary formatters
        if (members.Count != count)
        {
            MessagePackSerializationException.ThrowDuplicateMapKey();
        }
        state.Exit();
        value = result;
    }
}

/// <summary>
/// Serves <see cref="ExpandoObject"/>, which no default chain does (the [Obsolete] message
/// carries the reasons). Compose it anywhere in a chain, nothing else claims the type:
/// <c>MessagePackFormatterFactory.Combine(new ExpandoObjectFormatterFactory(), MessagePackFormatterFactory.Default)</c>.
/// </summary>
[Obsolete(ExpandoObjectMessages.Deprecated)]
public sealed partial class ExpandoObjectFormatterFactory : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(ExpandoObject) ? new ExpandoObjectFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}
