using System.Collections.Frozen;

namespace MessagePack;

/// <summary>
/// The type the runtime-type paths (an <see cref="object"/> slot, Typeless, <c>Serialize(value.GetType(), ...)</c>)
/// serialize a value as. A frozen collection's runtime type is a BCL-internal implementation
/// (<c>SmallValueTypeComparableFrozenSet&lt;int&gt;</c> behind <c>ToFrozenSet()</c>) that no factory recognizes and
/// whose name must not reach the Typeless wire; its public base is what serializes. A <see cref="Type"/> value is a
/// <c>System.RuntimeType</c> the same way (the opt-in TypeFormatterFactory serves <see cref="Type"/>).
/// </summary>
static class RuntimeTypeView
{
    public static Type Normalize(Type type)
    {
        if (type.IsVisible)
        {
            return type;
        }
        if (typeof(Type).IsAssignableFrom(type))
        {
            return typeof(Type);
        }
        for (var current = type.BaseType; current is not null; current = current.BaseType)
        {
            if (current.IsGenericType)
            {
                var definition = current.GetGenericTypeDefinition();
                if (definition == typeof(FrozenSet<>) || definition == typeof(FrozenDictionary<,>))
                {
                    return current;
                }
            }
        }
        return type;
    }
}
