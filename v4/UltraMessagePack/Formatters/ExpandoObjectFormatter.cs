using System.Dynamic;

namespace UltraMessagePack.Formatters;

// v3-compatible ExpandoObject support: a map of member name to value, values delegated
// to the resolver's object formatter (so a typeless chain embeds concrete types and a
// default chain writes primitive-object forms). Only the statically-requested root comes
// back as ExpandoObject; nested maps deserialize per the object formatter's rules
// (Dictionary<object, object?>), matching v3.
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
            members[key] = memberValue; // duplicate keys: last wins, same as the dictionary formatters
        }
        state.Exit();
        value = result;
    }
}
