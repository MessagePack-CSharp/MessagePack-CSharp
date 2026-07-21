using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace UltraMessagePack.SourceGenerator;

/// <summary>
/// Members that have a dedicated buffer read/write pair are emitted as direct calls (and
/// batched into shared reservations on the write side); everything else goes through an
/// Initialize-resolved formatter field — the shape the dispatch measurements picked
/// (DisasmProbe9/11: batch fixed-size writes, loop+switch reads, interface fields over
/// per-call resolution).
/// </summary>
public enum DirectKind
{
    None,
    Int32,
    UInt32,
    Int64,
    UInt64,
    Int16,
    UInt16,
    Byte,
    SByte,
    Boolean,
    Char,
    Single,
    Double,
    String,
    DateTime,
}

public sealed record MemberModel(
    string Name,
    string TypeName,
    int IntKey,
    string StringKey,
    DirectKind Direct);

public sealed record ObjectModel(
    string FullTypeName,
    string FormatterName,
    bool IsValueType,
    bool IsStringKey,
    int MaxIntKey,
    EquatableArray<MemberModel> Members);

public sealed record ParseResult(
    ObjectModel? Model,
    EquatableArray<DiagnosticInfo> Diagnostics);

[Generator(LanguageNames.CSharp)]
public sealed class MessagePackGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var results = context.SyntaxProvider.ForAttributeWithMetadataName(
            "MessagePack.MessagePackObjectAttribute",
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (ctx, _) => Parser.Parse(ctx));

        // per-type formatter files: incremental per model
        context.RegisterSourceOutput(results, static (spc, result) =>
        {
            foreach (var diagnostic in result.Diagnostics)
            {
                spc.ReportDiagnostic(diagnostic.ToDiagnostic());
            }
            if (result.Model is { } model)
            {
                spc.AddSource($"{model.FormatterName}.g.cs", Emitter.EmitFormatter(model));
            }
        });

        // one factory + module-initializer registration over every generated formatter
        var models = results
            .Select(static (r, _) => r.Model)
            .Where(static m => m is not null)
            .Collect();
        context.RegisterSourceOutput(models, static (spc, models) =>
        {
            if (models.Length > 0)
            {
                spc.AddSource("GeneratedMessagePackFormatterFactory.g.cs", Emitter.EmitFactory(models!));
            }
        });
    }
}

static class Parser
{
    const string KeyAttributeName = "MessagePack.KeyAttribute";
    const string IgnoreMemberAttributeName = "MessagePack.IgnoreMemberAttribute";

