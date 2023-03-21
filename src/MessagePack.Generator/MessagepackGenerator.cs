// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MessagePackGenerator : IIncrementalGenerator, ISourceGenerator
{
    public const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";
    public const string MessagePackUnionAttributeFullName = "MessagePack.UnionAttribute";

    public void Execute(GeneratorExecutionContext context)
    {
        throw new NotImplementedException();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var typeDeclarations = context.SyntaxProvider.ForAttributeWithMetadataName(
            MessagePackObjectAttributeFullName,
            predicate: static (node, _) => node is TypeDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);
        Register(typeDeclarations);

        var typeDeclarations2 = context.SyntaxProvider.ForAttributeWithMetadataName(
            MessagePackUnionAttributeFullName,
            predicate: static (node, _) => node is InterfaceDeclarationSyntax,
            transform: static (context, _) => (TypeDeclarationSyntax)context.TargetNode);
        Register(typeDeclarations2);

        void Register(IncrementalValuesProvider<TypeDeclarationSyntax> typeDeclarations)
        {
            var source = typeDeclarations
                .Combine(context.CompilationProvider)
                .WithComparer(Comparer.Instance);

            context.RegisterSourceOutput(source, static (context, source) =>
            {
                var (typeDeclaration, compilation) = source;
                Generate(typeDeclaration, compilation, new GeneratorContext(context));
            });
        }
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        throw new NotImplementedException();
    }

    private class Comparer : IEqualityComparer<(TypeDeclarationSyntax, Compilation)>
    {
        public static readonly Comparer Instance = new Comparer();

        public bool Equals((TypeDeclarationSyntax, Compilation) x, (TypeDeclarationSyntax, Compilation) y)
        {
            return x.Item1.Equals(y.Item1);
        }

        public int GetHashCode((TypeDeclarationSyntax, Compilation) obj)
        {
            return obj.Item1.GetHashCode();
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        private SourceProductionContext context;

        public GeneratorContext(SourceProductionContext context)
        {
            this.context = context;
        }

        public CancellationToken CancellationToken => context.CancellationToken;

        public void AddSource(string hintName, string source) => context.AddSource(hintName, source);
    }
}
