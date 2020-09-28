// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1649 // File name should match first type name

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace MessagePackCompiler.CodeAnalysis
{
    public class MessagePackGeneratorResolveFailedException : Exception
    {
        public MessagePackGeneratorResolveFailedException(string message)
            : base(message)
        {
        }
    }

    internal class ReferenceSymbols
    {
#pragma warning disable SA1401 // Fields should be private
        internal readonly INamedTypeSymbol Task;
        internal readonly INamedTypeSymbol TaskOfT;
        internal readonly INamedTypeSymbol MessagePackObjectAttribute;
        internal readonly INamedTypeSymbol UnionAttribute;
        internal readonly INamedTypeSymbol SerializationConstructorAttribute;
        internal readonly INamedTypeSymbol KeyAttribute;
        internal readonly INamedTypeSymbol IgnoreAttribute;
        internal readonly INamedTypeSymbol IgnoreDataMemberAttribute;
        internal readonly INamedTypeSymbol IMessagePackSerializationCallbackReceiver;
        internal readonly INamedTypeSymbol MessagePackFormatterAttribute;
#pragma warning restore SA1401 // Fields should be private

        public ReferenceSymbols(Compilation compilation, Action<string> logger)
        {
            TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (TaskOfT == null)
            {
                logger("failed to get metadata of System.Threading.Tasks.Task`1");
            }

            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            if (Task == null)
            {
                logger("failed to get metadata of System.Threading.Tasks.Task");
            }

            MessagePackObjectAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
            if (MessagePackObjectAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.MessagePackObjectAttribute");
            }

            UnionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute");
            if (UnionAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.UnionAttribute");
            }

            SerializationConstructorAttribute = compilation.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute");
            if (SerializationConstructorAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.SerializationConstructorAttribute");
            }

            KeyAttribute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
            if (KeyAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.KeyAttribute");
            }

            IgnoreAttribute = compilation.GetTypeByMetadataName("MessagePack.IgnoreMemberAttribute");
            if (IgnoreAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.IgnoreMemberAttribute");
            }

            IgnoreDataMemberAttribute = compilation.GetTypeByMetadataName("System.Runtime.Serialization.IgnoreDataMemberAttribute");
            if (IgnoreDataMemberAttribute == null)
            {
                logger("failed to get metadata of System.Runtime.Serialization.IgnoreDataMemberAttribute");
            }

            IMessagePackSerializationCallbackReceiver = compilation.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver");
            if (IMessagePackSerializationCallbackReceiver == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.IMessagePackSerializationCallbackReceiver");
            }

            MessagePackFormatterAttribute = compilation.GetTypeByMetadataName("MessagePack.MessagePackFormatterAttribute");
            if (MessagePackFormatterAttribute == null)
            {
                throw new InvalidOperationException("failed to get metadata of MessagePack.MessagePackFormatterAttribute");
            }
        }
    }

    public class TypeCollector
    {
        private const string CodegeneratorOnlyPreprocessorSymbol = "INCLUDE_ONLY_CODE_GENERATION";

        private static readonly SymbolDisplayFormat BinaryWriteFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

        private static readonly SymbolDisplayFormat ShortTypeNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        private readonly bool isForceUseMap;
        private readonly ReferenceSymbols typeReferences;
        private readonly INamedTypeSymbol[] targetTypes;
        private readonly HashSet<string> embeddedTypes = new HashSet<string>(new string[]
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

        private readonly Dictionary<string, string> knownGenericTypes = new Dictionary<string, string>
        {
#pragma warning disable SA1509 // Opening braces should not be preceded by blank line
            { "System.Collections.Generic.List<>", "global::MessagePack.Formatters.ListFormatter<TREPLACE>" },
            { "System.Collections.Generic.LinkedList<>", "global::MessagePack.Formatters.LinkedListFormatter<TREPLACE>" },
            { "System.Collections.Generic.Queue<>", "global::MessagePack.Formatters.QueueFormatter<TREPLACE>" },
            { "System.Collections.Generic.Stack<>", "global::MessagePack.Formatters.StackFormatter<TREPLACE>" },
            { "System.Collections.Generic.HashSet<>", "global::MessagePack.Formatters.HashSetFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ReadOnlyCollection<>", "global::MessagePack.Formatters.ReadOnlyCollectionFormatter<TREPLACE>" },
            { "System.Collections.Generic.IList<>", "global::MessagePack.Formatters.InterfaceListFormatter2<TREPLACE>" },
            { "System.Collections.Generic.ICollection<>", "global::MessagePack.Formatters.InterfaceCollectionFormatter2<TREPLACE>" },
            { "System.Collections.Generic.IEnumerable<>", "global::MessagePack.Formatters.InterfaceEnumerableFormatter<TREPLACE>" },
            { "System.Collections.Generic.Dictionary<,>", "global::MessagePack.Formatters.DictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.IDictionary<,>", "global::MessagePack.Formatters.InterfaceDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.SortedDictionary<,>", "global::MessagePack.Formatters.SortedDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.SortedList<,>", "global::MessagePack.Formatters.SortedListFormatter<TREPLACE>" },
            { "System.Linq.ILookup<,>", "global::MessagePack.Formatters.InterfaceLookupFormatter<TREPLACE>" },
            { "System.Linq.IGrouping<,>", "global::MessagePack.Formatters.InterfaceGroupingFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ObservableCollection<>", "global::MessagePack.Formatters.ObservableCollectionFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "global::MessagePack.Formatters.ReadOnlyObservableCollectionFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyList<>", "global::MessagePack.Formatters.InterfaceReadOnlyListFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyCollection<>", "global::MessagePack.Formatters.InterfaceReadOnlyCollectionFormatter<TREPLACE>" },
            { "System.Collections.Generic.ISet<>", "global::MessagePack.Formatters.InterfaceSetFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentBag<>", "global::MessagePack.Formatters.ConcurrentBagFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentQueue<>", "global::MessagePack.Formatters.ConcurrentQueueFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentStack<>", "global::MessagePack.Formatters.ConcurrentStackFormatter<TREPLACE>" },
            { "System.Collections.ObjectModel.ReadOnlyDictionary<,>", "global::MessagePack.Formatters.ReadOnlyDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Generic.IReadOnlyDictionary<,>", "global::MessagePack.Formatters.InterfaceReadOnlyDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Concurrent.ConcurrentDictionary<,>", "global::MessagePack.Formatters.ConcurrentDictionaryFormatter<TREPLACE>" },
            { "System.Lazy<>", "global::MessagePack.Formatters.LazyFormatter<TREPLACE>" },
            { "System.Threading.Tasks<>", "global::MessagePack.Formatters.TaskValueFormatter<TREPLACE>" },

            { "System.Tuple<>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },

            { "System.ValueTuple<>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },

            { "System.Collections.Generic.KeyValuePair<,>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>" },
            { "System.Threading.Tasks.ValueTask<>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>" },
            { "System.ArraySegment<>", "global::MessagePack.Formatters.ArraySegmentFormatter<TREPLACE>" },

            // extensions
            { "System.Collections.Immutable.ImmutableArray<>", "global::MessagePack.ImmutableCollection.ImmutableArrayFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableList<>", "global::MessagePack.ImmutableCollection.ImmutableListFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableDictionary<,>", "global::MessagePack.ImmutableCollection.ImmutableDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableHashSet<>", "global::MessagePack.ImmutableCollection.ImmutableHashSetFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableSortedDictionary<,>", "global::MessagePack.ImmutableCollection.ImmutableSortedDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableSortedSet<>", "global::MessagePack.ImmutableCollection.ImmutableSortedSetFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableQueue<>", "global::MessagePack.ImmutableCollection.ImmutableQueueFormatter<TREPLACE>" },
            { "System.Collections.Immutable.ImmutableStack<>", "global::MessagePack.ImmutableCollection.ImmutableStackFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableList<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableListFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableDictionary<,>", "global::MessagePack.ImmutableCollection.InterfaceImmutableDictionaryFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableQueue<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableQueueFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableSet<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableSetFormatter<TREPLACE>" },
            { "System.Collections.Immutable.IImmutableStack<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableStackFormatter<TREPLACE>" },

            { "Reactive.Bindings.ReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.IReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.IReadOnlyReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.ReactiveCollection<>", "global::MessagePack.ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>" },
#pragma warning restore SA1509 // Opening braces should not be preceded by blank line
        };

        private readonly Action<string> logger;

        private readonly bool disallowInternal;

        private HashSet<string> externalIgnoreTypeNames;

        // visitor workspace:
        private HashSet<ITypeSymbol> alreadyCollected;
        private List<ObjectSerializationInfo> collectedObjectInfo;
        private List<EnumSerializationInfo> collectedEnumInfo;
        private List<GenericSerializationInfo> collectedGenericInfo;
        private List<UnionSerializationInfo> collectedUnionInfo;

        public TypeCollector(Compilation compilation, bool disallowInternal, bool isForceUseMap, string[] ignoreTypeNames, Action<string> logger)
        {
            this.logger = logger;
            this.typeReferences = new ReferenceSymbols(compilation, logger);
            this.disallowInternal = disallowInternal;
            this.isForceUseMap = isForceUseMap;
            this.externalIgnoreTypeNames = new HashSet<string>(ignoreTypeNames ?? Array.Empty<string>());

            targetTypes = compilation.GetNamedTypeSymbols()
                .Where(x =>
                {
                    if (x.DeclaredAccessibility == Accessibility.Public)
                    {
                        return true;
                    }

                    if (!disallowInternal)
                    {
                        return x.DeclaredAccessibility == Accessibility.Friend;
                    }

                    return false;
                })
                .Where(x =>
                       ((x.TypeKind == TypeKind.Interface) && x.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(typeReferences.UnionAttribute)))
                    || ((x.TypeKind == TypeKind.Class && x.IsAbstract) && x.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(typeReferences.UnionAttribute)))
                    || ((x.TypeKind == TypeKind.Class) && x.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(typeReferences.MessagePackObjectAttribute)))
                    || ((x.TypeKind == TypeKind.Struct) && x.GetAttributes().Any(x2 => x2.AttributeClass.ApproximatelyEqual(typeReferences.MessagePackObjectAttribute))))
                .ToArray();
        }

        private void ResetWorkspace()
        {
            this.alreadyCollected = new HashSet<ITypeSymbol>();
            this.collectedObjectInfo = new List<ObjectSerializationInfo>();
            this.collectedEnumInfo = new List<EnumSerializationInfo>();
            this.collectedGenericInfo = new List<GenericSerializationInfo>();
            this.collectedUnionInfo = new List<UnionSerializationInfo>();
        }

        // EntryPoint
        public (ObjectSerializationInfo[] ObjectInfo, EnumSerializationInfo[] EnumInfo, GenericSerializationInfo[] GenericInfo, UnionSerializationInfo[] UnionInfo) Collect()
        {
            this.ResetWorkspace();

            foreach (INamedTypeSymbol item in this.targetTypes)
            {
                this.CollectCore(item);
            }

            return (
                this.collectedObjectInfo.OrderBy(x => x.FullName).ToArray(),
                this.collectedEnumInfo.OrderBy(x => x.FullName).ToArray(),
                this.collectedGenericInfo.Distinct().OrderBy(x => x.FullName).ToArray(),
                this.collectedUnionInfo.OrderBy(x => x.FullName).ToArray());
        }

        // Gate of recursive collect
        private void CollectCore(ITypeSymbol typeSymbol)
        {
            if (!this.alreadyCollected.Add(typeSymbol))
            {
                return;
            }

            if (this.embeddedTypes.Contains(typeSymbol.ToString()))
            {
                return;
            }

            if (this.externalIgnoreTypeNames.Contains(typeSymbol.ToString()))
            {
                return;
            }

            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                this.CollectArray(typeSymbol as IArrayTypeSymbol);
                return;
            }

            if (!this.IsAllowAccessibility(typeSymbol))
            {
                return;
            }

            if (!(typeSymbol is INamedTypeSymbol type))
            {
                return;
            }

            var customFormatterAttr = typeSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute));
            if (customFormatterAttr != null)
            {
                return;
            }

            if (typeSymbol.TypeKind == TypeKind.Enum)
            {
                this.CollectEnum(type);
                return;
            }

            if (type.IsGenericType)
            {
                this.CollectGeneric(type);
                return;
            }

            if (type.TupleUnderlyingType != null)
            {
                CollectGeneric(type.TupleUnderlyingType);
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

            this.CollectObject(type);
            return;
        }

        private void CollectEnum(INamedTypeSymbol type)
        {
            var info = new EnumSerializationInfo
            {
                Name = type.ToDisplayString(ShortTypeNameFormat).Replace(".", "_"),
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                UnderlyingType = type.EnumUnderlyingType.ToDisplayString(BinaryWriteFormat),
            };

            this.collectedEnumInfo.Add(info);
        }

        private void CollectUnion(INamedTypeSymbol type)
        {
            System.Collections.Immutable.ImmutableArray<TypedConstant>[] unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments).ToArray();
            if (unionAttrs.Length == 0)
            {
                throw new MessagePackGeneratorResolveFailedException("Serialization Type must mark UnionAttribute." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            // 0, Int  1, SubType
            var info = new UnionSerializationInfo
            {
                Name = type.Name,
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                SubTypes = unionAttrs.Select(x => new UnionSubTypeInfo
                {
                    Key = (int)x[0].Value,
                    Type = x[1].Value is ITypeSymbol typeSymbol ? typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) : throw new NotSupportedException($"AOT code generation only supports UnionAttribute that uses a Type parameter, but the {type.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)} type uses an unsupported parameter."),
                }).OrderBy(x => x.Key).ToArray(),
            };

            this.collectedUnionInfo.Add(info);
        }

        private void CollectGenericUnion(INamedTypeSymbol type)
        {
            System.Collections.Immutable.ImmutableArray<TypedConstant>[] unionAttrs = type.GetAttributes().Where(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.UnionAttribute)).Select(x => x.ConstructorArguments).ToArray();
            if (unionAttrs.Length == 0)
            {
                return;
            }

            var subTypes = unionAttrs.Select(x => x[1].Value).OfType<INamedTypeSymbol>().ToArray();
            foreach (var unionType in subTypes)
            {
                if (alreadyCollected.Contains(unionType) == false)
                {
                    CollectCore(unionType);
                }
            }
        }

        private void CollectArray(IArrayTypeSymbol array)
        {
            ITypeSymbol elemType = array.ElementType;
            this.CollectCore(elemType);

            var info = new GenericSerializationInfo
            {
                FullName = array.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsOpenGenericType = elemType is ITypeParameterSymbol,
            };

            if (array.IsSZArray)
            {
                info.FormatterName = $"global::MessagePack.Formatters.ArrayFormatter<{elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            }
            else if (array.Rank == 2)
            {
                info.FormatterName = $"global::MessagePack.Formatters.TwoDimensionalArrayFormatter<{elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            }
            else if (array.Rank == 3)
            {
                info.FormatterName = $"global::MessagePack.Formatters.ThreeDimensionalArrayFormatter<{elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            }
            else if (array.Rank == 4)
            {
                info.FormatterName = $"global::MessagePack.Formatters.FourDimensionalArrayFormatter<{elemType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>";
            }
            else
            {
                throw new InvalidOperationException("does not supports array dimension, " + info.FullName);
            }

            this.collectedGenericInfo.Add(info);

            return;
        }

        private void CollectGeneric(INamedTypeSymbol type)
        {
            INamedTypeSymbol genericType = type.ConstructUnboundGenericType();
            var genericTypeString = genericType.ToDisplayString();
            var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var isOpenGenericType = type.TypeArguments.Any(x => x is ITypeParameterSymbol);

            // special case
            if (fullName == "global::System.ArraySegment<byte>" || fullName == "global::System.ArraySegment<byte>?")
            {
                return;
            }

            // nullable
            if (genericTypeString == "T?")
            {
                this.CollectCore(type.TypeArguments[0]);

                if (!this.embeddedTypes.Contains(type.TypeArguments[0].ToString()))
                {
                    var info = new GenericSerializationInfo
                    {
                        FormatterName = $"global::MessagePack.Formatters.NullableFormatter<{type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>",
                        FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        IsOpenGenericType = isOpenGenericType,
                    };

                    this.collectedGenericInfo.Add(info);
                }

                return;
            }

            // collection
            if (this.knownGenericTypes.TryGetValue(genericTypeString, out var formatter))
            {
                foreach (ITypeSymbol item in type.TypeArguments)
                {
                    this.CollectCore(item);
                }

                var typeArgs = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var f = formatter.Replace("TREPLACE", typeArgs);

                var info = new GenericSerializationInfo
                {
                    FormatterName = f,
                    FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    IsOpenGenericType = isOpenGenericType,
                };

                this.collectedGenericInfo.Add(info);

                if (genericTypeString == "System.Linq.ILookup<,>")
                {
                    formatter = this.knownGenericTypes["System.Linq.IGrouping<,>"];
                    f = formatter.Replace("TREPLACE", typeArgs);

                    var groupingInfo = new GenericSerializationInfo
                    {
                        FormatterName = f,
                        FullName = $"global::System.Linq.IGrouping<{typeArgs}>",
                        IsOpenGenericType = isOpenGenericType,
                    };

                    this.collectedGenericInfo.Add(groupingInfo);

                    formatter = this.knownGenericTypes["System.Collections.Generic.IEnumerable<>"];
                    typeArgs = type.TypeArguments[1].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    f = formatter.Replace("TREPLACE", typeArgs);

                    var enumerableInfo = new GenericSerializationInfo
                    {
                        FormatterName = f,
                        FullName = $"global::System.Collections.Generic.IEnumerable<{typeArgs}>",
                        IsOpenGenericType = isOpenGenericType,
                    };

                    this.collectedGenericInfo.Add(enumerableInfo);
                }

                return;
            }

            // Generic types
            if (type.IsDefinition)
            {
                this.CollectGenericUnion(type);
                this.CollectObject(type);
                return;
            }
            else
            {
                // Collect substituted types for the properties and fields.
                // NOTE: It is used to register formatters from nested generic type.
                //       However, closed generic types such as `Foo<string>` are not registered as a formatter.
                GetObjectInfo(type);
            }

            // Collect substituted types for the type parameters (e.g. Bar in Foo<Bar>)
            foreach (var item in type.TypeArguments)
            {
                this.CollectCore(item);
            }

            var formatterBuilder = new StringBuilder();
            if (!type.ContainingNamespace.IsGlobalNamespace)
            {
                formatterBuilder.Append(type.ContainingNamespace.ToDisplayString() + ".");
            }

            formatterBuilder.Append(type.Name);
            formatterBuilder.Append("Formatter<");
            formatterBuilder.Append(string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))));
            formatterBuilder.Append(">");

            var genericSerializationInfo = new GenericSerializationInfo
            {
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                FormatterName = formatterBuilder.ToString(),
                IsOpenGenericType = isOpenGenericType,
            };

            this.collectedGenericInfo.Add(genericSerializationInfo);
        }

        private void CollectObject(INamedTypeSymbol type)
        {
            ObjectSerializationInfo info = GetObjectInfo(type);
            collectedObjectInfo.Add(info);
        }

        private ObjectSerializationInfo GetObjectInfo(INamedTypeSymbol type)
        {
            var isClass = !type.IsValueType;
            var isOpenGenericType = type.IsGenericType;

            AttributeData contractAttr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackObjectAttribute));
            if (contractAttr == null)
            {
                throw new MessagePackGeneratorResolveFailedException("Serialization Object must mark MessagePackObjectAttribute." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            var isIntKey = true;
            var intMembers = new Dictionary<int, MemberSerializationInfo>();
            var stringMembers = new Dictionary<string, MemberSerializationInfo>();

            if (this.isForceUseMap || (bool)contractAttr.ConstructorArguments[0].Value)
            {
                // All public members are serialize target except [Ignore] member.
                isIntKey = false;

                var hiddenIntKey = 0;

                foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>().Where(x => !x.IsOverride))
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.Name == this.typeReferences.IgnoreDataMemberAttribute.Name))
                    {
                        continue;
                    }

                    var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        StringKey = item.Name,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(BinaryWriteFormat),
                        CustomFormatterTypeName = customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);

                    this.CollectCore(item.Type); // recursive collect
                }

                foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.Name == this.typeReferences.IgnoreDataMemberAttribute.Name))
                    {
                        continue;
                    }

                    if (item.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic,
                        StringKey = item.Name,
                        IsProperty = false,
                        IsField = true,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(BinaryWriteFormat),
                        CustomFormatterTypeName = customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);
                    this.CollectCore(item.Type); // recursive collect
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

                    if (item.GetAttributes().Any(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreAttribute) || x.AttributeClass.ApproximatelyEqual(this.typeReferences.IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(BinaryWriteFormat),
                        CustomFormatterTypeName = customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                    if (key == null)
                    {
                        throw new MessagePackGeneratorResolveFailedException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                    }

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null)
                    {
                        throw new MessagePackGeneratorResolveFailedException("both IntKey and StringKey are null." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
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
                            throw new MessagePackGeneratorResolveFailedException("all members key type must be same." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        member.IntKey = (int)intKey;
                        if (intMembers.ContainsKey(member.IntKey))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = (string)stringKey;
                        if (stringMembers.ContainsKey(member.StringKey))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }

                    this.CollectCore(item.Type); // recursive collect
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

                    var customFormatterAttr = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.MessagePackFormatterAttribute))?.ConstructorArguments[0].Value as INamedTypeSymbol;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(BinaryWriteFormat),
                        CustomFormatterTypeName = customFormatterAttr?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    };
                    if (!member.IsReadable && !member.IsWritable)
                    {
                        continue;
                    }

                    TypedConstant? key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass.ApproximatelyEqual(this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                    if (key == null)
                    {
                        throw new MessagePackGeneratorResolveFailedException("all public members must mark KeyAttribute or IgnoreMemberAttribute." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                    }

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null)
                    {
                        throw new MessagePackGeneratorResolveFailedException("both IntKey and StringKey are null." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
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
                            throw new MessagePackGeneratorResolveFailedException("all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                        }
                    }

                    if (isIntKey)
                    {
                        member.IntKey = (int)intKey;
                        if (intMembers.ContainsKey(member.IntKey))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        intMembers.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = (string)stringKey;
                        if (stringMembers.ContainsKey(member.StringKey))
                        {
                            throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " member:" + item.Name);
                        }

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }

                    this.CollectCore(item.Type); // recursive collect
                }
            }

            // GetConstructor
            IEnumerator<IMethodSymbol> ctorEnumerator = null;
            IMethodSymbol ctor = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass.ApproximatelyEqual(this.typeReferences.SerializationConstructorAttribute)));
            if (ctor == null)
            {
                ctorEnumerator = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).OrderByDescending(x => x.Parameters.Length).GetEnumerator();

                if (ctorEnumerator.MoveNext())
                {
                    ctor = ctorEnumerator.Current;
                }
            }

            // struct allows null ctor
            if (ctor == null && isClass)
            {
                throw new MessagePackGeneratorResolveFailedException("can't find public constructor. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
            }

            var constructorParameters = new List<MemberSerializationInfo>();
            if (ctor != null)
            {
                ILookup<string, KeyValuePair<string, MemberSerializationInfo>> constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);
                do
                {
                    constructorParameters.Clear();
                    var ctorParamIndex = 0;
                    foreach (IParameterSymbol item in ctor.Parameters)
                    {
                        MemberSerializationInfo paramMember;
                        if (isIntKey)
                        {
                            if (intMembers.TryGetValue(ctorParamIndex, out paramMember))
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
                                        throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterIndex:" + ctorParamIndex + " paramterType:" + item.Type.Name);
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
                                    throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterIndex:" + ctorParamIndex);
                                }
                            }
                        }
                        else
                        {
                            IEnumerable<KeyValuePair<string, MemberSerializationInfo>> hasKey = constructorLookupDictionary[item.Name];
                            var len = hasKey.Count();
                            if (len != 0)
                            {
                                if (len != 1)
                                {
                                    if (ctorEnumerator != null)
                                    {
                                        ctor = null;
                                        continue;
                                    }
                                    else
                                    {
                                        throw new MessagePackGeneratorResolveFailedException("duplicate matched constructor parameter name:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name + " paramterType:" + item.Type.Name);
                                    }
                                }

                                paramMember = hasKey.First().Value;
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
                                        throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name + " paramterType:" + item.Type.Name);
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
                                    throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) + " parameterName:" + item.Name);
                                }
                            }
                        }

                        ctorParamIndex++;
                    }
                }
                while (TryGetNextConstructor(ctorEnumerator, ref ctor));

                if (ctor == null)
                {
                    throw new MessagePackGeneratorResolveFailedException("can't find matched constructor. type:" + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
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

            var info = new ObjectSerializationInfo
            {
                IsClass = isClass,
                IsOpenGenericType = isOpenGenericType,
                GenericTypeParameters = isOpenGenericType
                    ? type.TypeParameters.Select(ToGenericTypeParameterInfo).ToArray()
                    : Array.Empty<GenericTypeParameterInfo>(),
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = isIntKey ? intMembers.Values.ToArray() : stringMembers.Values.ToArray(),
                Name = isOpenGenericType ? GetGenericFormatterClassName(type) : GetMinimallyQualifiedClassName(type),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                HasIMessagePackSerializationCallbackReceiver = hasSerializationConstructor,
                NeedsCastOnAfter = needsCastOnAfter,
                NeedsCastOnBefore = needsCastOnBefore,
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
                if (typeParameter.ReferenceTypeConstraintNullableAnnotation == NullableAnnotation.Annotated)
                {
                    constraints.Add("class?");
                }
                else
                {
                    constraints.Add("class");
                }
            }

            if (typeParameter.HasValueTypeConstraint)
            {
                if (typeParameter.HasUnmanagedTypeConstraint)
                {
                    constraints.Add("unmanaged");
                }
                else
                {
                    constraints.Add("struct");
                }
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

        private static string GetGenericFormatterClassName(INamedTypeSymbol type)
        {
            return type.Name;
        }

        private static string GetMinimallyQualifiedClassName(INamedTypeSymbol type)
        {
            var name = type.ContainingType is object ? GetMinimallyQualifiedClassName(type.ContainingType) + "_" : string.Empty;
            name += type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            name = name.Replace(".", "_");
            name = name.Replace("<", "_");
            name = name.Replace(">", "_");
            name = Regex.Replace(name, @"\[([,])*\]", match => $"Array{match.Length - 1}");
            return name;
        }

        private static bool TryGetNextConstructor(IEnumerator<IMethodSymbol> ctorEnumerator, ref IMethodSymbol ctor)
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

        private bool IsAllowAccessibility(ITypeSymbol symbol)
        {
            do
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    if (this.disallowInternal)
                    {
                        return false;
                    }

                    if (symbol.DeclaredAccessibility != Accessibility.Internal)
                    {
                        return true;
                    }
                }

                symbol = symbol.ContainingType;
            }
            while (symbol != null);

            return true;
        }
    }
}
