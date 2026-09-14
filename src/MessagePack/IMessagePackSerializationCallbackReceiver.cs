namespace MessagePack;

/// <summary>
/// Lets a serialized object run code before it is written and after it has been read.
/// Honored by the object formatters, including those the source generator emits.
/// For struct types the callbacks run on the value being serialized or deserialized, so changes made there take effect.
/// </summary>
public interface IMessagePackSerializationCallbackReceiver
{
    /// <summary>Called before any member is written.</summary>
    void OnBeforeSerialize();

    /// <summary>Called after all members have been populated.</summary>
    void OnAfterDeserialize();
}
