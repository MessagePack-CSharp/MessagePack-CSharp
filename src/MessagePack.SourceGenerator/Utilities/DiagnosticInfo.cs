using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>Pre-formatted diagnostic carried through the pipeline as pure values.</summary>
// DiagnosticDescriptor has value equality, so holding it directly keeps the record equatable for the
// incremental cache; only the object[] message args would have broken that, hence the pre-formatted Message
public sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, string Message, LocationInfo? Location)
{
    public Diagnostic ToDiagnostic() =>
        Diagnostic.Create(Descriptor, Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, Message);
}
