using System.Diagnostics.CodeAnalysis;

namespace SerializerFoundation;

internal static class Throws
{
    [DoesNotReturn]
    internal static void ArgumentOutOfRange() => throw new ArgumentOutOfRangeException();

    [DoesNotReturn]
    internal static void InsufficientSpaceInBuffer() => throw new InvalidOperationException("Insufficient space in buffer.");

    [DoesNotReturn]
    internal static T InsufficientSpaceInBuffer<T>() => throw new InvalidOperationException("Insufficient space in buffer.");
}
