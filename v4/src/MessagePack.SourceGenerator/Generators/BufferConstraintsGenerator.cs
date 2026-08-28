using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace MessagePack.SourceGenerator.Generators;

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
            // a thrown exception here (e.g. a duplicate hint name from colliding mid-edit
            // declarations) would discard EVERY generated source of this generator for the
            // pass, turning one editing slip into a screenful of constraint errors — drop
            // just the offending output instead
            try
            {
                spc.AddSource(pair.Left!.HintName, EmitConstraints(pair.Left!, pair.Right));
            }
            catch (ArgumentException)
            {
            }
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
        // mid-edit code reaches this transform constantly; no shape may throw, because a
        // generator exception cancels the WHOLE pass and every formatter in the project
        // loses its constraints at once
        try
        {
            return ParseCore(context, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    static BufferConstraintModel? ParseCore(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is not INamedTypeSymbol symbol ||
            symbol.Arity == 0 ||
            symbol.Name.Length == 0) // a half-typed nameless declaration would collide on hint names
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

        foreach (var typeParameter in symbol.TypeParameters)
        {
            if (HasAnyConstraint(typeParameter))
            {
                return null;
            }
        }
        // a manual where-clause that is still being typed may not have reached the symbol
        // yet; the syntax-level check keeps us from fighting it with CS0265
        if (typeDeclaration.ConstraintClauses.Count > 0)
        {
            return null;
        }

        var roles = new byte[symbol.Arity];
        var found = false;
        var formatterDefinition = context.SemanticModel.Compilation.GetTypeByMetadataName("MessagePack.IMessagePackFormatter`3");
        if (formatterDefinition is not null)
        {
            foreach (var implemented in symbol.AllInterfaces)
            {
                if (!SymbolEqualityComparer.Default.Equals(implemented.OriginalDefinition, formatterDefinition))
                {
                    continue;
                }
                MarkRole(implemented.TypeArguments[0], symbol, roles, RoleWrite, ref found);
                MarkRole(implemented.TypeArguments[1], symbol, roles, RoleRead, ref found);
            }
        }
        if (!found)
        {
            // Syntactic fallback: while the file is mid-edit (unresolved serialized type,
            // incomplete base list, binding poisoned by errors elsewhere) the interface may
            // not bind semantically. The constraints must survive those states — otherwise
            // one real error explodes into a page of constraint violations. Convention
            // match by NAME: an IMessagePackFormatter<...> base whose first two arguments
            // are this type's own type parameters.
            MarkRolesFromBaseListSyntax(typeDeclaration, symbol, roles, ref found);
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

    static void MarkRolesFromBaseListSyntax(TypeDeclarationSyntax typeDeclaration, INamedTypeSymbol symbol, byte[] roles, ref bool found)
    {
        if (typeDeclaration.BaseList is null)
        {
            return;
        }
        foreach (var baseType in typeDeclaration.BaseList.Types)
        {
            var name = baseType.Type;
            while (true)
            {
                if (name is QualifiedNameSyntax qualified)
                {
                    name = qualified.Right;
                }
                else if (name is AliasQualifiedNameSyntax aliasQualified)
                {
                    name = aliasQualified.Name;
                }
                else
                {
                    break;
                }
            }
            if (name is not GenericNameSyntax { Identifier.ValueText: "IMessagePackFormatter" } generic)
            {
                continue;
            }
            var arguments = generic.TypeArgumentList.Arguments;
            if (arguments.Count < 2)
            {
                continue;
            }
            MarkRoleByName(arguments[0], symbol, roles, RoleWrite, ref found);
            MarkRoleByName(arguments[1], symbol, roles, RoleRead, ref found);
        }
    }

    static void MarkRoleByName(TypeSyntax argument, INamedTypeSymbol owner, byte[] roles, byte role, ref bool found)
    {
        if (argument is not IdentifierNameSyntax identifier)
        {
            return;
        }
        for (var i = 0; i < owner.TypeParameters.Length; i++)
        {
            if (owner.TypeParameters[i].Name == identifier.Identifier.ValueText)
            {
                roles[i] |= role;
                found = true;
                return;
            }
        }
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
        var writer = CodeWriter.Rent();
        writer.Line("// <auto-generated/>");

        var containers = new List<CodeWriter.BlockScope>();
        if (model.Namespace is not null)
        {
            containers.Add(writer.Block($"namespace {model.Namespace}"));
        }
        foreach (var containing in model.ContainingDeclarations)
        {
            containers.Add(writer.Block(containing));
        }

        writer.Line($"partial {model.TypeKeyword} {model.Name}<{string.Join(", ", model.TypeParameterNames.AsArray())}>");
        writer.Indent();
        for (var i = 0; i < model.ParameterRoles.Length; i++)
        {
            var role = model.ParameterRoles[i];
            if (role == 0)
            {
                continue;
            }
            var constraint = $"where {model.TypeParameterNames[i]} : struct";
            if ((role & RoleWrite) != 0)
            {
                constraint += ", global::SerializerFoundation.IWriteBuffer";
            }
            if ((role & RoleRead) != 0)
            {
                constraint += ", global::SerializerFoundation.IReadBuffer";
            }
            if (allowsRefStruct)
            {
                constraint += ", allows ref struct";
            }
            writer.Line(constraint);
        }
        writer.Unindent();
        using (writer.OpenScope())
        {
        }

        for (int i = containers.Count - 1; i >= 0; i--)
        {
            containers[i].Dispose();
        }
        return writer.ToStringAndReturn();
    }
}
