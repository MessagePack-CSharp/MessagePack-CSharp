// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Collections.Immutable;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        "object[]",
        "System.DateTime[]",
        "System.ArraySegment<byte>",
        "System.Memory<byte>",
        "System.ReadOnlyMemory<byte>",
        "System.Buffers.ReadOnlySequence<byte>",

        // List
        "System.Collections.Generic.List<short>",
        "System.Collections.Generic.List<int>",
        "System.Collections.Generic.List<long>",
        "System.Collections.Generic.List<ushort>",
        "System.Collections.Generic.List<uint>",
        "System.Collections.Generic.List<ulong>",
        "System.Collections.Generic.List<float>",
        "System.Collections.Generic.List<double>",
        "System.Collections.Generic.List<bool>",
        "System.Collections.Generic.List<byte>",
        "System.Collections.Generic.List<sbyte>",
        "System.Collections.Generic.List<char>",
        "System.Collections.Generic.List<string>",
        "System.Collections.Generic.List<object>",
        "System.Collections.Generic.List<System.DateTime>",

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

        "System.Numerics.BigInteger",
        "System.Numerics.Complex",
        "System.Numerics.Vector2",
        "System.Numerics.Vector3",
        "System.Numerics.Vector4",
        "System.Numerics.Quaternion",
        "System.Numerics.Matrix3x2",
        "System.Numerics.Matrix4x4",
    });

    private static readonly Dictionary<string, string> KnownGenericTypes = new()
    {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
        { "System.Collections.Generic.List<>", "MsgPack::Formatters.ListFormatter" },
        { "System.Collections.Generic.LinkedList<>", "MsgPack::Formatters.LinkedListFormatter" },
        { "System.Collections.Generic.Queue<>", "MsgPack::Formatters.QueueFormatter" },
        { "System.Collections.Generic.Stack<>", "MsgPack::Formatters.StackFormatter" },
        { "System.Collections.Generic.HashSet<>", "MsgPack::Formatters.HashSetFormatter" },
        { "System.Collections.ObjectModel.ReadOnlyCollection<>", "MsgPack::Formatters.ReadOnlyCollectionFormatter" },
        { "System.Collections.Generic.IList<>", "MsgPack::Formatters.InterfaceListFormatter2" },
        { "System.Collections.Generic.ICollection<>", "MsgPack::Formatters.InterfaceCollectionFormatter2" },
        { "System.Collections.Generic.IEnumerable<>", "MsgPack::Formatters.InterfaceEnumerableFormatter" },
        { "System.Collections.Generic.Dictionary<,>", "MsgPack::Formatters.DictionaryFormatter" },
        { "System.Collections.Generic.IDictionary<,>", "MsgPack::Formatters.InterfaceDictionaryFormatter" },
        { "System.Collections.Generic.SortedDictionary<,>", "MsgPack::Formatters.SortedDictionaryFormatter" },
        { "System.Collections.Generic.SortedList<,>", "MsgPack::Formatters.SortedListFormatter" },
        { "System.Linq.ILookup<,>", "MsgPack::Formatters.InterfaceLookupFormatter" },
        { "System.Linq.IGrouping<,>", "MsgPack::Formatters.InterfaceGroupingFormatter" },
        { "System.Collections.ObjectModel.ObservableCollection<>", "MsgPack::Formatters.ObservableCollectionFormatter" },
        { "System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "MsgPack::Formatters.ReadOnlyObservableCollectionFormatter" },
        { "System.Collections.Generic.IReadOnlyList<>", "MsgPack::Formatters.InterfaceReadOnlyListFormatter" },
        { "System.Collections.Generic.IReadOnlyCollection<>", "MsgPack::Formatters.InterfaceReadOnlyCollectionFormatter" },
        { "System.Collections.Generic.ISet<>", "MsgPack::Formatters.InterfaceSetFormatter" },
        { "System.Collections.Concurrent.ConcurrentBag<>", "MsgPack::Formatters.ConcurrentBagFormatter" },
        { "System.Collections.Concurrent.ConcurrentQueue<>", "MsgPack::Formatters.ConcurrentQueueFormatter" },
        { "System.Collections.Concurrent.ConcurrentStack<>", "MsgPack::Formatters.ConcurrentStackFormatter" },
        { "System.Collections.ObjectModel.ReadOnlyDictionary<,>", "MsgPack::Formatters.ReadOnlyDictionaryFormatter" },
        { "System.Collections.Generic.IReadOnlyDictionary<,>", "MsgPack::Formatters.InterfaceReadOnlyDictionaryFormatter" },
        { "System.Collections.Concurrent.ConcurrentDictionary<,>", "MsgPack::Formatters.ConcurrentDictionaryFormatter" },
        // NET5
        { "System.Collections.Generic.IReadOnlySet<>", "MsgPack::Formatters.InterfaceReadOnlySetFormatter" },
        // NET6
        { "System.Collections.Generic.PriorityQueue<,>", "MsgPack::Formatters.PriorityQueueFormatter" },
        // NET9
        { "System.Collections.Generic.OrderedDictionary<,>", "MsgPack::Formatters.OrderedDictionaryFormatter" },
        { "System.Collections.ObjectModel.ReadOnlySet<>", "MsgPack::Formatters.ReadOnlySetFormatter" },

        { "System.Lazy<>", "MsgPack::Formatters.LazyFormatter" },
        { "System.Threading.Tasks<>", "MsgPack::Formatters.TaskValueFormatter" },

        { "System.Tuple<>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,,,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,,,,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,,,,,>", "MsgPack::Formatters.TupleFormatter" },
        { "System.Tuple<,,,,,,,>", "MsgPack::Formatters.TupleFormatter" },

        { "System.ValueTuple<>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,,,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,,,,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter" },
        { "System.ValueTuple<,,,,,,,>", "MsgPack::Formatters.ValueTupleFormatter" },

        { "System.Collections.Generic.KeyValuePair<,>", "MsgPack::Formatters.KeyValuePairFormatter" },
        { "System.Threading.Tasks.ValueTask<>", "MsgPack::Formatters.KeyValuePairFormatter" },
        { "System.ArraySegment<>", "MsgPack::Formatters.ArraySegmentFormatter" },
        { "System.Memory<>", "MsgPack::Formatters.MemoryFormatter" },
        { "System.ReadOnlyMemory<>", "MsgPack::Formatters.ReadOnlyMemoryFormatter" },
        { "System.Buffers.ReadOnlySequence<>", "MsgPack::Formatters.ReadOnlySequenceFormatter" },

        // extensions
        { "System.Collections.Immutable.ImmutableArray<>", "MsgPack::ImmutableCollection.ImmutableArrayFormatter" },
        { "System.Collections.Immutable.ImmutableList<>", "MsgPack::ImmutableCollection.ImmutableListFormatter" },
        { "System.Collections.Immutable.ImmutableDictionary<,>", "MsgPack::ImmutableCollection.ImmutableDictionaryFormatter" },
        { "System.Collections.Immutable.ImmutableHashSet<>", "MsgPack::ImmutableCollection.ImmutableHashSetFormatter" },
        { "System.Collections.Immutable.ImmutableSortedDictionary<,>", "MsgPack::ImmutableCollection.ImmutableSortedDictionaryFormatter" },
        { "System.Collections.Immutable.ImmutableSortedSet<>", "MsgPack::ImmutableCollection.ImmutableSortedSetFormatter" },
        { "System.Collections.Immutable.ImmutableQueue<>", "MsgPack::ImmutableCollection.ImmutableQueueFormatter" },
        { "System.Collections.Immutable.ImmutableStack<>", "MsgPack::ImmutableCollection.ImmutableStackFormatter" },
        { "System.Collections.Immutable.IImmutableList<>", "MsgPack::ImmutableCollection.InterfaceImmutableListFormatter" },
        { "System.Collections.Immutable.IImmutableDictionary<,>", "MsgPack::ImmutableCollection.InterfaceImmutableDictionaryFormatter" },
        { "System.Collections.Immutable.IImmutableQueue<>", "MsgPack::ImmutableCollection.InterfaceImmutableQueueFormatter" },
        { "System.Collections.Immutable.IImmutableSet<>", "MsgPack::ImmutableCollection.InterfaceImmutableSetFormatter" },
        { "System.Collections.Immutable.IImmutableStack<>", "MsgPack::ImmutableCollection.InterfaceImmutableStackFormatter" },

        { "Reactive.Bindings.ReactiveProperty<>", "MsgPack::ReactivePropertyExtension.ReactivePropertyFormatter" },
        { "Reactive.Bindings.IReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReactivePropertyFormatter" },
        { "Reactive.Bindings.IReadOnlyReactiveProperty<>", "MsgPack::ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter" },
        { "Reactive.Bindings.ReactiveCollection<>", "MsgPack::ReactivePropertyExtension.ReactiveCollectionFormatter" },
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

    // visitor workspace:
    private readonly HashSet<ITypeSymbol> alreadyCollected = new(SymbolEqualityComparer.Default);
    private readonly ImmutableSortedSet<ObjectSerializationInfo>.Builder collectedObjectInfo = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<EnumSerializationInfo>.Builder collectedEnumInfo = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<GenericSerializationInfo>.Builder collectedGenericInfo = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<UnionSerializationInfo>.Builder collectedUnionInfo = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<ResolverRegisterInfo>.Builder collectedArrayInfo = ImmutableSortedSet.CreateBuilder<ResolverRegisterInfo>(ResolverRegisterInfoComparer.Default);

    private readonly Compilation compilation;

    private readonly CancellationToken cancellationToken;

    private TypeCollector(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, ITypeSymbol targetType, Action<Diagnostic>? reportAnalyzerDiagnostic, CancellationToken cancellationToken)
    {
        this.typeReferences = referenceSymbols;
        this.reportDiagnostic = reportAnalyzerDiagnostic;
        this.options = options;
        this.compilation = compilation;
        this.cancellationToken = cancellationToken;

        bool isInaccessible = false;
        foreach (BaseTypeDeclarationSyntax? decl in CodeAnalysisUtilities.FindInaccessibleTypes(targetType))
        {
            isInaccessible = true;
            reportAnalyzerDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.InaccessibleDataType, decl.Identifier.GetLocation()));
        }

        if (!isInaccessible)
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

    public static FullModel? Collect(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, Action<Diagnostic>? reportAnalyzerDiagnostic, ITypeSymbol targetType, CancellationToken cancellationToken)
    {
        TypeCollector collector = new(compilation, options, referenceSymbols, targetType, reportAnalyzerDiagnostic, cancellationToken);
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
        this.collectedArrayInfo.Clear();
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
            this.collectedArrayInfo.ToImmutable(),
            this.options);
    }

    // Gate of recursive collect
    private void CollectCore(ITypeSymbol typeSymbol, ISymbol? callerSymbol = null)
    {
        this.cancellationToken.ThrowIfCancellationRequested();

        if (!this.alreadyCollected.Add(typeSymbol))
        {
            return;
        }

        var typeSymbolString = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToString();
        if (!string.IsNullOrEmpty(typeSymbolString) && EmbeddedTypes.Contains(typeSymbolString))
        {
            return;
        }

        FormattableType formattableType = new(typeSymbol, null);
        if (this.options.AssumedFormattableTypes.Contains(formattableType) || this.options.AssumedFormattableTypes.Contains(formattableType with { IsFormatterInSameAssembly = true }))
        {
            return;
        }

        if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
        {
            this.CollectArray((IArrayTypeSymbol)this.ToTupleUnderlyingType(arrayTypeSymbol), callerSymbol);
            return;
        }

        if (typeSymbol is ITypeParameterSymbol)
        {
            return;
        }

        if (!IsAllowAccessibility(typeSymbol))
        {
            return;
        }

        if (!(typeSymbol is INamedTypeSymbol type))
        {
            return;
        }

        var customFormatterAttr = typeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute));
        if (customFormatterAttr != null)
        {
            this.CheckValidMessagePackFormatterAttribute(customFormatterAttr);
            return;
        }

        if (type.EnumUnderlyingType != null)
        {
            this.CollectEnum(type, type.EnumUnderlyingType);
            return;
        }

        if (type.IsGenericType)
        {
            this.CollectGeneric((INamedTypeSymbol)this.ToTupleUnderlyingType(type), callerSymbol);
            return;
        }

        if (type.Locations[0].IsInMetadata)
        {
            return;
        }

        if (type.TypeKind == TypeKind.Interface || (type.TypeKind == TypeKind.Class && type.IsAbstract))
        {
            this.CollectUnion(type);
            return;
        }

        this.CollectObject(type, callerSymbol);
    }

    private void CollectEnum(INamedTypeSymbol type, ISymbol enumUnderlyingType)
    {
        this.collectedEnumInfo.Add(EnumSerializationInfo.Create(type, enumUnderlyingType, this.options.Generator.Resolver));
    }

    private void CollectUnion(INamedTypeSymbol type)
    {
        ImmutableArray<TypedConstant>[] unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments).ToArray();
        if (unionAttrs.Length == 0)
        {
            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.UnionAttributeRequired, type.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
            return;
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

        var subTypes = unionAttrs.Select(UnionSubTypeInfoSelector).Where(i => i is not null).OrderBy(x => x!.Key).ToImmutableArray();

        if (!options.IsGeneratingSource)
        {
            return;
        }

        var info = UnionSerializationInfo.Create(
            type,
            subTypes!,
            this.options.Generator.Resolver);

        this.collectedUnionInfo.Add(info);
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
            if (x[1] is { Value: INamedTypeSymbol unionType } && !this.alreadyCollected.Contains(unionType))
            {
                this.CollectCore(unionType);
            }
        }
        while (enumerator.MoveNext());
    }

    private void CollectArray(IArrayTypeSymbol array, ISymbol? callerSymbol)
    {
        ITypeSymbol elemType = array.ElementType;

        this.CollectCore(elemType, callerSymbol);

        if (elemType is ITypeParameterSymbol || (elemType is INamedTypeSymbol symbol && IsOpenGenericTypeRecursively(symbol)))
        {
            return;
        }

        QualifiedTypeName elementTypeName = QualifiedTypeName.Create(elemType);
        string? formatterName = array.Rank switch
        {
            1 => "ArrayFormatter",
            2 => "TwoDimensionalArrayFormatter",
            3 => "ThreeDimensionalArrayFormatter",
            4 => "FourDimensionalArrayFormatter",
            _ => null,
        };
        if (formatterName is null)
        {
            ////this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AotArrayRankTooHigh));
            return;
        }

        var info = ResolverRegisterInfo.CreateArray(array, this.options.Generator.Resolver) with
        {
            Formatter = new QualifiedNamedTypeName(TypeKind.Class)
            {
                Container = new NamespaceTypeContainer("MsgPack::Formatters"),
                Name = formatterName,
                TypeParameters = ImmutableArray.Create(new GenericTypeParameterInfo("T")),
                TypeArguments = ImmutableArray.Create(elementTypeName),
            },
        };
        this.collectedArrayInfo.Add(info);
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

    private void CollectGeneric(INamedTypeSymbol type, ISymbol? callerSymbol)
    {
        INamedTypeSymbol typeDefinition = type.ConstructUnboundGenericType();
        var genericTypeDefinitionString = typeDefinition.ToDisplayString();
        string? genericTypeNamespace = type.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var isOpenGenericType = IsOpenGenericTypeRecursively(type);
        string formattedTypeFullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string? formatterName;

        // nullable
        if (genericTypeDefinitionString == "T?")
        {
            var firstTypeArgument = type.TypeArguments[0];

            if (EmbeddedTypes.Contains(firstTypeArgument.ToString()!))
            {
                return;
            }

            this.CollectCore(firstTypeArgument, callerSymbol);

            if (!isOpenGenericType)
            {
                formatterName = $"NullableFormatter<{firstTypeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
                var info = GenericSerializationInfo.Create(type, this.options.Generator.Resolver) with
                {
                    Formatter = new QualifiedNamedTypeName(TypeKind.Class)
                    {
                        Name = formatterName,
                        Container = new NamespaceTypeContainer("MsgPack::Formatters"),
                    },
                };
                this.collectedGenericInfo.Add(info);
            }

            return;
        }

        // collection
        string? formatterFullName;
        if (KnownGenericTypes.TryGetValue(genericTypeDefinitionString, out formatterFullName))
        {
            foreach (ITypeSymbol item in type.TypeArguments)
            {
                this.CollectCore(item, callerSymbol);
            }

            GenericSerializationInfo info;

            if (isOpenGenericType)
            {
                return;
            }
            else
            {
                info = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
                int indexOfLastPeriod = formatterFullName.LastIndexOf('.');
                info = info with
                {
                    Formatter = new(TypeKind.Class)
                    {
                        Container = new NamespaceTypeContainer(formatterFullName.Substring(0, indexOfLastPeriod)),
                        Name = formatterFullName.Substring(indexOfLastPeriod + 1),
                        TypeParameters = GetTypeParameters(info.DataType),
                        TypeArguments = GetTypeArguments(info.DataType),
                    },
                };

                this.collectedGenericInfo.Add(info);
            }

            if (genericTypeDefinitionString != "System.Linq.ILookup<,>")
            {
                return;
            }

            formatterName = KnownGenericTypes["System.Linq.IGrouping<,>"];

            var groupingInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            groupingInfo = groupingInfo with
            {
                DataType = new QualifiedNamedTypeName(TypeKind.Interface)
                {
                    Container = new NamespaceTypeContainer("global::System.Linq"),
                    Name = "IGrouping",
                    TypeParameters = GetTypeParameters(info.DataType),
                },
                Formatter = groupingInfo.Formatter with { Name = formatterName },
            };
            this.collectedGenericInfo.Add(groupingInfo);

            formatterName = KnownGenericTypes["System.Collections.Generic.IEnumerable<>"];

            GenericSerializationInfo enumerableInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            enumerableInfo = enumerableInfo with
            {
                DataType = new QualifiedNamedTypeName(TypeKind.Interface)
                {
                    Container = new NamespaceTypeContainer("System.Collections.Generic"),
                    Name = "IEnumerable",
                    TypeParameters = ImmutableArray.Create(GetTypeParameters(info.DataType)[1]),
                },
                Formatter = enumerableInfo.Formatter with { Name = formatterName },
            };
            this.collectedGenericInfo.Add(enumerableInfo);
            return;
        }

        if (type.AllInterfaces.FirstOrDefault(x => x.IsGenericType && !x.IsUnboundGenericType && x.ConstructUnboundGenericType() is { Name: nameof(ICollection<int>) }) is INamedTypeSymbol collectionIface
            && type.InstanceConstructors.Any(ctor => ctor.Parameters.Length == 0))
        {
            this.CollectCore(collectionIface.TypeArguments[0], callerSymbol);

            if (isOpenGenericType)
            {
                return;
            }
            else
            {
                GenericSerializationInfo info = GenericSerializationInfo.Create(type, this.options.Generator.Resolver) with
                {
                    Formatter = new(TypeKind.Class)
                    {
                        Container = new NamespaceTypeContainer("MsgPack::Formatters"),
                        Name = "GenericCollectionFormatter",
                        TypeParameters = ImmutableArray.Create<GenericTypeParameterInfo>(new("TElement"), new("TCollection")),
                        TypeArguments = ImmutableArray.Create<QualifiedTypeName>(QualifiedTypeName.Create(collectionIface.TypeArguments[0]), QualifiedTypeName.Create(type)),
                    },
                };

                this.collectedGenericInfo.Add(info);
                return;
            }
        }

        // Generic types
        if (type.IsDefinition)
        {
            this.CollectGenericUnion(type);
            this.CollectObject(type, callerSymbol);
        }
        else
        {
            // Collect substituted types for the properties and fields.
            // NOTE: It is used to register formatters from nested generic type.
            //       However, closed generic types such as `Foo<string>` are not registered as a formatter.
            this.GetObjectInfo(type, callerSymbol);

            // Collect generic type definition, that is not collected when it is defined outside target project.
            this.CollectCore(type.OriginalDefinition, callerSymbol);
        }

        // Collect substituted types for the type parameters (e.g. Bar in Foo<Bar>)
        foreach (var item in type.TypeArguments)
        {
            this.CollectCore(item, callerSymbol);
        }

        if (!isOpenGenericType)
        {
            GenericSerializationInfo genericSerializationInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            this.collectedGenericInfo.Add(genericSerializationInfo);
        }

        static ImmutableArray<GenericTypeParameterInfo> GetTypeParameters(QualifiedTypeName qtn)
            => qtn is QualifiedNamedTypeName named ? named.TypeParameters : ImmutableArray<GenericTypeParameterInfo>.Empty;

        static ImmutableArray<QualifiedTypeName> GetTypeArguments(QualifiedTypeName qtn)
            => qtn is QualifiedNamedTypeName named ? named.TypeArguments : ImmutableArray<QualifiedTypeName>.Empty;
    }

    private void CollectObject(INamedTypeSymbol type, ISymbol? callerSymbol)
    {
        ObjectSerializationInfo? info = this.GetObjectInfo(type, callerSymbol);
        if (info is not null)
        {
            this.collectedObjectInfo.Add(info);
        }
    }

    private void CheckValidMessagePackFormatterAttribute(AttributeData formatterAttribute)
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
        }
    }

    private ObjectSerializationInfo? GetObjectInfo(INamedTypeSymbol formattedType, ISymbol? callerSymbol)
    {
        var isClass = !formattedType.IsValueType;
        bool nestedFormatterRequired = false;
        List<IPropertySymbol> nestedFormatterRequiredIfPropertyIsNotSetByDeserializingCtor = new();

        AttributeData? contractAttr = formattedType.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute));

        // Examine properties set on the attribute such that we can discern whether they were explicitly set or not.
        // This is useful when we have assembly-level attributes or other environmentally-controlled defaults that the attribute may override either direction.
        bool? suppressSourceGeneration = (bool?)contractAttr?.NamedArguments.FirstOrDefault(kvp => kvp.Key == Constants.SuppressSourceGenerationPropertyName).Value.Value;

        // Do not source generate the formatter for this type if the attribute opted out.
        if (suppressSourceGeneration is true)
        {
            // Skip any source generation
            return null;
        }

        bool? allowPrivateAttribute = (bool?)contractAttr?.NamedArguments.FirstOrDefault(kvp => kvp.Key == Constants.AllowPrivatePropertyName).Value.Value;

        if (contractAttr is null)
        {
            if (this.reportDiagnostic != null)
            {
                var diagnostics = Diagnostic.Create(MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject, ((BaseTypeDeclarationSyntax)formattedType.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                this.reportDiagnostic.Invoke(diagnostics);
            }

            return null;
        }

        bool isIntKey = true;
        Dictionary<int, (MemberSerializationInfo Info, ITypeSymbol TypeSymbol)> intMembers = new();
        Dictionary<string, (MemberSerializationInfo Info, ITypeSymbol TypeSymbol)> stringMembers = new();

        // We default to not requiring non-public members to be attributed,
        // but as soon as the first non-public member *is* attributed,
        // we'll require all non-public members to be attributed.
        // Historically, only public members were able to be serialized by default,
        // so the analyzer only highlighted missing attributes on public members.
        // But once it's clear that the developer is serializing non-public members,
        // we need to raise the bar for all non-public members.
        bool nonPublicMembersAreSerialized = false;
        List<Diagnostic> deferredDiagnosticsForNonPublicMembers = new();

        FormatterDescriptor? GetSpecialFormatter(ISymbol member)
        {
            INamedTypeSymbol? name = member.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;
            if (name is not null)
            {
                if (FormatterDescriptor.TryCreate(name, out FormatterDescriptor? descriptor))
                {
                    return descriptor;
                }
                else
                {
                    // Report that the named type is not a formatter.
                    // this.reportDiagnostic();
                }
            }

            return null;
        }

        HashSet<Diagnostic> reportedDiagnostics = new();
        IEnumerable<ISymbol> instanceMembers = formattedType.GetAllMembers()
            .Where(m => m is IFieldSymbol or IPropertySymbol && !(m.IsStatic || m.IsOverride || m.IsImplicitlyDeclared));

        // The actual member identifiers of each serializable member,
        // such that we'll recognize collisions as we enumerate members in base types
        // and record that that happened so that the emitted source generator can apply the necessary casts.
        // In particular this does NOT store substitute names given by KeyAttribute.Name.
        // Members should be added to this set after they are verified to be accessible, instance, non-override properties,
        // whether or not those members are serialized.
        HashSet<string> collidingMemberNames = new(StringComparer.Ordinal);
        HashSet<string> observedMemberNames = new(StringComparer.Ordinal);
        foreach (ISymbol member in instanceMembers)
        {
            if (!observedMemberNames.Add(member.Name))
            {
                collidingMemberNames.Add(member.Name);
            }
        }

        QualifiedNamedTypeName? GetDeclaringTypeIfColliding(ISymbol symbol) => collidingMemberNames.Contains(symbol.Name) && !SymbolEqualityComparer.Default.Equals(symbol.ContainingType, formattedType) ? new(symbol.ContainingType) : null;

        if (this.options.Generator.Formatters.UsesMapMode || (contractAttr?.ConstructorArguments[0] is { Value: true }))
        {
            Dictionary<string, ISymbol> claimedKeys = new(StringComparer.Ordinal);
            void ReportNonUniqueNameIfApplicable(ISymbol item, string stringKey)
            {
                if (claimedKeys.TryGetValue(stringKey, out ISymbol newSymbol))
                {
                    SyntaxNode? syntax = newSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(this.cancellationToken);
                    var location = syntax switch
                    {
                        PropertyDeclarationSyntax { Identifier: SyntaxToken propertyId } => propertyId.GetLocation(),
                        VariableDeclaratorSyntax { Identifier: SyntaxToken fieldId } => fieldId.GetLocation(),
                        not null => syntax.GetLocation(),
                        _ => null,
                    };

                    reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.CollidingMemberNamesInForceMapMode, location));
                }

                claimedKeys[stringKey] = item;
            }

            // All public members are serialize target except [Ignore] member.
            // Include private members if the type opted into that.
            Accessibility minimumAccessibility = allowPrivateAttribute is true ? Accessibility.Private : Accessibility.Public;
            isIntKey = false;

            var hiddenIntKey = 0;

            foreach (ISymbol baseItem in instanceMembers)
            {
                switch (baseItem)
                {
                    case IPropertySymbol item:
                        {
                            ImmutableArray<AttributeData> attributes = item.GetAttributes();
                            if (attributes.Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)) ||
                                item.DeclaredAccessibility < minimumAccessibility)
                            {
                                continue;
                            }

                            bool isReadable = item.GetMethod is not null;
                            bool isWritable = item.SetMethod is not null;
                            if (!isReadable && !isWritable)
                            {
                                continue;
                            }

                            bool isInitOnly = item.SetMethod?.IsInitOnly is true;
                            AttributeData? keyAttribute = attributes.FirstOrDefault(attributes => attributes.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute));
                            string stringKey = keyAttribute?.ConstructorArguments.Length == 1 && keyAttribute.ConstructorArguments[0].Value is string name ? name : item.Name;
                            ReportNonUniqueNameIfApplicable(item, stringKey);

                            nestedFormatterRequired |= item.GetMethod is not null && IsPartialTypeRequired(item.GetMethod.DeclaredAccessibility);
                            nestedFormatterRequired |= item.SetMethod is not null && IsPartialTypeRequired(item.SetMethod.DeclaredAccessibility);
                            FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                            MemberSerializationInfo member = new(true, isWritable, isReadable, isInitOnly, item.IsRequired, hiddenIntKey++, stringKey, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                            {
                                DeclaringType = GetDeclaringTypeIfColliding(item),
                            };
                            if (!stringMembers.ContainsKey(member.StringKey))
                            {
                                stringMembers.Add(member.StringKey, (member, item.Type));
                            }

                            if (specialFormatter is null)
                            {
                                this.CollectCore(item.Type, item); // recursive collect
                            }
                        }

                        break;
                    case IFieldSymbol item:
                        {
                            ImmutableArray<AttributeData> attributes = item.GetAttributes();
                            if (attributes.Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)) ||
                                item.DeclaredAccessibility < minimumAccessibility)
                            {
                                continue;
                            }

                            AttributeData? keyAttribute = attributes.FirstOrDefault(attributes => attributes.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute));
                            string stringKey = keyAttribute?.ConstructorArguments.Length == 1 && keyAttribute.ConstructorArguments[0].Value is string name ? name : item.Name;
                            ReportNonUniqueNameIfApplicable(item, stringKey);

                            nestedFormatterRequired |= IsPartialTypeRequired(item.DeclaredAccessibility);
                            FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                            MemberSerializationInfo member = new(false, IsWritable: !item.IsReadOnly, IsReadable: true, IsInitOnly: false, item.IsRequired, hiddenIntKey++, stringKey, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                            {
                                DeclaringType = GetDeclaringTypeIfColliding(item),
                            };
                            if (!stringMembers.ContainsKey(member.StringKey))
                            {
                                stringMembers.Add(member.StringKey, (member, item.Type));
                            }

                            if (specialFormatter is null)
                            {
                                this.CollectCore(item.Type, item); // recursive collect
                            }
                        }

                        break;
                }
            }
        }
        else
        {
            // Only KeyAttribute members
            var searchFirst = true;
            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in instanceMembers.OfType<IPropertySymbol>())
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
                    nonPublicMembersAreSerialized |= (item.DeclaredAccessibility & Accessibility.Public) != Accessibility.Public;
                    continue;
                }

                bool isReadable = item.GetMethod is not null;
                bool isWritable = item.SetMethod is not null;
                bool isInitOnly = item.SetMethod?.IsInitOnly is true;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        // If the member is attributed with a type derived from KeyAttribute, that's incompatible with source generation
                        // since we cannot know what the key is using only C# syntax and the semantic model.
                        SyntaxReference? derivedKeyAttribute = item.GetAttributes().FirstOrDefault(att => att.AttributeClass.IsApproximatelyEqualOrDerivedFrom(this.typeReferences.KeyAttribute) == RoslynAnalyzerExtensions.EqualityMatch.LeftDerivesFromRight)?.ApplicationSyntaxReference;

                        if (derivedKeyAttribute is null || suppressSourceGeneration is not true)
                        {
                            if (SymbolEqualityComparer.Default.Equals(item.ContainingType, formattedType))
                            {
                                var syntax = item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                                var identifier = (syntax as PropertyDeclarationSyntax)?.Identifier ?? (syntax as ParameterSyntax)?.Identifier;

                                Diagnostic diagnostic = derivedKeyAttribute is not null && suppressSourceGeneration is not true
                                    ? Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AOTDerivedKeyAttribute, derivedKeyAttribute.GetSyntax(this.cancellationToken).GetLocation())
                                    : Diagnostic.Create(MsgPack00xMessagePackAnalyzer.MemberNeedsKey, identifier?.GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name);
                                if (nonPublicMembersAreSerialized || (item.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public)
                                {
                                    this.reportDiagnostic?.Invoke(diagnostic);
                                }
                                else
                                {
                                    deferredDiagnosticsForNonPublicMembers.Add(diagnostic);
                                }
                            }
                            else if (formattedType.BaseType is not null)
                            {
                                // The member was inherited, so we raise a special error at the location of the base type reference.
                                BaseTypeSyntax? baseSyntax = formattedType.DeclaringSyntaxReferences.SelectMany(sr => (IEnumerable<BaseTypeSyntax>?)((BaseTypeDeclarationSyntax)sr.GetSyntax()).BaseList?.Types ?? Array.Empty<BaseTypeSyntax>())
                                    .FirstOrDefault(bt => SymbolEqualityComparer.Default.Equals(this.compilation.GetSemanticModel(bt.SyntaxTree).GetTypeInfo(bt.Type).Type, item.ContainingType));
                                if (baseSyntax is not null)
                                {
                                    Diagnostic diagnostic = derivedKeyAttribute is not null && suppressSourceGeneration is not true
                                        ? Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AOTDerivedKeyAttribute, baseSyntax.GetLocation())
                                        : Diagnostic.Create(
                                            MsgPack00xMessagePackAnalyzer.BaseTypeContainsUnattributedPublicMembers,
                                            baseSyntax.GetLocation(),
                                            item.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                            item.Name);
                                    if (reportedDiagnostics.Add(diagnostic))
                                    {
                                        if (nonPublicMembersAreSerialized || (item.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public)
                                        {
                                            this.reportDiagnostic?.Invoke(diagnostic);
                                        }
                                        else
                                        {
                                            deferredDiagnosticsForNonPublicMembers.Add(diagnostic);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If this attributed member is non-public, then all non-public members should be attributed.
                    nonPublicMembersAreSerialized |= (item.DeclaredAccessibility & Accessibility.Public) != Accessibility.Public;

                    nestedFormatterRequired |= item.GetMethod is not null && IsPartialTypeRequired(item.GetMethod.DeclaredAccessibility);
                    if (item.SetMethod is not null && IsPartialTypeRequired(item.SetMethod.DeclaredAccessibility))
                    {
                        nestedFormatterRequiredIfPropertyIsNotSetByDeserializingCtor.Add(item);
                    }

                    var intKey = key is { Value: int intKeyValue } ? intKeyValue : default(int?);
                    var stringKey = key is { Value: string stringKeyValue } ? stringKeyValue : default;
                    if (intKey == null && stringKey == null)
                    {
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.BothStringAndIntKeyAreNull, ((PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                    }

                    // A property with an init accessor and an initializer has a default that will be discarded by the deserializer.
                    if (suppressSourceGeneration is not true && isInitOnly)
                    {
                        EqualsValueClauseSyntax? initializer = item.DeclaringSyntaxReferences.Select(s => (s.GetSyntax(this.cancellationToken) as PropertyDeclarationSyntax)?.Initializer).FirstOrDefault(i => i is not null);
                        if (initializer is not null)
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AOTInitProperty, initializer.GetLocation()));
                        }
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

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, isInitOnly, item.IsRequired, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                        {
                            DeclaringType = GetDeclaringTypeIfColliding(item),
                        };
                        intMembers.Add(member.IntKey, (member, item.Type));
                    }
                    else if (stringKey is not null)
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, isInitOnly, item.IsRequired, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                        {
                            DeclaringType = GetDeclaringTypeIfColliding(item),
                        };
                        stringMembers.Add(member.StringKey, (member, item.Type));
                    }

                    var messagePackFormatter = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0];

                    if (messagePackFormatter == null)
                    {
                        // recursive collect
                        this.CollectCore(item.Type, item);
                    }
                }
            }

            foreach (IFieldSymbol item in formattedType.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.IsImplicitlyDeclared || item.IsStatic)
                {
                    continue;
                }

                if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute)))
                {
                    nonPublicMembersAreSerialized |= (item.DeclaredAccessibility & Accessibility.Public) != Accessibility.Public;
                    continue;
                }

                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        // If the member is attributed with a type derived from KeyAttribute, that's incompatible with source generation
                        // since we cannot know what the key is using only C# syntax and the semantic model.
                        SyntaxReference? derivedKeyAttribute = item.GetAttributes().FirstOrDefault(att => att.AttributeClass.IsApproximatelyEqualOrDerivedFrom(this.typeReferences.KeyAttribute) == RoslynAnalyzerExtensions.EqualityMatch.LeftDerivesFromRight)?.ApplicationSyntaxReference;

                        if (derivedKeyAttribute is null || suppressSourceGeneration is not true)
                        {
                            if (SymbolEqualityComparer.Default.Equals(item.ContainingType, formattedType))
                            {
                                Diagnostic diagnostic = derivedKeyAttribute is not null && suppressSourceGeneration is not true
                                    ? Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AOTDerivedKeyAttribute, derivedKeyAttribute.GetSyntax(this.cancellationToken).GetLocation())
                                    : Diagnostic.Create(MsgPack00xMessagePackAnalyzer.MemberNeedsKey, item.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name);
                                if (nonPublicMembersAreSerialized || (item.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public)
                                {
                                    this.reportDiagnostic?.Invoke(diagnostic);
                                }
                                else
                                {
                                    deferredDiagnosticsForNonPublicMembers.Add(diagnostic);
                                }
                            }
                            else if (formattedType.BaseType is not null)
                            {
                                // The member was inherited, so we raise a special error at the location of the base type reference.
                                BaseTypeSyntax? baseSyntax = formattedType.DeclaringSyntaxReferences.SelectMany(sr => (IEnumerable<BaseTypeSyntax>?)((BaseTypeDeclarationSyntax)sr.GetSyntax()).BaseList?.Types ?? Array.Empty<BaseTypeSyntax>())
                                    .FirstOrDefault(bt => SymbolEqualityComparer.Default.Equals(this.compilation.GetSemanticModel(bt.SyntaxTree).GetTypeInfo(bt.Type).Type, item.ContainingType));
                                if (baseSyntax is not null)
                                {
                                    Diagnostic diagnostic = derivedKeyAttribute is not null && suppressSourceGeneration is not true
                                        ? Diagnostic.Create(MsgPack00xMessagePackAnalyzer.AOTDerivedKeyAttribute, baseSyntax.GetLocation())
                                        : Diagnostic.Create(
                                            MsgPack00xMessagePackAnalyzer.BaseTypeContainsUnattributedPublicMembers,
                                            baseSyntax.GetLocation(),
                                            item.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                                            item.Name);
                                    if (reportedDiagnostics.Add(diagnostic))
                                    {
                                        if (nonPublicMembersAreSerialized || (item.DeclaredAccessibility & Accessibility.Public) == Accessibility.Public)
                                        {
                                            this.reportDiagnostic?.Invoke(diagnostic);
                                        }
                                        else
                                        {
                                            deferredDiagnosticsForNonPublicMembers.Add(diagnostic);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    // If this attributed member is non-public, then all non-public members should be attributed.
                    nonPublicMembersAreSerialized |= (item.DeclaredAccessibility & Accessibility.Public) != Accessibility.Public;

                    nestedFormatterRequired |= IsPartialTypeRequired(item.DeclaredAccessibility);

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

                        var member = new MemberSerializationInfo(true, IsWritable: !item.IsReadOnly, IsReadable: true, IsInitOnly: false, item.IsRequired, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                        {
                            DeclaringType = GetDeclaringTypeIfColliding(item),
                        };
                        intMembers.Add(member.IntKey, (member, item.Type));
                    }
                    else
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, IsWritable: !item.IsReadOnly, IsReadable: true, IsInitOnly: false, item.IsRequired, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter)
                        {
                            DeclaringType = GetDeclaringTypeIfColliding(item),
                        };
                        stringMembers.Add(member.StringKey, (member, item.Type));
                    }

                    var messagePackFormatter = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.FormatterAttribute))?.ConstructorArguments[0];

                    if (messagePackFormatter == null)
                    {
                        // recursive collect
                        this.CollectCore(item.Type, item);
                    }
                }
            }
        }

        // If we discovered midway through that we should be reporting diagnostics for non-public members,
        // report the ones we missed along the way.
        if ((nonPublicMembersAreSerialized || allowPrivateAttribute is true) && this.reportDiagnostic is not null)
        {
            foreach (Diagnostic deferred in deferredDiagnosticsForNonPublicMembers)
            {
                this.reportDiagnostic(deferred);
            }
        }

        // GetConstructor
        var ctorEnumerator = default(IEnumerator<IMethodSymbol>);
        var ctor = formattedType.Constructors.SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass != null && y.AttributeClass.ApproximatelyEqual(this.typeReferences.SerializationConstructorAttribute)));
        if (ctor is null)
        {
            ctorEnumerator = formattedType.Constructors
                .OrderByDescending(x => x.DeclaredAccessibility)
                .ThenByDescending(x => x.Parameters.Length)
                .GetEnumerator();

            if (ctorEnumerator.MoveNext())
            {
                ctor = ctorEnumerator.Current;
            }
        }

        // struct allows null ctor
        if (ctor is null && isClass)
        {
            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.NoDeserializingConstructor, GetIdentifierLocation(formattedType)));
            return null;
        }

        var constructorParameters = new List<MemberSerializationInfo>();
        if (ctor is not null)
        {
            nestedFormatterRequired |= IsPartialTypeRequired(ctor.DeclaredAccessibility);

            var constructorLookupDictionary = stringMembers.ToLookup(x => x.Value.Info.Name, x => x, StringComparer.OrdinalIgnoreCase);
            IReadOnlyDictionary<int, (MemberSerializationInfo Info, ITypeSymbol TypeSymbol)> ctorParamIndexIntMembersDictionary = intMembers
                .OrderBy(x => x.Key).Select((x, i) => (Key: x.Value, Index: i))
                .ToDictionary(x => x.Index, x => x.Key);
            do
            {
                constructorParameters.Clear();
                var ctorParamIndex = 0;
                foreach (IParameterSymbol item in ctor!.Parameters)
                {
                    if (isIntKey)
                    {
                        if (ctorParamIndexIntMembersDictionary.TryGetValue(ctorParamIndex, out (MemberSerializationInfo Info, ITypeSymbol TypeSymbol) member))
                        {
                            if (this.compilation.ClassifyConversion(member.TypeSymbol, item.Type) is { IsImplicit: true } && member.Info.IsReadable)
                            {
                                constructorParameters.Add(member.Info);
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
                                    return null;
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
                                return null;
                            }
                        }
                    }
                    else
                    {
                        IEnumerable<KeyValuePair<string, (MemberSerializationInfo Info, ITypeSymbol TypeSymbol)>> hasKey = constructorLookupDictionary[item.Name];
                        using var enumerator = hasKey.GetEnumerator();

                        // hasKey.Count() == 0
                        if (!enumerator.MoveNext())
                        {
                            if (ctorEnumerator == null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterNameMissing, GetParameterListLocation(ctor)));
                                return null;
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
                                return null;
                            }

                            ctor = null;
                            continue;
                        }

                        MemberSerializationInfo paramMember = first.Info;
                        if (item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) == paramMember.Type && paramMember.IsReadable)
                        {
                            constructorParameters.Add(paramMember);
                        }
                        else
                        {
                            if (ctorEnumerator == null)
                            {
                                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.DeserializingConstructorParameterTypeMismatch, GetLocation(item)));
                                return null;
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
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.NoDeserializingConstructor, GetIdentifierLocation(formattedType)));
            }
        }

        var hasSerializationConstructor = formattedType.AllInterfaces.Any(x => x.ApproximatelyEqual(this.typeReferences.IMessagePackSerializationCallbackReceiver));
        var needsCastOnBefore = true;
        var needsCastOnAfter = true;
        if (hasSerializationConstructor)
        {
            needsCastOnBefore = !formattedType.GetMembers("OnBeforeSerialize").Any();
            needsCastOnAfter = !formattedType.GetMembers("OnAfterDeserialize").Any();
        }

        if (contractAttr is null)
        {
            if (formattedType.IsDefinition)
            {
                Location location = callerSymbol is not null ? callerSymbol.Locations[0] : formattedType.Locations[0];
                var targetName = callerSymbol is not null ? callerSymbol.ContainingType.Name + "." + callerSymbol.Name : formattedType.Name;

                ImmutableDictionary<string, string?> typeInfo = ImmutableDictionary.Create<string, string?>().Add("type", formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject, location, typeInfo, targetName));
            }

            // Indicate to our caller that we don't have a valid object.
            return null;
        }

        if (allowPrivateAttribute is not true && (nonPublicMembersAreSerialized || ctor is { DeclaredAccessibility: not Accessibility.Public } || formattedType.GetEffectiveAccessibility() is not Accessibility.Public))
        {
            if (contractAttr.ApplicationSyntaxReference?.GetSyntax(this.cancellationToken) is AttributeSyntax attSyntax)
            {
                Location location = attSyntax.GetLocation();

                // If the user is explicitly setting AllowPrivate = false, set the location more precisely.
                if (allowPrivateAttribute is false)
                {
                    location = attSyntax.ArgumentList?.Arguments.First(a => a.NameEquals?.Name.Identifier.ValueText == Constants.AllowPrivatePropertyName)?.Expression.GetLocation() ?? location;
                }

                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.MessagePackObjectAllowPrivateRequired, location));
            }
        }

        // If any property had a private setter and does not appear in the deserializing constructor signature,
        // we'll need a nested formatter.
        foreach (IPropertySymbol property in nestedFormatterRequiredIfPropertyIsNotSetByDeserializingCtor)
        {
            nestedFormatterRequired |= !constructorParameters.Any(m => m.Name == property.Name);
        }

        if (nestedFormatterRequired && nonPublicMembersAreSerialized)
        {
            // If the data type or any nesting types are not declared with partial, we cannot emit the formatter as a nested type within the data type
            // as required in order to access the private members.
            bool anyNonPartialTypesFound = false;
            BaseTypeDeclarationSyntax[] nonPartialTypes = FindNonPartialTypes(formattedType).ToArray();
            if (nonPartialTypes.Length > 0)
            {
                BaseTypeDeclarationSyntax? targetType = formattedType.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as BaseTypeDeclarationSyntax;
                Location? primaryLocation = targetType?.Identifier.GetLocation();
                Location[]? addlLocations = nonPartialTypes.Where(t => t != targetType).Select(t => t.Identifier.GetLocation()).ToArray();
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PartialTypeRequired, primaryLocation, (IEnumerable<Location>?)addlLocations));
                anyNonPartialTypesFound = true;
            }

            if (anyNonPartialTypesFound)
            {
                return null;
            }
        }

        INamedTypeSymbol? formattedTypeDefinition = formattedType.IsGenericType ? formattedType.ConstructUnboundGenericType() : null;
        ObjectSerializationInfo info = ObjectSerializationInfo.Create(
            formattedType,
            isClass: isClass,
            nestedFormatterRequired: nestedFormatterRequired,
            genericTypeParameters: formattedType.IsGenericType ? formattedType.TypeParameters.Select(t => GenericTypeParameterInfo.Create(t)).ToArray() : Array.Empty<GenericTypeParameterInfo>(),
            constructorParameters: constructorParameters.ToArray(),
            isIntKey: isIntKey,
            members: isIntKey ? intMembers.Values.Select(v => v.Info).ToArray() : stringMembers.Values.Select(v => v.Info).ToArray(),
            hasIMessagePackSerializationCallbackReceiver: hasSerializationConstructor,
            needsCastOnAfter: needsCastOnAfter,
            needsCastOnBefore: needsCastOnBefore,
            this.options.Generator.Resolver);

        return info;
    }

    private static IEnumerable<BaseTypeDeclarationSyntax> FindNonPartialTypes(ITypeSymbol target)
    {
        return from x in CodeAnalysisUtilities.EnumerateTypeAndContainingTypes(target)
               where x.FirstDeclaration?.Modifiers.Any(SyntaxKind.PartialKeyword) is false
               select x.FirstDeclaration;
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

    private static bool IsPartialTypeRequired(Accessibility accessibility) => accessibility is not (Accessibility.Public or Accessibility.Internal or Accessibility.ProtectedOrInternal);

    private static bool IsAllowAccessibility(ITypeSymbol symbol)
    {
        do
        {
            if (symbol.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                return false;
            }

            symbol = symbol.ContainingType;
        }
        while (symbol is not null);

        return true;
    }

    private static bool IsOpenGenericTypeRecursively(INamedTypeSymbol type)
    {
        return type.IsGenericType && type.TypeArguments.Any(x => x is ITypeParameterSymbol || (x is INamedTypeSymbol symbol && IsOpenGenericTypeRecursively(symbol)));
    }
}
