using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using MessagePack.SourceGenerator.Generators;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace MessagePack.Tests;

// A netstandard2.0 library using PolySharp got ModuleInitializerAttribute from both polyfill generators (CS0101).
// PolySharp's output is invisible to the generator, but its switches are compiler-visible build properties whose keys
// exist (empty or not) whenever the package is referenced; the generator reads them to decide on its own polyfill.
public class PolySharpDetectionTests
{
    static readonly Func<AnalyzerConfigOptions, bool> PolySharpProvidesModuleInitializer = (Func<AnalyzerConfigOptions, bool>)typeof(MessagePackGenerator)
        .GetMethod("PolySharpProvidesModuleInitializer", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate(typeof(Func<AnalyzerConfigOptions, bool>));

    sealed class Options(Dictionary<string, string> values) : AnalyzerConfigOptions
    {
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => values.TryGetValue(key, out value);
    }

    const string Include = "build_property.PolySharpIncludeGeneratedTypes";
    const string Exclude = "build_property.PolySharpExcludeGeneratedTypes";
    const string Attribute = "System.Runtime.CompilerServices.ModuleInitializerAttribute";

    [Fact]
    public void NoPolySharp_KeepsTheGeneratorsPolyfill()
    {
        Assert.False(PolySharpProvidesModuleInitializer(new Options([])));
    }

    [Fact]
    public void PolySharpReferenced_SuppliesTheAttribute()
    {
        // the default: every polyfill, the property present but empty
        Assert.True(PolySharpProvidesModuleInitializer(new Options(new() { [Include] = "" })));
        Assert.True(PolySharpProvidesModuleInitializer(new Options(new() { [Include] = "", [Exclude] = "System.Index" })));
        Assert.True(PolySharpProvidesModuleInitializer(new Options(new() { [Include] = $"{Attribute};System.Index" })));
    }

    [Fact]
    public void PolySharpWithoutTheAttribute_LeavesItToTheGenerator()
    {
        Assert.False(PolySharpProvidesModuleInitializer(new Options(new() { [Include] = "System.Index" })));
        Assert.False(PolySharpProvidesModuleInitializer(new Options(new() { [Include] = "", [Exclude] = Attribute })));
    }
}
