namespace UltraMessagePack;

/// <summary>The unit type representing the msgpack nil token.</summary>
public struct Nil : IEquatable<Nil>
{
    public static readonly Nil Default = default(Nil);

    public static bool operator ==(Nil left, Nil right) => true;

    public static bool operator !=(Nil left, Nil right) => false;

    public override bool Equals(object? obj) => obj is Nil;

    public bool Equals(Nil other) => true;

    public override int GetHashCode() => 0;

    public override string ToString() => "()";
}