    static readonly SymbolDisplayFormat FullyQualifiedWithNullability =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
            | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public static ParseResult Parse(GeneratorAttributeSyntaxContext context)
    {
        var diagnostics = new List<DiagnosticInfo>();
        var type = (INamedTypeSymbol)context.TargetSymbol;
        var typeLocation = LocationInfo.From(type);
        var typeName = type.ToDisplayString();

        if (type.IsGenericType || type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
        {
            diagnostics.Add(new DiagnosticInfo("UMP005", $"'{typeName}' is skipped: generic types and non-class/struct shapes are not supported yet.", typeLocation));
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        // the generated formatter lives in another namespace of the same assembly
        for (var accessible = type; accessible is not null; accessible = accessible.ContainingType)
        {
            if (accessible.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                diagnostics.Add(new DiagnosticInfo("UMP006", $"'{typeName}' (or a containing type) is {type.DeclaredAccessibility}; generated formatters can only reach public or internal types.", typeLocation));
                return new ParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
            }
        }

        if (!type.IsValueType && !type.InstanceConstructors.Any(static c =>
                c.Parameters.Length == 0 && c.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal))
        {
            diagnostics.Add(new DiagnosticInfo("UMP004", $"'{typeName}' needs an accessible parameterless constructor for deserialization ([SerializationConstructor] support is planned).", typeLocation));
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        var keyAsPropertyName = false;
        var objectAttribute = context.Attributes[0];
        if (objectAttribute.ConstructorArguments.Length > 0 && objectAttribute.ConstructorArguments[0].Value is bool b)
        {
            keyAsPropertyName = b;
        }

        // properties across the hierarchy first, then fields — mirrors the reflection
        // enumeration order MessagePack-CSharp's resolvers serialize maps in
        var members = new List<(ISymbol Symbol, ITypeSymbol Type, bool Settable)>();
        var seenNames = new HashSet<string>();
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IPropertySymbol { IsStatic: false, IsIndexer: false, IsImplicitlyDeclared: false } property
                    && property.DeclaredAccessibility == Accessibility.Public
                    && property.GetMethod is { DeclaredAccessibility: Accessibility.Public }
                    && seenNames.Add(property.Name))
                {
                    var settable = property.SetMethod is { DeclaredAccessibility: Accessibility.Public, IsInitOnly: false };
                    members.Add((property, property.Type, settable));
                }
            }
        }
        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object && current.SpecialType != SpecialType.System_ValueType; current = current.BaseType)
        {
            foreach (var member in current.GetMembers())
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } field
                    && field.DeclaredAccessibility == Accessibility.Public
                    && seenNames.Add(field.Name))
                {
                    members.Add((field, field.Type, !field.IsReadOnly));
                }
            }
        }

        var models = new List<MemberModel>();
        var hasIntKey = false;
        var hasStringKey = keyAsPropertyName;
        var intKeys = new HashSet<int>();
        var stringKeys = new HashSet<string>();
        var valid = true;

        foreach (var (symbol, memberType, settable) in members)
        {
            int? intKey = null;
            string? stringKey = null;
            var ignored = false;

            foreach (var attribute in symbol.GetAttributes())
            {
                var attributeName = attribute.AttributeClass?.ToDisplayString();
                if (attributeName == IgnoreMemberAttributeName)
                {
                    ignored = true;
                }
                else if (attributeName == KeyAttributeName && attribute.ConstructorArguments.Length > 0)
                {
                    var argument = attribute.ConstructorArguments[0].Value;
                    if (argument is int i)
                    {
                        intKey = i;
                    }
                    else if (argument is string s)
                    {
                        stringKey = s;
                    }
                }
            }

            if (ignored)
            {
                continue;
            }

            var memberLocation = LocationInfo.From(symbol);
            if (intKey is null && stringKey is null)
            {
                if (!keyAsPropertyName)
                {
                    diagnostics.Add(new DiagnosticInfo("UMP001", $"'{typeName}.{symbol.Name}' is a public member of a [MessagePackObject] type and needs [Key] or [IgnoreMember] (or use [MessagePackObject(true)]).", memberLocation));
                    valid = false;
                    continue;
                }
                stringKey = symbol.Name;
            }

            if (!settable)
            {
                diagnostics.Add(new DiagnosticInfo("UMP007", $"'{typeName}.{symbol.Name}' has a key but no accessible non-init setter, so it cannot be deserialized.", memberLocation));
                valid = false;
                continue;
            }

            if (intKey is { } key)
            {
                if (key < 0)
                {
                    diagnostics.Add(new DiagnosticInfo("UMP008", $"'{typeName}.{symbol.Name}' has a negative key ({key}).", memberLocation));
                    valid = false;
                    continue;
                }
                if (!intKeys.Add(key))
                {
                    diagnostics.Add(new DiagnosticInfo("UMP003", $"'{typeName}' declares key {key} more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasIntKey = true;
            }
            else
            {
                if (!stringKeys.Add(stringKey!))
                {
                    diagnostics.Add(new DiagnosticInfo("UMP003", $"'{typeName}' declares key \"{stringKey}\" more than once.", memberLocation));
                    valid = false;
                    continue;
                }
                hasStringKey = true;
            }

            models.Add(new MemberModel(
                Name: symbol.Name,
                TypeName: memberType.ToDisplayString(FullyQualifiedWithNullability),
                IntKey: intKey ?? -1,
                StringKey: stringKey ?? "",
                Direct: Classify(memberType)));
        }

        if (hasIntKey && hasStringKey)
        {
            diagnostics.Add(new DiagnosticInfo("UMP002", $"'{typeName}' mixes int keys and string keys; a type serializes as either an array (int keys) or a map (string keys), not both.", typeLocation));
            valid = false;
        }

        if (!valid)
        {
            return new ParseResult(null, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
        }

        var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var model = new ObjectModel(
            FullTypeName: fullTypeName,
            FormatterName: Sanitize(fullTypeName) + "Formatter",
            IsValueType: type.IsValueType,
            IsStringKey: hasStringKey,
            MaxIntKey: hasIntKey ? intKeys.Max() : -1,
            Members: new EquatableArray<MemberModel>([.. models]));
        return new ParseResult(model, new EquatableArray<DiagnosticInfo>([.. diagnostics]));
    }

    static DirectKind Classify(ITypeSymbol type) => type.SpecialType switch
    {
        SpecialType.System_Int32 => DirectKind.Int32,
        SpecialType.System_UInt32 => DirectKind.UInt32,
        SpecialType.System_Int64 => DirectKind.Int64,
        SpecialType.System_UInt64 => DirectKind.UInt64,
        SpecialType.System_Int16 => DirectKind.Int16,
        SpecialType.System_UInt16 => DirectKind.UInt16,
        SpecialType.System_Byte => DirectKind.Byte,
        SpecialType.System_SByte => DirectKind.SByte,
        SpecialType.System_Boolean => DirectKind.Boolean,
        SpecialType.System_Char => DirectKind.Char,
        SpecialType.System_Single => DirectKind.Single,
        SpecialType.System_Double => DirectKind.Double,
        SpecialType.System_String => DirectKind.String,
        SpecialType.System_DateTime => DirectKind.DateTime,
        _ => DirectKind.None,
    };

    static string Sanitize(string fullTypeName)
    {
        var builder = new StringBuilder(fullTypeName.Length);
        // drop the "global::" prefix, then flatten every separator
        var start = fullTypeName.StartsWith("global::", StringComparison.Ordinal) ? 8 : 0;
        for (int i = start; i < fullTypeName.Length; i++)
        {
            var c = fullTypeName[i];
            builder.Append(char.IsLetterOrDigit(c) ? c : '_');
        }
        return builder.ToString();
    }
}
