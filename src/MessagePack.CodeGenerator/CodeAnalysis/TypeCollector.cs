using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MessagePack.CodeGenerator
{
    public class ReferenceSymbols
    {
        public readonly INamedTypeSymbol Task;
        public readonly INamedTypeSymbol TaskOfT;
        public readonly INamedTypeSymbol MessagePackObjectAttribnute;
        public readonly INamedTypeSymbol UnionAttribute;
        public readonly INamedTypeSymbol SerializationConstructorAttribute;
        public readonly INamedTypeSymbol KeyAttribnute;
        public readonly INamedTypeSymbol IgnoreAttribnute;
        public readonly INamedTypeSymbol IMessagePackSerializationCallbackReceiver;

        public ReferenceSymbols(Compilation compilation)
        {
            TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            MessagePackObjectAttribnute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
            UnionAttribute = compilation.GetTypeByMetadataName("MessagePack.UnionAttribute");
            SerializationConstructorAttribute = compilation.GetTypeByMetadataName("MessagePack.SerializationConstructorAttribute");
            KeyAttribnute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
            IgnoreAttribnute = compilation.GetTypeByMetadataName("MessagePack.IgnoreAttribute");
            IMessagePackSerializationCallbackReceiver = compilation.GetTypeByMetadataName("MessagePack.IMessagePackSerializationCallbackReceiver");
        }
    }

    public class TypeCollector
    {
        const string CodegeneratorOnlyPreprocessorSymbol = "INCLUDE_ONLY_CODE_GENERATION";

        static readonly SymbolDisplayFormat binaryWriteFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

        static readonly SymbolDisplayFormat shortTypeNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        readonly string csProjPath;
        readonly ReferenceSymbols typeReferences;
        readonly INamedTypeSymbol[] targetTypes;
        readonly HashSet<string> embeddedTypes = new HashSet<string>(new string[]
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
            "System.Guid",
            "System.TimeSpan",
            "System.DateTime",
            "System.DateTimeOffset",
        });

        readonly Dictionary<string, string> knownGenericTypes = new Dictionary<string, string>
        {
            {"System.Collections.Generic.List<>", "global::MessagePack.Formatters.ListFormatter<TREPLACE>" },
            {"System.Collections.Generic.LinkedList<>", "global::MessagePack.Formatters.LinkedListFormatter<TREPLACE>"},
            {"System.Collections.Generic.Queue<>", "global::MessagePack.Formatters.QeueueFormatter<TREPLACE>"},
            {"System.Collections.Generic.Stack<>", "global::MessagePack.Formatters.StackFormatter<TREPLACE>"},
            {"System.Collections.Generic.HashSet<>", "global::MessagePack.Formatters.HashSetFormatter<TREPLACE>"},
            {"System.Collections.ObjectModel.ReadOnlyCollection<>", "global::MessagePack.Formatters.ReadOnlyCollectionFormatter<TREPLACE>"},
            {"System.Collections.Generic.IList<>", "global::MessagePack.Formatters.InterfaceListFormatter<TREPLACE>"},
            {"System.Collections.Generic.ICollection<>", "global::MessagePack.Formatters.InterfaceCollectionFormatter<TREPLACE>"},
            {"System.Collections.Generic.IEnumerable<>", "global::MessagePack.Formatters.InterfaceEnumerableFormatter<TREPLACE>"},
            {"System.Collections.Generic.Dictionary<,>", "global::MessagePack.Formatters.DictionaryFormatter<TREPLACE>"},
            {"System.Collections.Generic.IDictionary<,>", "global::MessagePack.Formatters.InterfaceDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Generic.SortedDictionary<,>", "global::MessagePack.Formatters.SortedDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Generic.SortedList<,>", "global::MessagePack.Formatters.SortedListFormatter<TREPLACE>"},
            {"System.Linq.ILookup<,>", "global::MessagePack.Formatters.InterfaceLookupFormatter<TREPLACE>"},
            {"System.Linq.IGrouping<,>", "global::MessagePack.Formatters.InterfaceGroupingFormatter<TREPLACE>"},
            {"System.Collections.ObjectModel.ObservableCollection<>", "global::MessagePack.Formatters.ObservableCollectionFormatter<TREPLACE>"},
            {"System.Collections.ObjectModel.ReadOnlyObservableCollection<>", "global::MessagePack.Formatters.ReadOnlyObservableCollectionFormatter<TREPLACE>" },
            {"System.Collections.Generic.IReadOnlyList<>", "global::MessagePack.Formatters.InterfaceReadOnlyListFormatter<TREPLACE>"},
            {"System.Collections.Generic.IReadOnlyCollection<>", "global::MessagePack.Formatters.InterfaceReadOnlyCollectionFormatter<TREPLACE>"},
            {"System.Collections.Generic.ISet<>", "global::MessagePack.Formatters.InterfaceSetFormatter<TREPLACE>"},
            {"System.Collections.Concurrent.ConcurrentBag<>", "global::MessagePack.Formatters.ConcurrentBagFormatter<TREPLACE>"},
            {"System.Collections.Concurrent.ConcurrentQueue<>", "global::MessagePack.Formatters.ConcurrentQueueFormatter<TREPLACE>"},
            {"System.Collections.Concurrent.ConcurrentStack<>", "global::MessagePack.Formatters.ConcurrentStackFormatter<TREPLACE>"},
            {"System.Collections.ObjectModel.ReadOnlyDictionary<,>", "global::MessagePack.Formatters.ReadOnlyDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Generic.IReadOnlyDictionary<,>", "global::MessagePack.Formatters.InterfaceReadOnlyDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Concurrent.ConcurrentDictionary<,>", "global::MessagePack.Formatters.ConcurrentDictionaryFormatter<TREPLACE>"},
            {"System.Lazy<>", "global::MessagePack.Formatters.LazyFormatter<TREPLACE>"},
            {"System.Threading.Tasks<>", "global::MessagePack.Formatters.TaskValueFormatter<TREPLACE>"},

            {"System.Tuple<>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},
            {"System.Tuple<,,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>"},

            {"System.ValueTuple<>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},
            {"System.ValueTuple<,,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>"},

            {"System.Collections.Generic.KeyValuePair<,>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>"},
            {"System.Threading.Tasks.ValueTask<>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>"},
            {"System.ArraySegment<>", "global::MessagePack.Formatters.ArraySegmentFormatter<TREPLACE>"},

            // extensions

            {"System.Collections.Immutable.ImmutableArray<>", "global::MessagePack.ImmutableCollections.ImmutableArrayFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableList<>", "global::MessagePack.ImmutableCollections.ImmutableListFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableDictionary<,>", "global::MessagePack.ImmutableCollections.ImmutableDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableHashSet<>", "global::MessagePack.ImmutableCollections.ImmutableHashSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableSortedDictionary<,>", "global::MessagePack.ImmutableCollections.ImmutableSortedDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableSortedSet<>", "global::MessagePack.ImmutableCollections.ImmutableSortedSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableQueue<>", "global::MessagePack.ImmutableCollections.ImmutableQueueFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableStack<>", "global::MessagePack.ImmutableCollections.ImmutableStackFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableList<>", "global::MessagePack.ImmutableCollections.InterfaceImmutableListFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableDictionary<,>", "global::MessagePack.ImmutableCollections.InterfaceImmutableDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableQueue<>", "global::MessagePack.ImmutableCollections.InterfaceImmutableQueueFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableSet<>", "global::MessagePack.ImmutableCollections.InterfaceImmutableSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableStack<>", "global::MessagePack.ImmutableCollections.InterfaceImmutableStackFormatter<TREPLACE>"},

            {"Reactive.Bindings.ReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.IReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.IReadOnlyReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.ReactiveCollection<>", "global::MessagePack.ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>"},
        };

        readonly bool disallowInternal;

        // visitor workspace:
        HashSet<ITypeSymbol> alreadyCollected;
        List<ObjectSerializationInfo> collectedObjectInfo;
        List<EnumSerializationInfo> collectedEnumInfo;
        List<GenericSerializationInfo> collectedGenericInfo;

        // --- 

        public TypeCollector(string csProjPath, IEnumerable<string> conditinalSymbols, bool disallowInternal)
        {
            this.csProjPath = csProjPath;
            var compilation = RoslynExtensions.GetCompilationFromProject(csProjPath, conditinalSymbols.Concat(new[] { CodegeneratorOnlyPreprocessorSymbol }).ToArray()).GetAwaiter().GetResult();
            this.typeReferences = new ReferenceSymbols(compilation);
            this.disallowInternal = disallowInternal;

            targetTypes = compilation.GetNamedTypeSymbols()
                .Where(x =>
                {
                    if (x.DeclaredAccessibility == Accessibility.Public) return true;
                    if (!disallowInternal)
                    {
                        return (x.DeclaredAccessibility == Accessibility.Friend);
                    }

                    return false;
                })
                .Where(x =>
                       ((x.TypeKind == TypeKind.Interface) && x.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.UnionAttribute))
                    || ((x.TypeKind == TypeKind.Class) && x.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
                    || ((x.TypeKind == TypeKind.Struct) && x.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
                    )
                .ToArray();
        }

        void ResetWorkspace()
        {
            alreadyCollected = new HashSet<ITypeSymbol>();
            collectedObjectInfo = new List<ObjectSerializationInfo>();
            collectedEnumInfo = new List<EnumSerializationInfo>();
            collectedGenericInfo = new List<GenericSerializationInfo>();
        }

        // EntryPoint
        public (ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, GenericSerializationInfo[] genericInfo) Collect()
        {
            ResetWorkspace();

            foreach (var item in targetTypes)
            {
                CollectCore(item);
            }

            return (collectedObjectInfo.ToArray(), collectedEnumInfo.ToArray(), collectedGenericInfo.Distinct().ToArray());
        }

        // Gate of recursive collect
        bool CollectCore(ITypeSymbol typeSymbol)
        {
            var type = typeSymbol as INamedTypeSymbol;

            if (type == null)
            {
                return false;
            }
            if (!alreadyCollected.Add(typeSymbol))
            {
                return false;
            }
            if (embeddedTypes.Contains(type.ToString()))
            {
                return false;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                CollectEnum(type);
                return true;
            }

            if (!IsAllowAccessibility(type))
            {
                return false;
            }

            if (type.IsGenericType || type.TypeKind == TypeKind.Array)
            {
                CollectGeneric(type);
                return true;
            }

            if (type.Locations[0].IsInMetadata)
            {
                return false;
            }

            if (type.TypeKind == TypeKind.Interface)
            {
                CollectUnion(type);
                return true;
            }

            CollectObject(type);
            return true;
        }

        void CollectEnum(INamedTypeSymbol type)
        {
            var info = new EnumSerializationInfo
            {
                Name = type.Name,
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                UnderlyingType = type.EnumUnderlyingType.ToDisplayString(binaryWriteFormat)
            };

            collectedEnumInfo.Add(info);
        }

        void CollectUnion(INamedTypeSymbol type)
        {
        }

        void CollectGeneric(INamedTypeSymbol type)
        {
            var genericType = type.ConstructUnboundGenericType();
            var genericTypeString = genericType.ToDisplayString();
            var fullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            
            // special case
            if (fullName == "global::System.ArraySegment<byte>" || fullName == "global::System.ArraySegment<byte>?")
            {
                return;
            }

            // nullable
            if (genericTypeString == "T?")
            {
                CollectCore(type.TypeArguments[0]);

                if (!embeddedTypes.Contains(type.TypeArguments[0].ToString()))
                {
                    var info = new GenericSerializationInfo
                    {
                        FormatterName = $"global::MessagePack.Formatters.NullableFormatter<{type.TypeArguments[0].ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}>",
                        FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString()
                    };

                    collectedGenericInfo.Add(info);
                }
                return;
            }

            if (knownGenericTypes.TryGetValue(genericTypeString, out var formatter))
            {
                var typeArgs = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                var f = formatter.Replace("TREPLACE", typeArgs);

                var info = new GenericSerializationInfo
                {
                    FormatterName = f,
                    FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                    Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                };

                collectedGenericInfo.Add(info);

                if (genericTypeString == "System.Linq.ILookup<,>")
                {
                    formatter = knownGenericTypes["System.Linq.IGrouping<,>"];
                    f = formatter.Replace("TREPLACE", typeArgs);

                    var groupingInfo = new GenericSerializationInfo
                    {
                        FormatterName = f,
                        FullName = $"global::System.Linq.IGrouping<{typeArgs}>",
                        Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                    };
                    collectedGenericInfo.Add(groupingInfo);
                }
            }
        }

        void CollectObject(INamedTypeSymbol type)
        {
            var isClass = !type.IsValueType;

            var contractAttr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.MessagePackObjectAttribnute);
            if (contractAttr == null)
            {
                return;
            }

            var isIntKey = true;
            var intMemebrs = new Dictionary<int, MemberSerializationInfo>();
            var stringMembers = new Dictionary<string, MemberSerializationInfo>();

            if ((bool)contractAttr.ConstructorArguments[0].Value)
            {
                // Opt-out: All public members are serialize target except [Ignore] member.
                isIntKey = false;

                var hiddenIntKey = 0;

                foreach (var item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        StringKey = item.Name,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(binaryWriteFormat)
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);

                    CollectCore(item.Type); // recursive collect
                }
                foreach (var item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic,
                        StringKey = item.Name,
                        IsProperty = false,
                        IsField = true,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(binaryWriteFormat)
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;
                    member.IntKey = hiddenIntKey++;
                    stringMembers.Add(member.StringKey, member);
                    CollectCore(item.Type); // recursive collect
                }
            }
            else
            {
                // Opt-in: Only KeyAttribute members
                var searchFirst = true;
                var hiddenIntKey = 0;

                foreach (var item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;

                    var key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.KeyAttribnute)?.ConstructorArguments[0];
                    if (key == null) continue;

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null) throw new MessagePackGeneratorResolveFailedException("both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);

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

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(binaryWriteFormat)
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    if (isIntKey)
                    {
                        member.IntKey = (int)intKey;
                        if (intMemebrs.ContainsKey(member.IntKey)) throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);

                        intMemebrs.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = (string)stringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }

                    CollectCore(item.Type); // recursive collect
                }

                foreach (var item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    var key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.KeyAttribnute)?.ConstructorArguments[0];
                    if (key == null) continue;

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null) throw new MessagePackGeneratorResolveFailedException("both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);

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

                    var member = new MemberSerializationInfo
                    {
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                        ShortTypeName = item.Type.ToDisplayString(binaryWriteFormat)
                    };
                    if (!member.IsReadable && !member.IsWritable) continue;

                    if (isIntKey)
                    {
                        member.IntKey = (int)intKey;
                        if (intMemebrs.ContainsKey(member.IntKey)) throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);

                        intMemebrs.Add(member.IntKey, member);
                    }
                    else
                    {
                        member.StringKey = (string)stringKey;
                        if (stringMembers.ContainsKey(member.StringKey)) throw new MessagePackGeneratorResolveFailedException("key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);

                        member.IntKey = hiddenIntKey++;
                        stringMembers.Add(member.StringKey, member);
                    }

                    CollectCore(item.Type); // recursive collect
                }
            }

            // GetConstructor
            var ctor = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).SingleOrDefault(x => x.GetAttributes().Any(y => y.AttributeClass == typeReferences.SerializationConstructorAttribute));
            if (ctor == null)
            {
                ctor = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).OrderBy(x => x.Parameters.Length).FirstOrDefault();
            }

            // struct allows null ctor
            if (ctor == null && isClass) throw new MessagePackGeneratorResolveFailedException("can't find public constructor. type:" + type.Name);

            var constructorParameters = new List<MemberSerializationInfo>();
            if (ctor != null)
            {
                var constructorLookupDictionary = stringMembers.ToLookup(x => x.Key, x => x, StringComparer.OrdinalIgnoreCase);

                var ctorParamIndex = 0;
                foreach (var item in ctor.Parameters)
                {
                    MemberSerializationInfo paramMember;
                    if (isIntKey)
                    {
                        if (intMemebrs.TryGetValue(ctorParamIndex, out paramMember))
                        {
                            if (item.Type.ToDisplayString() == paramMember.Type && paramMember.IsReadable)
                            {
                                constructorParameters.Add(paramMember);
                            }
                            else
                            {
                                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.Name + " parameterIndex:" + ctorParamIndex + " paramterType:" + item.Type.Name);
                            }
                        }
                        else
                        {
                            throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.Name + " parameterIndex:" + ctorParamIndex);
                        }
                    }
                    else
                    {
                        var hasKey = constructorLookupDictionary[item.Name];
                        var len = hasKey.Count();
                        if (len != 0)
                        {
                            if (len != 1)
                            {
                                throw new MessagePackGeneratorResolveFailedException("duplicate matched constructor parameter name:" + type.Name + " parameterName:" + item.Name + " paramterType:" + item.Type.Name);
                            }

                            paramMember = hasKey.First().Value;
                            if (item.Type.ToDisplayString() == paramMember.Type && paramMember.IsReadable)
                            {
                                constructorParameters.Add(paramMember);
                            }
                            else
                            {
                                throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, parameterType mismatch. type:" + type.Name + " parameterName:" + item.Name + " paramterType:" + item.Type.Name);
                            }
                        }
                        else
                        {
                            throw new MessagePackGeneratorResolveFailedException("can't find matched constructor parameter, index not found. type:" + type.Name + " parameterName:" + item.Name);
                        }
                    }
                    ctorParamIndex++;
                }
            }

            var hasSerializationConstructor = type.AllInterfaces.Any(x => x == typeReferences.IMessagePackSerializationCallbackReceiver);
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
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = (isIntKey) ? intMemebrs.Values.ToArray() : stringMembers.Values.ToArray(),
                Name = type.ToDisplayString(shortTypeNameFormat).Replace(".", "_"),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
                HasIMessagePackSerializationCallbackReceiver = hasSerializationConstructor,
                NeedsCastOnAfter = needsCastOnAfter,
                NeedsCastOnBefore = needsCastOnBefore
            };
            collectedObjectInfo.Add(info);
        }

        bool IsAllowAccessibility(INamedTypeSymbol symbol)
        {
            do
            {
                if (symbol.DeclaredAccessibility != Accessibility.Public)
                {
                    if (disallowInternal)
                    {
                        return false;
                    }

                    if (symbol.DeclaredAccessibility != Accessibility.Internal)
                    {
                        return true;
                    }
                }

                symbol = symbol.ContainingType;
            } while (symbol != null);

            return true;
        }
    }

    public class MessagePackGeneratorResolveFailedException : Exception
    {
        public MessagePackGeneratorResolveFailedException(string message)
            : base(message)
        {

        }
    }
}