using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace UltraMessagePack.SourceGenerator;

public sealed record FactoryBridgeModel(
    string? Namespace,
    EquatableArray<string> ContainingDeclarations,
    string TypeKeyword,
    string Name,
    EquatableArray<string> TypeParameterNames,
    string HintName);

/// <summary>
/// Companion to the two-tier IMessagePackFormatterFactory shape: the interface's only
/// abstract member is the Type-based CreateFormatter(writeBufferType, readBufferType,
/// valueType), and mapping those Types back into generic construction is pure
/// boilerplate over the library's built-in buffer pairs. This generator emits that
/// dispatch (see <see cref="BufferPairs"/>) for every PARTIAL factory that implements
/// the generic CreateFormatter&lt;TWriteBuffer, TReadBuffer&gt; and has not written the
/// Type-based overload itself — so a factory author writes only the generic method plus
/// the `partial` keyword.
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class FactoryBridgeGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is TypeDeclarationSyntax { BaseList: not null } typeDeclaration &&
                    typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, cancellationToken) => Parse(ctx, cancellationToken))
            .Where(static model => model is not null);

        context.RegisterSourceOutput(models, static (spc, model) =>
        {
            spc.AddSource(model!.HintName, EmitBridge(model!));
        });
    }

    static FactoryBridgeModel? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node, cancellationToken) is not INamedTypeSymbol symbol)
        {
            return null;
        }

        // one model per type, not per partial declaration
        var declarations = symbol.DeclaringSyntaxReferences;
        if (declarations.Length == 0)
        {
            return null;
        }
        var first = declarations[0];
        if (first.SyntaxTree != context.Node.SyntaxTree || first.Span != context.Node.Span)
        {
            return null;
        }

        var factoryDefinition = context.SemanticModel.Compilation.GetTypeByMetadataName("UltraMessagePack.IMessagePackFormatterFactory");
        if (factoryDefinition is null)
        {
            return null;
        }
        var implementsFactory = false;
        foreach (var implemented in symbol.AllInterfaces)
        {
            if (SymbolEqualityComparer.Default.Equals(implemented, factoryDefinition))
            {
                implementsFactory = true;
                break;
            }
        }
        if (!implementsFactory)
        {
            return null;
        }

        // the dispatch delegates into the type's own generic CreateFormatter — require it,
        // and stand down when the author wrote the Type-based overload themselves
        var hasGeneric = false;
        foreach (var member in symbol.GetMembers("CreateFormatter"))
        {
            if (member is not IMethodSymbol method)
            {
                continue;
            }
            if (method.Arity == 2 && method.Parameters.Length == 1)
            {
                hasGeneric = true;
            }
            if (method.Arity == 0 && method.Parameters.Length == 3)
            {
                return null; // hand-written Type-based overload wins
            }
        }
        if (!hasGeneric)
        {
            return null;
        }

        var containingDeclarations = new List<string>();
        var hintName = new StringBuilder();
        for (var parent = symbol.ContainingType; parent is not null; parent = parent.ContainingType)
        {
            containingDeclarations.Insert(0, $"partial {TypeKeyword(parent)} {parent.Name}{TypeParameterList(parent)}");
            hintName.Insert(0, parent.Name + ".");
        }
        if (symbol.ContainingNamespace is { IsGlobalNamespace: false } containingNamespace)
        {
            hintName.Insert(0, containingNamespace.ToDisplayString() + ".");
        }
        hintName.Append(symbol.Name).Append('_').Append(symbol.Arity).Append(".FactoryBridge.g.cs");

        var typeParameterNames = new string[symbol.Arity];
        for (var i = 0; i < symbol.Arity; i++)
        {
            typeParameterNames[i] = symbol.TypeParameters[i].Name;
        }

        return new FactoryBridgeModel(
            symbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
            new EquatableArray<string>([.. containingDeclarations]),
            TypeKeyword(symbol),
            symbol.Name,
            new EquatableArray<string>(typeParameterNames),
            hintName.ToString());
    }

    static string TypeKeyword(INamedTypeSymbol type) =>
        type.IsRecord
            ? (type.TypeKind == TypeKind.Struct ? "record struct" : "record")
            : type.TypeKind switch
            {
                TypeKind.Struct => "struct",
                _ => "class",
            };

    static string TypeParameterList(INamedTypeSymbol type)
    {
        if (type.Arity == 0)
        {
            return "";
        }
        var names = new string[type.Arity];
        for (var i = 0; i < type.Arity; i++)
        {
            names[i] = type.TypeParameters[i].Name;
        }
        return $"<{string.Join(", ", names)}>";
    }

    static string EmitBridge(FactoryBridgeModel model)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
        sb.AppendLine("#nullable enable annotations");
        var indent = 0;

        void OpenScope(string line)
        {
            sb.Append(' ', indent * 4).AppendLine(line);
            sb.Append(' ', indent * 4).AppendLine("{");
            indent++;
        }

        if (model.Namespace is not null)
        {
            OpenScope($"namespace {model.Namespace}");
        }
        foreach (var containing in model.ContainingDeclarations)
        {
            OpenScope(containing);
        }

        var typeParameters = model.TypeParameterNames.Length == 0
            ? ""
            : $"<{string.Join(", ", model.TypeParameterNames.AsArray())}>";
        OpenScope($"partial {model.TypeKeyword} {model.Name}{typeParameters}");

        BufferPairs.AppendCreateFormatterDispatch(sb, new string(' ', indent * 4));

        while (indent > 0)
        {
            indent--;
            sb.Append(' ', indent * 4).AppendLine("}");
        }
        return sb.ToString();
    }
}
