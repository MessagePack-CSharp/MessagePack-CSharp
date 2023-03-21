// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using MessagePack.Generator.CodeAnalysis;
using MessagePack.Generator.Transforms;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator;

public partial class MessagePackGenerator
{
    private static void Generate(TypeDeclarationSyntax syntax, Compilation compilation, IGeneratorContext context)
    {
        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

        var typeSymbol = semanticModel.GetDeclaredSymbol(syntax, context.CancellationToken) as ITypeSymbol;
        if (typeSymbol == null)
        {
            return;
        }

        var fullType = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
            .Replace("global::", string.Empty)
            .Replace("<", "_")
            .Replace(">", "_");

        var collector = new TypeCollector(compilation, true, isForceUseMap: false, ignoreTypeNames: null, typeSymbol);

        var (objectInfo, enumInfo, genericInfo, unionInfo) = collector.Collect();

        var code = GenerateFormatterSync(fullType.Replace(".", "_"), string.Empty, objectInfo, enumInfo, unionInfo, genericInfo);

        context.AddSource($"{fullType}.MessagePackFormatter.g.cs", code);
    }

    /// <summary>
    /// Generates the specialized resolver and formatters for the types that require serialization in a given compilation.
    /// </summary>
    /// <param name="resolverName">The resolver name.</param>
    /// <param name="namespaceDot">The namespace for the generated type to be created in.</param>
    /// <param name="objectInfo">The ObjectSerializationInfo array which TypeCollector.Collect returns.</param>
    /// <param name="enumInfo">The EnumSerializationInfo array which TypeCollector.Collect returns.</param>
    /// <param name="unionInfo">The UnionSerializationInfo array which TypeCollector.Collect returns.</param>
    /// <param name="genericInfo">The GenericSerializationInfo array which TypeCollector.Collect returns.</param>
    private static string GenerateFormatterSync(string resolverName, string namespaceDot, ObjectSerializationInfo[] objectInfo, EnumSerializationInfo[] enumInfo, UnionSerializationInfo[] unionInfo, GenericSerializationInfo[] genericInfo)
    {
        var objectFormatterTemplates = objectInfo
            .GroupBy(x => (x.Namespace, x.IsStringKey))
            .Select(x =>
            {
                var (nameSpace, isStringKey) = x.Key;
                var objectSerializationInfos = x.ToArray();
                var ns = namespaceDot + "Formatters" + (nameSpace is null ? string.Empty : "." + nameSpace);
                var template = isStringKey ? new StringKeyFormatterTemplate(ns, objectSerializationInfos) : (IFormatterTemplate)new FormatterTemplate(ns, objectSerializationInfos);
                return template;
            })
            .ToArray();

        string GetNamespace<T>(IGrouping<string?, T> x)
        {
            if (x.Key == null)
            {
                return namespaceDot + "Formatters";
            }

            return namespaceDot + "Formatters." + x.Key;
        }

        var sb = new StringBuilder();
        ResolverText(
            sb,
            namespaceDot + "Resolvers",
            resolverName,
            genericInfo
                .Where(x => !x.IsOpenGenericType)
                .Cast<IResolverRegisterInfo>()
                .Concat(enumInfo)
                .Concat(unionInfo)
                .Concat(objectInfo.Where(x => !x.IsOpenGenericType)));

        var enumFormatterTemplates = enumInfo
            .GroupBy(x => x.Namespace)
            .Select(x => new EnumTemplate(GetNamespace(x), x.ToArray()))
            .ToArray();

        var unionFormatterTemplates = unionInfo
            .GroupBy(x => x.Namespace)
            .Select(x => new UnionTemplate(GetNamespace(x), x.ToArray()))
            .ToArray();

        foreach (var item in enumFormatterTemplates)
        {
            var text = item.TransformText();
            sb.AppendLine(text);
        }

        sb.AppendLine();
        foreach (var item in objectFormatterTemplates)
        {
            var text = item.TransformText();
            sb.AppendLine(text);
            return sb.ToString();
        }

        sb.AppendLine();
        foreach (var item in unionFormatterTemplates)
        {
            var text = item.TransformText();
            sb.AppendLine(text);
            return sb.ToString();
        }

        return sb.ToString();
    }

    private static void ResolverText(StringBuilder sb, string ns, string resolverName, IEnumerable<IResolverRegisterInfo> registerInfos)
    {
        var begin = $$"""
using System.Runtime.CompilerServices;
using MessagePack;

namespace {{ns}}
{
    partial class FormatterRegister
    {
        [ModuleInitializer]
        internal static void {{resolverName}}FormatterRegister()
        {
""";

        var end = $$"""
        }
    }
}
""";

        sb.AppendLine(begin);

        foreach (var item in registerInfos)
        {
            var code = $$"""
            MessagePack.Resolvers.StaticCompositeResolver.Instance.AddGeneratedFormatter(new {{(item.FormatterName.StartsWith("global::") ? item.FormatterName : "global::Formatters." + item.FormatterName)}}());
""";
            sb.AppendLine(code);
        }

        sb.AppendLine(end);
    }
}
