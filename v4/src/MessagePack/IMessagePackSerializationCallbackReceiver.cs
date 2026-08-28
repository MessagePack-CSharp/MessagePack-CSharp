namespace MessagePack;

/// <summary>
/// Hooks invoked around serialization of the implementing object.
/// </summary>
/// <remarks>
/// Invoked by the source-generated formatters and by <see cref="Formatters.ReflectionObjectFormatter{TWriteBuffer, TReadBuffer, T}"/>:
/// <see cref="OnBeforeSerialize"/> runs after the null check and before any member is read,
/// <see cref="OnAfterDeserialize"/> runs after all members are populated. For struct types
/// the callbacks observe and mutate the value being (de)serialized, matching v3's
/// constrained-call semantics.
/// </remarks>
public interface IMessagePackSerializationCallbackReceiver
{
    /// <summary>Called before the object is serialized.</summary>
    void OnBeforeSerialize();

    /// <summary>Called after the object has been deserialized.</summary>
    void OnAfterDeserialize();
}
