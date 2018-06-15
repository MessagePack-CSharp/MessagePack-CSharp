using Microsoft.Build.Execution;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MessagePack.CodeGenerator
{
    // Utility and Extension methods for Roslyn
    internal static class RoslynExtensions
    {
        public static async Task<Compilation> GetCompilationFromProject(string csprojPath, string framework, bool quiet, params string[] preprocessorSymbols)
        {
            var projectInstance = CreateProjectInstance(csprojPath,ref framework);

            using (var workspace = CreateWorkspace(framework))
            {
                if (!quiet)
                {
                    workspace.WorkspaceFailed += (sender,eventArgs) =>
                    {
                        Console.WriteLine($"{eventArgs.Diagnostic.Kind}: {eventArgs.Diagnostic.Message}");
                    };
                }

                workspace.LoadMetadataForReferencedProjects = true;

                var project = await workspace.OpenProjectAsync(csprojPath).ConfigureAwait(false);
                project = WithAssemblyReferences(project,projectInstance); // workaround
                project = project.WithParseOptions((project.ParseOptions as CSharpParseOptions).WithPreprocessorSymbols(preprocessorSymbols));

                var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
                return compilation;
            }
        }

        private static MSBuildWorkspace CreateWorkspace(string framework) =>
            string.IsNullOrWhiteSpace(framework)
            ? MSBuildWorkspace.Create()
            : MSBuildWorkspace.Create(new Dictionary<string,string> {
                { PropertyNames.TargetFramework, framework },
            });

        //The project references don't get compiled correctly
        //Remove them and load compiled assemblies
        private static Project WithAssemblyReferences(Project project,ProjectInstance projectInstance)
        {
            var allAsmRefs = GetProjectReferences(projectInstance);

            var addExtRefs = allAsmRefs.GroupJoin(project.MetadataReferences.Select(x => x.Display),o => o,i => i,
              (o,i) => new { OuterItem = o,InnerItems = i.DefaultIfEmpty() })
              .SelectMany(x => x.InnerItems,(x,innerItem) => new { x.OuterItem,innerItem })
              .Where(x => x.innerItem == null)
              .Select(x => x.OuterItem)
              .Distinct()
              .Select(x => MetadataReference.CreateFromFile(x));

            var projRefs = project.AllProjectReferences;

            foreach (var p in projRefs)
            {
                project = project.RemoveProjectReference(p);
            }

            return project.AddMetadataReferences(addExtRefs);
        }

        private static ProjectInstance CreateProjectInstance(string projectFileName,ref string framework) =>
            string.IsNullOrWhiteSpace(framework)
                ? SetTargetFrameworkIfNeeded(new ProjectInstance(projectFileName),ref framework)
                : new ProjectInstance(projectFileName,new Dictionary<string,string>
                {
                    { PropertyNames.TargetFramework, framework },
                },null);

        private static IEnumerable<string> GetProjectReferences(ProjectInstance projectInstance)
        {
            var result = BuildManager.DefaultBuildManager.Build(
                new BuildParameters(),
                new BuildRequestData(projectInstance,new[]
                {
                    PropertyNames.ResolveProjectReferences,
                    PropertyNames.ResolveAssemblyReferences
                }));

            IEnumerable<string> GetResultItems(string targetName)
            {
                if (result.ResultsByTarget.TryGetValue(targetName,out TargetResult buildResult))
                {
                    var buildResultItems = buildResult.Items;
                    return buildResultItems.Select(item => item.ItemSpec);
                }

                return Array.Empty<string>();
            }

            return GetResultItems(PropertyNames.ResolveProjectReferences)
              .Concat(GetResultItems(PropertyNames.ResolveAssemblyReferences));
        }

        private static ProjectInstance SetTargetFrameworkIfNeeded(ProjectInstance project,ref string framework)
        {
            var target = project.GetPropertyValue(PropertyNames.TargetFramework);

            // If the project supports multiple target frameworks and specific framework isn't
            // selected, we must pick one before execution. Otherwise, the ResolveReferences
            // target might not be available to us.
            if (string.IsNullOrWhiteSpace(target))
            {
                var frameworks = project.GetPropertyValue(PropertyNames.TargetFrameworks).Split(new char[] { ';' },StringSplitOptions.RemoveEmptyEntries);

                if (frameworks.Length > 0)
                {
                    framework = frameworks[0];

                    return new ProjectInstance(project.ProjectFileLocation.LocationString,
                        new Dictionary<string,string> { [PropertyNames.TargetFramework] = framework },null);
                }
            }

            return project;
        }

        internal static class PropertyNames
        {
            public const string ResolveProjectReferences = nameof(ResolveProjectReferences);
            public const string ResolveAssemblyReferences = nameof(ResolveAssemblyReferences);
            public const string _ResolveReferenceDependencies = nameof(_ResolveReferenceDependencies);
            public const string TargetFramework = nameof(TargetFramework);
            public const string TargetFrameworks = nameof(TargetFrameworks);
            public const string PreprocessorSymbols = "DefineConstants";
            public const string LangVersion = nameof(LangVersion);
            public const string CheckForSystemRuntimeDependency = nameof(CheckForSystemRuntimeDependency);
            public const string DesignTimeBuild = nameof(DesignTimeBuild);
            public const string BuildProjectReferences = nameof(BuildProjectReferences);
            public const string BuildingInsideVisualStudio = nameof(BuildingInsideVisualStudio);
            public const string Configuration = nameof(Configuration);
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
                    if (item is INamedTypeSymbol namedType && !symbols.Contains(item))
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
