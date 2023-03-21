// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MessagePack.Generator;

[Generator(LanguageNames.CSharp)]
public partial class MessagePackGenerator : ISourceGenerator
{
    public const string MessagePackObjectAttributeFullName = "MessagePack.MessagePackObjectAttribute";

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(SyntaxContextReceiver.Create);
    }

    public void Execute(GeneratorExecutionContext context)
    {
        if (context.SyntaxContextReceiver is not SyntaxContextReceiver receiver || receiver.ClassDeclarations.Count == 0)
        {
            return;
        }

        var compilation = context.Compilation;
        var generateContext = new GeneratorContext(context);

        foreach (var syntax in receiver.ClassDeclarations)
        {
            Generate(syntax, compilation, generateContext);
        }
    }

    private class SyntaxContextReceiver : ISyntaxContextReceiver
    {
        internal static ISyntaxContextReceiver Create()
        {
            return new SyntaxContextReceiver();
        }

        public HashSet<TypeDeclarationSyntax> ClassDeclarations { get; } = new();

        public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
        {
            if (context.Node is TypeDeclarationSyntax typeSyntax)
            {
                if (typeSyntax.AttributeLists.Count > 0)
                {
                    var hasAttribute = typeSyntax.AttributeLists
                        .SelectMany(x => x.Attributes)
                        .Any(x => x.Name.ToString() is "MessagePackObject"
                            or "MessagePackObjectAttribute"
                            or "MessagePack.MessagePackObject"
                            or "MessagePack.MessagePackObjectAttribute"
                            or "Union"
                            or "UnionAttribute"
                            or "MessagePack.Union"
                            or "MessagePack.UnionAttribute");
                    if (hasAttribute)
                    {
                        ClassDeclarations.Add(typeSyntax);
                    }
                }
            }
        }
    }

    private class GeneratorContext : IGeneratorContext
    {
        private GeneratorExecutionContext context;

        public GeneratorContext(GeneratorExecutionContext context)
        {
            this.context = context;
        }

        public CancellationToken CancellationToken => context.CancellationToken;

        public void AddSource(string hintName, string source) => context.AddSource(hintName, source);
    }
}
