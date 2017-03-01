using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MessagePackAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MessagePackAnalyzer : DiagnosticAnalyzer
    {
        const string DiagnosticIdBase = "MessagePackAnalyzer";

        internal const string Title = "Lint of MessagePack Type.";
        internal const string Category = "Usage";

        internal const string MessagePackObjectAttributeShortName = "MessagePackObjectAttribute";
        internal const string KeyAttributeShortName = "KeyAttribute";
        internal const string IgnoreShortName = "IgnoreAttribute";
        internal const string UnionAttributeShortName = "UnionAttribute";

        internal static readonly DiagnosticDescriptor TypeMustBeMessagePackObject = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(TypeMustBeMessagePackObject), title: Title, category: Category,
            messageFormat: "Type must be marked with MessagePackObjectAttribute. {0}.", // type.Name
            description: "Type must be marked with MessagePackObjectAttribute.",
            defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor PublicMemberNeedsKey = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(PublicMemberNeedsKey), title: Title, category: Category,
            messageFormat: "Public member requires KeyAttribute or IgnoreAttribute. {0}.{1}.", // type.Name + "." + item.Name
            description: "Public member must be marked with KeyAttribute or IgnoreAttribute.",
            defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true);

        internal static readonly DiagnosticDescriptor InvalidMessagePackObject = new DiagnosticDescriptor(
            id: DiagnosticIdBase + "_" + nameof(InvalidMessagePackObject), title: Title, category: Category,
            messageFormat: "Invalid MessagePackObject definition. {0}", // details
            description: "Invalid MessagePackObject definition.",
            defaultSeverity: DiagnosticSeverity.Error, isEnabledByDefault: true);

        static readonly ImmutableArray<DiagnosticDescriptor> supportedDiagnostics = ImmutableArray.Create(
            TypeMustBeMessagePackObject,
            PublicMemberNeedsKey,
            InvalidMessagePackObject
            );

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get
            {
                return supportedDiagnostics;
            }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.InterfaceDeclaration);
        }

        static void Analyze(SyntaxNodeAnalysisContext context)
        {
            var model = context.SemanticModel;

            var typeDeclaration = context.Node as TypeDeclarationSyntax;
            if (typeDeclaration == null) return;

            var declaredSymbol = model.GetDeclaredSymbol(typeDeclaration);
            if (declaredSymbol == null) return;

            var typeReferences = new ReferenceSymbols(model.Compilation);

            if (
               ((declaredSymbol.TypeKind == TypeKind.Interface) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.UnionAttribute))
            || ((declaredSymbol.TypeKind == TypeKind.Class) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
            || ((declaredSymbol.TypeKind == TypeKind.Struct) && declaredSymbol.GetAttributes().Any(x2 => x2.AttributeClass == typeReferences.MessagePackObjectAttribnute))
            )
            {
                var reportContext = new DiagnosticsReportContext(context);
                var collector = new TypeCollector(reportContext, model.Compilation);
                collector.CollectCore(declaredSymbol);
                reportContext.ReportAll();
            }
        }
    }

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

    internal class TypeCollector
    {
        static readonly SymbolDisplayFormat binaryWriteFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
                miscellaneousOptions: SymbolDisplayMiscellaneousOptions.ExpandNullable,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameOnly);

        static readonly SymbolDisplayFormat shortTypeNameFormat = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes);

        readonly ReferenceSymbols typeReferences;
        static readonly HashSet<string> embeddedTypes = new HashSet<string>(new string[]
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

        static readonly Dictionary<string, string> knownGenericTypes = new Dictionary<string, string>
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

            {"System.Collections.Immutable.ImmutableArray<>", "global::MessagePack.ImmutableCollection.ImmutableArrayFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableList<>", "global::MessagePack.ImmutableCollection.ImmutableListFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableDictionary<,>", "global::MessagePack.ImmutableCollection.ImmutableDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableHashSet<>", "global::MessagePack.ImmutableCollection.ImmutableHashSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableSortedDictionary<,>", "global::MessagePack.ImmutableCollection.ImmutableSortedDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableSortedSet<>", "global::MessagePack.ImmutableCollection.ImmutableSortedSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableQueue<>", "global::MessagePack.ImmutableCollection.ImmutableQueueFormatter<TREPLACE>"},
            {"System.Collections.Immutable.ImmutableStack<>", "global::MessagePack.ImmutableCollection.ImmutableStackFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableList<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableListFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableDictionary<,>", "global::MessagePack.ImmutableCollection.InterfaceImmutableDictionaryFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableQueue<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableQueueFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableSet<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableSetFormatter<TREPLACE>"},
            {"System.Collections.Immutable.IImmutableStack<>", "global::MessagePack.ImmutableCollection.InterfaceImmutableStackFormatter<TREPLACE>"},

            {"Reactive.Bindings.ReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.ReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.IReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.IReadOnlyReactiveProperty<>", "global::MessagePack.ReactivePropertyExtension.InterfaceReadOnlyReactivePropertyFormatter<TREPLACE>"},
            {"Reactive.Bindings.ReactiveCollection<>", "global::MessagePack.ReactivePropertyExtension.ReactiveCollectionFormatter<TREPLACE>"},
        };

        HashSet<ITypeSymbol> alreadyCollected = new HashSet<ITypeSymbol>();

        public DiagnosticsReportContext ReportContext { get; set; }

        public TypeCollector(DiagnosticsReportContext reportContext, Compilation compilation)
        {
            this.typeReferences = new ReferenceSymbols(compilation);
            this.ReportContext = reportContext;
        }

        // Gate of recursive collect
        public void CollectCore(ITypeSymbol typeSymbol)
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

            if (embeddedTypes.Contains(type.ToString()))
            {
                return;
            }

            if (ReportContext.AdditionalAllowTypes.Contains(type.ToDisplayString()))
            {
                return;
            }


            if (type.TypeKind == TypeKind.Enum)
            {
                return;
            }

            if (type.IsGenericType)
            {
                foreach (var item in type.TypeArguments)
                {
                    CollectCore(item);
                }
                return;
            }

            if (type.TypeKind == TypeKind.Array)
            {
                var array = type as IArrayTypeSymbol;
                var t = array.ElementType;
                CollectCore(t);
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
            CollectObject(type);
            return;
        }


        void CollectObject(INamedTypeSymbol type)
        {
            var isClass = !type.IsValueType;

            var contractAttr = type.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.MessagePackObjectAttribnute);
            if (contractAttr == null)
            {
                ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.TypeMustBeMessagePackObject, type.Locations[0], type.Name));
                return;
            }

            var isIntKey = true;
            var intMemebers = new HashSet<int>();
            var stringMembers = new HashSet<string>();

            if ((bool)contractAttr.ConstructorArguments[0].Value)
            {
                // Opt-out: All public members are serialize target except [Ignore] member.
                isIntKey = false;

                foreach (var item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;

                    var IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;

                    if (!IsReadable && !IsWritable) continue;

                    stringMembers.Add(item.Name);
                    CollectCore(item.Type); // recursive collect
                }
                foreach (var item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;
                    if (item.IsImplicitlyDeclared) continue;

                    var IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic;

                    if (!IsReadable && !IsWritable) continue;

                    stringMembers.Add(item.Name);
                    CollectCore(item.Type); // recursive collect
                }
            }
            else
            {
                // Opt-in: Only KeyAttribute members
                var searchFirst = true;

                foreach (var item in type.GetAllMembers().OfType<IPropertySymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;

                    var IsReadable = (item.GetMethod != null) && item.GetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var IsWritable = (item.SetMethod != null) && item.SetMethod.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var Name = item.Name;
                    if (!IsReadable && !IsWritable) continue;

                    var key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.KeyAttribnute)?.ConstructorArguments[0];
                    if (key == null)
                    {
                        ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.PublicMemberNeedsKey, item.Locations[0], type.Name, item.Name));
                        continue;
                    }

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null)
                    {
                        ReportInvalid(item, "both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);
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
                            ReportInvalid(item, "all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                            break;
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMemebers.Contains((int)intKey))
                        {
                            ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        intMemebers.Add((int)intKey);
                    }
                    else
                    {
                        if (stringMembers.Contains((string)stringKey))
                        {
                            ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        stringMembers.Add((string)stringKey);
                    }

                    CollectCore(item.Type); // recursive collect
                }

                foreach (var item in type.GetAllMembers().OfType<IFieldSymbol>())
                {
                    if (item.GetAttributes().Any(x => x.AttributeClass == typeReferences.IgnoreAttribnute)) continue;
                    if (item.IsImplicitlyDeclared) continue;

                    var IsReadable = item.DeclaredAccessibility == Accessibility.Public && !item.IsStatic;
                    var IsWritable = item.DeclaredAccessibility == Accessibility.Public && !item.IsReadOnly && !item.IsStatic;
                    var Name = item.Name;
                    if (!IsReadable && !IsWritable) continue;

                    var key = item.GetAttributes().FirstOrDefault(x => x.AttributeClass == typeReferences.KeyAttribnute)?.ConstructorArguments[0];
                    if (key == null)
                    {
                        ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.PublicMemberNeedsKey, item.Locations[0], type.Name, item.Name));
                        continue;
                    }

                    var intKey = (key.Value.Value is int) ? (int)key.Value.Value : (int?)null;
                    var stringKey = (key.Value.Value is string) ? (string)key.Value.Value : (string)null;
                    if (intKey == null && stringKey == null)
                    {
                        ReportInvalid(item, "both IntKey and StringKey are null." + " type: " + type.Name + " member:" + item.Name);
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
                            ReportInvalid(item, "all members key type must be same." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }
                    }

                    if (isIntKey)
                    {
                        if (intMemebers.Contains((int)intKey))
                        {
                            ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        intMemebers.Add((int)intKey);
                    }
                    else
                    {
                        if (stringMembers.Contains((string)stringKey))
                        {
                            ReportInvalid(item, "key is duplicated, all members key must be unique." + " type: " + type.Name + " member:" + item.Name);
                            return;
                        }

                        stringMembers.Add((string)stringKey);
                    }

                    CollectCore(item.Type); // recursive collect
                }
            }
        }

        void ReportInvalid(ISymbol symbol, string message)
        {
            this.ReportContext.Add(Diagnostic.Create(MessagePackAnalyzer.InvalidMessagePackObject, symbol.Locations[0], message));
        }
    }
}
