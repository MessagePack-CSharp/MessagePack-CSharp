// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace MessagePack.SourceGenerator.CodeAnalysis;

public class MessagePackGeneratorResolveFailedException : Exception
{
    public MessagePackGeneratorResolveFailedException(string message)
        : base(message)
    {
    }
}

public class TypeCollector
{
    private static readonly SymbolDisplayFormat BinaryWriteFormat = new SymbolDisplayFormat(
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
        miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

    private static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

    private static readonly HashSet<string> EmbeddedTypes = new(new[]
    {
        "short",
        "int",
        "long",
        "ushort",
        "uint",
        "ulong",
        "float",
        "double",
        "bool",
        "byte",
        "sbyte",
        "decimal",
        "char",
        "string",
        "object",
        "System.Guid",
        "System.TimeSpan",
        "System.DateTime",
        "System.DateTimeOffset",

        "MessagePack.Nil",

        // and arrays
        "short[]",
        "int[]",
        "long[]",
        "ushort[]",
        "uint[]",
        "ulong[]",
        "float[]",
        "double[]",
        "bool[]",
        "byte[]",
        "sbyte[]",
        "decimal[]",
        "char[]",
        "string[]",
        "System.DateTime[]",
        "System.ArraySegment<byte>",
        "System.ArraySegment<byte>?",

        // extensions
        "UnityEngine.Vector2",
        "UnityEngine.Vector3",
        "UnityEngine.Vector4",
        "UnityEngine.Quaternion",
        "UnityEngine.Color",
        "UnityEngine.Bounds",
        "UnityEngine.Rect",
        "UnityEngine.AnimationCurve",
        "UnityEngine.RectOffset",
        "UnityEngine.Gradient",
        "UnityEngine.WrapMode",
        "UnityEngine.GradientMode",
        "UnityEngine.Keyframe",
        "UnityEngine.Matrix4x4",
        "UnityEngine.GradientColorKey",
        "UnityEngine.GradientAlphaKey",
        "UnityEngine.Color32",
        "UnityEngine.LayerMask",
        "UnityEngine.Vector2Int",
        "UnityEngine.Vector3Int",
        "UnityEngine.RangeInt",
        "UnityEngine.RectInt",
        "UnityEngine.BoundsInt",

        "System.Reactive.Unit",
    });

    private static readonly Dictionary<string, string> KnownGenericTypes = new()
    {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
        { "System.Collections.Generic.List<>", "MsgPack::Formatters.ListFormatter<TREPLACE>" },
        { "System.Collections.Generic.LinkedList<>", "MsgPack::Formatters.LinkedListFormatter<TREPLACE>" },
        { "System.Collections.Generic.Queue<>", "MsgPack::Formatters.QueueFormatter<TREPLACE>" },
        { "System.Collections.Generic.Stack<>", "MsgPack::Formatters.StackFormatter<TREPLACE>" },
        { "System.Collections.Generic.HashSet<>", "MsgPack::Formatters.HashSetFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyCollection<>", "MsgPack::Formatters.ReadOnlyCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.IList<>", "MsgPack::Formatters.InterfaceListFormatter2<TREPLACE>" },
        { "System.Collections.Generic.ICollection<>", "MsgPack::Formatters.InterfaceCollectionFormatter2<TREPLACE>" },
        { "System.Collections.Generic.IEnumerable<>", "MsgPack::Formatters.InterfaceEnumerableFormatter<TREPLACE>" },
        { "System.Collections.Generic.Dictionary<,>", "MsgPack::Formatters.DictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.IDictionary<,>", "MsgPack::Formatters.InterfaceDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.SortedDictionary<,>", "MsgPack::Formatters.SortedDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.SortedList<,>", "MsgPack::Formatters.SortedListFormatter<TREPLACE>" },
        { "System.Linq.ILookup<,>", "MsgPack::Formatters.InterfaceLookupFormatter<TREPLACE>" },
        { "System.Linq.IGrouping<,>", "MsgPack::Formatters.InterfaceGroupingFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ObservableCollection<>", "MsgPack::Formatters.ObservableCollectionFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "MsgPack::Formatters.ReadOnlyObservableCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyList<>", "MsgPack::Formatters.InterfaceReadOnlyListFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyCollection<>", "MsgPack::Formatters.InterfaceReadOnlyCollectionFormatter<TREPLACE>" },
        { "System.Collections.Generic.ISet<>", "MsgPack::Formatters.InterfaceSetFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentBag<>", "MsgPack::Formatters.ConcurrentBagFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentQueue<>", "MsgPack::Formatters.ConcurrentQueueFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentStack<>", "MsgPack::Formatters.ConcurrentStackFormatter<TREPLACE>" },
        { "System.Collections.ObjectModel.ReadOnlyDictionary<,>", "MsgPack::Formatters.ReadOnlyDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Generic.IReadOnlyDictionary<,>", "MsgPack::Formatters.InterfaceReadOnlyDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Concurrent.ConcurrentDictionary<,>", "MsgPack::Formatters.ConcurrentDictionaryFormatter<TREPLACE>" },
        { "System.Lazy<>", "MsgPack::Formatters.LazyFormatter<TREPLACE>" },
        { "System.Threading.Tasks<>", "MsgPack::Formatters.TaskValueFormatter<TREPLACE>" },

        { "System.Tuple<>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },
        { "System.Tuple<,,,,,,,>", "MsgPack::Formatters.TupleFormatter<TREPLACE>" },

        { "System.ValueTuple<>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },
        { "System.ValueTuple<,,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter<TREPLACE>" },

        { "System.Collections.Generic.KeyValuePair<,>", "MsgPack::Formatters.KeyValuePairFormatter<TREPLACE>" },
        { "System.Threading.Tasks.ValueTask<>", "MsgPack::Formatters.KeyValuePairFormatter<TREPLACE>" },
        { "System.ArraySegment<>", "MsgPack::Formatters.ArraySegmentFormatter<TREPLACE>" },

        // extensions
        { "System.Collections.Immutable.ImmutableArray<>", "MsgPack::ImmutableCollection.ImmutableArrayFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableList<>", "MsgPack::ImmutableCollection.ImmutableListFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableDictionary<,>", "MsgPack::ImmutableCollection.ImmutableDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableHashSet<>", "MsgPack::ImmutableCollection.ImmutableHashSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableSortedDictionary<,>", "MsgPack::ImmutableCollection.ImmutableSortedDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableSortedSet<>", "MsgPack::ImmutableCollection.ImmutableSortedSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableQueue<>", "MsgPack::ImmutableCollection.ImmutableQueueFormatter<TREPLACE>" },
        { "System.Collections.Immutable.ImmutableStack<>", "MsgPack::ImmutableCollection.ImmutableStackFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableList<>", "MsgPack::ImmutableCollection.InterfaceImmutableListFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableDictionary<,>", "MsgPack::ImmutableCollection.InterfaceImmutableDictionaryFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableQueue<>", "MsgPack::ImmutableCollection.InterfaceImmutableQueueFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableSet<>", "MsgPack::ImmutableCollection.InterfaceImmutableSetFormatter<TREPLACE>" },
        { "System.Collections.Immutable.IImmutableStack<>", "MsgPack::ImmutableCollection.InterfaceImmutableStackFormatter<TREPLACE>" },

        { "Reactive.Bindings.ReactiveProperty<>", "MsgPack::ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.IReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.IReadOnlyReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>" },
        { "Reactive.Bindings.ReactiveCollection<>", "MsgPack::ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>" },
#pragma warning restore SA1509 // Opening braces should not be preceded by blank line
    };

    private readonly AnalyzerOptions options;
    private readonly ReferenceSymbols typeReferences;

    /// <summary>
    /// The means of reporting diagnostics to the analyzer.
    /// This will be <see langword="null" /> when running in the context of a source generator so as to avoid duplicate diagnostics.
    /// </summary>
    private readonly Action<Diagnostic>? reportDiagnostic;

    private readonly ITypeSymbol? targetType;
    private readonly bool excludeArrayElement;

    // visitor workspace:
#pragma warning disable RS1024 // Compare symbols correctly (https://github.com/dotnet/roslyn-analyzers/issues/5246)
    private readonly Dictionary<ITypeSymbol, bool> alreadyCollected = new(SymbolEqualityComparer.Default);
#pragma warning restore RS1024 // Compare symbols correctly
    private readonly ImmutableSortedSet<ObjectSerializationInfo>.Builder collectedObjectInfo = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<EnumSerializationInfo>.Builder collectedEnumInfo = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<GenericSerializationInfo>.Builder collectedGenericInfo = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<UnionSerializationInfo>.Builder collectedUnionInfo = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);

    private readonly Compilation compilation;

    private TypeCollector(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, ITypeSymbol targetType, Action<Diagnostic>? reportAnalyzerDiagnostic)
    {
        this.typeReferences = referenceSymbols;
        this.reportDiagnostic = reportAnalyzerDiagnostic;
        this.options = options;
        this.compilation = compilation;
        this.excludeArrayElement = true;

        if (IsAllowedAccessibility(targetType.DeclaredAccessibility))
        {
            if (((targetType.TypeKind == TypeKind.Interface) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)))
                || ((targetType.TypeKind == TypeKind.Class && targetType.IsAbstract) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)))
                || ((targetType.TypeKind == TypeKind.Class) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute)))
                || ((targetType.TypeKind == TypeKind.Struct) && targetType.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute))))
            {
                this.targetType = targetType;
            }
        }
    }

    public static FullModel? Collect(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, Action<Diagnostic>? reportAnalyzerDiagnostic, TypeDeclarationSyntax typeDeclaration, CancellationToken cancellationToken)
    {
        SemanticModel semanticModel = compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
        if (semanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) is ITypeSymbol typeSymbol)
        {
            if (Collect(compilation, options, referenceSymbols, reportAnalyzerDiagnostic, typeSymbol) is FullModel model)
            {
                return model;
            }
        }

        return null;
    }

    public static FullModel? Collect(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, Action<Diagnostic>? reportAnalyzerDiagnostic, ITypeSymbol targetType)
    {
        TypeCollector collector = new(compilation, options, referenceSymbols, targetType, reportAnalyzerDiagnostic);
        if (collector.targetType is null)
        {
            return null;
        }

        FullModel model = collector.Collect();
        return model;
    }

    private void ResetWorkspace()
    {
        this.alreadyCollected.Clear();
        this.collectedObjectInfo.Clear();
        this.collectedEnumInfo.Clear();
        this.collectedGenericInfo.Clear();
        this.collectedUnionInfo.Clear();
    }

    // EntryPoint
    public FullModel Collect()
    {
        this.ResetWorkspace();

        if (this.targetType is not null)
        {
            this.CollectCore(this.targetType);
        }

        return new FullModel(
            this.collectedObjectInfo.ToImmutable(),
            this.collectedEnumInfo.ToImmutable(),
            this.collectedGenericInfo.ToImmutable(),
            this.collectedUnionInfo.ToImmutable(),
            ImmutableSortedSet<CustomFormatterRegisterInfo>.Empty,
            this.options);
    }

    // Gate of recursive collect
    private bool CollectCore(ITypeSymbol typeSymbol)
    {
        if (this.alreadyCollected.TryGetValue(typeSymbol, out bool result))
        {
            return result;
        }

        var typeSymbolString = typeSymbol.GetCanonicalTypeFullName();
        if (EmbeddedTypes.Contains(typeSymbolString))
        {
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (this.options.AssumedFormattableTypes.Contains(typeSymbolString) is true)
        {
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            return RecursiveProtection(() => this.CollectArray((IArrayTypeSymbol)this.ToTupleUnderlyingType(arrayTypeSymbol)));
        }

        if (typeSymbol is ITypeParameterSymbol)
        {
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (!this.IsAllowAccessibility(typeSymbol))
        {
            result = false;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (!(typeSymbol is INamedTypeSymbol type))
        {
            result = false;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        var customFormatterAttr = typeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute));
        if (customFormatterAttr != null)
        {
            this.CheckValidMessagePackFormatterAttribute(customFormatterAttr);
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (type.EnumUnderlyingType != null)
        {
            return RecursiveProtection(() => this.CollectEnum(type, type.EnumUnderlyingType));
        }

        if (type.IsGenericType)
        {
            return RecursiveProtection(() => this.CollectGeneric((INamedTypeSymbol)this.ToTupleUnderlyingType(type)));
        }

        if (type.Locations[0].IsInMetadata)
        {
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (type.TypeKind == TypeKind.Interface || (type.TypeKind == TypeKind.Class && type.IsAbstract))
        {
            return RecursiveProtection(() => this.CollectUnion(type));
        }

        return RecursiveProtection(() => this.CollectObject(type));

        bool RecursiveProtection(Func<bool> func)
        {
            this.alreadyCollected.Add(typeSymbol, true);
            bool result = func();
            this.alreadyCollected[typeSymbol] = result;
            return result;
        }
    }

    private bool CollectEnum(INamedTypeSymbol type, ISymbol enumUnderlyingType)
    {
        EnumSerializationInfo info = new(
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            type.ToDisplayString(ShortTypeNameFormat).Replace(".", "_"),
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            enumUnderlyingType.ToDisplayString(BinaryWriteFormat));
        this.collectedEnumInfo.Add(info);
        return true;
    }

    private bool CollectUnion(INamedTypeSymbol type)
    {
        if (!options.IsGeneratingSource)
        {
            // In analyzer-only mode, this method doesn't work.
            return true;
        }

        ImmutableArray<TypedConstant>[] unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments).ToArray();
        if (unionAttrs.Length == 0)
        {
            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.UnionAttributeRequired, type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
        }

        // 0, Int  1, SubType
        UnionSubTypeInfo? UnionSubTypeInfoSelector(ImmutableArray<TypedConstant> x)
        {
            if (!(x[0] is { Value: int key }) || !(x[1] is { Value: ITypeSymbol typeSymbol }))
            {
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AotUnionAttributeRequiresTypeArg, GetIdentifierLocation(type)));
                return null;
            }

            CollectCore(typeSymbol);

            var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            return new UnionSubTypeInfo(key, typeName);
        }

        var info = new UnionSerializationInfo(
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            type.Name,
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            unionAttrs.Select(UnionSubTypeInfoSelector).Where(i => i is not null).OrderBy(x => x!.Key).ToArray()!);

        this.collectedUnionInfo.Add(info);
        return true;
    }

    private void CollectGenericUnion(INamedTypeSymbol type)
    {
        var unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments);
        using var enumerator = unionAttrs.GetEnumerator();
        if (!enumerator.MoveNext())
        {
            return;
        }

        do
        {
            var x = enumerator.Current;
            if (x[1] is { Value: INamedTypeSymbol unionType } && !this.alreadyCollected.ContainsKey(unionType))
            {
                this.CollectCore(unionType);
            }
        }
        while (enumerator.MoveNext());
    }

    private bool CollectArray(IArrayTypeSymbol array)
    {
        ITypeSymbol elemType = array.ElementType;
        if (!this.excludeArrayElement)
        {
            if (!this.CollectCore(elemType))
            {
                return false;
            }
        }

        var fullName = array.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var elementTypeDisplayName = elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string? formatterName;
        if (array.IsSZArray)
        {
            formatterName = "MsgPack::Formatters.ArrayFormatter<" + elementTypeDisplayName + ">";
        }
        else
        {
            formatterName = array.Rank switch
            {
                2 => "MsgPack::Formatters.TwoDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                3 => "MsgPack::Formatters.ThreeDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                4 => "MsgPack::Formatters.FourDimensionalArrayFormatter<" + elementTypeDisplayName + ">",
                _ => null,
            };
            if (formatterName is null)
            {
                ////this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AotArrayRankTooHigh));
                return false;
            }
        }

        var info = new GenericSerializationInfo(fullName, formatterName, null, elemType is ITypeParameterSymbol);
        this.collectedGenericInfo.Add(info);
        return true;
    }

    private ITypeSymbol ToTupleUnderlyingType(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is IArrayTypeSymbol array)
        {
            return this.compilation.CreateArrayTypeSymbol(this.ToTupleUnderlyingType(array.ElementType), array.Rank);
        }

        if (typeSymbol is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return typeSymbol;
        }

        namedType = namedType.TupleUnderlyingType ?? namedType;
        var newTypeArguments = namedType.TypeArguments.Select(this.ToTupleUnderlyingType).ToArray();
        if (!namedType.TypeArguments.SequenceEqual(newTypeArguments))
        {
            return namedType.ConstructedFrom.Construct(newTypeArguments);
        }

        return namedType;
    }

    private bool CollectGeneric(INamedTypeSymbol type)
    {
        INamedTypeSymbol genericType = type.ConstructUnboundGenericType();
        var genericTypeString = genericType.ToDisplayString();
        string? genericTypeNamespace = genericType.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isOpenGenericType = this.IsOpenGenericTypeRecursively(type);

        // special case
        if (fullName == "global::System.ArraySegment<byte>" || fullName == "global::System.ArraySegment<byte>?")
        {
            return true;
        }

        // nullable
        if (genericTypeString == "T?")
        {
            var firstTypeArgument = type.TypeArguments[0];
            if (!this.CollectCore(firstTypeArgument))
            {
                return false;
            }

            if (EmbeddedTypes.Contains(firstTypeArgument.ToString()!))
            {
                return true;
            }

            var info = new GenericSerializationInfo(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                "MsgPack::Formatters.NullableFormatter<" + firstTypeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + ">",
                Namespace: null,
                isOpenGenericType);
            this.collectedGenericInfo.Add(info);
            return true;
        }

        // collection
        if (KnownGenericTypes.TryGetValue(genericTypeString, out var formatter))
        {
            foreach (ITypeSymbol item in type.TypeArguments)
            {
                this.CollectCore(item);
            }

            var typeArgs = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
            var f = formatter.Replace("TREPLACE", typeArgs);

            var info = new GenericSerializationInfo(
                type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                f,
                null,
                isOpenGenericType);

            this.collectedGenericInfo.Add(info);

            if (genericTypeString != "System.Linq.ILookup<,>")
            {
                return true;
            }

            formatter = KnownGenericTypes["System.Linq.IGrouping<,>"];
            f = formatter.Replace("TREPLACE", typeArgs);

            var groupingInfo = new GenericSerializationInfo(
                "global::System.Linq.IGrouping<" + typeArgs + ">",
                f,
                genericTypeNamespace,
                isOpenGenericType);
            this.collectedGenericInfo.Add(groupingInfo);

            formatter = KnownGenericTypes["System.Collections.Generic.IEnumerable<>"];
            typeArgs = type.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            f = formatter.Replace("TREPLACE", typeArgs);

            var enumerableInfo = new GenericSerializationInfo(
                "global::System.Collections.Generic.IEnumerable<" + typeArgs + ">",
                f,
                genericTypeNamespace,
                isOpenGenericType);
            this.collectedGenericInfo.Add(enumerableInfo);
            return true;
        }

        // Generic types
        if (type.IsDefinition)
        {
            this.CollectGenericUnion(type);
            this.CollectObject(type);
            return true;
        }
        else
        {
            // Collect substituted types for the properties and fields.
            // NOTE: It is used to register formatters from nested generic type.
            //       However, closed generic types such as `Foo<string>` are not registered as a formatter.
            this.GetObjectInfo(type);

            // Collect generic type definition, that is not collected when it is defined outside target project.
            this.CollectCore(type.OriginalDefinition);
        }

        // Collect substituted types for the type parameters (e.g. Bar in Foo<Bar>)
        foreach (var item in type.TypeArguments)
        {
            if (!this.CollectCore(item))
            {
                return false;
            }
        }

        var formatterBuilder = new StringBuilder();
        formatterBuilder.Append(type.Name);
        formatterBuilder.Append("Formatter<");
        var typeArgumentIterator = type.TypeArguments.GetEnumerator();
        {
            if (typeArgumentIterator.MoveNext())
            {
                formatterBuilder.Append(typeArgumentIterator.Current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            while (typeArgumentIterator.MoveNext())
            {
                formatterBuilder.Append(", ");
                formatterBuilder.Append(typeArgumentIterator.Current.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }
        }

        formatterBuilder.Append('>');

        var genericSerializationInfo = new GenericSerializationInfo(
            type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            formatterBuilder.ToString(),
            genericTypeNamespace,
            isOpenGenericType);
        this.collectedGenericInfo.Add(genericSerializationInfo);
        return true;
    }

    private bool CollectObject(INamedTypeSymbol type)
    {
        ObjectSerializationInfo? info = this.GetObjectInfo(type);
        if (info is not null)
        {
            this.collectedObjectInfo.Add(info);
        }

        return info is not null;
    }

    private bool CheckValidMessagePackFormatterAttribute(AttributeData formatterAttribute)
    {
        if (formatterAttribute.ConstructorArguments[0].Value is ITypeSymbol formatterType)
        {
            // Validate that the typed formatter is actually of `IMessagePackFormatter`
            bool isMessagePackFormatter = formatterType.AllInterfaces.Any(x => SymbolEqualityComparer.Default.Equals(x, this.typeReferences.MessagePackFormatter));
            if (!isMessagePackFormatter)
            {
                Location? location = ((AttributeSyntax?)formatterAttribute.ApplicationSyntaxReference?.GetSyntax())?.ArgumentList?.Arguments[0].GetLocation();
                ImmutableDictionary<string, string?> typeInfo = ImmutableDictionary.Create<string, string?>().Add("type", formatterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.MessageFormatterMustBeMessagePackFormatter, location, typeInfo));
            }

            return isMessagePackFormatter;
        }

        return false;
    }

    private ObjectSerializationInfo? GetObjectInfo(INamedTypeSymbol type)
    {
        var isClass = !type.IsValueType;
        var isOpenGenericType = type.IsGenericType;

        AttributeData? contractAttr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute));
        if (contractAttr is null)
        {
            ////this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject, ((BaseTypeDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        var isIntKey = true;
        var intMembers = new Dictionary<int, MemberSerializationInfo>();
        var stringMembers = new Dictionary<string, MemberSerializationInfo>();

        if (this.options.Generator.Formatters.UsesMapMode || (contractAttr?.ConstructorArguments[0] is { Value: bool firstConstructorArgument } && firstConstructorArgument))
        {
            // All public members are serialize target except [Ignore] member.
            isIntKey = false;

            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>().Where(x => !x.IsOverride))
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)))
                {
                    continue;
                }

                var isReadable = item.GetMethod != null && IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = item.SetMethod != null && IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility) && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                stringMembers.Add(member.StringKey, member);

                if (customFormatterAttr == null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }

            foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)))
                {
                    continue;
                }

                if (item.IsImplicitlyDeclared)
                {
                    continue;
                }

                var isReadable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsReadOnly && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                var member = new MemberSerializationInfo(false, isWritable, isReadable, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                stringMembers.Add(member.StringKey, member);
                if (customFormatterAttr == null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }
        }
        else
        {
            // Only KeyAttribute members
            var searchFirst = true;
            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>())
            {
                if (item.IsIndexer)
                {
                    continue; // .tt files don't generate good code for this yet: https://github.com/neuecc/MessagePack-CSharp/issues/390
                }

                if (item.GetAttributes().Any(x =>
                {
                    var typeReferencesIgnoreDataMemberAttribute = this.typeReferences.IgnoreDataMemberAttribute;
                    return typeReferencesIgnoreDataMemberAttribute != null && (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.ApproximatelyEqual(typeReferencesIgnoreDataMemberAttribute));
                }))
                {
                    continue;
                }

                var isReadable = item.GetMethod != null && IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = item.SetMethod != null && IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility) && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(item.ContainingType, type))
                        {
                            var syntax = item.DeclaringSyntaxReferences[0].GetSyntax();
                            var identifier = (syntax as PropertyDeclarationSyntax)?.Identifier ?? (syntax as ParameterSyntax)?.Identifier;

                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PublicMemberNeedsKey, identifier?.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                        }
                        else if (type.BaseType is not null)
                        {
                            // The member was inherited, so we raise a special error at the location of the base type reference.
                            BaseTypeSyntax? baseSyntax = type.DeclaringSyntaxReferences.SelectMany(sr => (IEnumerable<BaseTypeSyntax>?)((BaseTypeDeclarationSyntax)sr.GetSyntax()).BaseList?.Types ?? Array.Empty<BaseTypeSyntax>())
                                .FirstOrDefault(bt => SymbolEqualityComparer.Default.Equals(this.compilation.GetSemanticModel(bt.SyntaxTree).GetTypeInfo(bt.Type).Type, item.ContainingType));
                            if (baseSyntax is not null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(
                                    MsgPack00xMessagePackAnalyzer.BaseTypeContainsUnattributedPublicMembers,
                                    baseSyntax.GetLocation(),
                                    item.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                    item.Name));
                            }
                        }
                    }
                }
                else
                {
                    var intKey = key is { Value: int intKeyValue } ? intKeyValue : default(int?);
                    var stringKey = key is { Value: string stringKeyValue } ? stringKeyValue : default;
                    if (intKey == null && stringKey == null)
                    {
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.BothStringAndIntKeyAreNull, ((PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = intKey != null;
                    }
                    else
                    {
                        if ((isIntKey && intKey == null) || (!isIntKey && stringKey == null))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DoNotMixStringAndIntKeys, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMembers.ContainsKey(intKey!.Value))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        intMembers.Add(member.IntKey, member);
                    }
                    else if (stringKey is not null)
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                var messagePackFormatter = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0];

                if (messagePackFormatter == null)
                {
                    // recursive collect
                    if (!this.CollectCore(item.Type))
                    {
                        var syntax = item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();

                        var typeSyntax = (syntax as PropertyDeclarationSyntax)?.Type
                            ?? (syntax as ParameterSyntax)?.Type; // for primary constructor

                        // TODO: add the declaration of the referenced type as an additional location.
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(
                            MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject,
                            typeSyntax?.GetLocation(),
                            item.Type.ToDisplayString(ShortTypeNameFormat)));
                    }
                }
            }

            foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute)))
                {
                    continue;
                }

                var isReadable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsStatic;
                var isWritable = IsAllowedAccessibility(item.DeclaredAccessibility) && !item.IsReadOnly && !item.IsStatic;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PublicMemberNeedsKey, item.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }
                }
                else
                {
                    var intKey = key is { Value: int intKeyValue } ? intKeyValue : default(int?);
                    var stringKey = key is { Value: string stringKeyValue } ? stringKeyValue : default;
                    if (intKey == null && stringKey == null)
                    {
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.BothStringAndIntKeyAreNull, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                    }

                    if (searchFirst)
                    {
                        searchFirst = false;
                        isIntKey = intKey != null;
                    }
                    else
                    {
                        if ((isIntKey && intKey == null) || (!isIntKey && stringKey == null))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DoNotMixStringAndIntKeys, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMembers.ContainsKey(intKey!.Value))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                this.CollectCore(item.Type); // recursive collect
            }
        }

        // GetConstructor
        var ctorEnumerator = default(IEnumerator<IMethodSymbol>);
        var ctor = type.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass != null && y.AttributeClass.ApproximatelyEqual(this.typeReferences.SerializationConstructorAttribute)));
        if (ctor == null)
        {
            ctorEnumerator = type.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).OrderByDescending(x => x.Parameters.Length).GetEnumerator();

            if (ctorEnumerator.MoveNext())
            {
                ctor = ctorEnumerator.Current;
            }
        }

        // struct allows null ctor
        if (ctor == null && isClass)
        {
            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.NoDeserializingConstructor, GetIdentifierLocation(type)));
        }

        var constructorParameters = new List<MemberSerializationInfo>();
        if (ctor != null)
        {
            var constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
            do
            {
                constructorParameters.Clear();
                var ctorParamIndex = 0;
                foreach (IParameterSymbol item in ctor!.Parameters)
                {
                    MemberSerializationInfo paramMember;
                    if (isIntKey)
                    {
                        if (intMembers.TryGetValue(ctorParamIndex, out paramMember!))
                        {
                            if (item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == paramMember.Type && paramMember.IsReadable)
                            {
                                constructorParameters.Add(paramMember);
                            }
                            else
                            {
                                if (ctorEnumerator != null)
                                {
                                    ctor = null;
                                    continue;
                                }
                                else
                                {
                                    this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterTypeMismatch, GetLocation(item)));
                                }
                            }
                        }
                        else
                        {
                            if (ctorEnumerator != null)
                            {
                                ctor = null;
                                continue;
                            }
                            else
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterIndexMissing, GetParameterListLocation(ctor)));
                            }
                        }
                    }
                    else
                    {
                        IEnumerable<KeyValuePair<string, MemberSerializationInfo>> hasKey = constructorLookupDictionary[item.Name];
                        using var enumerator = hasKey.GetEnumerator();

                        // hasKey.Count() == 0
                        if (!enumerator.MoveNext())
                        {
                            if (ctorEnumerator == null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterNameMissing, GetParameterListLocation(ctor)));
                            }

                            ctor = null;
                            continue;
                        }

                        var first = enumerator.Current.Value;

                        // hasKey.Count() != 1
                        if (enumerator.MoveNext())
                        {
                            if (ctorEnumerator == null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterNameDuplicate, GetLocation(item)));
                            }

                            ctor = null;
                            continue;
                        }

                        paramMember = first;
                        if (item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == paramMember.Type && paramMember.IsReadable)
                        {
                            constructorParameters.Add(paramMember);
                        }
                        else
                        {
                            if (ctorEnumerator == null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterTypeMismatch, GetLocation(item)));
                            }

                            ctor = null;
                            continue;
                        }
                    }

                    ctorParamIndex++;
                }
            }
            while (TryGetNextConstructor(ctorEnumerator, ref ctor));

            if (ctor == null)
            {
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.NoDeserializingConstructor, GetIdentifierLocation(type)));
            }
        }

        var hasSerializationConstructor = type.AllInterfaces.Any(x => x.ApproximatelyEqual(this.typeReferences.IMessagePackSerializationCallbackReceiver));
        var needsCastOnBefore = true;
        var needsCastOnAfter = true;
        if (hasSerializationConstructor)
        {
            needsCastOnBefore = !type.GetMembers("OnBeforeSerialize").Any();
            needsCastOnAfter = !type.GetMembers("OnAfterDeserialize").Any();
        }

        if (contractAttr is null)
        {
            // Indicate to our caller that we don't have a valid object.
            return null;
        }

        // Do not source generate the formatter for this type if the attribute opted out.
        if (contractAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == Constants.SuppressSourceGenerationPropertyName).Value.Value is true)
        {
            // Skip any source generation
            return null;
        }

        ObjectSerializationInfo info = new(isClass, isOpenGenericType, isOpenGenericType ? type.TypeParameters.Select(ToGenericTypeParameterInfo).ToArray() : Array.Empty<GenericTypeParameterInfo>(), constructorParameters.ToArray(), isIntKey, isIntKey ? intMembers.Values.ToArray() : stringMembers.Values.ToArray(), isOpenGenericType ? GetGenericFormatterClassName(type) : GetMinimallyQualifiedClassName(type), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(), hasSerializationConstructor, needsCastOnAfter, needsCastOnBefore)
        {
        };

        return info;
    }

    private static GenericTypeParameterInfo ToGenericTypeParameterInfo(ITypeParameterSymbol typeParameter)
    {
        var constraints = new List<string>();

        // `notnull`, `unmanaged`, `class`, `struct` constraint must come before any constraints.
        if (typeParameter.HasNotNullConstraint)
        {
            constraints.Add("notnull");
        }

        if (typeParameter.HasReferenceTypeConstraint)
        {
            constraints.Add(typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated ? "class?" : "class");
        }

        if (typeParameter.HasValueTypeConstraint)
        {
            constraints.Add(typeParameter.HasUnmanagedTypeConstraint ? "unmanaged" : "struct");
        }

        // constraint types (IDisposable, IEnumerable ...)
        foreach (var t in typeParameter.ConstraintTypes)
        {
            var constraintTypeFullName = t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier));
            constraints.Add(constraintTypeFullName);
        }

        // `new()` constraint must be last in constraints.
        if (typeParameter.HasConstructorConstraint)
        {
            constraints.Add("new()");
        }

        return new GenericTypeParameterInfo(typeParameter.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), string.Join(", ", constraints));
    }

    private static Location? GetIdentifierLocation(INamedTypeSymbol type) => ((BaseTypeDeclarationSyntax?)type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax())?.Identifier.GetLocation();

    private static Location? GetLocation(IParameterSymbol parameter) => parameter.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation();

    private static Location? GetParameterListLocation(IMethodSymbol? method) => ((BaseMethodDeclarationSyntax?)method?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax())?.ParameterList.GetLocation();

    private static string GetGenericFormatterClassName(INamedTypeSymbol type)
    {
        return type.Name;
    }

    private static string GetMinimallyQualifiedClassName(INamedTypeSymbol type)
    {
        var name = type.ContainingType is object ? GetMinimallyQualifiedClassName(type.ContainingType) + "_" : string.Empty;
        name += type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        name = name.Replace('.', '_');
        name = name.Replace('<', '_');
        name = name.Replace('>', '_');
        name = Regex.Replace(name, @"\[([,])*\]", match => $"Array{match.Length - 1}");
        name = name.Replace("?", string.Empty);
        return name;
    }

    private static bool TryGetNextConstructor(IEnumerator<IMethodSymbol>? ctorEnumerator, ref IMethodSymbol? ctor)
    {
        if (ctorEnumerator == null || ctor != null)
        {
            return false;
        }

        if (ctorEnumerator.MoveNext())
        {
            ctor = ctorEnumerator.Current;
            return true;
        }
        else
        {
            ctor = null;
            return false;
        }
    }

    private static bool IsAllowedAccessibility(Accessibility accessibility) => accessibility is Accessibility.Public or Accessibility.Internal;

    private bool IsAllowAccessibility(ITypeSymbol symbol)
    {
        do
        {
            if (!IsAllowedAccessibility(symbol.DeclaredAccessibility))
            {
                return false;
            }

            symbol = symbol.ContainingType;
        }
        while (symbol is not null);

        return true;
    }

    private bool IsOpenGenericTypeRecursively(INamedTypeSymbol type)
    {
        return type.IsGenericType && type.TypeArguments.Any(x => x is ITypeParameterSymbol || (x is INamedTypeSymbol symbol && this.IsOpenGenericTypeRecursively(symbol)));
    }
}
