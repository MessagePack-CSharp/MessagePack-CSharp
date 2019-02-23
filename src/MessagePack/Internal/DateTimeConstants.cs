using System;

namespace MessagePack.Internal
{
    internal static class DateTimeConstants
    {
        internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        internal const long BclSecondsAtUnixEpoch = 62135596800;
        internal const int NanosecondsPerTick = 100;
    }
}

