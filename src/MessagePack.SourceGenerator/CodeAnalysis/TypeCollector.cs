// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type
#pragma warning disable SA1649 // File name should match first type name

using System.Collections.Immutable;
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
    private readonly bool excludeArrayElement;

    // visitor workspace:
    private readonly Dictionary<ITypeSymbol, bool> alreadyCollected = new(SymbolEqualityComparer.Default);
    private readonly ImmutableSortedSet<ObjectSerializationInfo>.Builder collectedObjectInfo = ImmutableSortedSet.CreateBuilder<ObjectSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<EnumSerializationInfo>.Builder collectedEnumInfo = ImmutableSortedSet.CreateBuilder<EnumSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<GenericSerializationInfo>.Builder collectedGenericInfo = ImmutableSortedSet.CreateBuilder<GenericSerializationInfo>(ResolverRegisterInfoComparer.Default);
    private readonly ImmutableSortedSet<UnionSerializationInfo>.Builder collectedUnionInfo = ImmutableSortedSet.CreateBuilder<UnionSerializationInfo>(ResolverRegisterInfoComparer.Default);

    private readonly Compilation compilation;

    private TypeCollector(Compilation compilation, AnalyzerOptions options, ReferenceSymbols referenceSymbols, ITypeSymbol targetType, Action<Diagnostic>? reportAnalyzerDiagnostic, CancellationToken cancellationToken)
    {
        this.typeReferences = referenceSymbols;
        this.reportDiagnostic = reportAnalyzerDiagnostic;
        this.options = options;
        this.compilation = compilation;
        this.excludeArrayElement = true;

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

        FormattableType formattableType = new(typeSymbol);
        if (EmbeddedTypes.Contains(formattableType.Name.Name))
        {
            result = true;
            this.alreadyCollected.Add(typeSymbol, result);
            return result;
        }

        if (this.options.AssumedFormattableTypes.Contains(formattableType))
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

        if (!IsAllowAccessibility(typeSymbol))
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
        this.collectedEnumInfo.Add(EnumSerializationInfo.Create(type, enumUnderlyingType, this.options.Generator.Resolver));
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

        var info = UnionSerializationInfo.Create(
            type,
            unionAttrs.Select(UnionSubTypeInfoSelector).Where(i => i is not null).OrderBy(x => x!.Key).ToImmutableArray()!,
            this.options.Generator.Resolver);

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

        if (this.alreadyCollected.ContainsKey(elemType))
        {
            return true;
        }

        if (elemType is ITypeParameterSymbol)
        {
            this.alreadyCollected.Add(elemType, true);
            return true;
        }

        QualifiedTypeName elementTypeName = new(elemType);
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
            return false;
        }

        var info = GenericSerializationInfo.Create(array, this.options.Generator.Resolver) with
        {
            Formatter = new QualifiedTypeName("MsgPack::Formatters", null, TypeKind.Class, formatterName, ImmutableArray.Create(elementTypeName.GetQualifiedName())),
        };
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
        INamedTypeSymbol typeDefinition = type.ConstructUnboundGenericType();
        var genericTypeDefinitionString = typeDefinition.ToDisplayString();
        string? genericTypeNamespace = type.ContainingNamespace?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        int unboundArity = this.IsOpenGenericTypeRecursively(type) ? type.Arity : 0;
        string formattedTypeFullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        string? formatterName;

        // special case
        if (fullName == "global::System.ArraySegment<byte>" || fullName == "global::System.ArraySegment<byte>?")
        {
            return true;
        }

        // nullable
        if (genericTypeDefinitionString == "T?")
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

            formatterName = $"NullableFormatter<{firstTypeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            var info = GenericSerializationInfo.Create(type, this.options.Generator.Resolver) with
            {
                Formatter = new QualifiedTypeName("MsgPack::Formatters", TypeKind.Class, formatterName),
            };
            this.collectedGenericInfo.Add(info);
            return true;
        }

        // collection
        if (KnownGenericTypes.TryGetValue(genericTypeDefinitionString, out string formatterFullName))
        {
            foreach (ITypeSymbol item in type.TypeArguments)
            {
                this.CollectCore(item);
            }

            GenericSerializationInfo info = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            int indexOfLastPeriod = formatterFullName.LastIndexOf('.');
            info = info with
            {
                Formatter = new(formatterFullName.Substring(0, indexOfLastPeriod), TypeKind.Class, formatterFullName.Substring(indexOfLastPeriod + 1), info.DataType.TypeParameters),
            };

            this.collectedGenericInfo.Add(info);

            if (genericTypeDefinitionString != "System.Linq.ILookup<,>")
            {
                return true;
            }

            formatterName = KnownGenericTypes["System.Linq.IGrouping<,>"];

            var groupingInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            groupingInfo = groupingInfo with
            {
                DataType = new QualifiedTypeName("global::System.Linq", TypeKind.Interface, $"IGrouping", info.DataType.TypeParameters),
                Formatter = groupingInfo.Formatter with { Name = formatterName },
            };
            this.collectedGenericInfo.Add(groupingInfo);

            formatterName = KnownGenericTypes["System.Collections.Generic.IEnumerable<>"];

            GenericSerializationInfo enumerableInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
            enumerableInfo = enumerableInfo with
            {
                DataType = new QualifiedTypeName("System.Collections.Generic", TypeKind.Interface, $"IEnumerable", ImmutableArray.Create(info.DataType.TypeParameters[1])),
                Formatter = enumerableInfo.Formatter with { Name = formatterName },
            };
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

        GenericSerializationInfo genericSerializationInfo = GenericSerializationInfo.Create(type, this.options.Generator.Resolver);
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

    private ObjectSerializationInfo? GetObjectInfo(INamedTypeSymbol formattedType)
    {
        var isClass = !formattedType.IsValueType;
        bool includesPrivateMembers = false;

        AttributeData? contractAttr = formattedType.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute));
        if (contractAttr is null)
        {
            ////this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.TypeMustBeMessagePackObject, ((BaseTypeDeclarationSyntax)type.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
        }

        var isIntKey = true;
        var intMembers = new Dictionary<int, MemberSerializationInfo>();
        var stringMembers = new Dictionary<string, MemberSerializationInfo>();

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

        if (this.options.Generator.Formatters.UsesMapMode || (contractAttr?.ConstructorArguments[0] is { Value: bool firstConstructorArgument } && firstConstructorArgument))
        {
            // All public members are serialize target except [Ignore] member.
            Accessibility minimumAccessibility = Accessibility.Internal;
            isIntKey = false;

            var hiddenIntKey = 0;

            foreach (IPropertySymbol item in formattedType.GetAllMembers().OfType<IPropertySymbol>())
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)) ||
                    item.IsStatic ||
                    item.IsOverride ||
                    item.IsImplicitlyDeclared ||
                    item.DeclaredAccessibility < minimumAccessibility)
                {
                    continue;
                }

                var isReadable = item.GetMethod is not null;
                var isWritable = item.SetMethod is not null;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                includesPrivateMembers |= item.GetMethod is not null && !IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility);
                includesPrivateMembers |= item.SetMethod is not null && !IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility);
                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
                stringMembers.Add(member.StringKey, member);

                if (specialFormatter is null)
                {
                    this.CollectCore(item.Type); // recursive collect
                }
            }

            foreach (IFieldSymbol item in formattedType.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.GetAttributes().Any(x => (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass?.Name == this.typeReferences.IgnoreDataMemberAttribute?.Name)) ||
                    item.IsStatic ||
                    item.IsImplicitlyDeclared ||
                    item.DeclaredAccessibility < minimumAccessibility)
                {
                    continue;
                }

                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                var member = new MemberSerializationInfo(false, IsWritable: !item.IsReadOnly, IsReadable: true, hiddenIntKey++, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
                stringMembers.Add(member.StringKey, member);
                if (specialFormatter is null)
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

            foreach (IPropertySymbol item in formattedType.GetAllMembers().OfType<IPropertySymbol>())
            {
                if (item.IsIndexer)
                {
                    continue; // .tt files don't generate good code for this yet: https://github.com/neuecc/MessagePack-CSharp/issues/390
                }

                if (item.IsStatic || item.IsImplicitlyDeclared)
                {
                    continue;
                }

                if (item.GetAttributes().Any(x =>
                {
                    var typeReferencesIgnoreDataMemberAttribute = this.typeReferences.IgnoreDataMemberAttribute;
                    return typeReferencesIgnoreDataMemberAttribute != null && (x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.ApproximatelyEqual(typeReferencesIgnoreDataMemberAttribute));
                }))
                {
                    continue;
                }

                var isReadable = item.GetMethod is not null;
                var isWritable = item.SetMethod is not null;
                if (!isReadable && !isWritable)
                {
                    continue;
                }

                includesPrivateMembers |= item.GetMethod is not null && !IsAllowedAccessibility(item.GetMethod.DeclaredAccessibility);
                includesPrivateMembers |= item.SetMethod is not null && !IsAllowedAccessibility(item.SetMethod.DeclaredAccessibility);
                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        if (SymbolEqualityComparer.Default.Equals(item.ContainingType, formattedType))
                        {
                            var syntax = item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                            var identifier = (syntax as PropertyDeclarationSyntax)?.Identifier ?? (syntax as ParameterSyntax)?.Identifier;

                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PublicMemberNeedsKey, identifier?.GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
                        }
                        else if (formattedType.BaseType is not null)
                        {
                            // The member was inherited, so we raise a special error at the location of the base type reference.
                            BaseTypeSyntax? baseSyntax = formattedType.DeclaringSyntaxReferences.SelectMany(sr => (IEnumerable<BaseTypeSyntax>?)((BaseTypeDeclarationSyntax)sr.GetSyntax()).BaseList?.Types ?? Array.Empty<BaseTypeSyntax>())
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
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.BothStringAndIntKeyAreNull, ((PropertyDeclarationSyntax)item.DeclaringSyntaxReferences[0].GetSyntax()).Identifier.GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
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

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
                        intMembers.Add(member.IntKey, member);
                    }
                    else if (stringKey is not null)
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, isWritable, isReadable, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
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

            foreach (IFieldSymbol item in formattedType.GetAllMembers().OfType<IFieldSymbol>())
            {
                if (item.IsImplicitlyDeclared || item.IsStatic)
                {
                    continue;
                }

                if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute)))
                {
                    continue;
                }

                includesPrivateMembers |= !IsAllowedAccessibility(item.DeclaredAccessibility);
                FormatterDescriptor? specialFormatter = GetSpecialFormatter(item);
                TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                if (key is null)
                {
                    if (contractAttr is not null)
                    {
                        this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PublicMemberNeedsKey, item.DeclaringSyntaxReferences[0].GetSyntax().GetLocation(), formattedType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Name));
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

                        var member = new MemberSerializationInfo(true, IsWritable: !item.IsReadOnly, IsReadable: true, intKey!.Value, item.Name, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        if (stringMembers.ContainsKey(stringKey!))
                        {
                            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.KeysMustBeUnique, item.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax().GetLocation()));
                        }

                        var member = new MemberSerializationInfo(true, IsWritable: !item.IsReadOnly, IsReadable: true, hiddenIntKey++, stringKey!, item.Name, item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat), item.Type.ToDisplayString(BinaryWriteFormat), specialFormatter);
                        stringMembers.Add(member.StringKey, member);
                    }
                }

                this.CollectCore(item.Type); // recursive collect
            }
        }

        // GetConstructor
        var ctorEnumerator = default(IEnumerator<IMethodSymbol>);
        var ctor = formattedType.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass != null && y.AttributeClass.ApproximatelyEqual(this.typeReferences.SerializationConstructorAttribute)));
        if (ctor == null)
        {
            ctorEnumerator = formattedType.Constructors.Where(x => IsAllowedAccessibility(x.DeclaredAccessibility)).OrderByDescending(x => x.Parameters.Length).GetEnumerator();

            if (ctorEnumerator.MoveNext())
            {
                ctor = ctorEnumerator.Current;
            }
        }

        // struct allows null ctor
        if (ctor == null && isClass)
        {
            this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.NoDeserializingConstructor, GetIdentifierLocation(formattedType)));
        }

        var constructorParameters = new List<MemberSerializationInfo>();
        if (ctor != null)
        {
            var constructorLookupDictionary = stringMembers.ToLookup(x => x.Value.Name, x => x, StringComparer.OrdinalIgnoreCase);
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
            // Indicate to our caller that we don't have a valid object.
            return null;
        }

        // Do not source generate the formatter for this type if the attribute opted out.
        if (contractAttr.NamedArguments.FirstOrDefault(kvp => kvp.Key == Constants.SuppressSourceGenerationPropertyName).Value.Value is true)
        {
            // Skip any source generation
            return null;
        }

        if (includesPrivateMembers)
        {
            // If the data type or any nesting types are not declared with partial, we cannot emit the formatter as a nested type within the data type
            // as required in order to access the private members.
            bool anyNonPartialTypesFound = false;
            foreach (BaseTypeDeclarationSyntax? decl in FindNonPartialTypes(formattedType))
            {
                this.reportDiagnostic?.Invoke(Diagnostic.Create(MsgPack00xMessagePackAnalyzer.PartialTypeRequired, decl.Identifier.GetLocation()));
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
            includesPrivateMembers: includesPrivateMembers,
            genericTypeParameters: formattedType.IsGenericType ? formattedType.TypeParameters.Select(ToGenericTypeParameterInfo).ToArray() : Array.Empty<GenericTypeParameterInfo>(),
            constructorParameters: constructorParameters.ToArray(),
            isIntKey: isIntKey,
            members: isIntKey ? intMembers.Values.ToArray() : stringMembers.Values.ToArray(),
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

    private static bool IsAllowAccessibility(ITypeSymbol symbol)
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
