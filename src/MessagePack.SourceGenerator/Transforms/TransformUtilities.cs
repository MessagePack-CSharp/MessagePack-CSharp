// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace MessagePack.SourceGenerator.Transforms;

internal static class TransformUtilities
{
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
