// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace MessagePackAnalyzer
{
    // Utility and Extension methods for Roslyn
    internal static class RoslynCodeFixExtensions
    {
        public static CompilationUnitSyntax WithUsing(this CompilationUnitSyntax root, string name)
        {
            if (!root.Usings.Any(u => u.Name.ToString() == name))
            {
                root = root.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(name)).WithAdditionalAnnotations(Formatter.Annotation));
            }

            return root;
        }

        public static TNode WithFormat<TNode>(this TNode node)
            where TNode : SyntaxNode
        {
            return node.WithAdditionalAnnotations(Formatter.Annotation);
        }

        public static AttributeListSyntax ParseAttributeList(string text)
        {
            return SyntaxFactory.ParseCompilationUnit(text)
                .DescendantNodes()
                .OfType<AttributeListSyntax>()
                .First()
                .WithFormat();
        }
    }
}
