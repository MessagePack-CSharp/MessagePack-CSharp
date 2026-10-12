using MessagePack.Formatters;

namespace MessagePack;

// CultureInfo and TimeZoneInfo are not on the default chain (v3 never served them either): reading one looks a
// payload-provided name up in the runtime's culture table or the OS time zone database (a file under
// /usr/share/zoneinfo on Unix, the registry on Windows), the same class of payload-driven runtime lookup that keeps
// System.Type opt-in. The formatters validate what they can (predefined culture names only, a name length cap, every
// lookup failure as the data error), and the opt-in makes the trust decision visible.

/// <summary>
/// Opt-in factory for <see cref="CultureInfoFormatter{TWriteBuffer, TReadBuffer}"/> (the culture name as a string;
/// only the runtime's predefined cultures read back). Compose it before the default chain:
/// <c>MessagePackFormatterFactory.Combine(new CultureInfoFormatterFactory(), MessagePackFormatterFactory.Default)</c>.
/// </summary>
public sealed partial class CultureInfoFormatterFactory : MessagePackFormatterFactory
{
    // one method, two signatures: net9+ overrides the base virtual (constraints inherited);
    // downlevel has no base member, so the constraints are spelled out
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(System.Globalization.CultureInfo) ? new CultureInfoFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}

/// <summary>
/// Opt-in factory for <see cref="TimeZoneInfoFormatter{TWriteBuffer, TReadBuffer}"/> (the time zone id as a string,
/// resolved with <see cref="TimeZoneInfo.FindSystemTimeZoneById"/> on the reading machine). Compose it before the default chain:
/// <c>MessagePackFormatterFactory.Combine(new TimeZoneInfoFormatterFactory(), MessagePackFormatterFactory.Default)</c>.
/// </summary>
public sealed partial class TimeZoneInfoFormatterFactory : MessagePackFormatterFactory
{
#if NET9_0_OR_GREATER
    public override object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
#else
    public object? CreateFormatter<TWriteBuffer, TReadBuffer>(Type type)
        where TWriteBuffer : struct, IWriteBuffer
        where TReadBuffer : struct, IReadBuffer
#endif
    {
        return type == typeof(TimeZoneInfo) ? new TimeZoneInfoFormatter<TWriteBuffer, TReadBuffer>() : null;
    }
}
