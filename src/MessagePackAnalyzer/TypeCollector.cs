// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace MessagePackAnalyzer
{
    internal class TypeCollector
    {
        private readonly ReferenceSymbols typeReferences;
        private static readonly HashSet<string> EmbeddedTypes = new HashSet<string>(new string[]
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

        private static readonly Dictionary<string, string> KnownGenericTypes = new Dictionary<string, string>
        {
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
            /* Tuple */
            { "System.Tuple<>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            { "System.Tuple<,,,,,,,>", "global::MessagePack.Formatters.TupleFormatter<TREPLACE>" },
            /* ValueTuple */
            { "System.ValueTuple<>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            { "System.ValueTuple<,,,,,,,>", "global::MessagePack.Formatters.ValueTupleFormatter<TREPLACE>" },
            /* other */
            { "System.Collections.Generic.KeyValuePair<,>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>" },
            { "System.Threading.Tasks.ValueTask<>", "global::MessagePack.Formatters.KeyValuePairFormatter<TREPLACE>" },
            { "System.ArraySegment<>", "global::MessagePack.Formatters.ArraySegmentFormatter<TREPLACE>" },

            /* extensions */
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
            /* Reactive bindings */
            { "Reactive.Bindings.ReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.IReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.IReadOnlyReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>" },
            { "Reactive.Bindings.ReactiveCollection<>", "global::MessagePack.ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>" },
        };

        private HashSet<ITypeSymbol> alreadyCollected = new HashSet<ITypeSymbol>();

        public DiagnosticsReportContext ReportContext { get; set; }

        public TypeCollector(DiagnosticsReportContext reportContext, ReferenceSymbols typeReferences)
        {
            this.typeReferences = typeReferences;
            this.ReportContext = reportContext;
        }

        // Gate of recursive collect
        public void CollectCore(ITypeSymbol typeSymbol, ISymbol? callerSymbol = null)
        {
            if (typeSymbol.TypeKind == TypeKind.Array)
            {
                var array = (IArrayTypeSymbol)typeSymbol;
                ITypeSymbol t = array.ElementType;
                this.CollectCore(t, callerSymbol);
                return;
            }

            var type = typeSymbol as INamedTypeSymbol;

            if (type == null)
            {
                return;
            }

            if (!this.alreadyCollected.Add(typeSymbol))
            {
                return;
            }

            if (EmbeddedTypes.Contains(type.ToString()))
            {
                return;
            }

            if (this.ReportContext.AdditionalAllowTypes.Contains(type.ToDisplayString()))
            {
                return;
            }

            if (type.TypeKind == TypeKind.Enum)
            {
                return;
            }

            if (type.IsGenericType)
            {
                foreach (ITypeSymbol item in type.TypeArguments)
                {
                    this.CollectCore(item, callerSymbol);
                }

                return;
            }

            if (type.Locations[0].IsInMetadata)
            {
                return;
            }

            if (type.TypeKind == TypeKind.Interface)
            {
                return;
            }

            // only do object:)
            this.CollectObject(type, callerSymbol);
            return;
        }

        private void ICollectObject(INamedTypeSymbol type, ISymbol? callerSymbol)
        {
            var isClass = !type.IsValueType;

            AttributeData formatterAttr = type.GetAttributes().FirstOrDefault(x => Equals(x.AttributeClass, this.typeReferences.FormatterAttribute));
            if( formatterAttr != null )
            {
                // Validate that the typed formatter is actually of `IMessagePackFormatter`
                var formatterType = (ITypeSymbol)formatterAttr.ConstructorArguments[0].Value;
                var isMessagePackFormatter = formatterType.AllInterfaces.Any(x => x.Equals(this.typeReferences.MessagePackFormatter));
                if( !isMessagePackFormatter )
                {
                    var typeInfo = ImmutableDictionary.Create<string, string>().Add("type", formatterType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.MessageFormatterMustBeMessagePackFormatter, formatterType.Locations[0], typeInfo));
                }
                return;
            }

            AttributeData contractAttr = type.GetAttributes().FirstOrDefault(x => Equals(x.AttributeClass, this.typeReferences.MessagePackObjectAttribute));
            if (contractAttr == null)
            {
                Location location = callerSymbol != null ? callerSymbol.Locations[0] : type.Locations[0];
                var targetName = callerSymbol != null ? callerSymbol.ContainingType.Name + "." + callerSymbol.Name : type.Name;

                ImmutableDictionary<string, string> typeInfo = ImmutableDictionary.Create<string, string>().Add("type", type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.TypeMustBeMessagePackObject, location, typeInfo, targetName));
                return;
            }

            var isIntKey = true;
            var intMembers = new HashSet<int>();
            var stringMembers = new HashSet<string>();

            if ((bool)contractAttr.ConstructorArguments[0].Value)
            {
                // Opt-out: All public members are serialize target except [Ignore] member.
                isIntKey = false;

                foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => Equals(x.AttributeClass, this.typeReferences.IgnoreAttribute) || Equals(x.AttributeClass, this.typeReferences.IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    var isReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var isWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;

                    if (!isReadable && !isWritable)
                    {
                        continue;
                    }

                    stringMembers.Add(item.Name);
                    this.CollectCore(item.Type, item); // recursive collect
                }

                foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => Equals(x.AttributeClass, this.typeReferences.IgnoreAttribute) || Equals(x.AttributeClass, this.typeReferences.IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    if (item.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    var isReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var isWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic;

                    if (!isReadable && !isWritable)
                    {
                        continue;
                    }

                    stringMembers.Add(item.Name);
                    this.CollectCore(item.Type, item); // recursive collect
                }
            }
            else
            {
                // Opt-in: Only KeyAttribute members
                var searchFirst = true;

                foreach (IPropertySymbol item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => Equals(x.AttributeClass, this.typeReferences.IgnoreAttribute) || Equals(x.AttributeClass, this.typeReferences.IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    var isReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var isWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var name = item.Name;
                    if (!isReadable && !isWritable)
                    {
                        continue;
                    }

                    TypedConstant? key = item.GetAttributes().FirstOrDefault(x => Equals(x.AttributeClass, this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                    if (key == null)
                    {
                        ImmutableDictionary<string, string> typeInfo = ImmutableDictionary.Create<string, string>().Add("type", type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.PublicMemberNeedsKey, item.Locations[0], typeInfo, type.Name, item.Name));
                        continue;
                    }

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : null;
                    if (intKey == null && stringKey == null)
                    {
                        this.ReportInvalid(item, "both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);
                        break;
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
                            this.ReportInvalid(item, "all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                            break;
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMembers.Contains(intKey!.Value))
                        {
                            this.ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        intMembers.Add((int)intKey);
                    }
                    else
                    {
                        if (stringMembers.Contains(stringKey!))
                        {
                            this.ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        stringMembers.Add(stringKey!);
                    }

                    this.CollectCore(item.Type, item); // recursive collect
                }

                foreach (IFieldSymbol item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => Equals(x.AttributeClass, this.typeReferences.IgnoreAttribute) || Equals(x.AttributeClass, this.typeReferences.IgnoreDataMemberAttribute)))
                    {
                        continue;
                    }

                    if (item.IsImplicitlyDeclared)
                    {
                        continue;
                    }

                    var isReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var isWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic;
                    var name = item.Name;
                    if (!isReadable && !isWritable)
                    {
                        continue;
                    }

                    TypedConstant? key = item.GetAttributes().FirstOrDefault(x => Equals(x.AttributeClass, this.typeReferences.KeyAttribute))?.ConstructorArguments[0];
                    if (key == null)
                    {
                        ImmutableDictionary<string, string> typeInfo = ImmutableDictionary.Create<string, string>().Add("type", type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.PublicMemberNeedsKey, item.Locations[0], typeInfo, type.Name, item.Name));
                        continue;
                    }

                    var intKey = key.Value.Value is int i ? (int?)i : null;
                    var stringKey = key.Value.Value as string;
                    if (intKey == null && stringKey == null)
                    {
                        this.ReportInvalid(item, "both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);
                        return;
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
                            this.ReportInvalid(item, "all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }
                    }

                    if (intKey.HasValue)
                    {
                        if (intMembers.Contains(intKey.Value))
                        {
                            this.ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        intMembers.Add(intKey.Value);
                    }
                    else if (stringKey is object)
                    {
                        if (stringMembers.Contains(stringKey))
                        {
                            this.ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        stringMembers.Add(stringKey);
                    }

                    this.CollectCore(item.Type, item); // recursive collect
                }
            }
        }

        private void ReportInvalid(ISymbol symbol, string message)
        {
            this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.InvalidMessagePackObject, symbol.Locations[0], message));
        }
    }
}
