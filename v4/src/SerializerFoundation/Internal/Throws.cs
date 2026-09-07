using System.Diagnostics.CodeAnalysis;

namespace SerializerFoundation;

internal static class Throws
{
    [DoesNotReturn]
    internal static void ArgumentOutOfRange() => throw new ArgumentOutOfRangeException();

    [DoesNotReturn]
    internal static void ArgumentNull(string? paramName) => throw new ArgumentNullException(paramName);

    [DoesNotReturn]
    internal static void AdvancedTooFar() => throw new InvalidOperationException("Cannot advance past the end of the buffer.");

    [DoesNotReturn]
    internal static void InsufficientSpaceInBuffer() => throw new InvalidOperationException("Insufficient space in buffer.");

    // read-side twin of InsufficientSpaceInBuffer, for a CopyTo destination longer than the remaining data
    [DoesNotReturn]
    internal static void InsufficientDataInBuffer() => throw new InvalidOperationException("Insufficient data in buffer.");

    [DoesNotReturn]
    internal static T InsufficientSpaceInBuffer<T>() => throw new InvalidOperationException("Insufficient space in buffer.");
}
