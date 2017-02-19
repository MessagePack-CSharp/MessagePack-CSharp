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
        public readonly INamedTypeSymbol KeyAttribnute;
        public readonly INamedTypeSymbol IgnoreAttribnute;

        public ReferenceSymbols(Compilation compilation)
        {
            TaskOfT = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            Task = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            MessagePackObjectAttribnute = compilation.GetTypeByMetadataName("MessagePack.MessagePackObjectAttribute");
            KeyAttribnute = compilation.GetTypeByMetadataName("MessagePack.KeyAttribute");
            IgnoreAttribnute = compilation.GetTypeByMetadataName("MessagePack.IgnoreAttribute");
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
        readonly bool disallowInternal;

        // visitor workspace:
        HashSet<ITypeSymbol> alreadyCollected;
        List<ObjectSerializationInfo> collectedObjectInfo;

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
                .Where(x => (x.TypeKind != TypeKind.Enum)
                    //|| ((x.TypeKind == TypeKind.Class) && x.GetAttributes().FindAttributeShortName(UnionAttributeShortName) != null)
                    //|| ((x.TypeKind == TypeKind.Interface) && x.GetAttributes().FindAttributeShortName(UnionAttributeShortName) != null)
                    || ((x.TypeKind == TypeKind.Class) && x.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
                    || ((x.TypeKind == TypeKind.Struct) && x.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
                    )
                .ToArray();
        }

        void ResetWorkspace()
        {
            alreadyCollected = new HashSet<ITypeSymbol>();
            collectedObjectInfo = new List<ObjectSerializationInfo>();
        }

        // EntryPoint
        public void Collect(out ObjectSerializationInfo[] objectFormatterInfos)
        {
            ResetWorkspace();

            foreach (var item in targetTypes)
            {
                CollectCore(item);
            }


            objectFormatterInfos = collectedObjectInfo.ToArray();
        }

        // Gate of recursive collect
        void CollectCore(ITypeSymbol typeSymbol)
        {
            var type = typeSymbol as INamedTypeSymbol;

            if (type == null)
            {
                return;
            }
            if (!alreadyCollected.Add(typeSymbol))
            {
                return;
            }
            if (embeddedTypes.Contains(type.ToString())) // TODO:no tostring
            {
                return;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                // CollectEnum(type, fromNullable, asKey);
                return;
            }

            if (type.Locations[0].IsInMetadata)
            {
                return;
            }

            if (!IsAllowType(type))
            {
                return;
            }

            if (type.IsGenericType)
            {
                var genericType = type.ConstructUnboundGenericType();
                var genericTypeString = genericType.ToDisplayString();

                if (genericTypeString == "T?")
                {
                    CollectCore(type.TypeArguments[0] as INamedTypeSymbol);
                    return;
                }
                else if (genericTypeString == "System.Collections.Generic.IList<>"
                      || genericTypeString == "System.Collections.Generic.IDictionary<,>"
                      || genericTypeString == "System.Collections.Generic.Dictionary<,>"
                      || genericTypeString == "System.Collections.Generic.IReadOnlyList<>"
                      || genericTypeString == "System.Collections.Generic.ICollection<>"
                      || genericTypeString == "System.Collections.Generic.IEnumerable<>"
                      || genericTypeString == "System.Collections.Generic.ISet<>"
                      || genericTypeString == "System.Collections.ObjectModel.ReadOnlyCollection<>"
                      || genericTypeString == "System.Linq.ILookup<,>"
                      || genericTypeString.StartsWith("System.Collections.Generic.KeyValuePair")
                      )
                {
                    //    var elementTypes = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));
                    //    var isDictionaryKey = false;

                    //    if (genericTypeString == "System.Collections.Generic.IList<>")
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.List, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "System.Collections.Generic.IReadOnlyList<>")
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.ReadOnlyList, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "System.Collections.Generic.IDictionary<,>" || genericTypeString == "System.Collections.Generic.Dictionary<,>")
                    //    {
                    //        isDictionaryKey = true;
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.Dictionary, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "ZeroFormatter.ILazyDictionary<,>")
                    //    {
                    //        isDictionaryKey = true;
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.LazyDictionary, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "ZeroFormatter.ILazyReadOnlyDictionary<,>")
                    //    {
                    //        isDictionaryKey = true;
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.LazyReadOnlyDictionary, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "ZeroFormatter.ILazyLookup<,>")
                    //    {
                    //        isDictionaryKey = true;
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.LazyLookup, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString == "System.Linq.ILookup<,>")
                    //    {
                    //        isDictionaryKey = true;
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.Lookup, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString.StartsWith("ZeroFormatter.KeyTuple"))
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.KeyTuple, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString.StartsWith("System.Collections.Generic.KeyValuePair"))
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.KeyValuePair, ElementTypes = elementTypes });
                    //    }

                    //    else if (genericTypeString.StartsWith("System.Collections.Generic.ICollection<>"))
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.InterfaceCollection, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString.StartsWith("System.Collections.Generic.IEnumerable<>"))
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.Enumerable, ElementTypes = elementTypes });
                    //    }
                    //    else if (genericTypeString.StartsWith("System.Collections.ObjectModel.ReadOnlyCollection<>"))
                    //    {
                    //        genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.ReadOnlyCollection, ElementTypes = elementTypes });
                    //    }

                    //    var argIndex = 0;
                    //    foreach (var t in type.TypeArguments)
                    //    {
                    //        if (isDictionaryKey && argIndex == 0)
                    //        {
                    //            CollectObjectSegment(t as INamedTypeSymbol, fromNullable, true);
                    //        }
                    //        else
                    //        {
                    //            CollectObjectSegment(t as INamedTypeSymbol, fromNullable, asKey);
                    //        }
                    //        argIndex++;
                    //    }
                    //    return;
                    //}
                    //else if (type.AllInterfaces.Any(x => (x.IsGenericType ? x.ConstructUnboundGenericType().ToDisplayString() : "") == "System.Collections.Generic.ICollection<>"))
                    //{
                    //    var elementTypes = string.Join(", ", type.TypeArguments.Select(x => x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))) + ", " + type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    //    genericTypeContainer.Add(new GenericType { TypeKind = GenericTypeKind.Collection, ElementTypes = elementTypes });

                    //    foreach (var t in type.TypeArguments)
                    //    {
                    //        CollectObjectSegment(t as INamedTypeSymbol, fromNullable, asKey);
                    //    }
                    //    return;
                    //}
                }
            }


            CollectObjectFormatter(type);
        }

        void CollectEnumFormatter()
        {
        }

        void CollectUnionFormatter()
        {
        }

        void CollectGenericFormatter()
        {
        }

        void CollectObjectFormatter(INamedTypeSymbol type)
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
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public,
                        StringKey = item.Name,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly,
                        StringKey = item.Name,
                        IsProperty = false,
                        IsField = true,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
                        IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public,
                        IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
                        IsReadable = item.DeclaredAccessibility == Accessibility.Public,
                        IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly,
                        IsProperty = true,
                        IsField = false,
                        Name = item.Name,
                        Type = item.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
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
            // TODO:supports SerializationConstructorAttribte
            // var ctor = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).SingleOrDefault(x => x.GetCustomAttribute<SerializationConstructorAttribute>(false) != null);
            //if (ctor == null)
            //{
            var ctor = type.Constructors.Where(x => x.DeclaredAccessibility == Accessibility.Public).OrderBy(x => x.Parameters.Length).FirstOrDefault();
            //}
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

            var info = new ObjectSerializationInfo
            {
                IsClass = isClass,
                // BestmatchConstructor = ctor,
                ConstructorParameters = constructorParameters.ToArray(),
                IsIntKey = isIntKey,
                Members = (isIntKey) ? intMemebrs.Values.ToArray() : stringMembers.Values.ToArray(),
                Name = type.ToDisplayString(shortTypeNameFormat).Replace(".", "_"),
                FullName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                Namespace = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            };
            collectedObjectInfo.Add(info);
        }

        bool IsAllowType(INamedTypeSymbol symbol)
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