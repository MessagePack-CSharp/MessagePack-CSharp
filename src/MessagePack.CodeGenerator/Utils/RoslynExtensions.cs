using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MessagePack.CodeGenerator
{
    // Utility and Extension methods for Roslyn
    internal static class RoslynExtensions
    {
        public static async Task<Compilation> GetCompilationFromProject(string csprojPath, params string[] preprocessorSymbols)
        {
            var workspace = MSBuildWorkspace.Create();

            workspace.WorkspaceFailed += (sender,eventArgs) =>
            {
                Console.WriteLine($"{eventArgs.Diagnostic.Kind}: {eventArgs.Diagnostic.Message}");
            };

            var project = await workspace.OpenProjectAsync(csprojPath).ConfigureAwait(false);
            project = WithAssemblyReferences(project); // workaround:)*/
            project = project.WithParseOptions((project.ParseOptions as CSharpParseOptions).WithPreprocessorSymbols(preprocessorSymbols));

            var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
            return compilation;
        }

        //The project references don't get compiled correctly
        //Remove them and load compiled assemblies
        private static Project WithAssemblyReferences(Project project)
        {
            var allAsmRefs = GetProjectReferences(project.FilePath);

            var addExtRefs = allAsmRefs.GroupJoin(project.MetadataReferences.Select(x => x.Display),o => o,i => i,
              (o,i) => new { OuterItem = o,InnerItems = i.DefaultIfEmpty() })
              .SelectMany(x => x.InnerItems,(x,innerItem) => new { x.OuterItem,innerItem })
              .Where(x => x.innerItem == null)
              .Select(x => MetadataReference.CreateFromFile(x.OuterItem));

            var projRefs = project.AllProjectReferences;

            foreach (var p in projRefs)
            {
                project = project.RemoveProjectReference(p);
            }

            return project.AddMetadataReferences(addExtRefs);
        }

        private static IEnumerable<string> GetProjectReferences(string projectFileName)
        {
            var projectInstance = new ProjectInstance(projectFileName);
            var result = BuildManager.DefaultBuildManager.Build(
              new BuildParameters(),
              new BuildRequestData(projectInstance,new[]
            {
                "ResolveProjectReferences",
                "ResolveAssemblyReferences"
            }));

            IEnumerable<string> GetResultItems(string targetName)
            {
                var buildResult = result.ResultsByTarget[targetName];
                var buildResultItems = buildResult.Items;

                return buildResultItems.Select(item => item.ItemSpec);
            }

            return GetResultItems("ResolveProjectReferences")
              .Concat(GetResultItems("ResolveAssemblyReferences"));
        }

        public static IEnumerable<INamedTypeSymbol> GetNamedTypeSymbols(this Compilation compilation)
        {
            var symbols = new HashSet<INamedTypeSymbol>();

            foreach (var syntaxTree in compilation.SyntaxTrees)
            {
                var semModel = compilation.GetSemanticModel(syntaxTree);

                foreach (var item in syntaxTree.GetRoot()
                    .DescendantNodes()
                    .Select(x =>
                    {
                        //include symbols defined in the references
                        var sym = semModel.GetSymbolInfo(x).Symbol;

                        if (sym != null && sym.Kind != SymbolKind.NamedType)
                            return null;

                        return sym ?? semModel.GetDeclaredSymbol(x);
                    })
                    .Where(x => x != null))
                {
                    var namedType = item as INamedTypeSymbol;
                    if (namedType != null && !symbols.Contains(item))
                    {
						symbols.Add(namedType);
                        yield return namedType;
                    }
                }
            }
        }

        public static IEnumerable<INamedTypeSymbol> EnumerateBaseType(this ITypeSymbol symbol)
        {
            var t = symbol.BaseType;
            while (t != null)
            {
                yield return t;
                t = t.BaseType;
            }
        }

        public static AttributeData FindAttribute(this IEnumerable<AttributeData> attributeDataList, string typeName)
        {
            return attributeDataList
                .Where(x => x.AttributeClass.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static AttributeData FindAttributeShortName(this IEnumerable<AttributeData> attributeDataList, string typeName)
        {
            return attributeDataList
                .Where(x => x.AttributeClass.Name == typeName)
                .FirstOrDefault();
        }

        public static AttributeData FindAttributeIncludeBasePropertyShortName(this IPropertySymbol property, string typeName)
        {
            do
            {
                var data = FindAttributeShortName(property.GetAttributes(), typeName);
                if (data != null) return data;
                property = property.OverriddenProperty;
            } while (property != null);

            return null;
        }

        public static AttributeSyntax FindAttribute(this BaseTypeDeclarationSyntax typeDeclaration, SemanticModel model, string typeName)
        {
            return typeDeclaration.AttributeLists
                .SelectMany(x => x.Attributes)
                .Where(x => model.GetTypeInfo(x).Type?.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static INamedTypeSymbol FindBaseTargetType(this ITypeSymbol symbol, string typeName)
        {
            return symbol.EnumerateBaseType()
                .Where(x => x.OriginalDefinition?.ToDisplayString() == typeName)
                .FirstOrDefault();
        }

        public static object GetSingleNamedArgumentValue(this AttributeData attribute, string key)
        {
            foreach (var item in attribute.NamedArguments)
            {
                if (item.Key == key)
                {
                    return item.Value.Value;
                }
            }

            return null;
        }

        public static bool IsNullable(this INamedTypeSymbol symbol)
        {
            if (symbol.IsGenericType)
            {
                if (symbol.ConstructUnboundGenericType().ToDisplayString() == "T?")
                {
                    return true;
                }
            }
            return false;
        }

        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
        {
            var t = symbol;
            while (t != null)
            {
                foreach (var item in t.GetMembers())
                {
                    yield return item;
                }
                t = t.BaseType;
            }
        }

        public static IEnumerable<ISymbol> GetAllInterfaceMembers(this ITypeSymbol symbol)
        {
            return symbol.GetMembers()
                .Concat(symbol.AllInterfaces.SelectMany(x => x.GetMembers()));
        }
    }
}
