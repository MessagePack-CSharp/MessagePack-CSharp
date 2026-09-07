// Every v3 MessagePack.Annotations type that survives in v4 under the same full name.
// Constructor signatures and property shapes match v3. MessagePackFormatterAttribute keeps the (Type) / (Type, object[]) pair,
// but the type it names is now a factory type; see the note in MessagePack's MessagePackAttributes.cs.
// Types v4 renamed or deleted are in LegacyAttributes.cs.

using System.Runtime.CompilerServices;

[assembly: TypeForwardedTo(typeof(MessagePack.MessagePackObjectAttribute))]
[assembly: TypeForwardedTo(typeof(MessagePack.KeyAttribute))]
[assembly: TypeForwardedTo(typeof(MessagePack.IgnoreMemberAttribute))]
[assembly: TypeForwardedTo(typeof(MessagePack.SerializationConstructorAttribute))]
[assembly: TypeForwardedTo(typeof(MessagePack.MessagePackFormatterAttribute))]
[assembly: TypeForwardedTo(typeof(MessagePack.IMessagePackSerializationCallbackReceiver))]
