using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace UltraMessagePack.SourceGenerator;

public sealed record BufferConstraintModel(
    string? Namespace,
    EquatableArray<string> ContainingDeclarations,
    string TypeKeyword,
    string Name,
    EquatableArray<string> TypeParameterNames,
    EquatableArray<byte> ParameterRoles,
    string HintName);

/// <summary>
/// The Round's multi-targeting escape hatch for formatter authors: write
/// <c>partial class FooFormatter&lt;TWriteBuffer, TReadBuffer&gt; : IMessagePackFormatter&lt;...&gt;</c>
/// with NO constraints, and this generator emits the matching partial declaration
/// carrying <c>where T : struct, IWriteBuffer/IReadBuffer</c> — plus
/// <c>allows ref struct</c> exactly when the current compilation can express it
/// (net9.0+ runtime and C# 13+). No <c>#if</c> in user code or generated code:
/// each TFM's compilation regenerates the right shape.
///
/// Convention, not attribute: any partial type implementing IMessagePackFormatter`3
/// whose buffer slots are its own type parameters. A single hand-written constraint
/// on ANY type parameter opts the whole type out — partial declarations that both
/// carry where-clauses must match exactly (CS0265), so generated and manual clauses
/// cannot coexist. (This also means method-level buffer generics, e.g. factory
/// CreateFormatter, stay manual: C# has no way to add constraints to a method from
/// another partial declaration.)
/// </summary>
[Generator(LanguageNames.CSharp)]
public sealed class BufferConstraintsGenerator : IIncrementalGenerator
{
    const byte RoleWrite = 1;
    const byte RoleRead = 2;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var models = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) =>
                    node is TypeDeclarationSyntax { BaseList: not null } typeDeclaration &&
                    typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword),
                transform: static (ctx, cancellationToken) => Parse(ctx, cancellationToken))
            .Where(static model => model is not null);

        var allowsRefStruct = context.CompilationProvider.Select(static (compilation, _) => SupportsAllowsRefStruct(compilation));

        context.RegisterSourceOutput(models.Combine(allowsRefStruct), static (spc, pair) =>
        {
            spc.AddSource(pair.Left!.HintName, EmitConstraints(pair.Left!, pair.Right));
        });
    }

    static bool SupportsAllowsRefStruct(Compilation compilation)
    {
        // corelib advertising ByRefLikeGenerics means the runtime executes ref-struct
        // generic instantiations. The language gate is numeric
        // because this project pins Microsoft.CodeAnalysis 4.8, whose LanguageVersion enum
        // predates C# 13 (values are major * 100; the compiler actually hosting the
        // generator is newer and resolves Latest/Default to its own real version).
        var runtimeFeature = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.RuntimeFeature");
        var runtimeSupports = runtimeFeature is not null && !runtimeFeature.GetMembers("ByRefLikeGenerics").IsEmpty;
        return runtimeSupports && compilation is CSharpCompilation csharp && (int)csharp.LanguageVersion >= 1300;
    }

    static BufferConstraintModel? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.SemanticModel.GetDeclaredSymbol((TypeDeclarationSyntax)context.Node, cancellationToken) is not INamedTypeSymbol symbol ||
            symbol.Arity == 0)
        {
            return null;
        }

        // one model per type, not per partial declaration: only the first declaration in
        // source order produces output (duplicate hint names would throw at AddSource)
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

        var formatterDefinition = context.SemanticModel.Compilation.GetTypeByMetadataName("UltraMessagePack.IMessagePackFormatter`3");
        if (formatterDefinition is null)
        {
            return null;
        }

        foreach (var typeParameter in symbol.TypeParameters)
        {
            if (HasAnyConstraint(typeParameter))
            {
                return null;
            }
        }

        var roles = new byte[symbol.Arity];
        var found = false;
        foreach (var implemented in symbol.AllInterfaces)
        {
            if (!SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, formatterDefinition))
            {
                continue;
            }
            MarkRole(implemented.TypeArguments[0], symbol, roles, RoleWrite, ref found);
            MarkRole(implemented.TypeArguments[1], symbol, roles, RoleRead, ref found);
        }
        if (!found)
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
        hintName.Append(symbol.Name).Append('_').Append(symbol.Arity).Append(".BufferConstraints.g.cs");

        var typeParameterNames = new string[symbol.Arity];
        for (var i = 0; i < symbol.Arity; i++)
        {
            typeParameterNames[i] = symbol.TypeParameters[i].Name;
        }

        return new BufferConstraintModel(
            symbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
            new EquatableArray<string>([.. containingDeclarations]),
            TypeKeyword(symbol),
            symbol.Name,
            new EquatableArray<string>(typeParameterNames),
            new EquatableArray<byte>(roles),
            hintName.ToString());
    }

    static void MarkRole(ITypeSymbol argument, INamedTypeSymbol owner, byte[] roles, byte role, ref bool found)
    {
        // only type parameters DECLARED ON this type can be constrained by its partial
        // declaration; parameters owned by containing types are out of reach
        if (argument is ITypeParameterSymbol typeParameter &&
            SymbolEqualityComparer.Default.Equals(typeParameter.ContainingSymbol, owner))
        {
            roles[typeParameter.Ordinal] |= role;
            found = true;
        }
    }

    static bool HasAnyConstraint(ITypeParameterSymbol typeParameter) =>
        typeParameter.HasValueTypeConstraint ||
        typeParameter.HasReferenceTypeConstraint ||
        typeParameter.HasConstructorConstraint ||
        typeParameter.HasNotNullConstraint ||
        typeParameter.HasUnmanagedTypeConstraint ||
        typeParameter.ConstraintTypes.Length > 0;

    static string TypeKeyword(INamedTypeSymbol type) =>
        type.IsRecord
            ? (type.TypeKind == TypeKind.Struct ? "record struct" : "record")
            : type.TypeKind switch
            {
                TypeKind.Struct => "struct",
                TypeKind.Interface => "interface",
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

    static string EmitConstraints(BufferConstraintModel model, bool allowsRefStruct)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// <auto-generated/>");
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

        sb.Append(' ', indent * 4)
          .AppendLine($"partial {model.TypeKeyword} {model.Name}<{string.Join(", ", model.TypeParameterNames.AsArray())}>");
        for (var i = 0; i < model.ParameterRoles.Length; i++)
        {
            var role = model.ParameterRoles[i];
            if (role == 0)
            {
                continue;
            }
            sb.Append(' ', (indent + 1) * 4)
              .Append("where ").Append(model.TypeParameterNames[i]).Append(" : struct");
            if ((role & RoleWrite) != 0)
            {
                sb.Append(", global::SerializerFoundation.IWriteBuffer");
            }
            if ((role & RoleRead) != 0)
            {
                sb.Append(", global::SerializerFoundation.IReadBuffer");
            }
            if (allowsRefStruct)
            {
                sb.Append(", allows ref struct");
            }
            sb.AppendLine();
        }
        sb.Append(' ', indent * 4).AppendLine("{");
        sb.Append(' ', indent * 4).AppendLine("}");

        while (indent > 0)
        {
            indent--;
            sb.Append(' ', indent * 4).AppendLine("}");
        }
        return sb.ToString();
    }
}
