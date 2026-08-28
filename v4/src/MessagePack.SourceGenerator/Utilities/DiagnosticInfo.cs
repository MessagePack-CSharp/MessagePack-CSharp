using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator;

/// <summary>Pre-formatted diagnostic carried through the pipeline as pure values.</summary>
public sealed record DiagnosticInfo(string Id, string Message, LocationInfo? Location)
{
    public Diagnostic ToDiagnostic()
    {
        var descriptor = Diagnostics.ById(Id);
        return Diagnostic.Create(descriptor, Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, Message);
    }
}
