// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.Transforms;

internal static class TransformUtilities
{
    internal static IDisposable? EmitNestingTypesAndNamespaces(this IFormatterTemplate template, Action<string> writer)
        => EmitNestingTypesAndNamespaces(template, template.Info.Formatter, template.Info.FormatterLocation, writer);

    internal static IDisposable? EmitNestingTypesAndNamespaces(this IFormatterTemplate template, QualifiedTypeName nestedType, ResolverRegisterInfo.FormatterPosition formatterLocation, Action<string> writer)
    {
        if (nestedType.NestingType is null)
        {
            return null;
        }

        Stack<IDisposable?> reverseAction = new();

        if (nestedType.NestingType.Namespace is not null)
        {
            writer($"namespace {nestedType.NestingType.Namespace} {{\r\n");
            reverseAction.Push(new DisposeAction(() => writer("}\r\n")));
        }

        // Start with outer-most type.
        if (nestedType.NestingType.NestingType is not null)
        {
            reverseAction.Push(EmitNestingTypesAndNamespaces(template, nestedType.NestingType, formatterLocation, writer));
        }

        string kind = nestedType.NestingType.Kind switch
        {
            TypeKind.Class when nestedType.NestingType.IsRecord => "record", // C# 9 doesn't want to see `record class`.
            TypeKind.Class => "class",
            TypeKind.Struct when nestedType.NestingType.IsRecord => "record struct",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => throw new NotSupportedException("Unsupported kind: " + nestedType.NestingType.Kind),
        };

        string? accessModifier = nestedType.NestingType.AccessModifier switch
        {
            Accessibility.Public => "public",
            Accessibility.Internal => "internal",
            Accessibility.Private => "private",
            _ => null,
        };
        if (accessModifier is not null)
        {
            writer(accessModifier);
            writer($" ");
        }

        writer($"partial {kind} {nestedType.NestingType.Name} {{\r\n");
        reverseAction.Push(new DisposeAction(() => writer("}\r\n")));

        return new DisposeAction(() =>
        {
            while (reverseAction.Count > 0)
            {
                reverseAction.Pop()?.Dispose();
            }
        });
    }

    internal static IDisposable? EmitClassesForNamespace(this IFormatterTemplate template, out string formatterVisibility, Action<string> writer)
    {
        string? ns = template.Info.DataType.Namespace;
        if (ns is null)
        {
            formatterVisibility = "private";
            return null;
        }

        int depth = 0;
        foreach (string segment in ns.Split('.'))
        {
            string visibility = depth == 0 ? "private" : "internal";
            writer($"{visibility} partial class {segment} {{ ");
            depth++;
        }

        writer("\r\n");

        formatterVisibility = "internal";
        return new DisposeAction(() =>
        {
            for (int i = 0; i < depth; i++)
            {
                writer("}");
            }
        });
    }

    private class DisposeAction : IDisposable
    {
        private Action? disposeAction;

        internal DisposeAction(Action? action)
        {
            this.disposeAction = action;
        }

        public void Dispose()
        {
            this.disposeAction?.Invoke();
            this.disposeAction = null;
        }
    }
}
