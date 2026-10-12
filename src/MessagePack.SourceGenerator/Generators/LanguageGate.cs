using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MessagePack.SourceGenerator.Generators;

/// <summary>
/// The language floor of generated code: C# 9 for [ModuleInitializer] (and the C# 8 nullable annotations),
/// C# 13 where the NET9_0_OR_GREATER branches emit <c>allows ref struct</c>. net48 and netstandard2.x default to
/// C# 7.3, where every generated file would fail with CS8370/CS8400 inside .g.cs; below the floor the generators emit
/// nothing and report MsgPack024 instead (only when they had something to emit, so a project that references
/// MessagePack for the reflection tier alone keeps building on C# 7.3).
/// </summary>
static class LanguageGate
{
    /// <summary>Required is 0 when generated code compiles here, otherwise the required version (major * 100).</summary>
    public static IncrementalValueProvider<(int Required, string Current)> Create(IncrementalGeneratorInitializationContext context) =>
        context.ParseOptionsProvider.Select(static (options, _) => Evaluate(options));

    static (int Required, string Current) Evaluate(ParseOptions options)
    {
        if (options is not CSharpParseOptions csharp)
        {
            return default;
        }
        // numeric: Microsoft.CodeAnalysis 4.3.1's LanguageVersion enum predates C# 13 (values are major * 100); the
        // hosting compiler resolves Latest/Default to its own real version
        var required = csharp.PreprocessorSymbolNames.Contains("NET9_0_OR_GREATER") ? 1300 : 900;
        return (int)csharp.LanguageVersion >= required ? default : (required, csharp.LanguageVersion.ToDisplayString());
    }

    /// <summary>
    /// The custom-formatter generators (BufferConstraints, FactoryBridge) only need the C# 9 floor: they emit
    /// <c>allows ref struct</c> only where the compilation can express it. Below the floor they emit nothing, and this
    /// reports MsgPack024 once per generator when it had a model.
    /// </summary>
    public static IncrementalValueProvider<bool> BelowFloor<T>(IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<T> models)
    {
        var gate = Create(context).Select(static (gate, _) => gate.Required == 900 ? gate : default);
        context.RegisterSourceOutput(models.Collect().Combine(gate), static (spc, pair) =>
        {
            if (pair.Right.Required != 0 && pair.Left.Length > 0)
            {
                spc.ReportDiagnostic(Diagnostic(pair.Right));
            }
        });
        return gate.Select(static (gate, _) => gate.Required != 0);
    }

    public static Diagnostic Diagnostic((int Required, string Current) gate)
    {
        var version = gate.Required == 900 ? "9.0" : (gate.Required / 100).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var reason = gate.Required == 900
            ? "generated formatters register through [ModuleInitializer]"
            : "generated formatters on .NET 9+ declare `allows ref struct` buffer parameters";
        return new DiagnosticInfo(
            Diagnostics.LanguageVersionTooLow,
            $"MessagePack source generation requires C# {version} or later ({reason}), but this project compiles as C# {gate.Current}. Set <LangVersion>{version}</LangVersion> or later in the project file; no formatters were generated.",
            null).ToDiagnostic();
    }
}
