// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.CodeAnalysis;

namespace MessagePack.SourceGenerator.Transforms;

internal static class TransformUtilities
{
    internal static IDisposable? EmitNestingTypesAndNamespaces(this IFormatterTemplate template, Action<string> writer)
        => EmitNestingTypesAndNamespaces(template.Info.Formatter, writer);

    internal static IDisposable? EmitNestingTypesAndNamespaces(QualifiedTypeName nestedType, Action<string> writer)
        => nestedType is QualifiedNamedTypeName { Container: { } container } ? EmitNestingTypesAndNamespaces(container, writer) : null;

    internal static IDisposable? EmitNestingTypesAndNamespaces(TypeContainer container, Action<string> writer)
    {
        switch (container)
        {
            case NamespaceTypeContainer { Namespace: string ns }:
                writer($"namespace {ns} {{\r\n");
                return new DisposeAction(() => writer("}\r\n"));
            case NestingTypeContainer { NestingType: { } nestingType }:
                Stack<IDisposable?> reverseAction = new();

                // Start with outer-most type.
                if (nestingType.Container is not null)
                {
                    reverseAction.Push(EmitNestingTypesAndNamespaces(nestingType.Container, writer));
                }

                var kind = nestingType.Kind switch
                {
                    TypeKind.Class when nestingType.IsRecord => "record", // C# 9 doesn't want to see `record class`.
                    TypeKind.Class => "class",
                    TypeKind.Struct when nestingType.IsRecord => "record struct",
                    TypeKind.Struct => "struct",
                    TypeKind.Interface => "interface",
                    _ => throw new NotSupportedException("Unsupported kind: " + nestingType.Kind),
                };

                var accessModifier = nestingType.AccessModifier switch
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

                writer($"partial {kind} {nestingType.Name} {{\r\n");
                reverseAction.Push(new DisposeAction(() => writer("}\r\n")));

                return new DisposeAction(() =>
                {
                    while (reverseAction.Count > 0)
                    {
                        reverseAction.Pop()?.Dispose();
                    }
                });
            default:
                throw new NotSupportedException();
        }
    }

    internal static IDisposable? EmitClassesForNamespace(this IFormatterTemplate template, out string formatterVisibility, Action<string> writer)
    {
        if (template.Info.DataType is not QualifiedNamedTypeName { Container: NamespaceTypeContainer { Namespace: string ns } })
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
