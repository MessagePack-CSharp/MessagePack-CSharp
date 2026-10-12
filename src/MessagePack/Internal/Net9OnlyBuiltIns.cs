namespace MessagePack;

/// <summary>
/// BCL types whose built-in formatters exist only in the net9.0+ build. A net8.0 consumer (or Unity, or .NET Framework
/// with a polyfill package) runs the netstandard build, where these are unknown: the contractless tier must not claim
/// them as plain objects (Half and Int128 have no public members and silently read back as 0, DateOnly and Rune took a
/// shape neither v3 nor the net9 build reads), and the not-found error names the cause.
/// </summary>
static class Net9OnlyBuiltIns
{
    public static bool Contains(Type type)
    {
#if NET9_0_OR_GREATER
        return false;
#else
        return type.FullName is "System.Half"
            or "System.Int128"
            or "System.UInt128"
            or "System.DateOnly"
            or "System.TimeOnly"
            or "System.Text.Rune"
            or "System.Runtime.InteropServices.NFloat"
#if NETSTANDARD2_0
            or "System.Index"
            or "System.Range"
#endif
            ;
#endif
    }

    public const string Hint = " MessagePack serves this type only in its net9.0+ build, and this process runs the netstandard build (a net8.0 or older target, .NET Framework or Unity). Target net9.0 or later, or compose a factory that serves the type.";
}
