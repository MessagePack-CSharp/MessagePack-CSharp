using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.SourceGenerator.Generators;

/// <summary>
/// Pipeline wiring only. Discovery is driven by one attribute, [MessagePackObject],
/// and each hit routes to exactly one shape: [UnionTag] present makes the type a union base
/// (<see cref="UnionParser"/>), otherwise it is an object (<see cref="ObjectParser"/>).
/// A [UnionTag] without [MessagePackObject] is invisible here by design;
/// the standalone MsgPack103 analyzer surfaces that mistake. Both model streams combine into one factory +
/// module-initializer registration file.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class MessagePackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // ModuleInitializerAttribute polyfill decision: the registration file needs the attribute,
        // netstandard2.x / .NET Framework targets do not have it in the BCL,
        // and the compiler recognizes it purely by name (module cctors run on every runtime).
        var needsModuleInitializerPolyfill = context.CompilationProvider
            .Select(static (compilation, _) => compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.ModuleInitializerAttribute") is null);

        var results = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackObjectAttribute",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) =>
            {
                if (UnionParser.HasUnionTag((INamedTypeSymbol)ctx.TargetSymbol))
                {
                    var union = UnionParser.Parse(ctx);
                    return new TypeParseResult(null, union.Model, union.Diagnostics);
                }
                else
                {
                    var result = ObjectParser.Parse(ctx);
                    return new TypeParseResult(result.Model, null, result.Diagnostics);
                }
            });

        // per-type formatter files: incremental per model
        context.RegisterSourceOutput(results, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
            if (result.Object is { } model)
            {
                spc.AddSource($"{ObjectEmitter.HintName(model)}.g.cs", ObjectEmitter.EmitFormatter(model));
            }
            else if (result.Union is { } union)
            {
                spc.AddSource($"{union.FormatterName}.g.cs", UnionEmitter.EmitFormatter(union));
            }
        });

        // Type-level [MessagePackFormatter]: bound at compile time so the generated factory constructs the
        // attribute-directed formatter directly, and registered through the same module-initializer path as generated
        // object formatters, the annotation is sugar over the registry tier,
        // not a separate resolution channel (no runtime attribute tier exists).
        // Precedence over a same-type generated object formatter is decided inside FactoryEmitter,
        // mirroring v3's attribute-outranks-generated order. ForAttributeWithMetadataName matches the exact attribute
        // class, so the generic MessagePackFormatterAttribute<TFactory> variant needs its own pipeline
        var attributeResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackFormatterAttribute",
            predicate: static (node, _) => node is BaseTypeDeclarationSyntax,
            transform: static (ctx, _) => AttributeFormatterParser.Parse(ctx));
        var genericAttributeResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackFormatterAttribute`1",
            predicate: static (node, _) => node is BaseTypeDeclarationSyntax,
            transform: static (ctx, _) => AttributeFormatterParser.Parse(ctx));

        context.RegisterSourceOutput(attributeResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });
        context.RegisterSourceOutput(genericAttributeResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        // [MessagePackSerializable] root-factory classes (the JsonSerializerContext pattern): each declared root runs
        // through the member-graph harvest, and the generated partial supplies the factory half plus module-initializer
        // registrations, root-only shapes (Person[], List<Person>) resolve on AOT.
        // Exact-name matching again forces one pipeline per attribute arity;
        // results targeting the same class (both forms used) merge in the factory node.
        var serializableResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackSerializableAttribute",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => SerializableParser.Parse(ctx));
        var genericSerializableResults = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackSerializableAttribute`1",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => SerializableParser.Parse(ctx));

        context.RegisterSourceOutput(serializableResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });
        context.RegisterSourceOutput(genericSerializableResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        // IMessagePackSurrogate implementations: the implementation is the declaration,
        // so the factory auto-registers each target through a statically closed SurrogateFormatter (an interface has no
        // attribute for FAWMN, a cheap base-list syntax gate feeds the semantic transform instead)
        var surrogateResults = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => SurrogateParser.IsCandidate(node),
                transform: static (ctx, cancellationToken) => SurrogateParser.Parse(ctx, cancellationToken))
            .Where(static result => result is not null)
            .Select(static (result, _) => result!);

        context.RegisterSourceOutput(surrogateResults, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
        });

        // duplicate-target detection is inherently cross-type: collected over the location-carrying sites,
        // separate from the factory node (which must stay free of locations so line-shifting edits do not re-emit it)
        var surrogateSites = surrogateResults
            .SelectMany(static (result, _) => result.Sites.AsArray())
            .Collect();
        context.RegisterSourceOutput(surrogateSites, static (spc, sites) =>
        {
            foreach (var group in sites
                .GroupBy(static site => (site.TargetTypeName, site.SurrogateTypeName))
                .Select(static group => group.First())
                .GroupBy(static site => site.TargetTypeName, StringComparer.Ordinal))
            {
                if (group.Count() <= 1)
                {
                    continue;
                }
                var surrogateNames = string.Join(", ", group.Select(static site => site.SurrogateTypeName).OrderBy(static name => name, StringComparer.Ordinal));
                foreach (var site in group)
                {
                    spc.ReportDiagnostic(new DiagnosticInfo("MsgPack018", $"'{group.Key}' has multiple surrogates ({surrogateNames}): the wire shape would be ambiguous. Keep exactly one IMessagePackSurrogate implementation per target.", site.Location).ToDiagnostic());
                }
            }
        });

        // one factory + module-initializer registration over every generated formatter. Models only,
        // deliberately not the whole parse results: diagnostics carry source locations,
        // and collecting them would re-emit the factory on every line-shifting edit near a diagnosed type
        var models = results
            .Select(static (r, _) => (r.Object, r.Union))
            .Where(static pair => pair.Object is not null || pair.Union is not null)
            .Collect();
        var attributeModels = attributeResults
            .Select(static (r, _) => r.Model)
            .Where(static model => model is not null)
            .Select(static (model, _) => model!)
            .Collect();
        var genericAttributeModels = genericAttributeResults
            .Select(static (r, _) => r.Model)
            .Where(static model => model is not null)
            .Select(static (model, _) => model!)
            .Collect();
        var serializableModels = serializableResults
            .Select(static (r, _) => r.Model)
            .Where(static model => model is not null)
            .Select(static (model, _) => model!)
            .Collect();
        var genericSerializableModels = genericSerializableResults
            .Select(static (r, _) => r.Model)
            .Where(static model => model is not null)
            .Select(static (model, _) => model!)
            .Collect();
        var surrogateModels = surrogateResults
            .SelectMany(static (result, _) => result.Sites.AsArray().Select(static site => new SurrogateModel(site.TargetTypeName, site.SurrogateTypeName)))
            .Collect();
        context.RegisterSourceOutput(models.Combine(attributeModels).Combine(genericAttributeModels).Combine(serializableModels.Combine(genericSerializableModels)).Combine(surrogateModels).Combine(needsModuleInitializerPolyfill), static (spc, pair) =>
        {
            var surrogates = pair.Left.Right;
            var allModels = pair.Left.Left.Left.Left.Left;
            var attributes = pair.Left.Left.Left.Left.Right.AddRange(pair.Left.Left.Left.Right);

            // a class using both attribute forms produced one result per pipeline: merge per class,
            // unioning the harvested sets (deduped by closed type name)
            var serializables = pair.Left.Left.Right.Left.AddRange(pair.Left.Left.Right.Right)
                .GroupBy(static m => m.FullTypeName, StringComparer.Ordinal)
                .Select(static group => group.Count() == 1
                    ? group.First()
                    : group.First() with
                    {
                        HarvestedGenerics = new EquatableArray<HarvestedGenericModel>([.. group.SelectMany(static m => m.HarvestedGenerics).GroupBy(static h => h.ClosedTypeName, StringComparer.Ordinal).Select(static g => g.First()).OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]),
                        HarvestedBuiltIns = new EquatableArray<HarvestedBuiltInModel>([.. group.SelectMany(static m => m.HarvestedBuiltIns).GroupBy(static h => h.ClosedTypeName, StringComparer.Ordinal).Select(static g => g.First()).OrderBy(static h => h.ClosedTypeName, StringComparer.Ordinal)]),
                    })
                .OrderBy(static m => m.FullTypeName, StringComparer.Ordinal)
                .ToArray();
            foreach (var serializable in serializables)
            {
                spc.AddSource($"{serializable.HintName}.g.cs", FactoryEmitter.EmitSerializableFactory(serializable, allModels));
            }

            if (allModels.Length > 0 || attributes.Length > 0 || surrogates.Length > 0)
            {
                spc.AddSource("GeneratedMessagePackFormatterFactory.g.cs", FactoryEmitter.EmitFactory(allModels, attributes, surrogates));
            }

            if (allModels.Length > 0 || attributes.Length > 0 || surrogates.Length > 0 || serializables.Length > 0)
            {
                // emitted only alongside a registration file, so compilations that merely reference the generator (no
                // [MessagePackObject]/[MessagePackFormatter]/[MessagePackSerializable] types)
                // never receive the polyfill. A remaining collision is possible when a downlevel compilation also gets
                // the attribute from another polyfill generator (e.g. PolySharp,
                // invisible to CompilationProvider): exclude one side there (PolySharp:
                // PolySharpExcludeGeneratedTypes).
                if (pair.Right)
                {
                    spc.AddSource("ModuleInitializerAttribute.g.cs", """
                        // <auto-generated/>
                        // ModuleInitializerAttribute polyfill for target frameworks without it
                        // (the compiler recognizes the attribute by name; module initializers
                        // run on every runtime)
                        namespace System.Runtime.CompilerServices
                        {
                            [global::System.AttributeUsage(global::System.AttributeTargets.Method, Inherited = false)]
                            internal sealed class ModuleInitializerAttribute : global::System.Attribute
                            {
                            }
                        }
                        """);
                }
            }
        });
    }
}
