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
        if (nestedType is not QualifiedNamedTypeName { Container: NestingTypeContainer { NestingType: { } nestingType } })
        {
            return null;
        }

        Stack<IDisposable?> reverseAction = new();

        if (nestingType.Container is NamespaceTypeContainer { Namespace: string ns })
        {
            writer($"namespace {ns} {{\r\n");
            reverseAction.Push(new DisposeAction(() => writer("}\r\n")));
        }

        // Start with outer-most type.
        if (nestingType is { Container: NestingTypeContainer { NestingType: not null } })
        {
            reverseAction.Push(EmitNestingTypesAndNamespaces(template, nestingType, formatterLocation, writer));
        }

        string kind = nestingType.Kind switch
        {
            TypeKind.Class when nestingType.IsRecord => "record", // C# 9 doesn't want to see `record class`.
            TypeKind.Class => "class",
            TypeKind.Struct when nestingType.IsRecord => "record struct",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => throw new NotSupportedException("Unsupported kind: " + nestingType.Kind),
        };

        string? accessModifier = nestingType.AccessModifier switch
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
