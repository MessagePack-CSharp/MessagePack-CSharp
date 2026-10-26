using MessagePack.Formatters;

namespace MessagePack;

/// <summary>
/// A plain [MessagePackObject] type missing from a chain without the reflection tier (DefaultAot, Native AOT): the
/// generator registers such a type itself, so the miss means it skipped the type or never ran, and the fix is at build
/// time. A [MessagePackSerializable] declaration would not help (MsgPack022 reports it as redundant).
/// </summary>
static class UngeneratedObjectHint
{
    public static bool Applies(Type type)
    {
        return !type.IsArray && !type.IsConstructedGenericType
            && AttributeFormatterActivator.FindAttribute(type.GetCustomAttributes(false), MessagePackAttributeNames.MessagePackObject) is not null;
    }

    public const string Hint = " The type carries [MessagePackObject], but no source-generated formatter is registered for it and this chain has no reflection fallback (DefaultAot and other source-generated-only chains). The source generator skipped it or never ran over it: look in the build output for its MsgPack diagnostic (MsgPack004-MsgPack006 and the other skip reports), check for [MessagePackObject(SuppressSourceGeneration = true)], a C# version below 9 (MsgPack024), or an assembly built against MessagePack v3. A [MessagePackSerializable] declaration does not register it.";
}
